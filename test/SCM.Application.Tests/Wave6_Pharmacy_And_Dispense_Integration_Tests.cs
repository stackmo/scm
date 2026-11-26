using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Pharmacy.Dtos;
using SCM.Application.Contracts.Prescription.Dtos;
using SCM.Application.Gateways;
using SCM.Application.Pharmacy;
using SCM.Application.Prescription;
using SCM.Domain.Branches;
using SCM.Domain.MasterData;
using SCM.Domain.MasterData.Gateways;
using SCM.Domain.Pharmacy;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Prescription;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave6_Pharmacy_And_Dispense_Integration_Tests
{
    private (SCMDbContext Context, PharmacyAppService PharmacyService, PrescriptionAppService PrescriptionService, Guid BranchId, Staff Doctor, Staff Pharmacist) CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SCMDbContext(options);
        var branchId = Guid.NewGuid();

        var doctor = new Staff("BS001", "Bác sĩ Chuyên Khoa I", StaffRole.Doctor, practiceCertificateCode: "001234/HCM-CCHN")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "0909123456",
            IsActive = true
        };
        context.Staffs.Add(doctor);

        var pharmacist = new Staff("DS001", "Dược sĩ Quản lý Kho", StaffRole.Nurse, practiceCertificateCode: "005678/HCM-CCHN")
        {
            Id = Guid.NewGuid(),
            PhoneNumber = "0909999888",
            IsActive = true
        };
        context.Staffs.Add(pharmacist);

        var license = new PractitionerLicense(
            doctor.Id,
            "001234/HCM-CCHN",
            "Nội tổng quát",
            NationalApprovedStatus.Approved,
            isActive: true);
        context.PractitionerLicenses.Add(license);

        context.NationalGatewayConfigs.Add(
            new NationalGatewayConfig(
                "79001",
                GatewayType.CongDonThuoc,
                "https://api.donthuocquocgia.vn",
                "KEY_BR01",
                "SECRET_BR01",
                "CERT_001",
                isActive: true)
        );

        context.SaveChanges();

        var gatewayClient = new NationalPrescriptionGatewayClient();
        var presService = new PrescriptionAppService(context, gatewayClient);
        var pharmService = new PharmacyAppService(context);

        return (context, pharmService, presService, branchId, doctor, pharmacist);
    }

    // =========================================================================
    // AC-6.6: Tự động chọn Lô theo FEFO (Hạn dùng gần nhất) & Trừ tồn kho chính xác
    // =========================================================================
    [Fact]
    public async Task DispensePrescription_Applies_FEFO_Strategy_And_Deducts_Stock()
    {
        var (context, pharmService, presService, branchId, doctor, pharmacist) = CreateTestContext();

        // 1. Tạo kho lẻ chi nhánh
        var warehouseDto = await pharmService.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-LE-01",
            WarehouseName = "Tủ Dược Cấp Phát Chi Nhánh 1",
            WarehouseType = WarehouseType.BranchDispensary,
            BranchId = branchId,
            BranchCode = "BR01"
        });

        // 2. Nhập kho 2 Lô thuốc cùng 1 hoạt chất:
        // Lô A: Hạn dùng còn 30 ngày (gần hơn) -> Tồn 20 viên
        // Lô B: Hạn dùng còn 90 ngày (xa hơn) -> Tồn 50 viên
        var drugId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await pharmService.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouseDto.Id,
            ReferenceDocNo = "PNK-001",
            SupplierName = "Dược Hậu Giang",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "AML-5",
                    DrugName = "Amlodipine 5mg",
                    LotNumber = "LOT-A-NEAR",
                    ManufactureDate = now.AddMonths(-2),
                    ExpiryDate = now.AddDays(30),
                    Quantity = 20,
                    Unit = "Viên",
                    InwardUnitPrice = 1000,
                    SellingUnitPrice = 1500
                },
                new()
                {
                    DrugId = drugId,
                    DrugCode = "AML-5",
                    DrugName = "Amlodipine 5mg",
                    LotNumber = "LOT-B-FAR",
                    ManufactureDate = now.AddMonths(-1),
                    ExpiryDate = now.AddDays(90),
                    Quantity = 50,
                    Unit = "Viên",
                    InwardUnitPrice = 1000,
                    SellingUnitPrice = 1500
                }
            }
        });

        // 3. Bác sĩ kê đơn và ký số đơn thuốc (30 viên)
        var presDto = await presService.CreatePrescriptionAsync(new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-001",
            PatientId = Guid.NewGuid(),
            PatientName = "Lê Văn Huyết Áp",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "I10",
            DiagnosisText = "Tăng huyết áp",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugName = "Amlodipine 5mg",
                    NationalDrugCode = "VD-12345-20",
                    Quantity = 30,
                    Unit = "Viên",
                    Route = "Uống",
                    UsageDays = 30,
                    MorningDose = 1
                }
            }
        });

        // Ký số và đồng bộ
        await presService.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = presDto.Id,
            DoctorId = doctor.Id
        });

        // Bệnh nhân thanh toán tại Thu ngân (INV-GATE-02 Pre-condition)
        var rxEntity = await context.Prescriptions.FindAsync(presDto.Id);
        rxEntity!.MarkPaid(Guid.NewGuid());
        await context.SaveChangesAsync();

        // 4. Dược sĩ cấp phát thuốc theo đơn (không chỉ định lô, để hệ thống FEFO tự động chọn)
        var dispenseResult = await pharmService.DispensePrescriptionAsync(new DispensePrescriptionDto
        {
            PrescriptionId = presDto.Id,
            WarehouseId = warehouseDto.Id,
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName,
            Notes = "Cấp phát thuốc đầy đủ"
        });

        // 5. Kiểm tra kết quả FEFO:
        // Cần 30 viên: Lấy sạch 20 viên Lô A (hạn gần nhất) + 10 viên Lô B (hạn xa hơn)
        Assert.NotNull(dispenseResult);
        Assert.Equal(DispenseStatus.Completed, dispenseResult.Status);
        Assert.Equal(2, dispenseResult.Items.Count);

        var itemLotA = dispenseResult.Items.FirstOrDefault(i => i.LotNumber == "LOT-A-NEAR");
        Assert.NotNull(itemLotA);
        Assert.Equal(20, itemLotA.QuantityDispensed);

        var itemLotB = dispenseResult.Items.FirstOrDefault(i => i.LotNumber == "LOT-B-FAR");
        Assert.NotNull(itemLotB);
        Assert.Equal(10, itemLotB.QuantityDispensed);

        // Kiểm tra tồn kho thực tế sau khi trừ
        var lotAInDb = await context.StockLotExpiries.FirstAsync(l => l.LotNumber == "LOT-A-NEAR");
        Assert.Equal(0, lotAInDb.QuantityOnHand);

        var lotBInDb = await context.StockLotExpiries.FirstAsync(l => l.LotNumber == "LOT-B-FAR");
        Assert.Equal(40, lotBInDb.QuantityOnHand); // 50 - 10 = 40

        // Kiểm tra đơn thuốc chuyển sang trạng thái Dispensed
        var prescriptionInDb = await context.Prescriptions.FindAsync(presDto.Id);
        Assert.Equal(PrescriptionStatus.Dispensed, prescriptionInDb!.Status);

        // Kiểm tra 2 bản ghi nhật ký biến động kho OutwardPrescription
        var transactions = await context.StockTransactions
            .Where(t => t.TransactionType == StockTransactionType.OutwardPrescription)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);
    }

    // =========================================================================
    // AC-6.7: Chặn cấp phát khi đơn thuốc chưa được ký số (Signature Hard-Gate)
    // =========================================================================
    [Fact]
    public async Task Cannot_Dispense_Unsigned_Prescription()
    {
        var (context, pharmService, presService, branchId, doctor, pharmacist) = CreateTestContext();

        var warehouseDto = await pharmService.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-02",
            WarehouseName = "Kho Test 02",
            BranchId = branchId,
            BranchCode = "BR01"
        });

        // Tạo đơn thuốc nhưng KHÔNG KÝ SỐ (Status = Draft, IsSigned = false)
        var presDto = await presService.CreatePrescriptionAsync(new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-002",
            PatientId = Guid.NewGuid(),
            PatientName = "Bệnh nhân Chưa Ký",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "K21",
            DiagnosisText = "Trào ngược dạ dày",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = Guid.NewGuid(),
                    DrugName = "Omeprazole 20mg",
                    NationalDrugCode = "VD-99999-20",
                    Quantity = 14,
                    Unit = "Viên",
                    Route = "Uống"
                }
            }
        });

        // Dược sĩ bấm cấp phát -> Phải bị chặn
        await Assert.ThrowsAsync<PrescriptionNotSignedException>(async () =>
        {
            await pharmService.DispensePrescriptionAsync(new DispensePrescriptionDto
            {
                PrescriptionId = presDto.Id,
                WarehouseId = warehouseDto.Id,
                PharmacistId = pharmacist.Id,
                PharmacistName = pharmacist.FullName
            });
        });
    }

    // =========================================================================
    // AC-6.8: Chặn cấp phát khi kho không đủ số lượng tồn (Insufficient Stock)
    // =========================================================================
    [Fact]
    public async Task Dispense_Fails_When_Stock_Is_Insufficient()
    {
        var (context, pharmService, presService, branchId, doctor, pharmacist) = CreateTestContext();

        var warehouseDto = await pharmService.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-03",
            WarehouseName = "Kho Test 03",
            BranchId = branchId,
            BranchCode = "BR01"
        });

        var drugId = Guid.NewGuid();
        // Nhập kho chỉ có 5 viên
        await pharmService.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouseDto.Id,
            ReferenceDocNo = "PNK-003",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "PARA-500",
                    DrugName = "Paracetamol 500mg",
                    LotNumber = "LOT-PARA",
                    ManufactureDate = DateTime.UtcNow.AddMonths(-1),
                    ExpiryDate = DateTime.UtcNow.AddMonths(12),
                    Quantity = 5,
                    Unit = "Viên",
                    InwardUnitPrice = 500,
                    SellingUnitPrice = 800
                }
            }
        });

        // Bác sĩ kê đơn 20 viên (kho chỉ có 5)
        var presDto = await presService.CreatePrescriptionAsync(new CreatePrescriptionDto
        {
            EncounterId = Guid.NewGuid(),
            EncounterCode = "ENC-003",
            PatientId = Guid.NewGuid(),
            PatientName = "Bệnh nhân Sốt Cao",
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            DoctorLicenseNo = doctor.PracticeCertificateCode!,
            DiagnosisIcd10 = "R50",
            DiagnosisText = "Sốt không rõ nguyên nhân",
            Items = new List<CreatePrescriptionItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugName = "Paracetamol 500mg",
                    NationalDrugCode = "VD-88888-20",
                    Quantity = 20,
                    Unit = "Viên",
                    Route = "Uống"
                }
            }
        });

        await presService.SignAndSendToNationalGatewayAsync(new SignPrescriptionDto
        {
            PrescriptionId = presDto.Id,
            DoctorId = doctor.Id
        });

        // Bệnh nhân thanh toán tại Thu ngân (INV-GATE-02 Pre-condition)
        var rxEntity = await context.Prescriptions.FindAsync(presDto.Id);
        rxEntity!.MarkPaid(Guid.NewGuid());
        await context.SaveChangesAsync();

        // Cấp phát -> Ném InsufficientStockException
        await Assert.ThrowsAsync<InsufficientStockException>(async () =>
        {
            await pharmService.DispensePrescriptionAsync(new DispensePrescriptionDto
            {
                PrescriptionId = presDto.Id,
                WarehouseId = warehouseDto.Id,
                PharmacistId = pharmacist.Id,
                PharmacistName = pharmacist.FullName
            });
        });
    }

    // =========================================================================
    // AC-6.9: Chặn xuất thuốc quá hạn sử dụng (Expired Drug Protection)
    // =========================================================================
    [Fact]
    public async Task Cannot_Dispense_Expired_Drug_Lot()
    {
        var (context, pharmService, presService, branchId, doctor, pharmacist) = CreateTestContext();

        var warehouseDto = await pharmService.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-04",
            WarehouseName = "Kho Test 04",
            BranchId = branchId,
            BranchCode = "BR01"
        });

        var drugId = Guid.NewGuid();
        // Nhập thuốc ĐÃ HẾT HẠN (ExpiryDate = 5 ngày trước)
        var lot = new StockLotExpiry(
            warehouseDto.Id,
            drugId,
            "EXP-DRUG",
            "Thuốc Quá Hạn",
            "LOT-EXPIRED",
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddYears(-2),
            50,
            "Viên",
            1000,
            1500,
            branchId,
            "BR01");
        context.StockLotExpiries.Add(lot);
        await context.SaveChangesAsync();

        // Cố tình xuất từ lô quá hạn -> Ném ExpiredDrugDispenseException
        Assert.Throws<ExpiredDrugDispenseException>(() =>
        {
            lot.DeductQuantity(10);
        });
    }

    // =========================================================================
    // AC-6.10: Bán lẻ tại quầy Nhà thuốc GPP (Thông tư 02/2018) & Trừ kho tức thì
    // =========================================================================
    [Fact]
    public async Task Gpp_Retail_Sale_Deducts_Stock_Immediately()
    {
        var (context, pharmService, presService, branchId, doctor, pharmacist) = CreateTestContext();

        var warehouseDto = await pharmService.CreateWarehouseAsync(new CreateWarehouseDto
        {
            WarehouseCode = "KHO-GPP",
            WarehouseName = "Nhà Thuốc GPP Quầy Chi Nhánh",
            WarehouseType = WarehouseType.GppPharmacy,
            BranchId = branchId,
            BranchCode = "BR01"
        });

        var drugId = Guid.NewGuid();
        await pharmService.ReceiveInwardStockBatchAsync(new InwardReceiptDto
        {
            WarehouseId = warehouseDto.Id,
            ReferenceDocNo = "PNK-GPP-001",
            CreatedBy = pharmacist.Id,
            Items = new List<InwardReceiptItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    DrugCode = "BERBERIN",
                    DrugName = "Berberin 50mg",
                    LotNumber = "LOT-BER-01",
                    ManufactureDate = DateTime.UtcNow.AddMonths(-1),
                    ExpiryDate = DateTime.UtcNow.AddMonths(24),
                    Quantity = 100,
                    Unit = "Lọ",
                    InwardUnitPrice = 10000,
                    SellingUnitPrice = 15000
                }
            }
        });

        // Bán lẻ cho khách vãng lai 3 lọ Berberin
        var retailSale = await pharmService.CreateGppRetailSaleAsync(new CreateGppRetailSaleDto
        {
            WarehouseId = warehouseDto.Id,
            CustomerName = "Trần Thị Khách Lẻ",
            CustomerPhone = "0912345678",
            PharmacistId = pharmacist.Id,
            PharmacistName = pharmacist.FullName,
            PaymentMethod = PaymentMethod.Cash,
            Items = new List<CreateGppRetailItemDto>
            {
                new()
                {
                    DrugId = drugId,
                    Quantity = 3
                }
            }
        });

        Assert.NotNull(retailSale);
        Assert.Equal(GppSaleStatus.Paid, retailSale.Status);
        Assert.Equal(45000, retailSale.FinalAmount); // 3 x 15000 = 45000

        // Kiểm tra tồn kho còn 97 lọ
        var lotInDb = await context.StockLotExpiries.FirstAsync(l => l.LotNumber == "LOT-BER-01");
        Assert.Equal(97, lotInDb.QuantityOnHand);

        // Kiểm tra nhật ký biến động kho OutwardRetailGpp
        var trans = await context.StockTransactions
            .FirstOrDefaultAsync(t => t.TransactionType == StockTransactionType.OutwardRetailGpp);
        Assert.NotNull(trans);
        Assert.Equal(3, trans.Quantity);
        Assert.Equal(97, trans.BalanceAfter);
    }
}
