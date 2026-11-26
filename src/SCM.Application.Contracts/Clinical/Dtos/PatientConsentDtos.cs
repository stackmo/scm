using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Clinical.Dtos;

public class PatientFormTemplateInfoDto
{
    public PatientFormType FormType { get; set; }
    public string FormCode { get; set; } = string.Empty;
    public string FormTitle { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string LegalBasis { get; set; } = string.Empty;
    public string VscTemplateRef { get; set; } = string.Empty;
}

public class SignPatientConsentDto
{
    public Guid EncounterId { get; set; }
    public string? EncounterCode { get; set; }
    public Guid PatientId { get; set; }
    public string? PatientName { get; set; }
    public PatientFormType FormType { get; set; }
    public string? FormTitle { get; set; }
    public string? ContentSnapshotJson { get; set; }
    public string SignerName { get; set; } = string.Empty;
    public string SignerRelation { get; set; } = "Bệnh nhân";
    public string SignatureImageBase64 { get; set; } = string.Empty;
    public Guid CollectedByStaffId { get; set; }
    public string CollectedByStaffName { get; set; } = string.Empty;
}

public class PatientConsentSignatureDto
{
    public Guid Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public PatientFormType FormType { get; set; }
    public string FormCode { get; set; } = string.Empty;
    public string FormTitle { get; set; } = string.Empty;
    public string ContentSnapshotJson { get; set; } = "{}";
    public string SignerName { get; set; } = string.Empty;
    public string SignerRelation { get; set; } = string.Empty;
    public string SignatureImageBase64 { get; set; } = string.Empty;
    public string SignatureHash { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string CollectedByStaffName { get; set; } = string.Empty;
}
