using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData.Gateways;

/// <summary>
/// Cấu hình kết nối và xác thực với 2 Cổng Quốc Gia:
/// 1. Cổng Y Tế Cộng Đồng (Mẫu M3 191 chỉ số KSK Đoàn)
/// 2. Cổng Đơn Thuốc Quốc Gia (donthuocquocgia.vn)
/// </summary>
public class NationalGatewayConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FacilityCode { get; set; } = string.Empty; // Mã CSKCB 5 số của Bộ Y tế cấp (vd: 79012)
    public GatewayType GatewayType { get; set; } = GatewayType.CongYTeM3;
    public string BaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string? SignCertSerial { get; set; }
    public bool IsActive { get; set; } = true;

    public NationalGatewayConfig() { }

    public NationalGatewayConfig(
        string facilityCode,
        GatewayType gatewayType,
        string baseUrl,
        string clientId,
        string clientSecret,
        string? signCertSerial = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(facilityCode))
            throw new ArgumentException("Mã cơ sở KCB (FacilityCode) không được để trống.", nameof(facilityCode));

        FacilityCode = facilityCode;
        GatewayType = gatewayType;
        BaseUrl = baseUrl;
        ClientId = clientId;
        ClientSecret = clientSecret;
        SignCertSerial = signCertSerial;
        IsActive = isActive;
    }
}
