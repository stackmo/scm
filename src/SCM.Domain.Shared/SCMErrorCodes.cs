namespace SCM.Domain.Shared;

public static class SCMErrorCodes
{
    // Reception
    public const string PatientDuplicateMpi = "SCM:REC:001";
    public const string PatientNotFound = "SCM:REC:002";
    public const string EncounterNotFound = "SCM:REC:003";
    public const string RoomQueueFull = "SCM:REC:004";
    public const string InvalidVitalSigns = "SCM:REC:005";

    // Billing
    public const string InvoiceNotFound = "SCM:BIL:001";
    public const string InvoiceAlreadyPaid = "SCM:BIL:002";
    public const string InvalidPaymentAmount = "SCM:BIL:003";
    public const string DuplicateTransactionIdempotency = "SCM:BIL:004";
    public const string RefundDoctorApprovalRequired = "SCM:BIL:005";
    public const string PharmacyLockUnpaidEncounter = "SCM:BIL:006";

    // Master Data
    public const string DoctorPracticeCertificateRequired = "SCM:MST:001";
    public const string InvalidServicePricing = "SCM:MST:002";
    public const string DrugIngredientRequired = "SCM:MST:003";
    public const string RoomQueueLimitExceeded = "SCM:MST:004";
    public const string DuplicateMasterDataCode = "SCM:MST:005";

    // B2B & Contracts
    public const string CompanyNotFound = "SCM:B2B:001";
    public const string DuplicateCompanyCode = "SCM:B2B:002";
    public const string ContractNotFound = "SCM:B2B:003";
    public const string DuplicateContractNumber = "SCM:B2B:004";

    // Packages
    public const string PackageNotFound = "SCM:PKG:001";
    public const string DuplicatePackageCode = "SCM:PKG:002";
    public const string FloorPriceViolation = "SCM:PKG:003";
    public const string InvalidPackageStatusTransition = "SCM:PKG:004";
    public const string TargetGroupNotFound = "SCM:PKG:005";

    // Company Employees
    public const string EmployeeNotFound = "SCM:EMP:001";
    public const string DuplicateEmployeeCccd = "SCM:EMP:002";
    public const string InvalidCccdFormat = "SCM:EMP:003";
    public const string EmployeeGenderMismatch = "SCM:EMP:004";
    public const string EmployeeBirthDateInvalid = "SCM:EMP:005";
}
