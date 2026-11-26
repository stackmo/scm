using SCM.Domain.Branches;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Prescription;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Pharmacy;

/// <summary>
/// Phiếu Cấp Phát Thuốc Ngoại Trú theo Đơn (Aggregate Root).
/// </summary>
public class MedicationDispense : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string DispenseCode { get; set; } = string.Empty;
    public Guid PrescriptionId { get; set; }
    public SCM.Domain.Prescription.Prescription? Prescription { get; set; }

    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;

    public Guid PharmacistId { get; set; }
    public string PharmacistName { get; set; } = string.Empty;
    public DateTime DispensedAt { get; set; } = DateTime.UtcNow;

    public DispenseStatus Status { get; set; } = DispenseStatus.Draft;
    public decimal TotalAmount { get; set; } = 0;
    public string? Notes { get; set; }

    public List<MedicationDispenseItem> Items { get; set; } = new();

    public MedicationDispense() { }

    public MedicationDispense(
        string dispenseCode,
        SCM.Domain.Prescription.Prescription prescription,
        Guid pharmacistId,
        string pharmacistName,
        Guid branchId,
        string branchCode,
        string? notes = null)
    {
        if (prescription == null)
            throw new ArgumentNullException(nameof(prescription));
        if (!prescription.IsSigned)
            throw new PrescriptionNotSignedException(prescription.Id);
        if (prescription.Status == PrescriptionStatus.Dispensed)
            throw new PrescriptionAlreadyDispensedException(prescription.Id);
        if (pharmacistId == Guid.Empty)
            throw new ArgumentException("PharmacistId không được để trống.", nameof(pharmacistId));

        DispenseCode = dispenseCode.Trim().ToUpperInvariant();
        PrescriptionId = prescription.Id;
        EncounterId = prescription.EncounterId;
        EncounterCode = prescription.EncounterCode;
        PatientId = prescription.PatientId;
        PatientName = prescription.PatientName;
        PharmacistId = pharmacistId;
        PharmacistName = pharmacistName.Trim();
        BranchId = branchId;
        BranchCode = branchCode;
        DispensedAt = DateTime.UtcNow;
        Status = DispenseStatus.Completed;
        Notes = notes;
    }

    public void AddItem(
        Guid prescriptionItemId,
        Guid drugId,
        string drugName,
        Guid stockLotExpiryId,
        string lotNumber,
        DateTime expiryDate,
        decimal quantity,
        string unit,
        decimal unitPrice)
    {
        var item = new MedicationDispenseItem
        {
            Id = Guid.NewGuid(),
            MedicationDispenseId = Id,
            PrescriptionItemId = prescriptionItemId,
            DrugId = drugId,
            DrugName = drugName,
            StockLotExpiryId = stockLotExpiryId,
            LotNumber = lotNumber,
            ExpiryDate = expiryDate,
            QuantityDispensed = quantity,
            Unit = unit,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice
        };

        Items.Add(item);
        TotalAmount += item.TotalPrice;
    }
}
