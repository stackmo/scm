namespace SCM.Domain.Pharmacy.Exceptions;

public class InsufficientStockException : Exception
{
    public string DrugName { get; }
    public string LotNumber { get; }
    public decimal AvailableQuantity { get; }
    public decimal RequestedQuantity { get; }

    public InsufficientStockException(string drugName, string lotNumber, decimal available, decimal requested)
        : base($"Không đủ tồn kho cho thuốc '{drugName}' (Lô {lotNumber}): Yêu cầu {requested}, tồn kho khả dụng chỉ còn {available}.")
    {
        DrugName = drugName;
        LotNumber = lotNumber;
        AvailableQuantity = available;
        RequestedQuantity = requested;
    }
}

public class ExpiredDrugDispenseException : Exception
{
    public string DrugName { get; }
    public string LotNumber { get; }
    public DateTime ExpiryDate { get; }

    public ExpiredDrugDispenseException(string drugName, string lotNumber, DateTime expiryDate)
        : base($"Thuốc '{drugName}' (Lô {lotNumber}) đã hết hạn sử dụng vào ngày {expiryDate:dd/MM/yyyy}. Nghiêm cấm cấp phát!")
    {
        DrugName = drugName;
        LotNumber = lotNumber;
        ExpiryDate = expiryDate;
    }
}

public class PrescriptionNotSignedException : Exception
{
    public Guid PrescriptionId { get; }

    public PrescriptionNotSignedException(Guid prescriptionId)
        : base($"Đơn thuốc ID '{prescriptionId}' chưa được Bác sĩ ký số hợp lệ. Chưa đủ điều kiện cấp phát!")
    {
        PrescriptionId = prescriptionId;
    }
}

public class PrescriptionAlreadyDispensedException : Exception
{
    public Guid PrescriptionId { get; }

    public PrescriptionAlreadyDispensedException(Guid prescriptionId)
        : base($"Đơn thuốc ID '{prescriptionId}' đã được cấp phát trước đó. Không được cấp phát trùng!")
    {
        PrescriptionId = prescriptionId;
    }
}
