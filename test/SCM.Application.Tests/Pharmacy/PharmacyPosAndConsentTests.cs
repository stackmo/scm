using Microsoft.EntityFrameworkCore;
using SCM.Application.Billing;
using SCM.Application.Billing.EInvoice;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Clinical.Dtos;
using SCM.Application.Contracts.Pharmacy.Dtos;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Gateways;
using SCM.Application.Pharmacy;
using SCM.Application.Prescription;
using SCM.Application.Queueing;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Pharmacy;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests.Pharmacy;

public class PharmacyPosAndConsentTests
{
    private static SCMDbContext CreateContext(string dbName, ICurrentBranch? branch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options, branch);
    }

    private static (
        SCMDbContext Db,
        PharmacyAppService Pharmacy,
        PrescriptionAppService Prescription,
        BillingAppService Billing,
        EInvoiceAppService EInvoice,
        PatientConsentAppService Consent,
        Branch Branch,
        Staff Doctor,
        Staff Pharmacist) CreateFullContext(string? dbName = null, Guid? branchIdOverride = null, string branchCode = "CNQ1")
    {
        var branchId = branchIdOverride ?? Guid.NewGuid();
        var currentBranch = new CurrentBranch(branchId, branchCode);
        var db = CreateContext(dbName ?? Guid.NewGuid().ToString(), currentBranch);

        var branch = new Branch(branchCode, $"Phòng khám {branchCode}", "79001", "GP-01")
        {
            Id = branchId,
            TaxCode = "0312345678",
            BankBin = "970436",
            BankAccountNo = "0071001234567",
            BankAccountName = "PHONG KHAM SCM",
            EnableDepositManagement = true
        };
        db.Branches.Add(branch);

        var doctor = new Staff("BS001", "Bác sĩ Nội Tổng Quát", StaffRole.Doctor, practiceCertificateCode: "001234/HCM-CCHN")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "0909123456",
            IsActive = true
        };
        var pharmacist = new Staff("DS001", "Dược sĩ Nhà Thuốc GPP", StaffRole.Pharmacist, practiceCertificateCode: "005678/HCM-CCHN")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "0909999888",
            IsActive = true
        };
        db.Staffs.AddRange(doctor, pharmacist);

        db.PractitionerLicenses.Add(new PractitionerLicense(
            doctor.Id,
            "001234/HCM-CCHN",
            "Nội tổng quát",
            NationalApprovedStatus.Approved,
            isActive: true));

        db.NationalGatewayConfigs.Add(new NationalGatewayConfig(
            "79001",
            GatewayType.CongDonThuoc,
            "https://api.donthuocquocgia.vn",
            "KEY_BR01",
            "SECRET_BR01",
            "CERT_001",
            isActive: true));

        db.NationalGatewayConfigs.Add(new NationalGatewayConfig(
            "79001",
            GatewayType.EInvoice,
            "https://sandbox.local/einvoice",
            "cid",
            "sec",
            "CERT01",
            isActive: true)
        {
            BranchId = branchId,
            BranchCode = branchCode
        });

        db.InvoiceSeries.Add(new InvoiceSeries(branchId, branchCode, "0312345678", "1", "C26TSM", 1, 100, true));

        db.SaveChanges();

        var lockService = new PessimisticQueueLockService(db);
        var billing = new BillingAppService(db, currentBranch, lockService: lockService);
        var einvoice = new EInvoiceAppService(db, currentBranch, lockService);
        var pharmacy = new PharmacyAppService(db, billing, einvoice, currentBranch, lockService);
        var prescription = new PrescriptionAppService(db, new NationalPrescriptionGatewayClient());
        var consent = new PatientConsentAppService(db, currentBranch);

        return (db, pharmacy, prescription, billing, einvoice, consent, branch, doctor, pharmacist);
    }

    [Fact]
    public async Task BDD_PHM_01_Prescribe_CollectPayment_DispenseFefo_MarksPrescriptionDispensed()
    {
        var (db, pharmacy, prescription, billing, einvoice, _, branch, doctor, pharmacist) = CreateFullContext();

        var warehouse = await pharmacy.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-GPP-CNQ1",
            WarehouseName = "Nhà thuốc GPP CNQ1",
            WarehouseType = WarehouseType.BranchDispensary,
            BranchId = branch.Id,
            BranchCode = branch.Code
        });

        var drugId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await pharmacy.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouse.Id,
            ReferenceDocNo = "PNK-PHM-01",
            SupplierName = "DHG Pharma",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "PARA500",
                    DrugName = "Paracetamol 500mg",
                    LotNumber = "LOT-NEAR",
                    ManufactureDate = now.AddMonths(-2),
                    ExpiryDate = now.AddDays(45),
                    Quantity = 10,
                    Unit = "Viên",
                    InwardUnitPrice = 1000,
                    SellingUnitPrice = 2000
                },
                new()
                {
                    DrugId = drugId,
                    DrugCode = "PARA500",
                    DrugName = "Paracetamol 500mg",
                    LotNumber = "LOT-FAR",
                    ManufactureDate = now.AddMonths(-1),
                    ExpiryDate = now.AddDays(180),
                    Quantity = 30,
                    Unit = "Viên",
                    InwardUnitPrice = 1000,
                    SellingUnitPrice = 2000
                }
            }
        });

        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var rx = await prescription.CreatePrescriptionAsync(new CreatePrescriptionDto
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-PHM-01",
            PatientId = patientId,
            PatientName = "Nguyễn Văn A",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "J00",
            DiagnosisText = "Viêm mũi họng cấp",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugName = "Paracetamol 500mg",
                    NationalDrugCode = "VD-11111-22",
                    Quantity = 15,
                    Unit = "Viên",
                    Route = "Uống",
                    UsageDays = 5,
                    MorningDose = 1,
                    NoonDose = 1,
                    EveningDose = 1
                }
            }
        });

        await prescription.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = rx.Id,
            DoctorId = doctor.Id
        });

        // Thu tiền đơn thuốc qua Billing & đánh dấu đã thanh toán
        var receipt = await billing.CreateReceiptAsync(new CreateReceiptDto
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-PHM-01",
            PatientId = patientId,
            PatientName = "Nguyễn Văn A",
            TotalAmount = 30000m,
            PaymentMethod = (int)PaymentMethod.Cash,
            ReceiptType = (int)ReceiptType.DirectFee,
            CashierId = pharmacist.Id,
            CashierName = pharmacist.FullName
        });
        await billing.ConfirmPaymentAsync(receipt.Id, "CASH-PHM-01");

        var rxEntity = await db.Prescriptions.FindAsync(rx.Id);
        rxEntity!.MarkPaid(receipt.Id);
        await db.SaveChangesAsync();

        var dispense = await pharmacy.DispensePrescriptionAsync(new DispensePrescriptionDto
        {
            PrescriptionId = rx.Id,
            WarehouseId = warehouse.Id,
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName
        });

        Assert.Equal(DispenseStatus.Completed, dispense.Status);
        Assert.Equal(2, dispense.Items.Count);
        Assert.Equal(10, dispense.Items.First(i => i.LotNumber == "LOT-NEAR").QuantityDispensed);
        Assert.Equal(5, dispense.Items.First(i => i.LotNumber == "LOT-FAR").QuantityDispensed);

        var updatedRx = await db.Prescriptions.AsNoTracking().FirstAsync(x => x.Id == rx.Id);
        Assert.Equal(PrescriptionStatus.Dispensed, updatedRx.Status);
    }

    [Fact]
    public async Task BDD_PHM_02_GppRetailSale_CreatesPaidReceipt_And_IssuesEInvoice()
    {
        var (db, pharmacy, _, _, _, _, branch, _, pharmacist) = CreateFullContext();

        var warehouse = await pharmacy.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-GPP-02",
            WarehouseName = "Nhà thuốc GPP 02",
            WarehouseType = WarehouseType.BranchDispensary,
            BranchId = branch.Id,
            BranchCode = branch.Code
        });

        var drugId = Guid.NewGuid();
        await pharmacy.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouse.Id,
            ReferenceDocNo = "PNK-PHM-02",
            SupplierName = "Imexpharm",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "VITC1000",
                    DrugName = "Vitamin C 1000mg",
                    LotNumber = "LOT-VITC-01",
                    ManufactureDate = DateTime.UtcNow.AddMonths(-1),
                    ExpiryDate = DateTime.UtcNow.AddMonths(12),
                    Quantity = 50,
                    Unit = "Viên",
                    InwardUnitPrice = 3000,
                    SellingUnitPrice = 5000
                }
            }
        });

        var sale = await pharmacy.CreateGppRetailSaleAsync(new CreateGppRetailSaleDto
        {
            WarehouseId = warehouse.Id,
            CustomerName = "Trần Thị Khách Lẻ",
            CustomerPhone = "0901234567",
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName,
            PaymentMethod = PaymentMethod.Cash,
            CreateReceipt = true,
            IssueEInvoice = true,
            Items = new List<CreateGppRetailItemDto>
            {
                new() { DrugId = drugId, Quantity = 10 }
            }
        });

        Assert.NotNull(sale.ReceiptId);
        Assert.NotNull(sale.ElectronicInvoiceId);
        Assert.False(string.IsNullOrWhiteSpace(sale.ElectronicInvoiceNumber));
        Assert.Equal(50000m, sale.FinalAmount);

        var receipt = await db.Receipts.AsNoTracking().FirstAsync(r => r.Id == sale.ReceiptId);
        Assert.Equal(ReceiptStatus.Paid, receipt.Status);

        var gppEntity = await db.GppRetailSales.AsNoTracking().FirstAsync(s => s.Id == sale.Id);
        Assert.Equal(sale.ReceiptId, gppEntity.ReceiptId);
        Assert.Equal(sale.ElectronicInvoiceId, gppEntity.ElectronicInvoiceId);
    }

    [Fact]
    public async Task BDD_PHM_03_UnpaidPrescription_And_ExpiredLot_AreBlocked()
    {
        var (db, pharmacy, prescription, _, _, _, branch, doctor, pharmacist) = CreateFullContext();

        var warehouse = await pharmacy.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-GPP-03",
            WarehouseName = "Nhà thuốc GPP 03",
            WarehouseType = WarehouseType.BranchDispensary,
            BranchId = branch.Id,
            BranchCode = branch.Code
        });

        var drugId = Guid.NewGuid();
        // Seed 1 lô thuốc ĐÃ HẾT HẠN
        await pharmacy.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouse.Id,
            ReferenceDocNo = "PNK-EXP-03",
            SupplierName = "DHG",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "EXP-01",
                    DrugName = "Thuốc Đã Hết Hạn",
                    LotNumber = "LOT-EXPIRED",
                    ManufactureDate = DateTime.UtcNow.AddYears(-2),
                    ExpiryDate = DateTime.UtcNow.AddDays(-5),
                    Quantity = 20,
                    Unit = "Viên",
                    InwardUnitPrice = 1000,
                    SellingUnitPrice = 2000
                }
            }
        });

        var rx = await prescription.CreatePrescriptionAsync(new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-PHM-03",
            PatientId = Guid.NewGuid(),
            PatientName = "Lê Chưa Đóng Tiền",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "K29",
            DiagnosisText = "Viêm dạ dày",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugName = "Thuốc Đã Hết Hạn",
                    NationalDrugCode = "VD-99999-22",
                    Quantity = 5,
                    Unit = "Viên",
                    Route = "Uống",
                    UsageDays = 5,
                    MorningDose = 1
                }
            }
        });

        await prescription.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = rx.Id,
            DoctorId = doctor.Id
        });

        // Case 1: Đơn thuốc chưa thanh toán -> Chặn với UnpaidPrescriptionDispenseBlockedException
        await Assert.ThrowsAsync<UnpaidPrescriptionDispenseBlockedException>(() =>
            pharmacy.DispensePrescriptionAsync(new DispensePrescriptionDto
            {
                PrescriptionId = rx.Id,
                WarehouseId = warehouse.Id,
                PharmacistId = pharmacist.Id,
                PharmacistName = pharmacist.FullName
            }));

        // Case 2: Đánh dấu đã thanh toán nhưng chỉ có lô hết hạn -> Chặn với InsufficientStockException
        var rxEntity = await db.Prescriptions.FindAsync(rx.Id);
        rxEntity!.MarkPaid(Guid.NewGuid());
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InsufficientStockException>(() =>
            pharmacy.DispensePrescriptionAsync(new DispensePrescriptionDto
            {
                PrescriptionId = rx.Id,
                WarehouseId = warehouse.Id,
                PharmacistId = pharmacist.Id,
                PharmacistName = pharmacist.FullName
            }));
    }

    [Fact]
    public async Task BDD_PHM_04_ConcurrentDispense_LastLot_OnlyOneSucceeds()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var (db, pharmacy, prescription, _, _, _, branch, doctor, pharmacist) = CreateFullContext(dbName, branchId, "CNQ1");

        var warehouse = await pharmacy.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-CONC-04",
            WarehouseName = "Nhà thuốc Concurrent",
            WarehouseType = WarehouseType.BranchDispensary,
            BranchId = branch.Id,
            BranchCode = branch.Code
        });

        var drugId = Guid.NewGuid();
        await pharmacy.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouse.Id,
            ReferenceDocNo = "PNK-CONC-04",
            SupplierName = "DHG",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "CONC-10",
                    DrugName = "Amoxicillin 500mg",
                    LotNumber = "LOT-LAST-10",
                    ManufactureDate = DateTime.UtcNow.AddMonths(-1),
                    ExpiryDate = DateTime.UtcNow.AddMonths(6),
                    Quantity = 10,
                    Unit = "Viên",
                    InwardUnitPrice = 2000,
                    SellingUnitPrice = 3000
                }
            }
        });

        async Task<Guid> CreatePaidRxAsync(string code)
        {
            var rx = await prescription.CreatePrescriptionAsync(new CreatePrescriptionDto
            {
                EncounterId = Guid.NewGuid(),
                EncounterCode = code,
                PatientId = Guid.NewGuid(),
                PatientName = $"BN {code}",
                DoctorId = doctor.Id,
                DoctorName = doctor.FullName,
                DoctorLicenseNo = doctor.PracticeCertificateCode!,
                DiagnosisIcd10 = "J02",
                DiagnosisText = "Viêm họng",
                Items = new List<CreatePrescriptionItemDto>
                {
                    new()
                    {
                        DrugId = drugId,
                        DrugName = "Amoxicillin 500mg",
                        NationalDrugCode = "VD-55555-22",
                        Quantity = 10,
                        Unit = "Viên",
                        Route = "Uống",
                        UsageDays = 5,
                        MorningDose = 1,
                        EveningDose = 1
                    }
                }
            });
            await prescription.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
            {
                PrescriptionId = rx.Id,
                DoctorId = doctor.Id
            });
            var entity = await db.Prescriptions.FindAsync(rx.Id);
            entity!.MarkPaid(Guid.NewGuid());
            await db.SaveChangesAsync();
            return rx.Id;
        }

        var rx1Id = await CreatePaidRxAsync("ENC-C1");
        var rx2Id = await CreatePaidRxAsync("ENC-C2");

        var branchCtx = new CurrentBranch(branchId, "CNQ1");
        using var db1 = CreateContext(dbName, branchCtx);
        using var db2 = CreateContext(dbName, branchCtx);
        var sharedLock1 = new PessimisticQueueLockService(db1);
        var sharedLock2 = new PessimisticQueueLockService(db2);
        var pharm1 = new PharmacyAppService(db1, currentBranch: branchCtx, lockService: sharedLock1);
        var pharm2 = new PharmacyAppService(db2, currentBranch: branchCtx, lockService: sharedLock2);

        var t1 = pharm1.DispensePrescriptionAsync(new DispensePrescriptionDto
        {
            PrescriptionId = rx1Id,
            WarehouseId = warehouse.Id,
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName
        });
        var t2 = pharm2.DispensePrescriptionAsync(new DispensePrescriptionDto
        {
            PrescriptionId = rx2Id,
            WarehouseId = warehouse.Id,
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName
        });

        var results = await Task.WhenAll(
            t1.ContinueWith(t => t.Exception == null ? "OK" : t.Exception.InnerException?.GetType().Name ?? "ERR"),
            t2.ContinueWith(t => t.Exception == null ? "OK" : t.Exception.InnerException?.GetType().Name ?? "ERR"));

        Assert.Equal(1, results.Count(r => r == "OK"));
        Assert.Equal(1, results.Count(r => r == nameof(InsufficientStockException)));
    }

    [Fact]
    public async Task BDD_SIGN_01_ValidSignature_SavesRecord_WithSha256Hash()
    {
        var (db, _, _, _, _, consent, branch, _, pharmacist) = CreateFullContext();

        var patient = new Patient("BN-SIGN-01", "Phạm Thị Ký Tên", new DateTime(1992, 5, 20, 0, 0, 0, DateTimeKind.Utc), Gender.Female, "079192000111");
        var encounter = new Encounter("ENC-SIGN-01", branch.Id, branch.Code, patient.Id, WorkflowRoute.Route2_Compensatory);
        db.Patients.Add(patient);
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync();

        var dto = await consent.SignAsync(new SignPatientConsentDto
        {
            EncounterId = encounter.Id,
            PatientId = patient.Id,
            PatientName = patient.FullName,
            FormType = PatientFormType.TT32_HistoryCommitment,
            SignerName = "Phạm Thị Ký Tên",
            SignerRelation = "Bản thân",
            SignatureImageBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
            CollectedByStaffId = pharmacist.Id,
            CollectedByStaffName = pharmacist.FullName
        });

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("BM-TT32-01", dto.FormCode);
        Assert.Equal(64, dto.SignatureHash.Length);

        var saved = await consent.GetByEncounterAsync(encounter.Id);
        Assert.Single(saved);
        Assert.Equal(dto.SignatureHash, saved[0].SignatureHash);
    }

    [Fact]
    public async Task BDD_SIGN_02_EmptyBase64_ThrowsBusinessException_Sign001()
    {
        var (db, _, _, _, _, consent, branch, _, pharmacist) = CreateFullContext();

        var patient = new Patient("BN-SIGN-02", "Trần Văn Trống", new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), Gender.Male, "079190000222");
        var encounter = new Encounter("ENC-SIGN-02", branch.Id, branch.Code, patient.Id, WorkflowRoute.Route2_Compensatory);
        db.Patients.Add(patient);
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessException>(() => consent.SignAsync(new SignPatientConsentDto
        {
            EncounterId = encounter.Id,
            PatientId = patient.Id,
            FormType = PatientFormType.ProcedureConsent,
            SignerName = "Trần Văn Trống",
            SignatureImageBase64 = "   ",
            CollectedByStaffId = pharmacist.Id,
            CollectedByStaffName = pharmacist.FullName
        }));

        Assert.Equal(SCMErrorCodes.PatientSignatureEmpty, ex.Code);
    }

    [Fact]
    public async Task BDD_SIGN_03_CrossBranchQuery_IsIsolated()
    {
        var dbName = Guid.NewGuid().ToString();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();

        var (db1, _, _, _, _, consent1, _, _, pharmacist1) = CreateFullContext(dbName, branch1Id, "CNQ1");
        var patient = new Patient("BN-SIGN-03", "Lê Chi Nhánh 1", new DateTime(1988, 3, 10, 0, 0, 0, DateTimeKind.Utc), Gender.Male, "079188000333");
        var encounter = new Encounter("ENC-SIGN-03", branch1Id, "CNQ1", patient.Id, WorkflowRoute.Route2_Compensatory);
        db1.Patients.Add(patient);
        db1.Encounters.Add(encounter);
        await db1.SaveChangesAsync();

        await consent1.SignAsync(new SignPatientConsentDto
        {
            EncounterId = encounter.Id,
            PatientId = patient.Id,
            FormType = PatientFormType.HandoverAndSummary,
            SignerName = "Lê Chi Nhánh 1",
            SignatureImageBase64 = "data:image/png;base64,AAAA",
            CollectedByStaffId = pharmacist1.Id,
            CollectedByStaffName = pharmacist1.FullName
        });

        // Query từ Branch 2 -> Global Query Filter phải chặn hoàn toàn, trả về rỗng
        using var db2 = CreateContext(dbName, new CurrentBranch(branch2Id, "CNQ7"));
        var consent2 = new PatientConsentAppService(db2, new CurrentBranch(branch2Id, "CNQ7"));
        var crossBranchList = await consent2.GetByEncounterAsync(encounter.Id);

        Assert.Empty(crossBranchList);
    }
}
