namespace SCM.Domain.Prescription;

public class PrescriptionItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PrescriptionId { get; set; }

    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; } // Mã Dược Quốc gia theo QĐ 7603/BYT (ví dụ: VD-12345-20)

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty; // Đường dùng: Uống, Tiêm, Bôi...
    public int UsageDays { get; set; } = 1;

    public decimal MorningDose { get; set; } = 0;
    public decimal NoonDose { get; set; } = 0;
    public decimal EveningDose { get; set; } = 0;
    public decimal NightDose { get; set; } = 0;
    public string? Instructions { get; set; }

    public PrescriptionItem() { }

    public PrescriptionItem(
        Guid prescriptionId,
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
        if (drugId == Guid.Empty)
            throw new ArgumentException("DrugId không được để trống.", nameof(drugId));
        if (string.IsNullOrWhiteSpace(drugName))
            throw new ArgumentException("DrugName không được để trống.", nameof(drugName));
        if (quantity <= 0)
            throw new ArgumentException("Số lượng thuốc phải lớn hơn 0.", nameof(quantity));

        PrescriptionId = prescriptionId;
        DrugId = drugId;
        DrugName = drugName.Trim();
        NationalDrugCode = nationalDrugCode?.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        Route = route.Trim();
        UsageDays = usageDays > 0 ? usageDays : 1;
        MorningDose = morningDose;
        NoonDose = noonDose;
        EveningDose = eveningDose;
        NightDose = nightDose;
        Instructions = instructions;
    }
}
