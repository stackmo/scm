using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Diagnostics.Exceptions;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Identity;

namespace SCM.Domain.Diagnostics;

/// <summary>
/// Bản ghi chẩn đoán hình ảnh & thăm dò chức năng (X-quang DR, Nhũ ảnh, Loãng xương, Siêu âm, Nội soi, Điện tim ECG).
/// Tích hợp vòng đời KTV chụp/đo -> BS đọc & ký số -> Trả phim (IsFilmDelivered) -> Khóa bất biến / Xin mở khóa / Addendum.
/// </summary>
public class DiagnosticImagingRecord : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid? MedicalOrderId { get; set; }
    public Guid? MedicalOrderItemId { get; set; }

    public ImagingModality Modality { get; set; } = ImagingModality.DR;
    public string RoomCode { get; set; } = string.Empty;
    public EndoscopySubType EndoscopyGroup { get; set; } = EndoscopySubType.None;
    public ImagingWorkflowStatus WorkflowStatus { get; set; } = ImagingWorkflowStatus.Pending;

    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;

    public string? DicomAccessionNumber { get; set; }
    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }
    public string? Description { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
    public Guid? ReportTemplateId { get; set; }

    public int LayoutMode { get; set; } = 2; // 1, 2, 4
    public bool IsAbnormal { get; set; } = false;
    public string? CloTestResult { get; set; } // Negative, Positive, NotPerformed

    public List<CapturedMedia> CapturedImages { get; set; } = new();

    // KTV thực hiện chụp/đo (tách biệt với BS đọc phim)
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? CapturedAt { get; set; }
    public string? TechNotes { get; set; }

    // Bác sĩ đọc kết quả & ký số
    public Guid? RadiologistDoctorId { get; set; }
    public string? RadiologistDoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public string? SignatureHash { get; set; }

    public bool IsSigned { get; set; } = false;
    public DateTime? SignedAt { get; set; }

    // Trả phim (VSC TraFilm parity)
    public bool IsFilmDelivered { get; set; } = false;
    public DateTime? FilmDeliveredAt { get; set; }
    public Guid? FilmDeliveredById { get; set; }
    public string? FilmDeliveredByName { get; set; }

    // Khóa bất biến, Hủy duyệt & Addendum
    public bool IsLocked { get; set; } = false;
    public string? UnlockReason { get; set; }
    public string? AddendumText { get; set; }
    public int AddendumCount { get; set; } = 0;

    [ConcurrencyCheck]
    public int Version { get; set; } = 1;
    public Guid ConcurrencyStamp { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DiagnosticImagingRecord() { }

    public DiagnosticImagingRecord(
        Guid encounterId,
        string encounterCode,
        ImagingModality modality,
        string serviceCode,
        string serviceName,
        string conclusion,
        string? studyInstanceUid = null,
        string? dicomWebUrl = null,
        Guid? radiologistDoctorId = null,
        string? radiologistDoctorName = null,
        string? doctorLicenseNo = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (encounterId == Guid.Empty)
            throw new ArgumentException("EncounterId không được để trống.", nameof(encounterId));
        if (string.IsNullOrWhiteSpace(serviceCode))
            throw new ArgumentException("ServiceCode không được để trống.", nameof(serviceCode));

        EncounterId = encounterId;
        EncounterCode = encounterCode;
        Modality = modality;
        ServiceCode = serviceCode.Trim();
        ServiceName = serviceName.Trim();
        Conclusion = conclusion.Trim();
        StudyInstanceUid = studyInstanceUid;
        DicomWebUrl = dicomWebUrl;
        RadiologistDoctorId = radiologistDoctorId;
        RadiologistDoctorName = radiologistDoctorName;
        DoctorLicenseNo = doctorLicenseNo;
        RoomCode = ResolveDefaultRoomCode(modality);
        CreatedAt = DateTime.UtcNow;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public static string ResolveDefaultRoomCode(ImagingModality modality) => modality switch
    {
        ImagingModality.US => "P.SA01",
        ImagingModality.Endoscopy => "P.NS01",
        ImagingModality.ECG => "P.ECG",
        _ => "P.XQ01"
    };

    public void EnsureMutable()
    {
        if (IsLocked || (IsFilmDelivered && WorkflowStatus != ImagingWorkflowStatus.UnlockedForRevision))
        {
            throw new ImagingRecordImmutableException(
                $"Kết quả CĐHA '{ServiceName}' đã được ký số hoặc đã Trả phim (IsLocked={IsLocked}, IsFilmDelivered={IsFilmDelivered}). Nghiêm cấm sửa/xóa trực tiếp; vui lòng xin mở khóa hoặc tạo Bản bổ sung (Addendum).");
        }
    }

    public void AddOrUpdateCapturedImage(int position, string mediaData, string? caption = null, string? dicomSopUid = null)
    {
        EnsureMutable();

        if (position < 1 || position > 4)
            throw new ArgumentOutOfRangeException(nameof(position), "Vị trí khung ảnh trên phiếu in phải từ 1 đến 4.");

        var existing = CapturedImages.FirstOrDefault(x => x.MediaType == CapturedMediaType.Image && x.Position == position);
        if (existing != null)
        {
            CapturedImages.Remove(existing);
        }

        CapturedImages.Add(new CapturedMedia(Id, position, mediaData, caption, CapturedMediaType.Image, null, null, dicomSopUid));
        if (WorkflowStatus == ImagingWorkflowStatus.Pending || WorkflowStatus == ImagingWorkflowStatus.InProgress)
        {
            WorkflowStatus = ImagingWorkflowStatus.Captured;
            CapturedAt ??= DateTime.UtcNow;
        }
    }

    public void AddVideoClip(string mediaData, int durationSeconds, string? caption = null, string? thumbnailUrlOrBase64 = null)
    {
        EnsureMutable();
        CapturedImages.Add(new CapturedMedia(
            Id,
            0,
            mediaData,
            caption,
            CapturedMediaType.Video,
            durationSeconds,
            thumbnailUrlOrBase64));

        if (WorkflowStatus == ImagingWorkflowStatus.Pending || WorkflowStatus == ImagingWorkflowStatus.InProgress)
        {
            WorkflowStatus = ImagingWorkflowStatus.Captured;
            CapturedAt ??= DateTime.UtcNow;
        }
    }

    public void ApplyTemplate(ImagingReportTemplate template)
    {
        EnsureMutable();
        if (template == null) throw new ArgumentNullException(nameof(template));
        ReportTemplateId = template.Id;
        Description = template.ContentTemplate;
        Conclusion = template.DefaultConclusion;
        Recommendation = template.DefaultRecommendation;
    }

    public void PushWorklist(string accessionNumber, string? roomCode = null)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(accessionNumber))
            throw new ArgumentException("AccessionNumber không được để trống.", nameof(accessionNumber));

        DicomAccessionNumber = accessionNumber.Trim();
        if (!string.IsNullOrWhiteSpace(roomCode)) RoomCode = roomCode.Trim();
        if (WorkflowStatus == ImagingWorkflowStatus.Pending)
            WorkflowStatus = ImagingWorkflowStatus.InProgress;
        BumpVersion();
    }

    public void MarkCaptured(
        Guid technicianId,
        string technicianName,
        string? studyInstanceUid,
        string? dicomWebUrl,
        string? techNotes = null,
        DateTime? capturedAt = null)
    {
        EnsureMutable();

        TechnicianId = technicianId;
        TechnicianName = technicianName.Trim();
        CapturedAt = capturedAt ?? DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(studyInstanceUid)) StudyInstanceUid = studyInstanceUid.Trim();
        if (!string.IsNullOrWhiteSpace(dicomWebUrl)) DicomWebUrl = dicomWebUrl.Trim();
        if (!string.IsNullOrWhiteSpace(techNotes)) TechNotes = techNotes.Trim();

        WorkflowStatus = ImagingWorkflowStatus.Captured;
        BumpVersion();
    }

    public void UpdateDraftReport(
        string? description,
        string conclusion,
        string? recommendation,
        int layoutMode = 2,
        bool isAbnormal = false,
        string? cloTestResult = null,
        EndoscopySubType? endoscopyGroup = null)
    {
        EnsureMutable();

        Description = description;
        Conclusion = conclusion?.Trim() ?? string.Empty;
        Recommendation = recommendation;
        LayoutMode = layoutMode is 1 or 2 or 4 ? layoutMode : 2;
        IsAbnormal = isAbnormal;
        CloTestResult = cloTestResult;
        if (endoscopyGroup.HasValue && endoscopyGroup.Value != EndoscopySubType.None)
            EndoscopyGroup = endoscopyGroup.Value;

        if (WorkflowStatus is ImagingWorkflowStatus.Pending or ImagingWorkflowStatus.InProgress or ImagingWorkflowStatus.Captured or ImagingWorkflowStatus.UnlockedForRevision)
        {
            WorkflowStatus = ImagingWorkflowStatus.DraftReported;
        }
        BumpVersion();
    }

    /// <summary>
    /// Ký số kết quả CĐHA (giữ tương thích ngược cho Wave 6/7 seeder & tests).
    /// </summary>
    public void SignResult(Guid doctorId, string doctorName, string licenseNo)
    {
        RadiologistDoctorId = doctorId;
        RadiologistDoctorName = doctorName;
        DoctorLicenseNo = licenseNo;
        IsSigned = true;
        SignedAt = DateTime.UtcNow;
        WorkflowStatus = ImagingWorkflowStatus.Signed;
        IsLocked = true;
        SignatureHash ??= $"IMG-SIG-{Id.ToString("N")[..8].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    /// <summary>
    /// Ký số kết quả CĐHA chuẩn Workstation với đầy đủ bất biến INV-RIS-02, INV-RIS-03, INV-RIS-04, INV-RIS-05.
    /// </summary>
    public void SignWorkstationReport(
        Guid doctorId,
        string doctorName,
        string licenseNo,
        ClinicalActorGroup actorGroup,
        string? primarySpecialty = null,
        IEnumerable<string>? secondarySpecialties = null,
        string? signatureHash = null)
    {
        EnsureMutable();

        // INV-RIS-02: KTV tuyệt đối không được ký số kết luận y khoa
        if (actorGroup == ClinicalActorGroup.ParaclinicalTechnician)
        {
            throw new UnauthorizedModalitySignException(
                "Kỹ thuật viên (Technician) chỉ được phép chụp/đo và xác nhận Đã chụp (Captured). Không có quyền ký số kết luận chẩn đoán y khoa.");
        }

        if (doctorId == Guid.Empty || string.IsNullOrWhiteSpace(licenseNo))
        {
            throw new UnauthorizedModalitySignException(
                "Bác sĩ ký số CĐHA bắt buộc phải có Chứng chỉ hành nghề (CCHN) hợp lệ.");
        }

        // INV-RIS-03: Phân tách quyền Nội soi Tiêu hóa vs Nội soi Tai Mũi Họng
        if (Modality == ImagingModality.Endoscopy)
        {
            ValidateEndoscopySigningAuthority(actorGroup, primarySpecialty, secondarySpecialties);
        }

        // INV-RIS-02: Nhóm DR / Mammography / BoneDensity / ECG bắt buộc đã qua KTV chụp/đo (Captured)
        if (Modality is ImagingModality.DR or ImagingModality.Mammography or ImagingModality.BoneDensity or ImagingModality.ECG)
        {
            bool hasStudyOrMedia = !string.IsNullOrWhiteSpace(StudyInstanceUid) ||
                                   !string.IsNullOrWhiteSpace(DicomWebUrl) ||
                                   CapturedImages.Any();
            if (!CapturedAt.HasValue || !hasStudyOrMedia || WorkflowStatus < ImagingWorkflowStatus.Captured)
            {
                throw new ImagingStudyNotCapturedException(
                    $"Ca {Modality} '{ServiceName}' chưa được KTV xác nhận Đã chụp/đo và đẩy phim PACS/sóng ECG. Không thể ký số kết luận.");
            }

            if (CapturedAt.Value > DateTime.UtcNow.AddMinutes(1))
            {
                throw new ImagingStudyNotCapturedException(
                    "Ngày giờ chụp/đo không hợp lệ (lớn hơn thời điểm ký kết luận).");
            }
        }

        // INV-RIS-04: Siêu âm & Nội soi bắt buộc có ít nhất 1 ảnh in (Position 1..4)
        if (Modality is ImagingModality.US or ImagingModality.Endoscopy)
        {
            bool hasPrintableImage = CapturedImages.Any(m => m.MediaType == CapturedMediaType.Image && m.Position >= 1 && m.Position <= 4);
            if (!hasPrintableImage)
            {
                throw new ImagingMediaRequiredException(
                    $"Phiếu {Modality} '{ServiceName}' bắt buộc phải có ít nhất 1 ảnh chụp hợp lệ (Slot 1..4) trước khi ký số.");
            }
        }

        if (string.IsNullOrWhiteSpace(Conclusion))
        {
            throw new ArgumentException("Kết luận chẩn đoán không được để trống trước khi ký số.");
        }

        RadiologistDoctorId = doctorId;
        RadiologistDoctorName = doctorName.Trim();
        DoctorLicenseNo = licenseNo.Trim();
        IsSigned = true;
        SignedAt = DateTime.UtcNow;
        WorkflowStatus = ImagingWorkflowStatus.Signed;
        IsLocked = true;
        SignatureHash = string.IsNullOrWhiteSpace(signatureHash)
            ? $"IMG-SIG-{Id.ToString("N")[..8].ToUpperInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : signatureHash.Trim();
        BumpVersion();
    }

    private void ValidateEndoscopySigningAuthority(
        ClinicalActorGroup actorGroup,
        string? primarySpecialty,
        IEnumerable<string>? secondarySpecialties)
    {
        if (actorGroup == ClinicalActorGroup.RadiologyDoctor)
            return; // BS CĐHA được ký cả Nội soi Tiêu hóa và TMH

        var specialties = new List<string>();
        if (!string.IsNullOrWhiteSpace(primarySpecialty)) specialties.Add(primarySpecialty);
        if (secondarySpecialties != null) specialties.AddRange(secondarySpecialties);

        bool isEntSpecialist = DoctorCapability.ParseSpecialty(primarySpecialty) == ClinicalSpecialtyType.Otorhinolaryngology ||
                               specialties.Any(s => DoctorCapability.ParseSpecialty(s) == ClinicalSpecialtyType.Otorhinolaryngology);

        bool isGiEndoscopySpecialist = specialties.Any(s =>
            s.Contains("Tiêu Hóa", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Tieu Hoa", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Gastro", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Chẩn Đoán Hình Ảnh", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Chan Doan Hinh Anh", StringComparison.OrdinalIgnoreCase));

        if (EndoscopyGroup == EndoscopySubType.ENT)
        {
            if (!isEntSpecialist && !isGiEndoscopySpecialist)
            {
                throw new UnauthorizedModalitySignException(
                    "Nội soi Tai Mũi Họng chỉ cho phép Bác sĩ CĐHA hoặc Bác sĩ chuyên khoa Tai Mũi Họng ký số.");
            }
            return;
        }

        if (EndoscopyGroup == EndoscopySubType.GastroIntestinal)
        {
            if (!isGiEndoscopySpecialist)
            {
                throw new UnauthorizedModalitySignException(
                    "Nội soi Tiêu hóa (Thực quản - Dạ dày - Đại tràng) chỉ cho phép Bác sĩ CĐHA hoặc Bác sĩ Nội soi Tiêu hóa ký số.");
            }
            return;
        }

        if (!isEntSpecialist && !isGiEndoscopySpecialist)
        {
            throw new UnauthorizedModalitySignException(
                "Bạn không có thẩm quyền chuyên khoa để ký kết quả Nội soi.");
        }
    }

    public void ConfirmFilmDelivery(Guid staffId, string staffName)
    {
        if (!IsSigned)
        {
            throw new ImagingStudyNotCapturedException("Chỉ được xác nhận Trả phim sau khi Bác sĩ CĐHA đã ký số kết quả.");
        }

        IsFilmDelivered = true;
        FilmDeliveredAt = DateTime.UtcNow;
        FilmDeliveredById = staffId;
        FilmDeliveredByName = staffName.Trim();
        IsLocked = true;
        BumpVersion();
    }

    public void RequestUnlock(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
        {
            throw new ArgumentException("Lý do xin hủy duyệt / mở khóa phải có tối thiểu 10 ký tự.", nameof(reason));
        }

        WorkflowStatus = ImagingWorkflowStatus.UnlockRequested;
        UnlockReason = reason.Trim();
        BumpVersion();
    }

    public void ApproveUnlock()
    {
        IsLocked = false;
        IsSigned = false;
        WorkflowStatus = ImagingWorkflowStatus.UnlockedForRevision;
        BumpVersion();
    }

    public void AddAddendum(string addendumText, string actorName)
    {
        if (string.IsNullOrWhiteSpace(addendumText))
            throw new ArgumentException("Nội dung bản bổ sung (Addendum) không được để trống.", nameof(addendumText));

        var entry = $"[{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC - {actorName.Trim()}]: {addendumText.Trim()}";
        AddendumText = string.IsNullOrWhiteSpace(AddendumText) ? entry : $"{AddendumText}\n{entry}";
        AddendumCount++;
        BumpVersion();
    }

    private void BumpVersion()
    {
        Version++;
        ConcurrencyStamp = Guid.NewGuid();
    }
}
