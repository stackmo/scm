using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Identity;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Clinical;

/// <summary>
/// Service tra cứu Lịch Sử Khám Bệnh Đa Phiên toàn diện (Longitudinal Patient EMR History).
/// Áp dụng RBAC projection: ẩn toa thuốc cũ đối với nhóm KTV/Điều dưỡng không có quyền xem đơn thuốc.
/// </summary>
public class PatientClinicalHistoryAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentDoctorContext? _currentDoctor;
    private readonly ICurrentBranch? _currentBranch;

    public PatientClinicalHistoryAppService(
        SCMDbContext dbContext,
        ICurrentDoctorContext? currentDoctor = null,
        ICurrentBranch? currentBranch = null)
    {
        _dbContext = dbContext;
        _currentDoctor = currentDoctor;
        _currentBranch = currentBranch;
    }

    public async Task<PatientClinicalHistoryDto> GetPatientHistoryAsync(Guid patientId)
    {
        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == patientId);

        var encounters = await _dbContext.Encounters
            .IgnoreQueryFilters()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.EncounterDate)
            .ThenByDescending(e => e.CheckInTime)
            .ToListAsync();

        var encounterIds = encounters.Select(e => e.Id).ToList();

        // 1. Sinh hiệu & Chi tiết 8 chuyên khoa
        var clinicalExams = await _dbContext.ClinicalExaminations
            .IgnoreQueryFilters()
            .Include(c => c.VitalSigns)
            .Include(c => c.SpecialtyDetails)
                .ThenInclude(s => s.DoctorStaff)
            .Where(c => encounterIds.Contains(c.EncounterId))
            .ToListAsync();

        // 2. Chỉ định Cận lâm sàng
        var medicalOrders = await _dbContext.MedicalOrders
            .IgnoreQueryFilters()
            .Include(m => m.Items)
            .Where(m => encounterIds.Contains(m.EncounterId))
            .ToListAsync();

        // 3. Kết quả Xét nghiệm LIS
        var labResults = await _dbContext.LabResultRecords
            .IgnoreQueryFilters()
            .Where(l => encounterIds.Contains(l.EncounterId))
            .OrderBy(l => l.M3FieldMapping)
            .ThenBy(l => l.TestCode)
            .ToListAsync();

        // 4. Kết quả Chẩn đoán hình ảnh (kèm ảnh capture)
        var imagingRecords = await _dbContext.DiagnosticImagingRecords
            .IgnoreQueryFilters()
            .Include(r => r.CapturedImages)
            .Where(r => encounterIds.Contains(r.EncounterId))
            .ToListAsync();

        // 5. Toa thuốc (ẩn với ParaclinicalTechnician trừ khi có quyền)
        bool hidePrescriptions = _currentDoctor?.IsAuthenticated == true &&
                                 _currentDoctor.ActorGroup == ClinicalActorGroup.ParaclinicalTechnician &&
                                 !_currentDoctor.HasPermission("tab_prescription", PermissionAction.View);

        var prescriptions = hidePrescriptions
            ? new List<SCM.Domain.Prescription.Prescription>()
            : await _dbContext.Prescriptions
                .IgnoreQueryFilters()
                .Include(p => p.Items)
                .Where(p => encounterIds.Contains(p.EncounterId))
                .ToListAsync();

        // 6. Kết luận tổng kết hồ sơ
        var summaries = await _dbContext.MedicalRecordSummaries
            .IgnoreQueryFilters()
            .Where(s => encounterIds.Contains(s.EncounterId))
            .ToDictionaryAsync(s => s.EncounterId);

        if (_currentDoctor?.IsAuthenticated == true)
        {
            _dbContext.AuditLogPIIs.Add(new AuditLogPII(
                _currentDoctor.UserId,
                _currentDoctor.UserName,
                _currentBranch?.Id,
                _currentBranch?.Code ?? "CNQ1",
                "VIEW_PATIENT_HISTORY",
                nameof(PatientClinicalHistoryDto),
                patientId.ToString(),
                details: $"Bác sĩ/NV {_currentDoctor.UserName} ({_currentDoctor.ActorGroup}) tra cứu {encounters.Count} lần khám cũ."));
            await _dbContext.SaveChangesAsync();
        }

        var result = new PatientClinicalHistoryDto
        {
            PatientId = patientId,
            PatientName = patient?.FullName ?? "Bệnh nhân",
            PatientCode = patient?.PatientCode ?? string.Empty,
            Dob = patient != null ? patient.BirthDate.ToString("dd/MM/yyyy") : string.Empty,
            Gender = patient != null ? patient.Gender.ToString() : string.Empty,
            TotalEncounters = encounters.Count,
            Encounters = encounters.Select(e =>
            {
                var exam = clinicalExams.FirstOrDefault(c => c.EncounterId == e.Id);
                var orders = medicalOrders.Where(m => m.EncounterId == e.Id).ToList();
                var labs = labResults.Where(l => l.EncounterId == e.Id).ToList();
                var images = imagingRecords.Where(r => r.EncounterId == e.Id).ToList();
                var rxs = prescriptions.Where(p => p.EncounterId == e.Id).ToList();
                summaries.TryGetValue(e.Id, out var summary);

                return new HistoricalEncounterDto
                {
                    EncounterId = e.Id,
                    EncounterCode = e.EncounterCode,
                    EncounterDate = e.EncounterDate,
                    BranchCode = e.BranchCode,
                    Status = e.Status.ToString(),
                    EncounterType = e.EncounterType.ToString(),
                    CompanyName = string.Empty,
                    PackageName = string.Empty,
                    LeadDoctorName = summary?.LeadDoctorName,
                    OverallGrade = summary?.OverallGrade,
                    OverallGradeText = summary?.OverallGradeText,
                    ConclusionText = summary?.ConclusionText,
                    Recommendations = summary?.Recommendations,
                    VitalSigns = exam?.VitalSigns == null ? null : new VitalSignsDto
                    {
                        Id = exam.VitalSigns.Id,
                        ClinicalExamId = exam.VitalSigns.ClinicalExamId,
                        HeightCm = exam.VitalSigns.HeightCm,
                        WeightKg = exam.VitalSigns.WeightKg,
                        Bmi = exam.VitalSigns.Bmi,
                        Pulse = exam.VitalSigns.Pulse,
                        SystolicBp = exam.VitalSigns.SystolicBp,
                        DiastolicBp = exam.VitalSigns.DiastolicBp,
                        Temperature = exam.VitalSigns.Temperature,
                        RespiratoryRate = exam.VitalSigns.RespiratoryRate,
                        SpO2 = exam.VitalSigns.SpO2,
                        RecordedAt = exam.VitalSigns.RecordedAt,
                        RecordedByStaffId = exam.VitalSigns.RecordedByStaffId,
                        Notes = exam.VitalSigns.Notes
                    },
                    SpecialtyDetails = exam?.SpecialtyDetails.Select(s => new SpecialtyExamDetailDto
                    {
                        Id = s.Id,
                        SpecialtyType = s.SpecialtyType,
                        DoctorStaffId = s.DoctorStaffId,
                        DoctorStaffName = s.DoctorStaff?.FullName ?? string.Empty,
                        IsNormal = s.IsNormal,
                        ClinicalDescription = s.ClinicalDescription,
                        IcdCode = s.IcdCode,
                        IcdDiseaseName = s.IcdDiseaseName,
                        HealthClassificationRefId = s.HealthClassificationRefId,
                        Advice = s.Advice,
                        ExaminedAt = s.ExaminedAt
                    }).ToList() ?? new(),
                    MedicalOrders = orders.Select(o => new MedicalOrderDto
                    {
                        Id = o.Id,
                        OrderCode = o.OrderCode,
                        EncounterId = o.EncounterId,
                        EncounterCode = o.EncounterCode,
                        DoctorId = o.DoctorId,
                        DoctorName = o.DoctorName,
                        OrderType = o.OrderType,
                        SpecialtyType = o.SpecialtyType,
                        Status = o.Status,
                        IsInPackage = o.IsInPackage,
                        PaymentStatus = o.PaymentStatus,
                        TotalAmount = o.TotalAmount,
                        Items = o.Items.Select(i => new MedicalOrderItemDto
                        {
                            Id = i.Id,
                            MedicalOrderId = i.MedicalOrderId,
                            ServiceId = i.ServiceId,
                            ServiceCode = i.ServiceCode,
                            ServiceName = i.ServiceName,
                            UnitPrice = i.UnitPrice,
                            Quantity = i.Quantity,
                            TotalPrice = i.TotalPrice,
                            IsInPackage = i.IsInPackage,
                            PaymentStatus = i.PaymentStatus,
                            ReceiptId = i.ReceiptId,
                            PaidAt = i.PaidAt,
                            Notes = i.Notes
                        }).ToList()
                    }).ToList(),
                    LabResults = labs.Select(l => new LabResultDto
                    {
                        Id = l.Id,
                        EncounterId = l.EncounterId,
                        EncounterCode = l.EncounterCode,
                        TubeBarcode = l.TubeBarcode,
                        TestCode = l.TestCode,
                        TestName = l.TestName,
                        NumericResult = l.NumericResult,
                        StringResult = l.StringResult,
                        Unit = l.Unit,
                        ReferenceRange = l.ReferenceRange,
                        IsAbnormal = l.IsAbnormal,
                        M3FieldMapping = l.M3FieldMapping,
                        ResultTime = l.ResultTime
                    }).ToList(),
                    ImagingRecords = images.Select(img => new DiagnosticImagingDto
                    {
                        Id = img.Id,
                        EncounterId = img.EncounterId,
                        EncounterCode = img.EncounterCode,
                        Modality = (int)img.Modality,
                        ServiceCode = img.ServiceCode,
                        ServiceName = img.ServiceName,
                        Description = img.Description,
                        Conclusion = img.Conclusion,
                        StudyInstanceUid = img.StudyInstanceUid,
                        DicomWebUrl = img.DicomWebUrl,
                        RadiologistDoctorName = img.RadiologistDoctorName,
                        IsSigned = img.IsSigned,
                        SignedAt = img.SignedAt,
                        CapturedMedia = img.CapturedImages.Select(m => new CapturedMediaDto
                        {
                            Id = m.Id,
                            Position = m.Position,
                            MediaUrlOrBase64 = m.MediaUrlOrBase64,
                            Caption = m.Caption,
                            CapturedAt = m.CapturedAt
                        }).ToList()
                    }).ToList(),
                    Prescriptions = rxs.Select(rx => new PrescriptionDto
                    {
                        Id = rx.Id,
                        EncounterId = rx.EncounterId,
                        EncounterCode = rx.EncounterCode,
                        DoctorId = rx.DoctorId,
                        DoctorName = rx.DoctorName,
                        DiagnosisIcd10 = rx.DiagnosisIcd10,
                        DiagnosisText = rx.DiagnosisText,
                        Status = (int)rx.Status,
                        IsSigned = rx.IsSigned,
                        IsPaid = rx.IsPaid,
                        IsExternalPurchase = rx.IsExternalPurchase,
                        SpecialtyType = rx.SpecialtyType,
                        NationalPrescriptionCode = rx.NationalPrescriptionCode,
                        Items = rx.Items.Select(item => new PrescriptionItemDto
                        {
                            Id = item.Id,
                            DrugId = item.DrugId,
                            DrugName = item.DrugName,
                            NationalDrugCode = item.NationalDrugCode,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Route = item.Route,
                            UsageDays = item.UsageDays,
                            Instructions = item.Instructions
                        }).ToList()
                    }).ToList()
                };
            }).ToList()
        };

        return result;
    }
}
