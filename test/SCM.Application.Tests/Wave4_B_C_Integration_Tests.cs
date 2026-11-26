using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using SCM.Application.B2B;
using SCM.Application.Billing;
using SCM.Application.Billing.EInvoice;
using SCM.Application.Contracts.B2B.Dtos;
using SCM.Application.Contracts.Billing.Dtos;
using SCM.Application.Contracts.Grading;
using SCM.Application.Contracts.Grading.Dtos;
using SCM.Application.Grading;
using SCM.Domain.B2B;
using SCM.Domain.Billing;
using SCM.Domain.Branches;
using SCM.Domain.Clinical;
using SCM.Domain.MasterData;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Wave4_B_C_Integration_Tests
{
    private static SCMDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new SCMDbContext(options);
    }

    [Fact]
    public async Task HealthSummary_Resolves_Real_Patient_From_Encounter_Without_Hardcoded_PII()
    {
        using var db = CreateContext(nameof(HealthSummary_Resolves_Real_Patient_From_Encounter_Without_Hardcoded_PII));
        var branch = new Branch("CNQ1", "Phòng khám Q1", "79001", "GP-01");
        db.Branches.Add(branch);

        var patient = new Patient("BN-001", "Trần Thị Minh Châu", new DateTime(1992, 5, 10), Gender.Female, "079092001234");
        db.Patients.Add(patient);

        var encounter = new Encounter("ENC-W4-001", branch.Id, branch.Code, patient.Id, WorkflowRoute.Route1_FastTrack, EncounterType.PersonalHealthCheckup);
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync();

        var service = new HealthSummaryAppService(db, new NullCurrentBranch());
        var summary = await service.SaveDraftSummaryAsync(new SaveDraftSummaryDto
        {
            EncounterId = encounter.Id,
            OverallGrade = 1,
            ConclusionCode = 5172,
            ConclusionText = "Đủ sức khỏe",
            Recommendations = "Tái khám định kỳ",
            IsHealthQualified = true
        });

        Assert.Equal(patient.Id, summary.PatientId);
        Assert.Equal("Trần Thị Minh Châu", summary.PatientName);
        Assert.Equal("ENC-W4-001", summary.EncounterCode);
    }

    [Fact]
    public async Task BatchDossier_Generates_Valid_Docx_And_Batch_Zip()
    {
        using var db = CreateContext(nameof(BatchDossier_Generates_Valid_Docx_And_Batch_Zip));
        var branch = new Branch("CNQ1", "Phòng khám SCM Q1", "79001", "GP-01");
        db.Branches.Add(branch);

        var contractId = Guid.NewGuid();
        var p1 = new Patient("BN-B1", "Nguyễn Văn An", new DateTime(1990, 1, 1), Gender.Male, "079090001111");
        var p2 = new Patient("BN-B2", "Lê Thị Bình", new DateTime(1995, 2, 2), Gender.Female, "079095002222");
        db.Patients.AddRange(p1, p2);

        var e1 = new Encounter("ENC-B2B-01", branch.Id, branch.Code, p1.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B, companyContractId: contractId);
        var e2 = new Encounter("ENC-B2B-02", branch.Id, branch.Code, p2.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B, companyContractId: contractId);
        db.Encounters.AddRange(e1, e2);
        await db.SaveChangesAsync();

        var summaryService = new HealthSummaryAppService(db, new NullCurrentBranch());
        var dossierService = new BatchDossierAppService(db, summaryService);

        var single = await dossierService.GenerateSingleDossierDocxAsync(e1.Id);
        Assert.EndsWith(".docx", single.FileName);
        Assert.True(single.FileBytes.Length > 100);

        using (var ms = new MemoryStream(single.FileBytes))
        using (var docxZip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            var docEntry = docxZip.GetEntry("word/document.xml");
            Assert.NotNull(docEntry);
            using var reader = new StreamReader(docEntry!.Open());
            var xml = await reader.ReadToEndAsync();
            Assert.Contains("Nguyễn Văn An", xml);
        }

        var batchZip = await dossierService.GenerateBatchDossierZipAsync(new BatchDossierRequestDto { CompanyContractId = contractId });
        Assert.Equal(2, batchZip.DossierCount);
        using var bms = new MemoryStream(batchZip.FileBytes);
        using var archive = new ZipArchive(bms, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
    }

    [Fact]
    public async Task Settlement_AutoCounts_Completed_And_Extra_MedicalOrders_And_Issues_Fast_EInvoice()
    {
        using var db = CreateContext(nameof(Settlement_AutoCounts_Completed_And_Extra_MedicalOrders_And_Issues_Fast_EInvoice));
        var branch = new Branch("CNQ1", "Phòng khám Q1", "79001", "GP-01") { TaxCode = "0312345678" };
        db.Branches.Add(branch);

        var series = new InvoiceSeries(branch.Id, "CNQ1", "0312345678", "1", "C26TSM", 1, 10, true);
        db.InvoiceSeries.Add(series);

        var contractId = Guid.NewGuid();
        var p1 = new Patient("BN-S1", "Phạm Văn Cường", new DateTime(1988, 3, 15), Gender.Male, "079088003333");
        db.Patients.Add(p1);
        var enc = new Encounter("ENC-S1", branch.Id, branch.Code, p1.Id, WorkflowRoute.Route1_FastTrack, EncounterType.CorporateB2B, companyContractId: contractId);
        enc.StartConsultation();
        enc.CompleteEncounter();
        db.Encounters.Add(enc);

        var ordInPkg = new MedicalOrder(enc.Id, enc.EncounterCode, p1.Id, p1.FullName, Guid.NewGuid(), "BS Test", MedicalOrderType.Laboratory, true, branchId: branch.Id, branchCode: branch.Code);
        ordInPkg.AddItem(Guid.NewGuid(), "CBC", "CTM", 100000m, 1);
        ordInPkg.StartExecution();
        ordInPkg.CompleteOrder();

        var ordExtra = new MedicalOrder(enc.Id, enc.EncounterCode, p1.Id, p1.FullName, Guid.NewGuid(), "BS Test", MedicalOrderType.DiagnosticImaging, false, branchId: branch.Id, branchCode: branch.Code);
        ordExtra.AddItem(Guid.NewGuid(), "US_THYROID", "Siêu âm giáp", 250000m, 1);
        db.MedicalOrders.AddRange(ordInPkg, ordExtra);
        await db.SaveChangesAsync();

        var eInvoiceService = new EInvoiceAppService(db, new NullCurrentBranch());
        var settlementService = new SettlementAppService(db, new NullCurrentBranch(), eInvoiceService);
        var settled = await settlementService.CalculateB2bSettlementAsync(new CalculateB2bSettlementDto
        {
            CompanyContractId = contractId,
            ContractNumber = "HD-B2B-2026-01",
            CompanyName = "Công ty ABC",
            TotalContractEmployees = 10,
            ActualCompletedEmployees = 0, // Auto-count from DB
            UnitPricePerEmployee = 500000m,
            DiscountPercentage = 10m
        });

        Assert.Equal(1, settled.ActualCompletedEmployees);
        Assert.Equal(1, settled.CompletedInPackageItemsCount);
        Assert.Equal(1, settled.ExtraOutOfPackageItemsCount);
        Assert.Equal(250000m, settled.ExtraOrdersAmount);
        Assert.Equal(450000m, settled.NetAmount);

        var invoiced = await settlementService.IssueFastEInvoiceAsync(settled.Id, "1/C26TSM");
        Assert.Equal((int)SettlementStatus.Invoiced, invoiced.Status);
        Assert.False(string.IsNullOrWhiteSpace(invoiced.InvoiceNumber));
        Assert.False(string.IsNullOrWhiteSpace(invoiced.InvoiceLookupCode));
    }

    [Fact]
    public async Task B2bCompany_Package_Prefix_And_InvoiceFields_Persist_Correctly()
    {
        using var db = CreateContext(nameof(B2bCompany_Package_Prefix_And_InvoiceFields_Persist_Correctly));
        var companyApp = new CompanyAppService(db);
        var contractApp = new CompanyContractAppService(db);
        var pkgApp = new PackageAppService(db);

        var createdComp = await companyApp.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "fpt-soft",
            Name = "Công ty TNHH FPT Software",
            TaxCode = "0101248141",
            Address = "Khu CNC Hòa Lạc, Hà Nội",
            ContactPerson = "Nguyễn Thị HR",
            ContactPhone = "0909888777",
            ContactEmail = "einvoice@fpt.com"
        });

        Assert.Equal("FPT-SOFT", createdComp.Code);
        Assert.Equal("0101248141", createdComp.TaxCode);
        Assert.Equal("einvoice@fpt.com", createdComp.ContactEmail);

        var contract = await contractApp.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = createdComp.Id,
            ContractNumber = $"HD-{createdComp.Code}-2026-01",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6),
            TotalEstimatedEmployees = 50
        });

        var svc = new MedicalService("XN-CTM", "Tổng phân tích tế bào máu", Guid.NewGuid(), "Lần", 110000m, 80000m, 95000m);
        db.MedicalServices.Add(svc);
        await db.SaveChangesAsync();

        var pkg = await pkgApp.CreateAsync(new CreateUpdatePackageDto
        {
            Code = $"GK-{createdComp.Code}-2026",
            Name = "Gói KSK Định Kỳ FPT 2026",
            CompanyId = createdComp.Id,
            ContractId = contract.Id
        });

        Assert.StartsWith("GK-FPT-SOFT-", pkg.Code);

        var addedSvc = await pkgApp.AddServiceAsync(new CreateUpdatePackageServiceDto
        {
            PackageId = pkg.Id,
            ServiceId = svc.Id,
            ServiceCode = svc.ServiceCode,
            ServiceName = svc.ServiceName,
            OriginalPrice = 110000m,
            DealPrice = 95000m,
            FloorPrice = 70000m,
            Quantity = 1
        });

        Assert.Equal("XN-CTM", addedSvc.ServiceCode);
        Assert.Equal(95000m, addedSvc.DealPrice);
    }

    [Fact]
    public async Task B2bExcelImport_Validates_Cccd_And_Imports_Employees_From_MiniExcel_Stream()
    {
        using var db = CreateContext(nameof(B2bExcelImport_Validates_Cccd_And_Imports_Employees_From_MiniExcel_Stream));
        var companyApp = new CompanyAppService(db);
        var contractApp = new CompanyContractAppService(db);
        var pkgApp = new PackageAppService(db);

        var comp = await companyApp.CreateAsync(new CreateUpdateCompanyDto
        {
            Code = "VCB",
            Name = "Ngân hàng Vietcombank",
            TaxCode = "0100112437",
            Address = "198 Trần Quang Khải",
            ContactEmail = "hr@vcb.com.vn"
        });
        var contract = await contractApp.CreateAsync(new CreateUpdateCompanyContractDto
        {
            CompanyId = comp.Id,
            ContractNumber = "HD-VCB-2026-01",
            SignDate = DateTime.Today,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6),
            TotalEstimatedEmployees = 20
        });
        var pkg = await pkgApp.CreateAsync(new CreateUpdatePackageDto
        {
            Code = $"GK-{comp.Code}-2026",
            Name = "Gói VCB 2026",
            CompanyId = comp.Id,
            ContractId = contract.Id
        });

        var rows = new[]
        {
            new Dictionary<string, object>
            {
                ["Họ và tên"] = "Nguyễn Văn Hợp Lệ",
                ["Số CCCD"] = "079090001234",
                ["Ngày sinh"] = "1990-05-15",
                ["Giới tính"] = "Nam",
                ["Phòng ban"] = "Tín dụng",
                ["Mã NV"] = "VCB-001",
                ["Số điện thoại"] = "0909111222"
            },
            new Dictionary<string, object>
            {
                ["Họ và tên"] = "Trần Văn Sai CCCD",
                ["Số CCCD"] = "12345",
                ["Ngày sinh"] = "1992-01-01",
                ["Giới tính"] = "Nam",
                ["Phòng ban"] = "IT",
                ["Mã NV"] = "VCB-002",
                ["Số điện thoại"] = "0909333444"
            }
        };

        using var ms = new MemoryStream();
        MiniExcel.SaveAs(ms, rows);
        ms.Position = 0;

        var empImportApp = new EmployeeImportAppService(db);
        var excelService = new ExcelEmployeeImportService(empImportApp);

        var result = await excelService.ImportFromExcelStreamAsync(ms, comp.Id, contract.Id, pkg.Id);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);

        var savedEmployees = await db.CompanyEmployees.Where(e => e.CompanyId == comp.Id).ToListAsync();
        Assert.Single(savedEmployees);
        Assert.Equal("Nguyễn Văn Hợp Lệ", savedEmployees[0].FullName);
    }
}
