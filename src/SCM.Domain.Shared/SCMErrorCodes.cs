namespace SCM.Domain.Shared;

public static class SCMErrorCodes
{
    // Reception
    public const string PatientDuplicateMpi = "SCM:REC:001";
    public const string PatientNotFound = "SCM:REC:002";
    public const string EncounterNotFound = "SCM:REC:003";
    public const string RoomQueueFull = "SCM:REC:004";
    public const string InvalidVitalSigns = "SCM:REC:005";
    public const string BhytCardInvalid = "SCM:REC:006";
    public const string InvalidVitalSignsRange = "SCM:CLI:001";

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
    public const string ContractNotActive = "SCM:B2B:005";
    public const string ContractImmutabilityViolation = "SCM:B2B:006";
    public const string TargetGroupGenderExclusivityViolation = "SCM:B2B:007";

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

    // Phlebotomy & Mobile Sampling
    public const string PhlebotomyBatchNotFound = "SCM:PHL:001";
    public const string DuplicateBatchCode = "SCM:PHL:002";
    public const string InvalidBatchStatusTransition = "SCM:PHL:003";
    public const string SpecimenTubeNotFound = "SCM:PHL:004";
    public const string DuplicateTubeBarcode = "SCM:PHL:005";
    public const string BatchEmployeeNotFound = "SCM:PHL:006";
    public const string InvalidSpecimenStateTransition = "SCM:PHL:007";

    // Cold Chain Logistics
    public const string ColdChainManifestNotFound = "SCM:COLD:001";
    public const string DuplicateManifestCode = "SCM:COLD:002";
    public const string ColdChainTemperatureBreach = "SCM:COLD:003";
    public const string ManifestAlreadyReceived = "SCM:COLD:004";
    public const string TransitTimeExceeded = "SCM:COLD:005";
    public const string QuantityDiscrepancy = "SCM:COLD:006";

    // Wave 6: Diagnostics & Prescription
    public const string UnmappedNationalDrug = "SCM:RX:001";
    public const string DoctorLicenseInvalid = "SCM:RX:002";
    public const string PrescriptionNotFound = "SCM:RX:003";
    public const string PrescriptionAlreadySigned = "SCM:RX:004";
    public const string LabResultNotFound = "SCM:LAB:001";
    public const string DiagnosticImagingNotFound = "SCM:IMG:001";

    // Wave 7: Physical Fitness Auto-Grading & Outbox M3
    public const string HealthGradingViolation = "SCM:GRD:001";
    public const string IncompleteMedicalRecord = "SCM:GRD:002";
    public const string DualSigningSequenceViolation = "SCM:GRD:003";
    public const string NationalSyncOutboxNotFound = "SCM:OBX:001";

    // Wave 8: Cashier, BHYT & B2B Settlement
    public const string ReceiptNotFound = "SCM:BIL:007";
    public const string ReceiptModificationNotSupported = "SCM:BIL:008";
    public const string RefundUnauthorized = "SCM:BIL:009";
    public const string XmlBalanceMismatch = "SCM:XML:001";
    public const string CompanySettlementNotFound = "SCM:SET:001";

    // Wave 1 (SPEC-DOCTOR-RBAC-QUEUE-01): Multi-Doctor RBAC & Room Queue
    public const string ClinicalAccessDenied = "SCM:RBAC:001";
    public const string SpecialtyMismatch = "SCM:RBAC:002";
    public const string RoomNotAssigned = "SCM:RBAC:003";
    public const string CrossDoctorOwnershipViolation = "SCM:RBAC:004";
    public const string EncounterClinicallyLocked = "SCM:RBAC:005";
    public const string ConcurrentQueueCallConflict = "SCM:RBAC:006";

    // Wave 2 (SPEC-LIS-PACS-WORK-01): LIS & RIS/PACS Workstations & Gateway
    public const string LabReviewSequenceViolation = "SCM:LAB:002";
    public const string LabDoctorLicenseRequired = "SCM:LAB:003";
    public const string PanicValueUnacknowledged = "SCM:LAB:004";
    public const string LabResultRecordImmutable = "SCM:LAB:005";
    public const string SpecimenTubeOrderNotPaid = "SCM:LAB:006";

    public const string ImagingStudyNotCaptured = "SCM:IMG:002";
    public const string UnauthorizedModalitySign = "SCM:IMG:003";
    public const string ImagingMediaRequired = "SCM:IMG:004";
    public const string ImagingRecordImmutable = "SCM:IMG:005";
    public const string InvalidImagingUnlockReason = "SCM:IMG:006";

    public const string ConcurrencyConflict = "SCM:CON:001";

    public const string LisGatewayCancelBlocked = "SCM:GW:001";
    public const string LisGatewayInvalidSignature = "SCM:GW:002";

    // Wave 3 (SPEC-RECEPTION-BILLING-EINVOICE-01): Cashier Shifts, Sequential Receipts & E-Invoice TT78
    public const string CashierShiftAlreadyOpen = "SCM:BIL:010";
    public const string CashierShiftNotOpen = "SCM:BIL:011";
    public const string DrugPriceNotConfigured = "SCM:BIL:012";
    public const string DepositInsufficient = "SCM:BIL:013";
    public const string RefundExceedsPaidAmount = "SCM:BIL:014";
    public const string ElectronicInvoiceDuplicateReceipt = "SCM:INV:001";
    public const string InvoiceSeriesExhausted = "SCM:INV:002";
    public const string InvoiceProviderUnavailable = "SCM:INV:003";
    public const string InvoiceAlreadyCancelled = "SCM:INV:004";
    public const string ElectronicInvoiceNotFound = "SCM:INV:005";

    // Wave 4 Cum A: Patient Signatures & Consent Forms (TT32/2023/TT-BYT)
    public const string PatientSignatureEmpty = "SCM:SIGN:001";
    public const string PatientConsentEncounterInvalid = "SCM:SIGN:002";
    public const string PatientConsentImmutable = "SCM:SIGN:003";
}
