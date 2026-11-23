using SCM.Domain.Phlebotomy;
using SCM.Domain.Shared.Enums;
using Xunit;

namespace SCM.Domain.Tests;

public class Phlebotomy_Domain_Tests
{
    [Fact]
    public void PhlebotomyBatch_Creation_Should_Validate_Required_Fields()
    {
        // Missing companyId
        Assert.Throws<ArgumentException>(() => new PhlebotomyBatch(
            companyId: Guid.Empty,
            contractId: Guid.NewGuid(),
            packageId: Guid.NewGuid(),
            batchCode: "DOT-01",
            batchName: "Đợt 1"
        ));

        // Missing batchCode
        Assert.Throws<ArgumentException>(() => new PhlebotomyBatch(
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid(),
            packageId: Guid.NewGuid(),
            batchCode: "",
            batchName: "Đợt 1"
        ));

        // Valid creation
        var batch = new PhlebotomyBatch(
            companyId: Guid.NewGuid(),
            contractId: Guid.NewGuid(),
            packageId: Guid.NewGuid(),
            batchCode: "dot-01",
            batchName: "Đợt lấy mẫu FPT",
            samplingDate: new DateTime(2026, 11, 25),
            samplingLocation: "FPT Tower"
        );

        Assert.Equal("DOT-01", batch.BatchCode);
        Assert.Equal(BatchStatus.Draft, batch.Status);
    }

    [Fact]
    public void ColdChainManifest_Safe_Temperature_Range_Should_Be_2_To_8_Degrees()
    {
        // Safe temperatures
        Assert.True(ColdChainManifest.IsSafeTemperature(2.0m));
        Assert.True(ColdChainManifest.IsSafeTemperature(5.5m));
        Assert.True(ColdChainManifest.IsSafeTemperature(8.0m));

        // Unsafe temperatures
        Assert.False(ColdChainManifest.IsSafeTemperature(1.9m));
        Assert.False(ColdChainManifest.IsSafeTemperature(0.0m));
        Assert.False(ColdChainManifest.IsSafeTemperature(-2.5m));
        Assert.False(ColdChainManifest.IsSafeTemperature(8.1m));
        Assert.False(ColdChainManifest.IsSafeTemperature(12.0m));
    }

    [Fact]
    public void ColdChainManifest_Creation_With_Breach_Temperature_Should_Set_Breach_Status()
    {
        var manifestBreach = new ColdChainManifest(
            batchId: Guid.NewGuid(),
            manifestCode: "MAN-BREACH-01",
            handoverStaff: "ĐDV Nguyễn Văn A",
            courierStaff: "Tài xế B",
            departureTemperature: 9.5m
        );

        Assert.Equal(ManifestStatus.TemperatureBreach, manifestBreach.Status);
        Assert.True(manifestBreach.HasTemperatureBreach());

        var manifestNormal = new ColdChainManifest(
            batchId: Guid.NewGuid(),
            manifestCode: "MAN-NORM-01",
            handoverStaff: "ĐDV Nguyễn Văn A",
            courierStaff: "Tài xế B",
            departureTemperature: 4.2m
        );

        Assert.Equal(ManifestStatus.InTransit, manifestNormal.Status);
        Assert.False(manifestNormal.HasTemperatureBreach());
    }

    [Fact]
    public void SpecimenTube_Status_Transitions_Should_Work_Correctly()
    {
        var tube = new SpecimenTube(
            batchEmployeeMappingId: Guid.NewGuid(),
            employeeId: Guid.NewGuid(),
            tubeBarcode: "HD01-DOT01-NV01-E",
            tubeType: TubeType.EDTA,
            capColor: "Tím",
            targetTests: "CBC"
        );

        Assert.Equal(TubeStatus.Prepared, tube.Status);

        tube.MarkCollected("ĐDV Hoa");
        Assert.Equal(TubeStatus.Collected, tube.Status);
        Assert.Equal("ĐDV Hoa", tube.CollectorId);
        Assert.NotNull(tube.CollectedAt);

        var manifestId = Guid.NewGuid();
        tube.AssignToManifest(manifestId);
        Assert.Equal(TubeStatus.Transporting, tube.Status);
        Assert.Equal(manifestId, tube.ManifestId);

        tube.Receive();
        Assert.Equal(TubeStatus.Received, tube.Status);

        tube.Reject("Mẫu tan máu (Hemolysis)");
        Assert.Equal(TubeStatus.Rejected, tube.Status);
        Assert.Equal("Mẫu tan máu (Hemolysis)", tube.RejectionReason);
    }
}
