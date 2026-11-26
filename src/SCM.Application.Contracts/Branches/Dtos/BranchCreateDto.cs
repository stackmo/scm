namespace SCM.Application.Contracts.Branches.Dtos;

public class BranchCreateDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MinistryFacilityCode { get; set; } = string.Empty;
    public string LicenseNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorInCharge { get; set; } = string.Empty;
    public bool ActiveStatus { get; set; } = true;
    public string BankAccountDetails { get; set; } = string.Empty;
    public string DigitalCertConfig { get; set; } = string.Empty;
}

public class BranchUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string MinistryFacilityCode { get; set; } = string.Empty;
    public string LicenseNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorInCharge { get; set; } = string.Empty;
    public bool ActiveStatus { get; set; } = true;
    public string BankAccountDetails { get; set; } = string.Empty;
    public string DigitalCertConfig { get; set; } = string.Empty;
}

public class SwitchBranchInputDto
{
    public string BranchCode { get; set; } = string.Empty;
}
