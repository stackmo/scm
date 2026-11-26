using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SCM.Domain.Diagnostics.Gateway;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Diagnostics.Gateway;

public class PartnerRestWebhookAdapter : ILisProviderAdapter
{
    public LisProviderType ProviderType => LisProviderType.PartnerRestWebhook;

    public Task<LisDispatchResult> PushOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
    {
        return Task.FromResult(new LisDispatchResult
        {
            Success = true,
            ExternalReferenceId = $"REST:{partner.PartnerCode}:{payload.MedicalOrderItemId}"
        });
    }

    public Task<LisDispatchResult> CancelOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
    {
        return Task.FromResult(new LisDispatchResult
        {
            Success = true,
            ExternalReferenceId = $"REST-CANCEL:{partner.PartnerCode}:{payload.MedicalOrderItemId}"
        });
    }

    public Task<List<LisInboundResultDto>> PullResultsAsync(LisPartnerConfig partner, DateTime sinceUtc, CancellationToken ct = default)
        => Task.FromResult(new List<LisInboundResultDto>());

    public static string ExtractWebhookSecret(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("webhookSecret", out var prop))
                return prop.GetString() ?? string.Empty;
        }
        catch
        {
            // Fallback raw string
        }
        return configJson;
    }

    public static string ComputeHmacSha256(string secret, string rawBody)
    {
        if (string.IsNullOrEmpty(secret)) return string.Empty;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody ?? string.Empty));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifyHmacSignature(string secret, string rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        var expected = ComputeHmacSha256(secret, rawBody);
        var provided = signatureHeader.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            provided = provided["sha256=".Length..];
        provided = provided.ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

public class DirectDeviceBridgeAdapter : ILisProviderAdapter
{
    public LisProviderType ProviderType => LisProviderType.DirectDeviceBridge;

    public Task<LisDispatchResult> PushOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
        => Task.FromResult(new LisDispatchResult { Success = true, ExternalReferenceId = $"ASTM-MLLP:{payload.OrderCode}" });

    public Task<LisDispatchResult> CancelOrderAsync(LisPartnerConfig partner, LisOrderDispatchPayload payload, CancellationToken ct = default)
        => Task.FromResult(new LisDispatchResult { Success = true, ExternalReferenceId = $"ASTM-CANCEL:{payload.OrderCode}" });

    public Task<List<LisInboundResultDto>> PullResultsAsync(LisPartnerConfig partner, DateTime sinceUtc, CancellationToken ct = default)
        => Task.FromResult(new List<LisInboundResultDto>());
}
