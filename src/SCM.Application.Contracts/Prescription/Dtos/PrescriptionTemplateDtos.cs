namespace SCM.Application.Contracts.Prescription.Dtos;

public class CreatePrescriptionTemplateItemDto
{
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int UsageDays { get; set; } = 1;
    public decimal MorningDose { get; set; } = 0;
    public decimal NoonDose { get; set; } = 0;
    public decimal EveningDose { get; set; } = 0;
    public decimal NightDose { get; set; } = 0;
    public string? Instructions { get; set; }
}

public class CreatePrescriptionTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }

    public List<CreatePrescriptionTemplateItemDto> Items { get; set; } = new();
}

public class PrescriptionTemplateItemDto
{
    public Guid Id { get; set; }
    public Guid DrugId { get; set; }
    public string DrugName { get; set; } = string.Empty;
    public string? NationalDrugCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int UsageDays { get; set; }
    public decimal MorningDose { get; set; }
    public decimal NoonDose { get; set; }
    public decimal EveningDose { get; set; }
    public decimal NightDose { get; set; }
    public string? Instructions { get; set; }
}

public class PrescriptionTemplateDto
{
    public Guid Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string DiagnosisIcd10 { get; set; } = string.Empty;
    public string DiagnosisText { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid? DoctorId { get; set; }
    public string? DoctorName { get; set; }
    public bool IsActive { get; set; }

    public List<PrescriptionTemplateItemDto> Items { get; set; } = new();
}
