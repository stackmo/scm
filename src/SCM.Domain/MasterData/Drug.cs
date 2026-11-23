using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;

namespace SCM.Domain.MasterData;

public class ActiveIngredient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IngredientCode { get; set; } = string.Empty;
    public string IngredientName { get; set; } = string.Empty;
    public string? AtcCode { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Drug> Drugs { get; set; } = new();

    public ActiveIngredient() { }

    public ActiveIngredient(string code, string name, string? atcCode = null, string? description = null)
    {
        IngredientCode = code;
        IngredientName = name;
        AtcCode = atcCode;
        Description = description;
        IsActive = true;
    }
}

public class Drug
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public Guid ActiveIngredientId { get; set; }
    public ActiveIngredient? ActiveIngredient { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Unit { get; set; } = "Viên";
    public string? PackagingSpecification { get; set; }
    public UsageRoute UsageRoute { get; set; } = UsageRoute.Oral;
    public decimal StandardPrice { get; set; }
    public decimal InsurancePrice { get; set; }
    public decimal CostPrice { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? NationalDrugCode { get; set; }
    public string? Manufacturer { get; set; }
    public string? CountryOfOrigin { get; set; }
    public bool IsPrescriptionRequired { get; set; } = true;
    public bool IsHighAlert { get; set; } = false;
    public int MinStockLevel { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    public int? LegacyId { get; set; }

    public Drug() { }

    public Drug(
        string drugCode,
        string drugName,
        Guid activeIngredientId,
        string content,
        string unit,
        UsageRoute usageRoute,
        decimal standardPrice,
        decimal insurancePrice,
        decimal costPrice = 0,
        string? registrationNumber = null,
        string? nationalDrugCode = null,
        string? manufacturer = null,
        string? countryOfOrigin = null,
        bool isPrescriptionRequired = true,
        bool isHighAlert = false,
        int minStockLevel = 50,
        int? legacyId = null)
    {
        if (activeIngredientId == Guid.Empty)
        {
            throw new ArgumentException("Thuốc bắt buộc phải liên kết với một hoạt chất dược thư hợp lệ.", nameof(activeIngredientId));
        }

        if (standardPrice < 0 || insurancePrice < 0 || costPrice < 0)
        {
            throw new ArgumentException("Giá thuốc không được nhỏ hơn 0.");
        }

        DrugCode = drugCode;
        DrugName = drugName;
        ActiveIngredientId = activeIngredientId;
        Content = content;
        Unit = unit;
        UsageRoute = usageRoute;
        StandardPrice = standardPrice;
        InsurancePrice = insurancePrice;
        CostPrice = costPrice;
        RegistrationNumber = registrationNumber;
        NationalDrugCode = nationalDrugCode;
        Manufacturer = manufacturer;
        CountryOfOrigin = countryOfOrigin;
        IsPrescriptionRequired = isPrescriptionRequired;
        IsHighAlert = isHighAlert;
        MinStockLevel = minStockLevel;
        LegacyId = legacyId;
        IsActive = true;
    }
}
