using System.IO.Compression;
using System.Security;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Grading;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Grading;

public class BatchDossierAppService : IBatchDossierAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly IHealthSummaryAppService _healthSummaryService;

    public BatchDossierAppService(
        SCMDbContext dbContext,
        IHealthSummaryAppService healthSummaryService)
    {
        _dbContext = dbContext;
        _healthSummaryService = healthSummaryService;
    }

    public async Task<DossierFileResult> GenerateSingleDossierDocxAsync(Guid encounterId)
    {
        var consolidated = await _healthSummaryService.GetConsolidatedEncounterAsync(encounterId, "all");
        var encounter = await _dbContext.Encounters.AsNoTracking()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        var branch = encounter != null
            ? await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == encounter.BranchId)
            : await _dbContext.Branches.AsNoTracking().FirstOrDefaultAsync();

        var docxBytes = BuildDocxBytes(consolidated, encounter?.Patient?.Gender.ToString(), encounter?.Patient?.BirthDate, branch?.Name, branch?.MinistryFacilityCode);
        var safeCode = SanitizeFileName(string.IsNullOrWhiteSpace(consolidated.EncounterCode) ? encounterId.ToString("N")[..8] : consolidated.EncounterCode);
        var safeName = SanitizeFileName(string.IsNullOrWhiteSpace(consolidated.PatientName) ? "BenhNhan" : consolidated.PatientName);

        return new DossierFileResult
        {
            FileBytes = docxBytes,
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FileName = $"HoSoKSK_{safeCode}_{safeName}.docx",
            DossierCount = 1
        };
    }

    public async Task<DossierFileResult> GenerateBatchDossierZipAsync(BatchDossierRequestDto input)
    {
        var targetIds = new List<Guid>();
        if (input.EncounterIds != null && input.EncounterIds.Count > 0)
        {
            targetIds.AddRange(input.EncounterIds.Distinct());
        }
        else if (input.CompanyContractId.HasValue && input.CompanyContractId.Value != Guid.Empty)
        {
            targetIds = await _dbContext.Encounters.AsNoTracking()
                .Where(e => e.CompanyContractId == input.CompanyContractId.Value)
                .OrderBy(e => e.EncounterCode)
                .Select(e => e.Id)
                .Take(500)
                .ToListAsync();
        }

        if (targetIds.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy hồ sơ nào để xuất hàng loạt.");
        }

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var encId in targetIds)
            {
                var single = await GenerateSingleDossierDocxAsync(encId);
                var entry = archive.CreateEntry(single.FileName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await entryStream.WriteAsync(single.FileBytes);
            }
        }

        return new DossierFileResult
        {
            FileBytes = zipStream.ToArray(),
            ContentType = "application/zip",
            FileName = $"HoSoKSK_HangLoat_{DateTime.UtcNow:yyyyMMdd_HHmm}.zip",
            DossierCount = targetIds.Count
        };
    }

    private static byte[] BuildDocxBytes(
        ConsolidatedEncounterSummaryDto dto,
        string? gender,
        DateTime? birthDate,
        string? branchName,
        string? cskcbCode)
    {
        var docXml = BuildWordDocumentXml(dto, gender, birthDate, branchName, cskcbCode);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipText(zip, "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
                </Types>
                """);

            WriteZipText(zip, "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
                </Relationships>
                """);

            WriteZipText(zip, "word/document.xml", docXml);
        }

        return ms.ToArray();
    }

    private static string BuildWordDocumentXml(
        ConsolidatedEncounterSummaryDto dto,
        string? gender,
        DateTime? birthDate,
        string? branchName,
        string? cskcbCode)
    {
        var sb = new StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sb.Append("""<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>""");

        AppendParagraph(sb, $"{branchName ?? "PHÒNG KHÁM ĐA KHOA SCM"} (Mã CSKCB: {cskcbCode ?? "79001"})", bold: true, fontSizeHalfPt: 22);
        AppendParagraph(sb, "HỒ SƠ KHÁM SỨC KHỎE TỔNG HỢP (THÔNG TƯ 32/2023/TT-BYT)", bold: true, fontSizeHalfPt: 28);
        AppendParagraph(sb, $"Mã lượt khám: {dto.EncounterCode} | Họ tên: {dto.PatientName} | Giới tính: {gender ?? "-"} | Ngày sinh: {birthDate?.ToString("dd/MM/yyyy") ?? "-"}", bold: false, fontSizeHalfPt: 20);

        if (dto.VitalSigns != null)
        {
            AppendParagraph(sb, "I. CHỈ SỐ SINH HIỆU", bold: true, fontSizeHalfPt: 24);
            AppendParagraph(sb, $"Chiều cao: {dto.VitalSigns.HeightCm} cm | Cân nặng: {dto.VitalSigns.WeightKg} kg | BMI: {dto.VitalSigns.Bmi:F1} | Huyết áp: {dto.VitalSigns.SystolicBp}/{dto.VitalSigns.DiastolicBp} mmHg | Mạch: {dto.VitalSigns.Pulse} l/p", false, 20);
        }

        AppendParagraph(sb, "II. KẾT QUẢ KHÁM CHUYÊN KHOA", bold: true, fontSizeHalfPt: 24);
        if (dto.Specialties.Count == 0)
        {
            AppendParagraph(sb, "(Chưa ghi nhận kết quả chuyên khoa)", false, 20);
        }
        else
        {
            var rows = dto.Specialties.Select(s => new[]
            {
                s.SpecialtyType.ToString(),
                string.IsNullOrWhiteSpace(s.IcdCode) ? "-" : $"{s.IcdCode} - {s.IcdDiseaseName}",
                s.ClinicalDescription ?? (s.IsNormal ? "Bình thường" : "Bất thường"),
                s.DoctorStaffName ?? "-"
            }).ToList();
            AppendTable(sb, new[] { "Chuyên khoa", "ICD-10", "Mô tả lâm sàng", "Bác sĩ" }, rows);
        }

        AppendParagraph(sb, "III. KẾT QUẢ XÉT NGHIỆM (LIS)", bold: true, fontSizeHalfPt: 24);
        if (dto.LabResults.Count == 0)
        {
            AppendParagraph(sb, "(Không có chỉ định xét nghiệm)", false, 20);
        }
        else
        {
            var rows = dto.LabResults.Select(l => new[]
            {
                l.TestCode,
                l.TestName,
                $"{l.Value} {l.Unit}".Trim(),
                l.ReferenceRange,
                l.IsAbnormal ? "Bất thường" : "Bình thường"
            }).ToList();
            AppendTable(sb, new[] { "Mã XN", "Tên xét nghiệm", "Kết quả", "Trị số tham chiếu", "Đánh giá" }, rows);
        }

        AppendParagraph(sb, "IV. CHẨN ĐOÁN HÌNH ẢNH (PACS)", bold: true, fontSizeHalfPt: 24);
        if (dto.ImagingRecords.Count == 0)
        {
            AppendParagraph(sb, "(Không có chỉ định CĐHA)", false, 20);
        }
        else
        {
            var rows = dto.ImagingRecords.Select(r => new[]
            {
                r.ServiceName,
                r.Conclusion ?? "-",
                r.RadiologistDoctorName ?? "-",
                r.IsSigned ? "Đã ký" : "Chưa ký"
            }).ToList();
            AppendTable(sb, new[] { "Dịch vụ", "Kết luận", "Bác sĩ CĐHA", "Trạng thái" }, rows);
        }

        AppendParagraph(sb, "V. KẾT LUẬN & PHÂN LOẠI SỨC KHỎE", bold: true, fontSizeHalfPt: 24);
        var summary = dto.ExistingSummary;
        var gradeText = summary?.OverallGradeText ?? dto.Proposed.OverallGradeText;
        var conclusionText = summary?.ConclusionText ?? dto.Proposed.ConclusionText;
        var advice = summary?.Recommendations ?? dto.Proposed.Recommendations;
        AppendParagraph(sb, $"Phân loại sức khỏe: {gradeText} | Kết luận: {conclusionText}", bold: true, fontSizeHalfPt: 22);
        AppendParagraph(sb, $"Đề nghị / Lời khuyên: {advice}", false, 20);
        AppendParagraph(sb, $"Bác sĩ Kết luận: {summary?.LeadDoctorName ?? "(Chưa ký)"} ({summary?.LeadDoctorSignedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-"})", false, 20);
        AppendParagraph(sb, $"Đại diện Cơ sở Y tế: {summary?.FacilityRepresentativeName ?? "(Chưa đóng dấu)"} ({summary?.FacilitySignedAt?.ToString("dd/MM/yyyy HH:mm") ?? "-"})", false, 20);

        sb.Append("""<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1000" w:right="1000" w:bottom="1000" w:left="1000"/></w:sectPr>""");
        sb.Append("</w:body></w:document>");
        return sb.ToString();
    }

    private static void AppendParagraph(StringBuilder sb, string text, bool bold, int fontSizeHalfPt)
    {
        var escaped = SecurityElement.Escape(text) ?? string.Empty;
        sb.Append("<w:p><w:pPr><w:spacing w:after=\"120\"/></w:pPr><w:r><w:rPr>");
        if (bold) sb.Append("<w:b/>");
        sb.Append($"<w:sz w:val=\"{fontSizeHalfPt}\"/><w:szCs w:val=\"{fontSizeHalfPt}\"/>");
        sb.Append($"</w:rPr><w:t xml:space=\"preserve\">{escaped}</w:t></w:r></w:p>");
    }

    private static void AppendTable(StringBuilder sb, string[] headers, List<string[]> rows)
    {
        sb.Append("<w:tbl><w:tblPr><w:tblBorders><w:top w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"999999\"/><w:left w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"999999\"/><w:bottom w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"999999\"/><w:right w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"999999\"/><w:insideH w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"CCCCCC\"/><w:insideV w:val=\"single\" w:sz=\"4\" w:space=\"0\" w:color=\"CCCCCC\"/></w:tblBorders></w:tblPr>");
        AppendRow(sb, headers, bold: true);
        foreach (var row in rows)
        {
            AppendRow(sb, row, bold: false);
        }
        sb.Append("</w:tbl>");
    }

    private static void AppendRow(StringBuilder sb, string[] cells, bool bold)
    {
        sb.Append("<w:tr>");
        foreach (var cell in cells)
        {
            var escaped = SecurityElement.Escape(cell ?? string.Empty) ?? string.Empty;
            sb.Append("<w:tc><w:p><w:r>");
            if (bold) sb.Append("<w:rPr><w:b/></w:rPr>");
            sb.Append($"<w:t xml:space=\"preserve\">{escaped}</w:t></w:r></w:p></w:tc>");
        }
        sb.Append("</w:tr>");
    }

    private static void WriteZipText(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static string SanitizeFileName(string raw)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(raw.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return cleaned.Replace(' ', '_');
    }
}
