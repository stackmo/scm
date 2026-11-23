using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Phlebotomy;

public class ColdChainManifest
{
    public const decimal MinSafeTemperature = 2.0m;
    public const decimal MaxSafeTemperature = 8.0m;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public PhlebotomyBatch? Batch { get; set; }
    public string ManifestCode { get; set; } = string.Empty;
    public string HandoverStaff { get; set; } = string.Empty;
    public string CourierStaff { get; set; } = string.Empty;
    public string LabReceiverStaff { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public decimal DepartureTemperature { get; set; }
    public decimal? ArrivalTemperature { get; set; }
    public int TotalTubesHandedOver { get; set; }
    public int TotalTubesAccepted { get; set; }
    public int TotalTubesRejected { get; set; }
    public ManifestStatus Status { get; set; } = ManifestStatus.InTransit;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SpecimenTube> SpecimenTubes { get; set; } = new();

    public ColdChainManifest() { }

    public ColdChainManifest(
        Guid batchId,
        string manifestCode,
        string handoverStaff,
        string courierStaff,
        decimal departureTemperature,
        DateTime? departureTime = null,
        string notes = "")
    {
        if (batchId == Guid.Empty)
            throw new ArgumentException("BatchId không được để trống.", nameof(batchId));
        if (string.IsNullOrWhiteSpace(manifestCode))
            throw new ArgumentException("Mã biên bản không được để trống.", nameof(manifestCode));
        if (string.IsNullOrWhiteSpace(handoverStaff))
            throw new ArgumentException("Người bàn giao không được để trống.", nameof(handoverStaff));
        if (string.IsNullOrWhiteSpace(courierStaff))
            throw new ArgumentException("Người vận chuyển không được để trống.", nameof(courierStaff));

        BatchId = batchId;
        ManifestCode = manifestCode.Trim().ToUpperInvariant();
        HandoverStaff = handoverStaff.Trim();
        CourierStaff = courierStaff.Trim();
        DepartureTemperature = departureTemperature;
        DepartureTime = departureTime ?? DateTime.UtcNow;
        Notes = notes.Trim();
        CreatedAt = DateTime.UtcNow;

        // If departure temperature is already outside safe range, flag immediately
        if (!IsSafeTemperature(departureTemperature))
        {
            Status = ManifestStatus.TemperatureBreach;
        }
        else
        {
            Status = ManifestStatus.InTransit;
        }
    }

    public static bool IsSafeTemperature(decimal temperature)
    {
        return temperature >= MinSafeTemperature && temperature <= MaxSafeTemperature;
    }

    public bool HasTemperatureBreach()
    {
        if (!IsSafeTemperature(DepartureTemperature))
            return true;

        if (ArrivalTemperature.HasValue && !IsSafeTemperature(ArrivalTemperature.Value))
            return true;

        return false;
    }

    public void Receive(
        string labReceiverStaff,
        decimal arrivalTemperature,
        DateTime? arrivalTime = null,
        int acceptedCount = 0,
        int rejectedCount = 0,
        string? additionalNotes = null)
    {
        if (string.IsNullOrWhiteSpace(labReceiverStaff))
            throw new ArgumentException("Người nhận mẫu Lab không được để trống.", nameof(labReceiverStaff));

        LabReceiverStaff = labReceiverStaff.Trim();
        ArrivalTemperature = arrivalTemperature;
        ArrivalTime = arrivalTime ?? DateTime.UtcNow;
        TotalTubesAccepted = acceptedCount;
        TotalTubesRejected = rejectedCount;

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) 
                ? additionalNotes.Trim() 
                : $"{Notes}; {additionalNotes.Trim()}";
        }

        if (HasTemperatureBreach())
        {
            Status = ManifestStatus.TemperatureBreach;
        }
        else
        {
            Status = ManifestStatus.Received;
        }
    }
}
