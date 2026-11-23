using SCM.Domain.MasterData;

namespace SCM.Domain.MasterData.Gateways;

/// <summary>
/// Ánh xạ chỉ số xét nghiệm LIS với 35 chỉ số cận lâm sàng Mẫu M3
/// Tự động chuyển đổi kết quả máy xét nghiệm sang JSON keys chuẩn của Cổng Y Tế
/// </summary>
public class ServiceM3Mapping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceId { get; set; }
    public MedicalService? Service { get; set; }
    public string LabParameterCode { get; set; } = string.Empty; // Mã LIS (vd: CBC_RBC, BIO_GLU)
    public string M3JsonKey { get; set; } = string.Empty; // Key JSON M3 (vd: xnm_slhc, sinhhoa_glucose)
    public string? ConversionFormula { get; set; } // Công thức quy đổi (vd: x * 10, x / 18.0, 1:1)
    public string StandardUnit { get; set; } = string.Empty; // Đơn vị chuẩn M3 (vd: T/L, mmol/L)
    public bool IsActive { get; set; } = true;

    public ServiceM3Mapping() { }

    public ServiceM3Mapping(
        Guid serviceId,
        string labParameterCode,
        string m3JsonKey,
        string standardUnit,
        string? conversionFormula = null,
        bool isActive = true)
    {
        if (serviceId == Guid.Empty)
            throw new ArgumentException("ServiceId không được để trống.", nameof(serviceId));
        if (string.IsNullOrWhiteSpace(labParameterCode))
            throw new ArgumentException("Mã chỉ số xét nghiệm LIS không được để trống.", nameof(labParameterCode));
        if (string.IsNullOrWhiteSpace(m3JsonKey))
            throw new ArgumentException("Key JSON Mẫu M3 không được để trống.", nameof(m3JsonKey));

        ServiceId = serviceId;
        LabParameterCode = labParameterCode;
        M3JsonKey = m3JsonKey;
        StandardUnit = standardUnit;
        ConversionFormula = conversionFormula;
        IsActive = isActive;
    }
}
