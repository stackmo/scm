namespace SCM.Domain.Pharmacy;

/// <summary>
/// Chi tiết dòng cấp phát thuốc theo số lô.
/// </summary>
public class MedicationDispenseItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MedicationDispenseId { get; set; }
    public MedicationDispense? MedicationDispense { get; set; }

    public Guid PrescriptionItemId { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;

    public Guid StockLotExpiryId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }

    public decimal QuantityDispensed { get; set; }
    public string Unit { get; set; } = "Viên";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
