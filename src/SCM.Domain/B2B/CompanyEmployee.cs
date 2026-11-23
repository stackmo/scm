using SCM.Domain.Shared.Enums;
using SCM.Domain.B2B.Validators;

namespace SCM.Domain.B2B;

public class CompanyEmployee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }
    public Guid ContractId { get; set; }
    public CompanyContract? Contract { get; set; }
    public Guid PackageId { get; set; }
    public Package? Package { get; set; }
    public Guid? TargetGroupId { get; set; }
    public PackageTargetGroup? TargetGroup { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty; // CCCD 12 số
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public string Department { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string HealthInsuranceCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CompanyEmployeeStatus Status { get; set; } = CompanyEmployeeStatus.ChuaKham;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public CompanyEmployee() { }

    public CompanyEmployee(
        Guid companyId,
        Guid contractId,
        Guid packageId,
        string fullName,
        string identificationNumber,
        Gender gender = Gender.Male,
        DateTime? dateOfBirth = null,
        Guid? targetGroupId = null,
        string department = "",
        string employeeCode = "",
        string phoneNumber = "",
        string email = "",
        string healthInsuranceCode = "",
        string notes = "",
        CompanyEmployeeStatus status = CompanyEmployeeStatus.ChuaKham)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId không được để trống.", nameof(companyId));
        if (contractId == Guid.Empty)
            throw new ArgumentException("ContractId không được để trống.", nameof(contractId));
        if (packageId == Guid.Empty)
            throw new ArgumentException("PackageId không được để trống.", nameof(packageId));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên nhân viên không được để trống.", nameof(fullName));

        var cccdValidation = CccdValidator.ValidateDetailed(identificationNumber, dateOfBirth, gender);
        if (!cccdValidation.IsValid)
            throw new ArgumentException(cccdValidation.ErrorMessage, nameof(identificationNumber));

        CompanyId = companyId;
        ContractId = contractId;
        PackageId = packageId;
        TargetGroupId = targetGroupId;
        FullName = fullName.Trim();
        IdentificationNumber = identificationNumber.Trim();
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Department = department.Trim();
        EmployeeCode = employeeCode.Trim();
        PhoneNumber = phoneNumber.Trim();
        Email = email.Trim();
        HealthInsuranceCode = healthInsuranceCode.Trim();
        Notes = notes.Trim();
        Status = status;
        CreatedAt = DateTime.UtcNow;
    }
}
