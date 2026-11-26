namespace SCM.Application.Contracts.Branches.Dtos;

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
    public bool ActiveStatus { get; set; } = true;
    public string BankAccountDetails { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string BankBin { get; set; } = string.Empty;
    public string BankAccountNo { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public bool EnableDepositManagement { get; set; } = true;
    public string DigitalCertConfig { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
