using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Billing;
using SCM.Application.Billing.EInvoice;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Application.Queueing;
using SCM.Domain.B2B;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Prescription;
using SCM.Domain.Reception;
using SCM.Domain.Shared;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests.Billing;

public class ReceptionBillingEInvoiceTests
{
    private static SCMDbContext CreateContext(string dbName, ICurrentBranch? branch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options, branch);
    }

    private static async Task<Branch> SeedBranchAndRoomsAsync(SCMDbContext db, Guid branchId, string branchCode = "CNQ1")
    {
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
        await db.SaveChangesAsync();

        var smartQueue = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance);
        await smartQueue.InitializeRoomLoadsAsync();
        return branch;
    }

    [Fact]
    public async Task BDD_REC_01_B2B_Checkin_Creates_Patient_Encounter_ClinicalExam_PaidPackageOrder_And_Queue()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        await SeedBranchAndRoomsAsync(db, branchId, "CNQ1");

        var svcGroup = new ServiceGroup("KB", "Khám bệnh", ServiceType.Examination);
        db.ServiceGroups.Add(svcGroup);
        var srvNoi = new MedicalService("DV_NOI_TONG_QUAT", "Khám Nội Tổng Quát", svcGroup.Id, "Lần", 150000m, 42100m, 120000m);
        db.MedicalServices.Add(srvNoi);

        var company = new Company { Id = Guid.NewGuid(), Code = "FPT", Name = "FPT Software", TaxCode = "0102030405" };
        var contract = new CompanyContract
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ContractNumber = "HD-FPT-2026",
            Status = ContractStatus.Executing
        };
        var pkg = new Package("PKG-STD", "Gói Tiêu Chuẩn", company.Id, contract.Id);
        pkg.Services.Add(new PackageService
        {
            Id = Guid.NewGuid(),
            PackageId = pkg.Id,
            ServiceId = srvNoi.Id,
            ServiceCode = srvNoi.ServiceCode,
            ServiceName = srvNoi.ServiceName,
            OriginalPrice = 150000m,
            DealPrice = 100000m
        });

        var emp = new CompanyEmployee
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            ContractId = contract.Id,
            PackageId = pkg.Id,
            FullName = "NGUYỄN VĂN B2B",
            IdentificationNumber = "079090012345",
            DateOfBirth = new DateTime(1990, 5, 10),
            Gender = Gender.Male,
            EmployeeCode = "FPT-9901",
            IsSampleCollected = true,
            Status = CompanyEmployeeStatus.ChuaKham
        };

        db.Companies.Add(company);
        db.CompanyContracts.Add(contract);
        db.Packages.Add(pkg);
        db.CompanyEmployees.Add(emp);
        await db.SaveChangesAsync();

        var lockService = new PessimisticQueueLockService(db);
        var smartQueue = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance, lockService: lockService);
        var reception = new ReceptionAppService(db, smartQueue, NullLogger<ReceptionAppService>.Instance, branch, lockService);

        var ticket = await reception.CheckinAsync(new CheckinRequestDto
        {
            Cccd = "079090012345",
            HasAdditionalServices = false,
            PaymentMode = "covered"
        });

        Assert.NotEqual(Guid.Empty, ticket.PatientId);
        Assert.NotNull(ticket.EncounterId);
        Assert.False(string.IsNullOrWhiteSpace(ticket.EncounterCode));
        Assert.NotNull(ticket.MedicalOrderId);
        Assert.Equal(ReceptionStreamType.StreamA_FastTrack, ticket.StreamType);
        Assert.Null(ticket.ReceiptId);
        Assert.NotEmpty(ticket.Assignments);

        var enc = await db.Encounters.FirstOrDefaultAsync(e => e.Id == ticket.EncounterId);
        Assert.NotNull(enc);
        Assert.Equal(EncounterType.CorporateB2B, enc!.EncounterType);
        Assert.Equal(contract.Id, enc.CompanyContractId);

        var clinExam = await db.ClinicalExaminations.FirstOrDefaultAsync(c => c.EncounterId == enc.Id);
        Assert.NotNull(clinExam);

        var order = await db.MedicalOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == ticket.MedicalOrderId);
        Assert.NotNull(order);
        Assert.True(order!.IsInPackage);
        Assert.Equal(OrderPaymentStatus.PackageCovered, order.PaymentStatus);

        var updatedEmp = await db.CompanyEmployees.FirstAsync(e => e.Id == emp.Id);
        Assert.Equal(CompanyEmployeeStatus.DangKham, updatedEmp.Status);
    }

    [Fact]
    public async Task BDD_REC_02_03_Walkin_ExtraService_Creates_PendingReceipt_Then_Cashier_Confirm_Unlocks_Order()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        await SeedBranchAndRoomsAsync(db, branchId, "CNQ1");

        var svcGroup = new ServiceGroup("CDHA", "Chẩn đoán hình ảnh", ServiceType.Imaging);
        db.ServiceGroups.Add(svcGroup);
        var srvSieuAm = new MedicalService("DV-SA-BUNG", "Siêu âm ổ bụng tổng quát", svcGroup.Id, "Lần", 220000m, 49300m, 180000m);
        db.MedicalServices.Add(srvSieuAm);
        await db.SaveChangesAsync();

        var lockService = new PessimisticQueueLockService(db);
        var smartQueue = new SmartQueueingService(db, NullLogger<SmartQueueingService>.Instance, lockService: lockService);
        var reception = new ReceptionAppService(db, smartQueue, NullLogger<ReceptionAppService>.Instance, branch, lockService);

        // Step A: Register walk-in without pre-collection, then check-in extra service via VietQR -> PendingPayment + VietQrPayload
        var walkinTicket = await reception.RegisterWalkinPatientAsync(new WalkinPatientRegistrationDto
        {
            FullName = "TRẦN THỊ KHÁCH LẺ",
            BirthDate = new DateTime(1994, 8, 12),
            Gender = "Nữ",
            IdentityCardNumber = "079194008899",
            PhoneNumber = "0909888777",
            AutoCheckin = true
        });
        Assert.NotNull(walkinTicket.EncounterId);

        var ticket = await reception.CheckinAsync(new CheckinRequestDto
        {
            PatientId = walkinTicket.PatientId,
            PatientName = "TRẦN THỊ KHÁCH LẺ",
            BirthDate = new DateTime(1994, 8, 12),
            Gender = "Nữ",
            Cccd = "079194008899",
            PhoneNumber = "0909888777",
            SelectedServiceCodes = new List<string> { "DV-SA-BUNG" },
            HasAdditionalServices = true,
            PaymentMode = "direct",
            PaymentMethod = (int)PaymentMethod.VietQR
        });

        Assert.NotNull(ticket.EncounterId);
        Assert.NotNull(ticket.MedicalOrderId);
        Assert.NotNull(ticket.ReceiptId);
        Assert.StartsWith("PT-", ticket.ReceiptNumber);
        Assert.Contains("970436", ticket.VietQrPayload);
        Assert.Equal(220000m, ticket.PatientPayAmount);

        var orderItemBefore = await db.MedicalOrderItems.FirstAsync(i => i.MedicalOrderId == ticket.MedicalOrderId);
        Assert.Equal(OrderPaymentStatus.PendingPayment, orderItemBefore.PaymentStatus);

        var billing = new BillingAppService(db, branch, lockService: lockService);
        var paidReceipt = await billing.ConfirmPaymentAsync(ticket.ReceiptId!.Value, "TXN-VIETQR-01");

        Assert.Equal((int)ReceiptStatus.Paid, paidReceipt.Status);
        var orderItemAfter = await db.MedicalOrderItems.FirstAsync(i => i.MedicalOrderId == ticket.MedicalOrderId);
        Assert.Equal(OrderPaymentStatus.Paid, orderItemAfter.PaymentStatus);
    }

    [Fact]
    public async Task BDD_BIL_04_Prescription_Billing_Uses_Db_Drug_Price_And_Rejects_Zero_Priced_Drug()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        await SeedBranchAndRoomsAsync(db, branchId, "CNQ1");

        var ingredient = new ActiveIngredient("ACT01", "Paracetamol");
        db.ActiveIngredients.Add(ingredient);

        var pricedDrug = new Drug("DRG-PARA", "Paracetamol 500mg", ingredient.Id, "500mg", "Viên", UsageRoute.Oral, 15000m, 12000m, 8000m, nationalDrugCode: "VD-12345-20");
        var zeroDrug = new Drug("DRG-ZERO", "Thuốc Chưa Cấu Hình Giá", ingredient.Id, "10mg", "Viên", UsageRoute.Oral, 0m, 0m, 0m, nationalDrugCode: "VD-99999-20");
        db.Drugs.AddRange(pricedDrug, zeroDrug);

        var patient = new Patient("BN-RX-01", "Lê Thanh Tú", new DateTime(1992, 3, 20), Gender.Male, "079092004455", "0903334455");
        var enc = new Encounter("ENC-RX-01", branchId, "CNQ1", patient.Id, WorkflowRoute.Route1_FastTrack);
        var rx = new Domain.Prescription.Prescription(enc.Id, enc.EncounterCode, patient.Id, patient.FullName, Guid.NewGuid(), "BS. Hùng", "001122/HCM-CCHN", "J06", "Viêm họng cấp", null, branchId, "CNQ1");
        rx.AddItem(pricedDrug.Id, pricedDrug.DrugName, pricedDrug.NationalDrugCode, 2m, "Viên", "Uống", 5);
        rx.AddItem(zeroDrug.Id, zeroDrug.DrugName, zeroDrug.NationalDrugCode, 1m, "Viên", "Uống", 3);

        db.Patients.Add(patient);
        db.Encounters.Add(enc);
        db.Prescriptions.Add(rx);
        await db.SaveChangesAsync();

        var pricedRxItem = rx.Items[0];
        var zeroRxItem = rx.Items[1];
        var billing = new BillingAppService(db, branch);

        // Client tampers UnitPrice = 0m, Backend must enforce 15000m * 2 = 30000m from DbSet<Drug>
        var receipt = await billing.CreateSelectiveReceiptAsync(new CreateSelectiveReceiptDto
        {
            EncounterId = enc.Id,
            EncounterCode = enc.EncounterCode,
            PatientId = patient.Id,
            PatientName = patient.FullName,
            PaymentMethod = (int)PaymentMethod.Cash,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu ngân",
            SelectedItems = new List<SelectiveReceiptItemRequestDto>
            {
                new()
                {
                    ItemType = ReceiptItemType.PrescriptionDrug,
                    PrescriptionId = rx.Id,
                    PrescriptionItemId = pricedRxItem.Id,
                    ItemCode = pricedDrug.DrugCode,
                    ItemName = pricedDrug.DrugName,
                    UnitPrice = 0m, // Client-tampered price
                    Quantity = 2
                }
            }
        });

        Assert.Equal(30000m, receipt.TotalAmount);
        Assert.Single(receipt.Items);
        Assert.Equal(15000m, receipt.Items[0].UnitPrice);

        // Zero-priced drug must be rejected with DrugPriceNotConfigured
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            billing.CreateSelectiveReceiptAsync(new CreateSelectiveReceiptDto
            {
                EncounterId = enc.Id,
                EncounterCode = enc.EncounterCode,
                PatientId = patient.Id,
                PatientName = patient.FullName,
                PaymentMethod = (int)PaymentMethod.Cash,
                CashierId = Guid.NewGuid(),
                CashierName = "Thu ngân",
                SelectedItems = new List<SelectiveReceiptItemRequestDto>
                {
                    new()
                    {
                        ItemType = ReceiptItemType.PrescriptionDrug,
                        PrescriptionId = rx.Id,
                        PrescriptionItemId = zeroRxItem.Id,
                        ItemCode = zeroDrug.DrugCode,
                        ItemName = zeroDrug.DrugName,
                        UnitPrice = 1000m,
                        Quantity = 1
                    }
                }
            }));

        Assert.Equal(SCMErrorCodes.DrugPriceNotConfigured, ex.Code);
    }

    [Fact]
    public async Task BDD_BIL_Deposit_Settlement_Deducts_And_Refunds_Excess_And_Collects_Shortfall()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        await SeedBranchAndRoomsAsync(db, branchId, "CNQ1");

        // Case A: Deposit 500,000 >= Charges 200,000 -> Refund Excess 300,000 & auto-unlock order
        var p1 = new Patient("BN-DEP-1", "Phạm Tạm Ứng Dư", new DateTime(1991, 1, 1), Gender.Female, "079191001122", "0901112233");
        var enc1 = new Encounter("ENC-DEP-1", branchId, "CNQ1", p1.Id, WorkflowRoute.Route3_Additional) { DepositAmount = 500000m };
        var ord1 = new MedicalOrder(enc1.Id, enc1.EncounterCode, p1.Id, p1.FullName, Guid.NewGuid(), "BS. Lan", MedicalOrderType.Laboratory, false, branchId, "CNQ1");
        ord1.AddItem(Guid.NewGuid(), "XN-GAN", "Men gan AST/ALT", 200000m, 1);

        // Case B: Deposit 100,000 < Charges 250,000 -> Collect additional 150,000
        var p2 = new Patient("BN-DEP-2", "Đỗ Tạm Ứng Thiếu", new DateTime(1991, 2, 2), Gender.Male, "079091002233", "0902223344");
        var enc2 = new Encounter("ENC-DEP-2", branchId, "CNQ1", p2.Id, WorkflowRoute.Route3_Additional) { DepositAmount = 100000m };
        var ord2 = new MedicalOrder(enc2.Id, enc2.EncounterCode, p2.Id, p2.FullName, Guid.NewGuid(), "BS. Lan", MedicalOrderType.DiagnosticImaging, false, branchId, "CNQ1");
        ord2.AddItem(Guid.NewGuid(), "DV-CT", "Chụp CT nhẹ", 250000m, 1);

        db.Patients.AddRange(p1, p2);
        db.Encounters.AddRange(enc1, enc2);
        db.MedicalOrders.AddRange(ord1, ord2);
        await db.SaveChangesAsync();

        var billing = new BillingAppService(db, branch);
        var cashierId = Guid.NewGuid();

        var res1 = await billing.SettleAdvanceDepositAsync(new SettleDepositRequestDto
        {
            EncounterId = enc1.Id,
            CashierId = cashierId,
            CashierName = "Thu ngân A",
            PaymentMethod = (int)PaymentMethod.Cash,
            RefundExcess = true
        });

        Assert.Equal(500000m, res1.InitialDeposit);
        Assert.Equal(200000m, res1.DepositDeducted);
        Assert.Equal(0m, res1.AdditionalAmountDue);
        Assert.Equal(300000m, res1.ExcessRefunded);
        Assert.Equal(0m, res1.RemainingDeposit);
        Assert.NotNull(res1.SettlementReceipt);
        Assert.Equal((int)ReceiptStatus.Paid, res1.SettlementReceipt!.Status);
        Assert.Equal(OrderPaymentStatus.Paid, (await db.MedicalOrderItems.FirstAsync(i => i.MedicalOrderId == ord1.Id)).PaymentStatus);

        var res2 = await billing.SettleAdvanceDepositAsync(new SettleDepositRequestDto
        {
            EncounterId = enc2.Id,
            CashierId = cashierId,
            CashierName = "Thu ngân A",
            PaymentMethod = (int)PaymentMethod.Cash,
            RefundExcess = true
        });

        Assert.Equal(100000m, res2.InitialDeposit);
        Assert.Equal(100000m, res2.DepositDeducted);
        Assert.Equal(150000m, res2.AdditionalAmountDue);
        Assert.Equal(0m, res2.ExcessRefunded);
        Assert.NotNull(res2.SettlementReceipt);
        Assert.Equal((int)ReceiptStatus.Pending, res2.SettlementReceipt!.Status);
        Assert.Equal(150000m, res2.SettlementReceipt.TotalAmount);

        await billing.ConfirmPaymentAsync(res2.SettlementReceipt.Id);
        Assert.Equal(OrderPaymentStatus.Paid, (await db.MedicalOrderItems.FirstAsync(i => i.MedicalOrderId == ord2.Id)).PaymentStatus);
    }

    [Fact]
    public async Task BDD_INV_05_06_Concurrent_Issue_Allocates_Sequential_Numbers_And_Enforces_Duplicate_And_Exhausted_Guards()
    {
        var dbName = Guid.NewGuid().ToString();
        var branchId = Guid.NewGuid();
        var branch = new CurrentBranch(branchId, "CNQ1");
        using var db = CreateContext(dbName, branch);
        await SeedBranchAndRoomsAsync(db, branchId, "CNQ1");

        var cfg = new NationalGatewayConfig("79001", GatewayType.EInvoice, "https://sandbox.local/einvoice", "cid", "sec", "CERT01", true)
        {
            BranchId = branchId,
            BranchCode = "CNQ1",
            ProviderCode = "SANDBOX",
            TaxCode = "0312345678",
            DefaultTemplateCode = "1",
            DefaultSeriesCode = "C26TSM"
        };
        var series = new InvoiceSeries(branchId, "CNQ1", "0312345678", "1", "C26TSM", 1, 10, true);
        db.NationalGatewayConfigs.Add(cfg);
        db.InvoiceSeries.Add(series);

        var cashierId = Guid.NewGuid();
        var receipts = new List<Receipt>();
        for (int i = 1; i <= 11; i++)
        {
            var r = new Receipt(Guid.NewGuid(), $"ENC-INV-{i:D2}", Guid.NewGuid(), $"Bệnh nhân {i}", $"PT-INV-{i:D3}", 150000m, PaymentMethod.Cash, cashierId, "Thu ngân", branchId, "CNQ1");
            r.MarkPaid($"TXN-{i}");
            receipts.Add(r);
        }
        db.Receipts.AddRange(receipts);
        await db.SaveChangesAsync();

        var lockService = new PessimisticQueueLockService(db);
        var einvoice = new EInvoiceAppService(db, branch, lockService);

        // Fire 10 concurrent issue requests against series (capacity 1..10)
        var tasks = receipts.Take(10).Select(r => einvoice.IssueAsync(new IssueEInvoiceDto
        {
            InvoiceSeriesId = series.Id,
            ReceiptIds = new List<Guid> { r.Id },
            BuyerType = EInvoiceBuyerType.Retail,
            BuyerName = r.PatientName,
            VatRate = 0m,
            IssuedByUserId = cashierId,
            IssuedByName = "Thu ngân"
        }));

        var issued = await Task.WhenAll(tasks);
        var numbers = issued.Select(x => x.InvoiceNumber).OrderBy(n => n).ToList();

        Assert.Equal(10, numbers.Distinct().Count());
        Assert.Equal("0000001", numbers.First());
        Assert.Equal("0000010", numbers.Last());

        // Guard 1: Issuing a receipt that already has an E-Invoice throws SCM:INV:001
        var dupSeries = new InvoiceSeries(branchId, "CNQ1", "0312345678", "1", "C26DUP", 1, 100, false);
        db.InvoiceSeries.Add(dupSeries);
        await db.SaveChangesAsync();

        var dupEx = await Assert.ThrowsAsync<BusinessException>(() => einvoice.IssueAsync(new IssueEInvoiceDto
        {
            InvoiceSeriesId = dupSeries.Id,
            ReceiptIds = new List<Guid> { receipts[0].Id },
            BuyerType = EInvoiceBuyerType.Retail,
            BuyerName = receipts[0].PatientName,
            IssuedByUserId = cashierId,
            IssuedByName = "Thu ngân"
        }));
        Assert.Equal(SCMErrorCodes.ElectronicInvoiceDuplicateReceipt, dupEx.Code);

        // Guard 2: 11th receipt on exhausted series (1..10 already used) throws SCM:INV:002
        var exhaustedEx = await Assert.ThrowsAsync<BusinessException>(() => einvoice.IssueAsync(new IssueEInvoiceDto
        {
            InvoiceSeriesId = series.Id,
            ReceiptIds = new List<Guid> { receipts[10].Id },
            BuyerType = EInvoiceBuyerType.Retail,
            BuyerName = receipts[10].PatientName,
            IssuedByUserId = cashierId,
            IssuedByName = "Thu ngân"
        }));
        Assert.Equal(SCMErrorCodes.InvoiceSeriesExhausted, exhaustedEx.Code);
    }

    [Fact]
    public async Task BDD_SEC_01_02_CashierShift_Reconciles_Cash_Refunds_And_Branch_Isolation()
    {
        var dbName = Guid.NewGuid().ToString();
        var branch1Id = Guid.NewGuid();
        var branch2Id = Guid.NewGuid();
        var branch1 = new CurrentBranch(branch1Id, "CNQ1");
        var branch2 = new CurrentBranch(branch2Id, "CNQ2");

        using var db1 = CreateContext(dbName, branch1);
        await SeedBranchAndRoomsAsync(db1, branch1Id, "CNQ1");
        await SeedBranchAndRoomsAsync(db1, branch2Id, "CNQ2");

        var cashierId = Guid.NewGuid();
        var shiftService1 = new CashierShiftAppService(db1, branch1);
        var billing1 = new BillingAppService(db1, branch1);

        var openShift = await shiftService1.OpenShiftAsync(new OpenCashierShiftDto
        {
            CashierStaffId = cashierId,
            CashierName = "Thu ngân Hương",
            OpeningCash = 1000000m
        });

        // Opening a second concurrent shift must throw SCM:BIL:010
        var shiftEx = await Assert.ThrowsAsync<BusinessException>(() => shiftService1.OpenShiftAsync(new OpenCashierShiftDto
        {
            CashierStaffId = cashierId,
            CashierName = "Thu ngân Hương",
            OpeningCash = 500000m
        }));
        Assert.Equal(SCMErrorCodes.CashierShiftAlreadyOpen, shiftEx.Code);

        // Collect 250k Cash + 300k VietQR + 50k Refund
        var rCash = await billing1.CreateReceiptAsync(new CreateReceiptDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-SH-1",
            PatientId = Guid.NewGuid(),
            PatientName = "Nguyễn TM",
            TotalAmount = 250000m,
            PaymentMethod = (int)PaymentMethod.Cash,
            CashierId = cashierId,
            CashierName = "Thu ngân Hương"
        });
        await billing1.ConfirmPaymentAsync(rCash.Id);

        var rQr = await billing1.CreateReceiptAsync(new CreateReceiptDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-SH-2",
            PatientId = Guid.NewGuid(),
            PatientName = "Trần QR",
            TotalAmount = 300000m,
            PaymentMethod = (int)PaymentMethod.VietQR,
            CashierId = cashierId,
            CashierName = "Thu ngân Hương"
        });
        await billing1.ConfirmPaymentAsync(rQr.Id);

        await billing1.ProcessRefundAsync(new RefundRequestDto
        {
            ReceiptId = rCash.Id,
            AdminId = Guid.NewGuid(),
            AdminName = "Admin Chi Nhánh",
            IsAdminRole = true,
            RefundAmount = 50000m,
            RefundReason = "Hủy bớt dịch vụ"
        });

        var closed = await shiftService1.CloseShiftAsync(new CloseCashierShiftDto
        {
            ShiftId = openShift.Id,
            CountedCash = 1200000m,
            Notes = "Khớp quỹ"
        });

        Assert.Equal(CashierShiftStatus.Closed, closed.Status);
        Assert.Equal(250000m, closed.CashCollected);
        Assert.Equal(50000m, closed.CashRefunded);
        Assert.Equal(1200000m, closed.ExpectedCash); // 1,000,000 + 250,000 - 50,000
        Assert.Equal(0m, closed.CashVariance);
        Assert.Equal(300000m, closed.ExpectedTransfer);

        // Cross-branch isolation: CNQ2 cannot see CNQ1 receipts or cashier shifts
        using var db2 = CreateContext(dbName, branch2);
        var billing2 = new BillingAppService(db2, branch2);
        var einvoice2 = new EInvoiceAppService(db2, branch2);
        Assert.Empty(await billing2.GetRecentReceiptsAsync(50));
        Assert.Empty(await einvoice2.GetSeriesAsync());
    }
}
