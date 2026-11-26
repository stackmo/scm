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

    [Fact]
    public void ColdChainManifest_Transit_Time_Exceeding_4_Hours_Should_Set_Degraded_Flag()
    {
        // C-3.2: Quá trình vận chuyển > 4h -> Bắt buộc kích hoạt cờ IsDegraded = true
        var departureTime = new DateTime(2026, 11, 25, 6, 0, 0, DateTimeKind.Utc);
        var manifest = new ColdChainManifest(
            batchId: Guid.NewGuid(),
            manifestCode: "MAN-DELAY-01",
            handoverStaff: "ĐDV Nguyễn Thị A",
            courierStaff: "Tài xế C",
            departureTemperature: 4.5m,
            departureTime: departureTime
        );

        // Lúc đến Lab: sau 5.5 giờ (> 4h) mặc dù nhiệt độ đo vẫn là 5.0°C
        var arrivalTime = departureTime.AddHours(5.5);
        manifest.Receive(
            labReceiverStaff: "KTV Trưởng Lab",
            arrivalTemperature: 5.0m,
            arrivalTime: arrivalTime,
            acceptedCount: 50,
            rejectedCount: 0
        );

        Assert.True(manifest.HasTransitTimeExceeded());
        Assert.True(manifest.IsDegraded);
        Assert.Equal(ManifestStatus.TemperatureBreach, manifest.Status);
    }

    [Fact]
    public void ColdChainManifest_Receive_With_Quantity_Discrepancy_Should_Flag_Warning()
    {
        // C-3.5: Lệch số lượng thực giao vs thực nhận -> Cảnh báo thất lạc mẫu
        var manifest = new ColdChainManifest(
            batchId: Guid.NewGuid(),
            manifestCode: "MAN-DISC-01",
            handoverStaff: "ĐDV Nguyễn Thị A",
            courierStaff: "Tài xế C",
            departureTemperature: 4.5m
        )
        {
            TotalTubesHandedOver = 100 // Giao 100 ống
        };

        // Nhận 98 ống (95 accepted + 3 rejected) -> Thất lạc 2 ống!
        manifest.Receive(
            labReceiverStaff: "KTV Trưởng Lab",
            arrivalTemperature: 5.0m,
            acceptedCount: 95,
            rejectedCount: 3
        );

        Assert.True(manifest.HasDiscrepancy);
        Assert.Equal(2, manifest.DiscrepancyCount);
        Assert.Contains("CẢNH BÁO THẤT LẠC MẪU", manifest.DiscrepancyNote);
    }

    [Fact]
    public void SpecimenTube_Cannot_Receive_Uncollected_Specimen_Should_Throw_BusinessException()
    {
        // AC-3.3: Ống nghiệm đang ở trạng thái PLANNED / PREPARED, không được nhảy cóc sang RECEIVED
        var tube = new SpecimenTube(
            batchEmployeeMappingId: Guid.NewGuid(),
            employeeId: Guid.NewGuid(),
            tubeBarcode: "HD01-DOT01-NV01-E",
            tubeType: TubeType.EDTA,
            capColor: "Tím",
            targetTests: "CBC"
        );

        Assert.Equal(TubeStatus.Prepared, tube.Status);

        var ex = Assert.Throws<SCM.Domain.Shared.Exceptions.BusinessException>(() => tube.Receive());
        Assert.Equal(SCM.Domain.Shared.SCMErrorCodes.InvalidSpecimenStateTransition, ex.Code);
    }
}
