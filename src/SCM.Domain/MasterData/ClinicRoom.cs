using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public bool IsClinical { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    public List<ClinicRoom> Rooms { get; set; } = new();

    public Department() { }

    public Department(string code, string name, bool isClinical = true, int sortOrder = 0, int? legacyId = null)
    {
        DepartmentCode = code;
        DepartmentName = name;
        IsClinical = isClinical;
        SortOrder = sortOrder;
        LegacyId = legacyId;
        IsActive = true;
    }
}

public class ClinicRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public RoomType RoomType { get; set; } = RoomType.Consultation;
    public Guid? DefaultDoctorId { get; set; }
    public decimal StandardExamFee { get; set; } = 0;
    public int CurrentCapacity { get; set; } = 10;
    public int MaxQueueLimit { get; set; } = 20;
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    public ClinicRoom() { }

    public ClinicRoom(
        string roomCode,
        string roomName,
        Guid departmentId,
        RoomType roomType = RoomType.Consultation,
        decimal standardExamFee = 0,
        int currentCapacity = 10,
        int maxQueueLimit = 20,
        int? legacyId = null)
    {
        RoomCode = roomCode;
        RoomName = roomName;
        DepartmentId = departmentId;
        RoomType = roomType;
        StandardExamFee = standardExamFee;
        CurrentCapacity = currentCapacity;
        MaxQueueLimit = maxQueueLimit;
        LegacyId = legacyId;
        IsActive = true;
    }
}
