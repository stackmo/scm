using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Clinical;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Clinical;

public class PatientConsentAppService : IPatientConsentAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ICurrentBranch _currentBranch;

    private static readonly IReadOnlyList<PatientFormTemplateInfoDto> Catalog = new List<PatientFormTemplateInfoDto>
    {
        new()
        {
            FormType = PatientFormType.TT32_HistoryCommitment,
            FormCode = "BM-TT32-01",
            FormTitle = "Giấy KSK TT32 — Phần I: Tiền sử bệnh & Cam kết khai báo",
            LegalBasis = "Thông tư 32/2023/TT-BYT (Phụ lục XXIV)",
            VscTemplateRef = "template_TongQuat.docx",
            Stage = "Tiếp nhận / Trước khi khám"
        },
        new()
        {
            FormType = PatientFormType.ReceptionChecklist,
            FormCode = "BM-CHK-02",
            FormTitle = "Phiếu Hướng dẫn & Checklist Quy trình Khám đoàn",
            LegalBasis = "VSC Parity",
            VscTemplateRef = "template_checklist.docx",
            Stage = "Quầy Tiếp nhận"
        },
        new()
        {
            FormType = PatientFormType.ProcedureConsent,
            FormCode = "BM-TT-03",
            FormTitle = "Giấy Cam kết Thực hiện Thủ thuật / Nội soi / CĐHA can thiệp",
            LegalBasis = "Luật KCB 2023 & VSC",
            VscTemplateRef = "frmNoiSoi",
            Stage = "Trước khi làm Thủ thuật / Nội soi"
        },
        new()
        {
            FormType = PatientFormType.ObGynRefusal,
            FormCode = "BM-SPK-04",
            FormTitle = "Giấy Xác nhận Từ chối Khám Sản Phụ khoa theo nguyện vọng",
            LegalBasis = "TT32/2023/TT-BYT (Consent Guard Sản phụ khoa)",
            VscTemplateRef = "IsObGynRefused",
            Stage = "Phòng Khám Sản / Tổng quát"
        },
        new()
        {
            FormType = PatientFormType.DepositSettlementSlip,
            FormCode = "BM-KQ-05",
            FormTitle = "Phiếu Thu Tạm ứng / Tất toán / Hoàn ứng Ký quỹ",
            LegalBasis = "VSC Parity",
            VscTemplateRef = "frmPhieuTamUng",
            Stage = "Thu ngân / Quầy Dược trước khi ra về"
        },
        new()
        {
            FormType = PatientFormType.ServiceDrugReceipt,
            FormCode = "BM-PT-06",
            FormTitle = "Phiếu Thu Viện phí Dịch vụ / Đơn thuốc / Xác nhận BHYT",
            LegalBasis = "QĐ 130/QĐ-BYT & NĐ 123/TT78",
            VscTemplateRef = "rptPhieuThu",
            Stage = "Quầy Thu ngân / Quầy Dược"
        },
        new()
        {
            FormType = PatientFormType.HandoverAndSummary,
            FormCode = "BM-BG-07",
            FormTitle = "Phiếu Bàn giao Lĩnh thuốc / Trả phim & Tổng kết Hồ sơ KSK",
            LegalBasis = "VSC Parity",
            VscTemplateRef = "KLLK_Sum",
            Stage = "Quầy Dược / Bộ phận Trả hồ sơ"
        }
    };

    public PatientConsentAppService(SCMDbContext dbContext, ICurrentBranch? currentBranch = null)
    {
        _dbContext = dbContext;
        _currentBranch = currentBranch ?? new NullCurrentBranch();
    }

    public List<PatientFormTemplateInfoDto> GetFormCatalog() => Catalog.ToList();

    public async Task<PatientConsentSignatureDto> SignAsync(SignPatientConsentDto input)
    {
        if (input.EncounterId == Guid.Empty)
            throw new BusinessException(SCMErrorCodes.PatientConsentEncounterInvalid, "EncounterId không hợp lệ.");

        var enc = await _dbContext.Encounters
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == input.EncounterId)
            ?? throw new BusinessException(SCMErrorCodes.EncounterNotFound, $"Không tìm thấy lượt khám {input.EncounterId}.");

        var tpl = Catalog.FirstOrDefault(c => c.FormType == input.FormType)
            ?? throw new ArgumentException($"Loại biểu mẫu {input.FormType} không hợp lệ.");

        var (branchId, branchCode) = await ResolveBranchAsync(enc.BranchId, enc.BranchCode);
        var entity = new PatientConsentSignature(
            branchId,
            branchCode,
            enc.Id,
            string.IsNullOrWhiteSpace(input.EncounterCode) ? enc.EncounterCode : input.EncounterCode!,
            input.PatientId == Guid.Empty ? enc.PatientId : input.PatientId,
            string.IsNullOrWhiteSpace(input.PatientName) ? (enc.Patient?.FullName ?? "Bệnh nhân") : input.PatientName!,
            input.FormType,
            tpl.FormCode,
            tpl.FormTitle,
            input.ContentSnapshotJson ?? "{}",
            input.SignerName,
            input.SignerRelation,
            input.SignatureImageBase64,
            input.CollectedByStaffId,
            input.CollectedByStaffName);

        _dbContext.PatientConsentSignatures.Add(entity);
        _dbContext.AuditLogPIIs.Add(new AuditLogPII(
            input.CollectedByStaffId,
            entity.CollectedByStaffName,
            branchId,
            branchCode,
            "PATIENT_SIGN_CONSENT",
            nameof(PatientConsentSignature),
            entity.Id.ToString(),
            details: $"Bệnh nhân [{entity.SignerName}] ký [{entity.FormCode}] cho HS {entity.EncounterCode} (Hash={entity.SignatureHash[..12]})"));

        await _dbContext.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<List<PatientConsentSignatureDto>> GetByEncounterAsync(Guid encounterId)
    {
        var list = await _dbContext.PatientConsentSignatures.AsNoTracking()
            .Where(s => s.EncounterId == encounterId)
            .OrderByDescending(s => s.SignedAt)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    private async Task<(Guid BranchId, string BranchCode)> ResolveBranchAsync(Guid fallbackId, string fallbackCode)
    {
        if (_currentBranch.Id.HasValue && _currentBranch.Id.Value != Guid.Empty && !string.IsNullOrWhiteSpace(_currentBranch.Code))
            return (_currentBranch.Id.Value, _currentBranch.Code!);

        var code = string.IsNullOrWhiteSpace(_currentBranch.Code) ? fallbackCode : _currentBranch.Code!;
        if (!string.IsNullOrWhiteSpace(code))
        {
            var b = await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code);
            if (b != null) return (b.Id, b.Code);
        }

        return (fallbackId, string.IsNullOrWhiteSpace(fallbackCode) ? "CNQ1" : fallbackCode);
    }

    private static PatientConsentSignatureDto MapToDto(PatientConsentSignature s) => new()
    {
        Id = s.Id,
        BranchCode = s.BranchCode,
        EncounterId = s.EncounterId,
        EncounterCode = s.EncounterCode,
        PatientId = s.PatientId ?? Guid.Empty,
        PatientName = s.PatientName,
        FormType = s.FormType,
        FormCode = s.FormCode,
        FormTitle = s.FormTitle,
        ContentSnapshotJson = s.ContentSnapshotJson,
        SignerName = s.SignerName,
        SignerRelation = s.SignerRelation,
        SignatureImageBase64 = s.SignatureImageBase64,
        SignatureHash = s.SignatureHash,
        SignedAt = s.SignedAt,
        CollectedByStaffName = s.CollectedByStaffName ?? string.Empty
    };
}
