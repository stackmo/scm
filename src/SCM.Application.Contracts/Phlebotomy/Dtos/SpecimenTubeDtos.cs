using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Phlebotomy.Dtos;

public class SpecimenTubeDto
{
    public Guid Id { get; set; }
    public Guid BatchEmployeeMappingId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string TubeBarcode { get; set; } = string.Empty;
    public TubeType TubeType { get; set; }
    public string TubeTypeName => TubeType switch
    {
        TubeType.EDTA => "EDTA (Huyết học)",
        TubeType.Serum => "Serum (Sinh hóa/Miễn dịch)",
        TubeType.Heparin => "Heparin",
        TubeType.NaF => "NaF (Đường huyết)",
        TubeType.Urine => "Urine (Nước tiểu)",
        _ => TubeType.ToString()
    };
    public string CapColor { get; set; } = string.Empty;
    public string TargetTests { get; set; } = string.Empty;
    public TubeStatus Status { get; set; }
    public string StatusName => Status switch
    {
        TubeStatus.Prepared => "Đã in tem (Prepared)",
        TubeStatus.Collected => "Đã thu mẫu (Collected)",
        TubeStatus.Transporting => "Đang vận chuyển (Transporting)",
        TubeStatus.Received => "Lab đã nhận (Received)",
        TubeStatus.Rejected => "Từ chối mẫu (Rejected)",
        _ => Status.ToString()
    };
    public DateTime? CollectedAt { get; set; }
    public string CollectorId { get; set; } = string.Empty;
    public Guid? ManifestId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class GeneratedBarcodeDto
{
    public Guid TubeId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string TubeBarcode { get; set; } = string.Empty;
    public TubeType TubeType { get; set; }
    public string CapColor { get; set; } = string.Empty;
    public string TargetTests { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
}
