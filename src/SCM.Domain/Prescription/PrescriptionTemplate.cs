using SCM.Domain.Branches;

namespace SCM.Domain.Prescription;

/// <summary>
/// Đơn thuốc mẫu (Toa mẫu) dùng để bác sĩ nạp nhanh theo mã bệnh ICD-10.
/// Kế thừa và nâng cấp từ tính năng VSC frmKeToaThuoc.
/// </summary>
public class PrescriptionTemplate : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Guid? DoctorId { get; set; } // Null = Toa mẫu chung; Có ID = Toa mẫu riêng của Bác sĩ
    public string? DoctorName { get; set; }
    public bool IsActive { get; set; } = true;

    public List<PrescriptionTemplateItem> Items { get; set; } = new();

    public PrescriptionTemplate() { }

    public PrescriptionTemplate(
        string templateCode,
        string templateName,
        string diagnosisIcd10,
        string diagnosisText,
        Guid? doctorId = null,
        string? doctorName = null,
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
            throw new ArgumentException("Mã toa mẫu không được để trống.", nameof(templateCode));
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Tên toa mẫu không được để trống.", nameof(templateName));

        TemplateCode = templateCode.Trim().ToUpperInvariant();
        TemplateName = templateName.Trim();
        DiagnosisIcd10 = diagnosisIcd10.Trim();
        DiagnosisText = diagnosisText.Trim();
        DoctorId = doctorId;
        DoctorName = doctorName;

        if (branchId.HasValue) BranchId = branchId.Value;
        if (!string.IsNullOrEmpty(branchCode)) BranchCode = branchCode;
    }

    public void AddItem(
        Guid drugId,
        string drugName,
        string? nationalDrugCode,
        decimal quantity,
        string unit,
        string route,
        int usageDays,
        decimal morningDose = 0,
        decimal noonDose = 0,
        decimal eveningDose = 0,
        decimal nightDose = 0,
        string? instructions = null)
    {
        var item = new PrescriptionTemplateItem
        {
            PrescriptionTemplateId = Id,
            DrugId = drugId,
            DrugName = drugName,
            NationalDrugCode = nationalDrugCode,
            Quantity = quantity,
            Unit = unit,
            Route = route,
            UsageDays = usageDays,
            MorningDose = morningDose,
            NoonDose = noonDose,
            EveningDose = eveningDose,
            NightDose = nightDose,
            Instructions = instructions
        };
        Items.Add(item);
    }
}

public class PrescriptionTemplateItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PrescriptionTemplateId { get; set; }

    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int UsageDays { get; set; } = 1;

    public decimal MorningDose { get; set; } = 0;
    public decimal NoonDose { get; set; } = 0;
    public decimal EveningDose { get; set; } = 0;
    public decimal NightDose { get; set; } = 0;

    public string? Instructions { get; set; }
}
