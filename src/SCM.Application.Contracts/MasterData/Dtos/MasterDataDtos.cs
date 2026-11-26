using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.MasterData.Dtos;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? MinistryDepartmentCode { get; set; } // Mã khoa chuẩn BYT QĐ 130
    public bool IsClinical { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public int? LegacyId { get; set; }
}

public class ClinicRoomDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public RoomType RoomType { get; set; }
    public Guid? DefaultDoctorId { get; set; }
    public string? DefaultDoctorName { get; set; }
    public decimal StandardExamFee { get; set; }
    public int CurrentCapacity { get; set; }
    public int MaxQueueLimit { get; set; }
    public bool IsActive { get; set; }
}

public class MedicalServiceDto
{
    public Guid Id { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid ServiceGroupId { get; set; }
    public string? ServiceGroupName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }
    public decimal InsurancePrice { get; set; }
    public decimal CorporatePrice { get; set; }
    public decimal DoctorCommissionRate { get; set; }
    public string? MinistryEquivalentCode { get; set; } // Mã tương đương BYT
    public string? MinistryEquivalentName { get; set; }
    public string? MinistryPriceCode { get; set; } // Mã giá QĐ 130
    public int? MinistryCostGroupId { get; set; } // Nhóm chi phí BHYT (1-18)
    public decimal InsuranceCoverageRate { get; set; }
    public bool IsCoveredByInsurance { get; set; }
    public string? SurgeryType { get; set; }
    public bool IsActive { get; set; }
    public int? LegacyId { get; set; }
}

public class DrugDto
{
    public Guid Id { get; set; }
    public string DrugCode { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public Guid ActiveIngredientId { get; set; }
    public string? ActiveIngredientName { get; set; }
    public string? ActiveIngredientCode { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public UsageRoute UsageRoute { get; set; }
    public string? MinistryRouteCode { get; set; } // Mã đường dùng chuẩn BYT (1.01, 2.01...)
    public decimal StandardPrice { get; set; }
    public decimal InsurancePrice { get; set; }
    public decimal CostPrice { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? NationalDrugCode { get; set; }
    public decimal InsuranceCoverageRate { get; set; }
    public bool IsInsuranceCovered { get; set; }
    public string? Manufacturer { get; set; }
    public string? CountryOfOrigin { get; set; }
    public bool IsPrescriptionRequired { get; set; }
    public bool IsHighAlert { get; set; }
    public bool IsActive { get; set; }
}

public class MedicalSupplyDto
{
    public Guid Id { get; set; }
    public string SupplyCode { get; set; } = string.Empty;
    public string SupplyName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string? Specification { get; set; }
    public decimal StandardPrice { get; set; }
    public decimal InsurancePrice { get; set; }
    public string? MinistrySupplyCode { get; set; }
    public int MinistryCostGroupId { get; set; }
    public decimal InsuranceCoverageRate { get; set; }
    public bool IsCoveredByInsurance { get; set; }
    public bool IsActive { get; set; }
}

public class StaffDto
{
    public Guid Id { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public StaffRole StaffRole { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? PracticeCertificateCode { get; set; } // Mã CCHN
    public string? Specialty { get; set; }
    public string? AcademicTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? NationalStaffId { get; set; }
    public bool CanFinalizeHealthCheckup { get; set; }
    public string? CertificateTrainingKsk { get; set; } // Chứng chỉ KSK Điều 33 TT 32/2023
    public string? DigitalCertSerial { get; set; }
    public bool IsActive { get; set; }
}

public class Icd10Dto
{
    public int Id { get; set; }
    public string IcdCode { get; set; } = string.Empty;
    public string DiseaseName { get; set; } = string.Empty;
    public string? DiseaseNameEn { get; set; }
    public string? ChapterCode { get; set; }
}

public class DentalStatusCatalogDto
{
    public int Id { get; set; }
    public string TenGoi { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int? MaSo { get; set; }
}

public class BranchDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MinistryFacilityCode { get; set; } = string.Empty;
    public string LicenseNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorInCharge { get; set; } = string.Empty;
    public bool ActiveStatus { get; set; }
    public string BankAccountDetails { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBranchDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MinistryFacilityCode { get; set; } = string.Empty;
    public string LicenseNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorInCharge { get; set; } = string.Empty;
    public string BankAccountDetails { get; set; } = string.Empty;
    public string DigitalCertConfig { get; set; } = string.Empty;
}

public class BranchServiceAvailabilityDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public Guid MedicalServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? CustomPrice { get; set; }
    public decimal? CustomFloorPrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsAvailable { get; set; }
}

public class SetBranchServiceAvailabilityDto
{
    public Guid BranchId { get; set; }
    public Guid MedicalServiceId { get; set; }
    public bool IsAvailable { get; set; }
    public decimal? CustomPrice { get; set; }
    public decimal? CustomFloorPrice { get; set; }
}

public class ProvisionBranchRoomsDto
{
    public Guid BranchId { get; set; }
    public string TemplateType { get; set; } = "POLYCLINIC_STANDARD";
}
