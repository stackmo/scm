namespace SCM.Application.Contracts.Diagnostics;

public class PanicAlertEventDto
{
    public Guid LabResultId { get; set; }
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string? TubeBarcode { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string ResultDisplay { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid? OrderingDoctorId { get; set; }
    public DateTime AlertedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IDiagnosticsRealtimeNotifier
{
    Task NotifyPanicAlertAsync(PanicAlertEventDto alert);
    Task NotifyWorklistUpdatedAsync(string branchCode, string workstationType, Guid? encounterId = null);
}

public class NullDiagnosticsRealtimeNotifier : IDiagnosticsRealtimeNotifier
{
    public Task NotifyPanicAlertAsync(PanicAlertEventDto alert) => Task.CompletedTask;
    public Task NotifyWorklistUpdatedAsync(string branchCode, string workstationType, Guid? encounterId = null) => Task.CompletedTask;
}
