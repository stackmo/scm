namespace SCM.Application.Contracts.Billing.Dtos;

public class CreateReceiptDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int PaymentMethod { get; set; } // 1: Cash, 2: VietQR, 3: POS_Card, 4: B2B_CorporateDebt, 5: BHTM_Guarantee, 6: BhytInsurance
    public int ReceiptType { get; set; } = 1; // 1: DirectFee, 2: AdvanceDeposit, 3: FinalSettlement
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
}

public class RefundRequestDto
{
    public Guid ReceiptId { get; set; }
    public Guid AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public bool IsAdminRole { get; set; }
    public decimal RefundAmount { get; set; }
    public string RefundReason { get; set; } = string.Empty;
}

public class ReceiptDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public int ReceiptType { get; set; } = 1;
    public decimal TotalAmount { get; set; }
    public int PaymentMethod { get; set; }
    public int Status { get; set; }
    public string? VietQrPayload { get; set; }
    public string? VietQrTransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CashierShiftId { get; set; }
    public Guid? ElectronicInvoiceId { get; set; }
    public string? ElectronicInvoiceNumber { get; set; }
    public decimal DepositAppliedAmount { get; set; }
    public decimal RemainingDepositBalance { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
}

public class EncounterBillingSummaryDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal DepositAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal NetPayableAmount => Math.Max(0m, PendingAmount - DepositAmount);
    public decimal ExcessRefundAmount => Math.Max(0m, DepositAmount - PendingAmount);
}

public class SettleDepositRequestDto
{
    public Guid EncounterId { get; set; }
    public Guid CashierId { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public int PaymentMethod { get; set; } = 1;
    public bool RefundExcess { get; set; } = true;
    public List<SelectiveReceiptItemRequestDto>? SelectedItems { get; set; }
}

public class SettleDepositResultDto
{
    public Guid EncounterId { get; set; }
    public string EncounterCode { get; set; } = string.Empty;
    public decimal InitialDeposit { get; set; }
    public decimal TotalCharges { get; set; }
    public decimal DepositDeducted { get; set; }
    public decimal AdditionalAmountDue { get; set; }
    public decimal ExcessRefunded { get; set; }
    public decimal RemainingDeposit { get; set; }
    public ReceiptDto? SettlementReceipt { get; set; }
}

public class CalculateB2bSettlementDto
{
    public Guid CompanyContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int TotalContractEmployees { get; set; }
    public int ActualCompletedEmployees { get; set; }
    public decimal UnitPricePerEmployee { get; set; }
    public decimal DiscountPercentage { get; set; }
}

public class CompanySettlementDto
{
    public Guid Id { get; set; }
    public Guid CompanyContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int TotalContractEmployees { get; set; }
    public int ActualCompletedEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public decimal UnitPricePerEmployee { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public int Status { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? InvoiceLookupCode { get; set; }
    public int CompletedInPackageItemsCount { get; set; }
    public int ExtraOutOfPackageItemsCount { get; set; }
    public decimal ExtraOrdersAmount { get; set; }
}

public class BhytXmlItemInput
{
    public string MaDichVu { get; set; } = string.Empty;
    public string TenDichVu { get; set; } = string.Empty;
    public int NhomChiPhi { get; set; } // 1..18
    public decimal DonGia { get; set; }
    public decimal SoLuong { get; set; } = 1;
    public decimal ThanhTien => DonGia * SoLuong;
}

public class BhytXml130ExportResultDto
{
    public string Xml1TongHop { get; set; } = string.Empty;
    public string Xml2Thuoc { get; set; } = string.Empty;
    public string Xml3Vtyt { get; set; } = string.Empty;
    public string Xml4Dvkt { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public bool IsBalanced { get; set; }
}
