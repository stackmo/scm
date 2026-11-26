using System.Security.Cryptography;
using System.Text;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Clinical;

/// <summary>
/// Lưu chữ ký điện tử của Bệnh nhân / Người giám hộ cho 7 biểu mẫu theo TT32/2023/TT-BYT và Luật KCB 2023.
/// Bất biến: Sau khi ký (Created), bản ghi này chỉ được thêm, không được xóa hoặc sửa nội dung.
/// </summary>
public class PatientConsentSignature : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid? PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public PatientFormType FormType { get; set; }
    public string FormCode { get; set; } = string.Empty;
    public string FormTitle { get; set; } = string.Empty;
    public string ContentSnapshotJson { get; set; } = "{}";

    public string SignerName { get; set; } = string.Empty;
    public string SignerRelation { get; set; } = "Bản thân";
    public string SignatureImageBase64 { get; set; } = string.Empty;
    public string SignatureHash { get; set; } = string.Empty;

    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public Guid? CollectedByStaffId { get; set; }
    public string? CollectedByStaffName { get; set; }

    public PatientConsentSignature() { }

    public PatientConsentSignature(
        Guid branchId,
        string branchCode,
        Guid encounterId,
        string encounterCode,
        Guid? patientId,
        string patientName,
        PatientFormType formType,
        string formCode,
        string formTitle,
        string contentSnapshotJson,
        string signerName,
        string? signerRelation,
        string signatureImageBase64,
        Guid? collectedByStaffId = null,
        string? collectedByStaffName = null)
    {
        if (encounterId == Guid.Empty)
            throw new BusinessException(
                SCMErrorCodes.PatientConsentEncounterInvalid,
                "EncounterId không được để trống khi ký biểu mẫu.");

        if (string.IsNullOrWhiteSpace(signatureImageBase64))
            throw new BusinessException(
                SCMErrorCodes.PatientSignatureEmpty,
                $"Chữ ký không được để trống cho biểu mẫu {formCode}.");

        if (string.IsNullOrWhiteSpace(signerName))
            throw new BusinessException(
                SCMErrorCodes.PatientSignatureEmpty,
                "Tên người ký không được để trống.");

        BranchId = branchId;
        BranchCode = branchCode;
        EncounterId = encounterId;
        EncounterCode = encounterCode;
        PatientId = patientId;
        PatientName = patientName;
        FormType = formType;
        FormCode = formCode;
        FormTitle = formTitle;
        ContentSnapshotJson = string.IsNullOrWhiteSpace(contentSnapshotJson) ? "{}" : contentSnapshotJson;
        SignerName = signerName.Trim();
        SignerRelation = string.IsNullOrWhiteSpace(signerRelation) ? "Bản thân" : signerRelation.Trim();
        SignatureImageBase64 = signatureImageBase64;
        SignatureHash = ComputeSha256Hash(signatureImageBase64);
        SignedAt = DateTime.UtcNow;
        CollectedByStaffId = collectedByStaffId;
        CollectedByStaffName = collectedByStaffName;
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
