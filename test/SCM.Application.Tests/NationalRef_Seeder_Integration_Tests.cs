using Microsoft.EntityFrameworkCore;
using SCM.Domain.MasterData.NationalRef;
using SCM.EntityFrameworkCore;
using SCM.EntityFrameworkCore.Seeding;
using Xunit;

namespace SCM.Application.Tests;

public class NationalRef_Seeder_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    [Fact]
    public async Task AC_Wave1_08_JsonSeeder_Should_Seed_22Catalogs_From_Frontend_Folder()
    {
        // Arrange
        using var context = CreateInMemoryContext("SCM_NationalRef_Test_Db");
        string catalogDir = Path.Combine("D:", "workspace", "001_YTE", "SmartClinicManagement", "frontend", "src", "data", "catalogs");
        string vscExportDir = Path.Combine("D:", "workspace", "001_YTE", "SmartClinicManagement", "data", "vsc_export");

        // Act
        await NationalRefJsonSeeder.SeedAsync(context, catalogDir, vscExportDir);

        // Assert - Verify all 22 tables have data loaded from JSON
        var tinhCount = await context.AppRef_TinhThanhs.CountAsync();
        var xaCount = await context.AppRef_XaPhuongs.CountAsync();
        var icdCount = await context.AppRef_ICDs.CountAsync();
        var dtCount = await context.AppRef_M3DoiTuongKhams.CountAsync();
        var ddCount = await context.AppRef_DiaDiemKhams.CountAsync();
        var gtCount = await context.AppRef_GioiTinhs.CountAsync();
        var dtocCount = await context.AppRef_DanTocs.CountAsync();
        var nmCount = await context.AppRef_NhomMaus.CountAsync();
        var ytnmCount = await context.AppRef_YeuToNhomMaus.CountAsync();
        var nnCount = await context.AppRef_NgheNghieps.CountAsync();
        var nctCount = await context.AppRef_NoiCongTacs.CountAsync();
        var nctraCount = await context.AppRef_NguonChiTras.CountAsync();
        var ctctCount = await context.AppRef_ChiTraChiTiets.CountAsync();
        var ttxnCount = await context.AppRef_TrangThaiXetNghiems.CountAsync();
        var bgdCount = await context.AppRef_BenhGiaDinhs.CountAsync();
        var ynCount = await context.AppRef_YesNos.CountAsync();
        var plskCount = await context.AppRef_PhanLoaiSucKhoes.CountAsync();
        var ttrCount = await context.AppRef_TinhTrangRangs.CountAsync();
        var kqmtCount = await context.AppRef_KetQuaMaTuys.CountAsync();
        var kldnCount = await context.AppRef_KetLuanDeNghis.CountAsync();
        var metaCount = await context.AppRef_DanhMucMetas.CountAsync();
        var dqgCount = await context.AppRef_DuocQuocGias.CountAsync();

        Assert.Equal(34, tinhCount);
        Assert.True(xaCount > 0, $"Expected XaPhuong count > 0, got {xaCount}");
        Assert.True(icdCount > 10000, $"Expected ICD count > 10,000, got {icdCount}");
        Assert.Equal(21, dtCount);
        Assert.Equal(5, ddCount);
        Assert.Equal(2, gtCount);
        Assert.Equal(77, dtocCount);
        Assert.Equal(4, nmCount);
        Assert.Equal(2, ytnmCount);
        Assert.Equal(792, nnCount);
        Assert.Equal(5440, nctCount);
        Assert.Equal(4, nctraCount);
        Assert.Equal(3, ctctCount);
        Assert.Equal(4, ttxnCount);
        Assert.Equal(9, bgdCount);
        Assert.Equal(2, ynCount);
        Assert.Equal(5, plskCount);
        Assert.Equal(10, ttrCount);
        Assert.Equal(2, kqmtCount);
        Assert.Equal(5, kldnCount);
        Assert.Equal(21, metaCount);
        Assert.True(dqgCount > 0, $"Expected DuocQuocGia count > 0, got {dqgCount}");

        // Assert - Relational integrity: every XaPhuong must have a valid City in AppRef_TinhThanh
        var validCityIds = await context.AppRef_TinhThanhs.Select(t => t.Id).ToHashSetAsync();
        var sampleWards = await context.AppRef_XaPhuongs.Take(50).ToListAsync();
        foreach (var ward in sampleWards)
        {
            Assert.Contains(ward.CityId, validCityIds);
        }

        // Assert - Idempotency: Calling SeedAsync a second time does not throw and does not duplicate
        await NationalRefJsonSeeder.SeedAsync(context, catalogDir, vscExportDir);
        var icdCountAfter = await context.AppRef_ICDs.CountAsync();
        Assert.Equal(icdCount, icdCountAfter);
    }
}
