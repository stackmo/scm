using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Phlebotomy.Dtos;

public class ColdChainManifestDto
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
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
    public ManifestStatus Status { get; set; }
    public string StatusName => Status switch
    {
        ManifestStatus.InTransit => "Đang vận chuyển (In Transit)",
        ManifestStatus.Received => "Đã nghiệm thu (Received)",
        ManifestStatus.TemperatureBreach => "CẢNH BÁO: Vi phạm nhiệt độ (Breach Alert)",
        _ => Status.ToString()
    };
    public bool IsTemperatureBreach => Status == ManifestStatus.TemperatureBreach;
    public bool IsDegraded { get; set; }
    public bool HasDiscrepancy { get; set; }
    public int DiscrepancyCount { get; set; }
    public string DiscrepancyNote { get; set; } = string.Empty;
    public double? TransitDurationHours { get; set; }
    public string? BreachWarningMessage { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SpecimenTubeDto> SpecimenTubes { get; set; } = new();
}

public class CreateColdChainManifestDto
{
    public Guid BatchId { get; set; }
    public string ManifestCode { get; set; } = string.Empty;
    public string HandoverStaff { get; set; } = string.Empty;
    public string CourierStaff { get; set; } = string.Empty;
    public decimal DepartureTemperature { get; set; }
    public DateTime? DepartureTime { get; set; }
    public List<Guid> TubeIds { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class ReceiveColdChainManifestDto
{
    public string LabReceiverStaff { get; set; } = string.Empty;
    public decimal ArrivalTemperature { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public List<string> AcceptedBarcodes { get; set; } = new();
    public List<RejectedTubeInputDto> RejectedBarcodes { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class RejectedTubeInputDto
{
    public string Barcode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class TemperatureCheckResultDto
{
    public decimal Temperature { get; set; }
    public bool IsSafe { get; set; }
    public bool IsBreach { get; set; }
    public string Message { get; set; } = string.Empty;
}
