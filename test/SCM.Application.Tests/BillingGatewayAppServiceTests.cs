using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Billing;
using SCM.Application.Clinical;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Diagnostics.Dtos;
using SCM.Application.Contracts.Pharmacy.Dtos;
using SCM.Application.Diagnostics;
using SCM.Application.Pharmacy;
using SCM.Domain.Billing;
using SCM.Domain.Billing.Exceptions;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.Pharmacy;
using SCM.Domain.Pharmacy.Exceptions;
using SCM.Domain.Prescription;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;
using PrescriptionEntity = SCM.Domain.Prescription.Prescription;

namespace SCM.Application.Tests;

public class BillingGatewayAppServiceTests
{
    private SCMDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task GetPendingBillingItems_And_SelectivePayment_Unlocks_Service()
    {
        var context = CreateTestContext();
        var branch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: true, role: "admin");
        var billingService = new BillingAppService(context, branch);

        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // 1. Tạo 1 MedicalOrder ngoài gói có 2 dịch vụ
        var order = new MedicalOrder(
            encounterId,
            "ENC-001",
            patientId,
            "Nguyen Van An",
            doctorId,
            "BS. Tri",
            MedicalOrderType.DiagnosticImaging,
            isInPackage: false,
            specialtyType: ClinicalSpecialtyType.InternalMedicine);

        order.AddItem(Guid.NewGuid(), "US_ABDOMEN", "Sieu am o bung", 250000m, 1);
        order.AddItem(Guid.NewGuid(), "US_CARDIAC", "Sieu am tim", 450000m, 1);
        context.MedicalOrders.Add(order);

        // 2. Tạo 1 Đơn thuốc đã ký
        var rx = new PrescriptionEntity(
            encounterId,
            "ENC-001",
            patientId,
            "Nguyen Van An",
            doctorId,
            "BS. Tri",
            "001234/CCHN",
            "I10",
            "Tang huyet ap");
        rx.AddItem(Guid.NewGuid(), "Amlodipine 5mg", "VD-21045-14", 30, "Vien", "Uong", 30);
        rx.ApplySignature("SIG-TEST-123");
        context.Prescriptions.Add(rx);

        await context.SaveChangesAsync();

        // 3. Quầy thu ngân lấy danh sách chờ thanh toán
        var pendingItems = await billingService.GetPendingBillingItemsAsync(encounterId);
        Assert.Equal(3, pendingItems.Count); // 2 CLS + 1 thuốc

        // 4. Bệnh nhân CHỈ ĐỒNG Ý thanh toán dịch vụ "US_ABDOMEN" (250,000 VND)
        var itemToPay = pendingItems.First(i => i.Code == "US_ABDOMEN");
        var selectiveDto = new CreateSelectiveReceiptDto
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-001",
            PatientId = patientId,
            PatientName = "Nguyen Van An",
            PaymentMethod = 1,
            CashierId = Guid.NewGuid(),
            CashierName = "Thu Ngan 1",
            SelectedItems = new List<SelectiveReceiptItemRequestDto>
            {
                new SelectiveReceiptItemRequestDto
                {
                    ItemType = itemToPay.ItemType,
                    MedicalOrderId = itemToPay.OrderOrPrescriptionId,
                    MedicalOrderItemId = itemToPay.ItemId,
                    ItemCode = itemToPay.Code,
                    ItemName = itemToPay.Name,
                    UnitPrice = itemToPay.UnitPrice,
                    Quantity = itemToPay.Quantity
                }
            }
        };

        var receiptDto = await billingService.CreateSelectiveReceiptAsync(selectiveDto);
        Assert.Equal(250000m, receiptDto.TotalAmount);
        Assert.Equal(1, receiptDto.Items.Count);

        // 5. Xác nhận thu tiền
        var confirmedReceipt = await billingService.ConfirmPaymentAsync(receiptDto.Id, "TXN-999");
        Assert.Equal(2, confirmedReceipt.Status); // Paid

        // 6. Kiểm tra Transactional State Unlock
        var updatedOrder = await context.MedicalOrders
            .Include(o => o.Items)
            .FirstAsync(o => o.Id == order.Id);

        var abdomenItem = updatedOrder.Items.First(i => i.ServiceCode == "US_ABDOMEN");
        var cardiacItem = updatedOrder.Items.First(i => i.ServiceCode == "US_CARDIAC");

        Assert.Equal(OrderPaymentStatus.Paid, abdomenItem.PaymentStatus);
        Assert.True(abdomenItem.IsReadyForExecution());
        Assert.Equal(OrderPaymentStatus.PendingPayment, cardiacItem.PaymentStatus);
        Assert.False(cardiacItem.IsReadyForExecution());
    }

    [Fact]
    public async Task DiagnosticImaging_Should_Block_Unpaid_Service_And_Allow_Paid_Service()
    {
        var context = CreateTestContext();
        var branch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: true, role: "admin");
        var imagingService = new DiagnosticImagingAppService(context, branch);

        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // Tạo chỉ định dịch vụ ngoài gói chưa thanh toán
        var order = new MedicalOrder(
            encounterId,
            "ENC-002",
            patientId,
            "Tran Thi B",
            doctorId,
            "BS. Huong",
            MedicalOrderType.DiagnosticImaging,
            isInPackage: false);

        order.AddItem(Guid.NewGuid(), "US_THYROID", "Sieu am tuyen giap", 220000m, 1);
        context.MedicalOrders.Add(order);
        await context.SaveChangesAsync();

        var input = new CreateDiagnosticImagingDto
        {
            EncounterId = encounterId,
            EncounterCode = "ENC-002",
            Modality = 2, // US
            ServiceCode = "US_THYROID",
            ServiceName = "Sieu am tuyen giap",
            Conclusion = "Binh thuong",
            RadiologistDoctorId = doctorId,
            RadiologistDoctorName = "BS. CĐHA",
            DoctorLicenseNo = "005678/CCHN"
        };

        // 1. Cố lưu kết quả khi dịch vụ chưa thanh toán -> Bị chặn (INV-GATE-01)
        await Assert.ThrowsAsync<UnpaidServiceExecutionBlockedException>(() =>
            imagingService.CreateOrUpdateRecordAsync(input));

        // 2. Sau khi thanh toán dịch vụ
        var orderItem = order.Items.First();
        orderItem.MarkPaid(Guid.NewGuid());
        order.UpdatePaymentStatusFromItems();
        await context.SaveChangesAsync();

        // 3. Giờ lưu kết quả thành công
        var result = await imagingService.CreateOrUpdateRecordAsync(input);
        Assert.NotNull(result);
        Assert.Equal("US_THYROID", result.ServiceCode);
    }

    [Fact]
    public async Task Pharmacy_Should_Block_Unpaid_Prescription_Dispense()
    {
        var context = CreateTestContext();
        var branchId = Guid.NewGuid();
        var branchCode = "BR01";
        var pharmacyService = new PharmacyAppService(context);

        var encounterId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var drugId = Guid.NewGuid();

        // Thiết lập MasterData: Kho thuốc & Tồn kho
        var warehouse = new Warehouse
        {
            Id = warehouseId,
            WarehouseCode = "K01",
            WarehouseName = "Kho Chan",
            WarehouseType = WarehouseType.CentralHub,
            BranchId = branchId,
            BranchCode = branchCode,
            IsActive = true
        };
        context.Warehouses.Add(warehouse);

        var drug = new Drug
        {
            Id = drugId,
            DrugCode = "PARA500",
            DrugName = "Paracetamol 500mg",
            Unit = "Vien",
            PackagingSpecification = "Hộp 10 vỉ",
            RegistrationNumber = "VN-12345-10",
            UsageRoute = UsageRoute.Oral,
            StandardPrice = 3000m,
            IsActive = true
        };
        context.Drugs.Add(drug);

        var lot = new StockLotExpiry
        {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            DrugId = drugId,
            DrugCode = "PARA500",
            DrugName = "Paracetamol 500mg",
            LotNumber = "LOT001",
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            ManufactureDate = DateTime.UtcNow.AddMonths(-1),
            QuantityOnHand = 100,
            Unit = "Vien",
            InwardUnitPrice = 2000m,
            SellingUnitPrice = 3000m,
            BranchId = branchId,
            BranchCode = branchCode,
            IsActive = true
        };
        context.StockLotExpiries.Add(lot);

        // Tạo đơn thuốc đã ký nhưng CHƯA THANH TOÁN (IsPaid = false)
        var rx = new PrescriptionEntity(
            encounterId,
            "ENC-003",
            patientId,
            "Le Van C",
            doctorId,
            "BS. Tri",
            "001234/CCHN",
            "R50.9",
            "Sot chua ro nguyen nhan");

        rx.AddItem(drugId, "Paracetamol 500mg", "VN-12345-10", 10, "Vien", "Uong", 3);
        rx.ApplySignature("SIG-TEST-789");
        context.Prescriptions.Add(rx);
        await context.SaveChangesAsync();

        var dispenseDto = new DispensePrescriptionDto
        {
            PrescriptionId = rx.Id,
            WarehouseId = warehouseId,
            PharmacistId = Guid.NewGuid(),
            PharmacistName = "Duoc Si 1"
        };

        // 1. Quầy thuốc xuất kho khi chưa thanh toán -> Bị chặn (INV-GATE-02)
        await Assert.ThrowsAsync<UnpaidPrescriptionDispenseBlockedException>(() =>
            pharmacyService.DispensePrescriptionAsync(dispenseDto));

        // 2. Sau khi thu ngân xác nhận đã thu tiền thuốc
        rx.MarkPaid(Guid.NewGuid());
        await context.SaveChangesAsync();

        // 3. Xuất kho thành công
        var dispenseResult = await pharmacyService.DispensePrescriptionAsync(dispenseDto);
        Assert.NotNull(dispenseResult);
        Assert.Equal(PrescriptionStatus.Dispensed, rx.Status);
    }

    [Fact]
    public async Task PatientClinicalHistoryAppService_Should_Return_Longitudinal_Timeline()
    {
        var context = CreateTestContext();
        var historyService = new PatientClinicalHistoryAppService(context);

        var patientId = Guid.NewGuid();
        var patient = new Patient("BN-00123", "Nguyen Van An", DateTime.UtcNow.AddYears(-30), Gender.Male, "079090001234", "0901234567");
        patient.Id = patientId;
        context.Patients.Add(patient);

        var enc1 = new Encounter("ENC-2025-001", Guid.NewGuid(), "BR01", patientId, WorkflowRoute.Route1_FastTrack)
        {
            EncounterDate = DateTime.UtcNow.AddMonths(-6)
        };
        var enc2 = new Encounter("ENC-2026-001", Guid.NewGuid(), "BR01", patientId, WorkflowRoute.Route1_FastTrack)
        {
            EncounterDate = DateTime.UtcNow
        };

        context.Encounters.AddRange(enc1, enc2);
        await context.SaveChangesAsync();

        var history = await historyService.GetPatientHistoryAsync(patientId);

        Assert.Equal(patientId, history.PatientId);
        Assert.Equal(2, history.TotalEncounters);
        Assert.Equal("ENC-2026-001", history.Encounters[0].EncounterCode); // sắp xếp giảm dần theo thời gian
        Assert.Equal("ENC-2025-001", history.Encounters[1].EncounterCode);
    }
}
