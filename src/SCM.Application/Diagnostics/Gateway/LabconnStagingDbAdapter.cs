using System.Text.Json;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Diagnostics.Gateway;

public class LabconnOrderRecord
{
    public string OrderCode { get; set; } = string.Empty;
    public string EncounterCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string TestCodeHIS { get; set; } = string.Empty;
    public string? TubeBarcode { get; set; }
}

public class LabconnUploadedResultRow
{
    public string TubeBarcode { get; set; } = string.Empty;
    public string TestCodeHIS { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public string? AbnormalFlag { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Abstraction gọi Stored Procedures HIS_LIS (sp_HIS_upd_Tbl_Order, sp_HIS_del_Tbl_Order, sp_HIS_get_Result / Tbl_Result_Upload).
/// Cho phép mock khi unit test và gắn SQL Server connector thực khi kết nối Labconn.
/// </summary>
public interface ILabconnStagingSqlClient
{
    Task ExecuteUpdOrderAsync(string connectionString, LabconnOrderRecord order, CancellationToken ct = default);
    Task ExecuteDelOrderAsync(string connectionString, string orderCode, string testCodeHis, CancellationToken ct = default);
    Task<List<LabconnUploadedResultRow>> QueryResultUploadAsync(string connectionString, DateTime sinceUtc, CancellationToken ct = default);
}

public class DefaultLabconnStagingSqlClient : ILabconnStagingSqlClient
{
    public Task ExecuteUpdOrderAsync(string connectionString, LabconnOrderRecord order, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ExecuteDelOrderAsync(string connectionString, string orderCode, string testCodeHis, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<LabconnUploadedResultRow>> QueryResultUploadAsync(string connectionString, DateTime sinceUtc, CancellationToken ct = default)
        => Task.FromResult(new List<LabconnUploadedResultRow>());
}

public class LabconnStagingDbAdapter : ILisProviderAdapter
{
    private readonly ILabconnStagingSqlClient _sqlClient;

    public LabconnStagingDbAdapter(ILabconnStagingSqlClient? sqlClient = null)
    {
        _sqlClient = sqlClient ?? new DefaultLabconnStagingSqlClient();
    }

    public LisProviderType ProviderType => LisProviderType.LabconnStagingDb;

    public async Task<LisDispatchResult> PushOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
    {
        var connStr = ExtractConnectionString(partner.ConnectionConfigEncrypted);
        await _sqlClient.ExecuteUpdOrderAsync(connStr, new LabconnOrderRecord
        {
            OrderCode = payload.OrderCode,
            EncounterCode = payload.EncounterCode,
            PatientName = payload.PatientName,
            TestCodeHIS = payload.PartnerTestCode,
            TubeBarcode = payload.TubeBarcode
        }, ct);

        return new LisDispatchResult
        {
            Success = true,
            ExternalReferenceId = $"LABCONN:{payload.OrderCode}:{payload.PartnerTestCode}"
        };
    }

    public async Task<LisDispatchResult> CancelOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
    {
        var connStr = ExtractConnectionString(partner.ConnectionConfigEncrypted);
        await _sqlClient.ExecuteDelOrderAsync(connStr, payload.OrderCode, payload.PartnerTestCode, ct);

        return new LisDispatchResult
        {
            Success = true,
            ExternalReferenceId = $"LABCONN-DEL:{payload.OrderCode}:{payload.PartnerTestCode}"
        };
    }

    public async Task<List<LisInboundResultDto>> PullResultsAsync(LisPartnerConfig partner, DateTime sinceUtc, CancellationToken ct = default)
    {
        var connStr = ExtractConnectionString(partner.ConnectionConfigEncrypted);
        var rows = await _sqlClient.QueryResultUploadAsync(connStr, sinceUtc, ct);
        return rows.Select(r => new LisInboundResultDto
        {
            TubeBarcode = r.TubeBarcode,
            PartnerTestCode = r.TestCodeHIS,
            TestName = r.TestName,
            NumericResult = r.NumericValue,
            StringResult = r.TextValue,
            Unit = r.Unit,
            ReferenceRange = r.ReferenceRange,
            AbnormalFlag = r.AbnormalFlag,
            PayloadHash = LisAstmParser.ComputePayloadHash($"LABCONN:{r.TubeBarcode}:{r.TestCodeHIS}:{r.NumericValue}:{r.TextValue}")
        }).ToList();
    }

    private static string ExtractConnectionString(string configJsonOrStr)
    {
        if (string.IsNullOrWhiteSpace(configJsonOrStr)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(configJsonOrStr);
            if (doc.RootElement.TryGetProperty("connectionString", out var cs))
            {
                return cs.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Raw connection string
        }
        return configJsonOrStr;
    }
}
