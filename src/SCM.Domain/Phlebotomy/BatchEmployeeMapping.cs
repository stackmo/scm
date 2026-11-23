using SCM.Domain.B2B;

namespace SCM.Domain.Phlebotomy;

public class BatchEmployeeMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BatchId { get; set; }
    public PhlebotomyBatch? Batch { get; set; }
    public Guid EmployeeId { get; set; }
    public CompanyEmployee? Employee { get; set; }
    public int ShiftOrder { get; set; } = 1;
    public DateTime? CheckinTime { get; set; }
    public bool IsCollected { get; set; }
    public string CollectorStaff { get; set; } = string.Empty;
    public string SamplingNotes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<SpecimenTube> SpecimenTubes { get; set; } = new();

    public BatchEmployeeMapping() { }

    public BatchEmployeeMapping(
        Guid batchId,
        Guid employeeId,
        int shiftOrder = 1,
        string samplingNotes = "")
    {
        if (batchId == Guid.Empty)
            throw new ArgumentException("BatchId không được để trống.", nameof(batchId));
        if (employeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId không được để trống.", nameof(employeeId));

        BatchId = batchId;
        EmployeeId = employeeId;
        ShiftOrder = shiftOrder;
        IsCollected = false;
        SamplingNotes = samplingNotes.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Checkin(DateTime? time = null)
    {
        CheckinTime = time ?? DateTime.UtcNow;
    }

    public void MarkCollected(string collectorStaff, string? notes = null)
    {
        IsCollected = true;
        CollectorStaff = collectorStaff.Trim();
        if (!string.IsNullOrWhiteSpace(notes))
        {
            SamplingNotes = notes.Trim();
        }
    }
}
