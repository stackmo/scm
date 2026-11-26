namespace SCM.Domain.Prescription.Exceptions;

/// <summary>
/// Ngoại lệ ném ra khi kê đơn cho bệnh nhi <= 72 tháng tuổi mà thiếu Cân nặng (kg) hoặc thông tin Người giám hộ.
/// Ràng buộc theo quy chế kê đơn ngoại trú của Bộ Y tế và tính năng chuẩn VSC Legacy (C-PED-01).
/// </summary>
public class PediatricPrescriptionViolationException : Exception
{
    public int AgeMonths { get; }
    public decimal? WeightKg { get; }
    public string? GuardianName { get; }

    public PediatricPrescriptionViolationException(int ageMonths, decimal? weightKg, string? guardianName, string message)
        : base(message)
    {
        AgeMonths = ageMonths;
        WeightKg = weightKg;
        GuardianName = guardianName;
    }
}
