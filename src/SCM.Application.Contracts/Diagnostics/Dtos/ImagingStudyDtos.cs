using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Diagnostics.Dtos;

public class CapturedMediaDto
{
    public Guid Id { get; set; }
    public Guid DiagnosticImagingRecordId { get; set; }
    public CapturedMediaType MediaType { get; set; } = CapturedMediaType.Image;
    public int Position { get; set; } // 1, 2, 3, 4 (0 for Video)
    public string MediaUrlOrBase64 { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int? DurationSeconds { get; set; }
    public string? ThumbnailUrlOrBase64 { get; set; }
    public string? DicomSopInstanceUid { get; set; }
    public DateTime CapturedAt { get; set; }
}

public class SaveImagingResultWithMediaDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public ImagingModality Modality { get; set; } = ImagingModality.US;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;

    public Guid? ReportTemplateId { get; set; }
    public string? Description { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public string? Recommendation { get; set; }

    public string? StudyInstanceUid { get; set; }
    public string? DicomWebUrl { get; set; }

    public Guid? RadiologistDoctorId { get; set; }
    public string? RadiologistDoctorName { get; set; }
    public string? DoctorLicenseNo { get; set; }
    public bool SignImmediately { get; set; } = false;

    public List<CapturedMediaDto> CapturedImages { get; set; } = new();
}

public class ImagingReportTemplateDto
{
    public Guid Id { get; set; }
    public ImagingModality Modality { get; set; }
    public string SpecialtyCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int Gender { get; set; }
    public string ContentTemplate { get; set; } = string.Empty;
    public string DefaultConclusion { get; set; } = string.Empty;
    public string? DefaultRecommendation { get; set; }
    public bool IsActive { get; set; }
}

public class CreateImagingReportTemplateDto
{
    public ImagingModality Modality { get; set; } = ImagingModality.US;
    public string SpecialtyCode { get; set; } = "ABDOMEN";
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public int Gender { get; set; } = 0;
    public string ContentTemplate { get; set; } = string.Empty;
    public string DefaultConclusion { get; set; } = string.Empty;
    public string? DefaultRecommendation { get; set; }
}
