using System.ComponentModel.DataAnnotations;
using SCM.Domain.Branches;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Exceptions;

namespace SCM.Domain.Billing;

/// <summary>
/// Quản lý dải Ký hiệu / Mẫu số / Số Hóa Đơn Điện Tử theo từng Chi nhánh (TT78/2021/TT-BTC).
/// Kế thừa trực tiếp bảng QuanLySoHoaDon trong VSC cũ.
/// </summary>
public class InvoiceSeries : IBranchScopedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BranchId { get; set; } = Guid.Empty;
    public string BranchCode { get; set; } = string.Empty;

    public string TaxCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "1"; // Mẫu số: 1, 2...
    public string SeriesCode { get; set; } = "C26TSM"; // Ký hiệu: C26TSM
    public int FromNumber { get; set; } = 1;
    public int ToNumber { get; set; } = 10000;
    public int CurrentNumber { get; set; } = 0;

    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public InvoiceSeries() { }

    public InvoiceSeries(
        Guid branchId,
        string branchCode,
        string taxCode,
        string templateCode,
        string seriesCode,
        int fromNumber = 1,
        int toNumber = 10000,
        bool isDefault = true)
    {
        if (string.IsNullOrWhiteSpace(seriesCode))
            throw new ArgumentException("Ký hiệu hóa đơn (SeriesCode) không được để trống.", nameof(seriesCode));
        if (fromNumber < 1 || toNumber < fromNumber)
            throw new ArgumentException("Dải số hóa đơn không hợp lệ (ToNumber phải >= FromNumber >= 1).");

        BranchId = branchId;
        BranchCode = (branchCode ?? string.Empty).Trim().ToUpperInvariant();
        TaxCode = (taxCode ?? string.Empty).Trim();
        TemplateCode = string.IsNullOrWhiteSpace(templateCode) ? "1" : templateCode.Trim();
        SeriesCode = seriesCode.Trim().ToUpperInvariant();
        FromNumber = fromNumber;
        ToNumber = toNumber;
        CurrentNumber = fromNumber - 1;
        IsDefault = isDefault;
        IsActive = true;
    }

    public int AllocateNextNumber()
    {
        if (!IsActive)
            throw new BusinessException(SCMErrorCodes.InvoiceSeriesExhausted, $"Dải hóa đơn [{TemplateCode}/{SeriesCode}] đã ngừng sử dụng.");

        int next = CurrentNumber < FromNumber ? FromNumber : CurrentNumber + 1;
        if (next > ToNumber)
        {
            throw new BusinessException(
                SCMErrorCodes.InvoiceSeriesExhausted,
                $"Dải hóa đơn [{TemplateCode}/{SeriesCode}] đã dùng hết ({ToNumber}/{ToNumber}). Vui lòng khai báo dải số mới.");
        }

        CurrentNumber = next;
        Version = Guid.NewGuid();
        return next;
    }

    public string FormatInvoiceNumber(int number) => number.ToString("D7");
}
