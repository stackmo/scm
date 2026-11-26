using SCM.Domain.Branches;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.Phlebotomy;

public class ColdChainManifest : IBranchScopedEntity
{
    public const decimal MinSafeTemperature = 2.0m;
    public const decimal MaxSafeTemperature = 8.0m;
    public const double MaxSafeTransitHours = 4.0; // C-3.2: Quá 4 giờ vận chuyển mẫu coi là suy giảm chất lượng

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;
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
    public bool IsDegraded { get; set; } = false; // C-3.2 Sample Degraded Flag
    public bool HasDiscrepancy { get; set; } = false; // C-3.5 Dual Signoff Discrepancy
    public int DiscrepancyCount { get; set; } = 0;
    public string DiscrepancyNote { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Aliases matching PROMPT-WAVE-03-PHLEBOTOMY
    public int SentTubeCount => TotalTubesHandedOver;
    public int ReceivedTubeCount => TotalTubesAccepted + TotalTubesRejected;
    public DateTime DispatchTime => DepartureTime;
    public DateTime? DeliveryTime => ArrivalTime;
    public decimal StartTempCelsius => DepartureTemperature;
    public decimal? ArrivalTempCelsius => ArrivalTemperature;

    public List<SpecimenTube> SpecimenTubes { get; set; } = new();

    public ColdChainManifest() { }

    public ColdChainManifest(
        Guid batchId,
        string manifestCode,
        string handoverStaff,
        string courierStaff,
        decimal departureTemperature,
        DateTime? departureTime = null,
        string notes = "",
        Guid? branchId = null,
        string? branchCode = null)
    {
        if (batchId == Guid.Empty)
            throw new ArgumentException("BatchId không được để trống.", nameof(batchId));
        if (string.IsNullOrWhiteSpace(manifestCode))
            throw new ArgumentException("Mã biên bản không được để trống.", nameof(manifestCode));
        if (string.IsNullOrWhiteSpace(handoverStaff))
            throw new ArgumentException("Người bàn giao không được để trống.", nameof(handoverStaff));
        if (string.IsNullOrWhiteSpace(courierStaff))
            throw new ArgumentException("Người vận chuyển không được để trống.", nameof(courierStaff));

        BranchId = branchId ?? Guid.Empty;
        BranchCode = branchCode ?? string.Empty;
        BatchId = batchId;
        ManifestCode = manifestCode.Trim().ToUpperInvariant();
        HandoverStaff = handoverStaff.Trim();
        CourierStaff = courierStaff.Trim();
        DepartureTemperature = departureTemperature;
        DepartureTime = departureTime ?? DateTime.UtcNow;
        Notes = notes.Trim();
        CreatedAt = DateTime.UtcNow;

        ValidateQuality();
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

    public bool HasTransitTimeExceeded()
    {
        if (!ArrivalTime.HasValue) return false;
        var duration = (ArrivalTime.Value - DepartureTime).TotalHours;
        return duration > MaxSafeTransitHours;
    }

    /// <summary>
    /// C-3.2 Cold-Chain Temperature & Transit Time Quality Guard
    /// </summary>
    public bool ValidateQuality()
    {
        bool breach = HasTemperatureBreach();
        bool timeExceeded = HasTransitTimeExceeded();

        if (breach || timeExceeded)
        {
            IsDegraded = true;
            Status = ManifestStatus.TemperatureBreach;
            return false;
        }

        IsDegraded = false;
        if (Status != ManifestStatus.Received)
        {
            Status = ManifestStatus.InTransit;
        }
        return true;
    }

    /// <summary>
    /// C-3.5 Dual Signoff Handover Manifest: Nghiệm thu tại Lab và đối soát số lượng ống
    /// </summary>
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

        // C-3.5: Kiểm tra lệch số lượng bàn giao vs thực nhận
        int totalReceived = acceptedCount + rejectedCount;
        if (TotalTubesHandedOver > 0 && totalReceived != TotalTubesHandedOver)
        {
            HasDiscrepancy = true;
            DiscrepancyCount = Math.Abs(TotalTubesHandedOver - totalReceived);
            DiscrepancyNote = $"CẢNH BÁO THẤT LẠC MẪU: Đội lưu động bàn giao {TotalTubesHandedOver} ống, phòng Lab nhận {totalReceived} ống (Lệch {DiscrepancyCount} ống)!";
        }
        else
        {
            HasDiscrepancy = false;
            DiscrepancyCount = 0;
            DiscrepancyNote = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) 
                ? additionalNotes.Trim() 
                : $"{Notes}; {additionalNotes.Trim()}";
        }

        if (HasDiscrepancy)
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? DiscrepancyNote
                : $"{Notes}; {DiscrepancyNote}";
        }

        ValidateQuality();

        if (!IsDegraded)
        {
            Status = ManifestStatus.Received;
        }
    }
}
