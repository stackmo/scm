using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Phlebotomy.Dtos;

public class PhlebotomyBatchDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime SamplingDate { get; set; }
    public string SamplingLocation { get; set; } = string.Empty;
    public int PlannedCount { get; set; }
    public int ActualCollectedCount { get; set; }
    public BatchStatus Status { get; set; }
    public string StatusName => Status switch
    {
        BatchStatus.Draft => "Khởi tạo (Draft)",
        BatchStatus.Scheduled => "Đã lên lịch (Scheduled)",
        BatchStatus.InProgress => "Đang lấy mẫu (In Progress)",
        BatchStatus.Completed => "Hoàn thành (Completed)",
        BatchStatus.Cancelled => "Đã hủy (Cancelled)",
        _ => Status.ToString()
    };
    public string Notes { get; set; } = string.Empty;
    public int TotalTubesPrepared { get; set; }
    public int TotalTubesCollected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePhlebotomyBatchDto
{
    public Guid CompanyId { get; set; }
    public Guid ContractId { get; set; }
    public Guid PackageId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public DateTime SamplingDate { get; set; } = DateTime.UtcNow.Date;
    public string SamplingLocation { get; set; } = string.Empty;
    public int PlannedCount { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class AssignEmployeesToBatchDto
{
    public List<Guid> EmployeeIds { get; set; } = new();
    public int ShiftOrder { get; set; } = 1;
}

public class CheckinEmployeeDto
{
    public string CollectorStaff { get; set; } = string.Empty;
    public string? SamplingNotes { get; set; }
    public List<Guid>? CollectedTubeIds { get; set; }
}

public class CheckinEmployeeResultDto
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public bool IsCollected { get; set; }
    public DateTime CheckinTime { get; set; }
    public int CollectedTubesCount { get; set; }
}

public class BatchEmployeeDetailDto
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Department { get; set; } = string.Empty;
    public int ShiftOrder { get; set; }
    public DateTime? CheckinTime { get; set; }
    public bool IsCollected { get; set; }
    public string CollectorStaff { get; set; } = string.Empty;
    public string SamplingNotes { get; set; } = string.Empty;
    public List<SpecimenTubeDto> SpecimenTubes { get; set; } = new();
}

public class PreExamBloodResultGateDto
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public bool IsEligibleForRoute1FastTrack { get; set; }
    public int TotalBloodTubes { get; set; }
    public int CompletedBloodTubes { get; set; }
    public int PendingBloodTubes { get; set; }
    public string Message { get; set; } = string.Empty;
}
