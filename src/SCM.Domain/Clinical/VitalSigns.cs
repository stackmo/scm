namespace SCM.Domain.Clinical;

/// <summary>
/// Sinh hiệu bắt buộc trước khi khám chuyên khoa:
/// Chiều cao, cân nặng, huyết áp tâm thu/tâm trương, mạch, nhịp thở, nhiệt độ, SpO2.
/// Tự động tính chỉ số BMI = Cân nặng (kg) / (Chiều cao (m))^2.
/// </summary>
public class VitalSigns
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicalExamId { get; set; }
    public ClinicalExamination? ClinicalExam { get; set; }

    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public double Bmi { get; set; }

    public int SystolicBp { get; set; }   // Huyết áp tâm thu (mmHg)
    public int DiastolicBp { get; set; }  // Huyết áp tâm trương (mmHg)
    public int Pulse { get; set; }        // Mạch (lần/phút)
    public double? Temperature { get; set; } // Nhiệt độ (°C)
    public int? RespiratoryRate { get; set; } // Nhịp thở (lần/phút)
    public double? SpO2 { get; set; }     // Độ bão hòa oxy máu (%)

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public Guid? RecordedByStaffId { get; set; }
    public string Notes { get; set; } = string.Empty;

    public VitalSigns() { }

    public VitalSigns(
        Guid clinicalExamId,
        double heightCm,
        double weightKg,
        int systolicBp,
        int diastolicBp,
        int pulse,
        double? temperature = null,
        int? respiratoryRate = null,
        double? spO2 = null,
        Guid? recordedByStaffId = null,
        string notes = "")
    {
        if (clinicalExamId == Guid.Empty)
            throw new ArgumentException("ClinicalExamId không được để trống.", nameof(clinicalExamId));
        if (heightCm <= 0 || heightCm > 300)
            throw new ArgumentException("Chiều cao không hợp lệ (phải từ 1cm đến 300cm).", nameof(heightCm));
        if (weightKg <= 0 || weightKg > 500)
            throw new ArgumentException("Cân nặng không hợp lệ (phải từ 1kg đến 500kg).", nameof(weightKg));
        if (systolicBp <= 0 || systolicBp > 300)
            throw new ArgumentException("Huyết áp tâm thu không hợp lệ.", nameof(systolicBp));
        if (diastolicBp <= 0 || diastolicBp > 200)
            throw new ArgumentException("Huyết áp tâm trương không hợp lệ.", nameof(diastolicBp));
        if (pulse <= 0 || pulse > 300)
            throw new ArgumentException("Mạch không hợp lệ.", nameof(pulse));
        if (temperature.HasValue && (temperature.Value < 30 || temperature.Value > 45))
            throw new ArgumentException("Nhiệt độ không hợp lệ (30°C - 45°C).", nameof(temperature));
        if (respiratoryRate.HasValue && (respiratoryRate.Value < 5 || respiratoryRate.Value > 80))
            throw new ArgumentException("Nhịp thở không hợp lệ (5 - 80 lần/phút).", nameof(respiratoryRate));
        if (spO2.HasValue && (spO2.Value < 50 || spO2.Value > 100))
            throw new ArgumentException("Chỉ số SpO2 không hợp lệ (50% - 100%).", nameof(spO2));

        ClinicalExamId = clinicalExamId;
        HeightCm = heightCm;
        WeightKg = weightKg;
        Bmi = CalculateBmi(heightCm, weightKg);
        SystolicBp = systolicBp;
        DiastolicBp = diastolicBp;
        Pulse = pulse;
        Temperature = temperature;
        RespiratoryRate = respiratoryRate;
        SpO2 = spO2;
        RecordedAt = DateTime.UtcNow;
        RecordedByStaffId = recordedByStaffId;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public static double CalculateBmi(double heightCm, double weightKg)
    {
        if (heightCm <= 0 || weightKg <= 0) return 0;
        double heightMeter = heightCm / 100.0;
        return Math.Round(weightKg / (heightMeter * heightMeter), 2);
    }
}
