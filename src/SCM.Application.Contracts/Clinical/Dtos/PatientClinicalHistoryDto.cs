using System;
using System.Collections.Generic;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Prescription.Dtos;

namespace SCM.Application.Contracts.Clinical.Dtos;

/// <summary>
/// Hồ sơ lịch sử khám bệnh đa phiên toàn diện (Longitudinal Patient EMR History).
/// </summary>
public class PatientClinicalHistoryDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientCode { get; set; } = string.Empty;
    public string Dob { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int TotalEncounters { get; set; }
    public List<HistoricalEncounterDto> Encounters { get; set; } = new();
}

public class HistoricalEncounterDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public DateTime EncounterDate { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EncounterType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string? LeadDoctorName { get; set; }
    public int? OverallGrade { get; set; }
    public string? OverallGradeText { get; set; }
    public string? ConclusionText { get; set; }
    public string? Recommendations { get; set; }

    public VitalSignsDto? VitalSigns { get; set; }
    public List<SpecialtyExamDetailDto> SpecialtyDetails { get; set; } = new();
    public List<MedicalOrderDto> MedicalOrders { get; set; } = new();
    public List<LabResultDto> LabResults { get; set; } = new();
    public List<DiagnosticImagingDto> ImagingRecords { get; set; } = new();
    public List<PrescriptionDto> Prescriptions { get; set; } = new();
}
