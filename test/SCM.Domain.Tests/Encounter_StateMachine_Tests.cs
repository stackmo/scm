using System;
using System.Threading.Tasks;
using SCM.Domain.Branches;
using SCM.Domain.Reception;
using SCM.Domain.Shared.Enums;
using SCM.Domain.Shared.Exceptions;
using Xunit;

namespace SCM.Domain.Tests;

public class Encounter_StateMachine_Tests
{
    /// <summary>
    /// AC-4.5: State Transition Validation (Hard Constraint C-4.5)
    /// Given: Lượt khám đang ở trạng thái CHECKED_IN.
    /// When: Có request cố tình chuyển thẳng sang SETTLED hoặc CLOSED khi chưa hoàn tất khám lâm sàng.
    /// Then: Hệ thống ném ngoại lệ với mã lỗi ERR_ENCOUNTER_CANNOT_SKIP_EXAM.
    /// </summary>
    [Fact]
    public void Cannot_Skip_Exam_Phase()
    {
        var encounter = new Encounter(
            encounterCode: "V261124000001",
            branchId: Guid.NewGuid(),
            branchCode: "BR01",
            patientId: Guid.NewGuid(),
            workflowRoute: WorkflowRoute.Route1_FastTrack
        );

        Assert.Equal(EncounterStatus.CheckedIn, encounter.Status);
        Assert.False(encounter.IsCompleted);

        // Cố tình chuyển thẳng sang Completed/Closed mà chưa qua khám
        var ex = Assert.Throws<BusinessException>(() => encounter.CompleteEncounter());
        Assert.Equal("ERR_ENCOUNTER_CANNOT_SKIP_EXAM", ex.Code);

        // Cố tình chuyển sang thanh toán khi chưa khám
        var exPayment = Assert.Throws<BusinessException>(() => encounter.MoveToWaitingPayment());
        Assert.Equal("ERR_ENCOUNTER_CANNOT_SKIP_EXAM", exPayment.Code);
    }

    [Fact]
    public void Valid_Lifecycle_Should_Succeed()
    {
        var encounter = new Encounter(
            encounterCode: "V261124000002",
            branchId: Guid.NewGuid(),
            branchCode: "BR01",
            patientId: Guid.NewGuid(),
            workflowRoute: WorkflowRoute.Route1_FastTrack
        );

        // Step 1: CheckedIn -> InConsultation
        encounter.StartConsultation();
        Assert.Equal(EncounterStatus.InConsultation, encounter.Status);

        // Step 2: InConsultation -> WaitingPayment
        encounter.MoveToWaitingPayment();
        Assert.Equal(EncounterStatus.WaitingPayment, encounter.Status);

        // Step 3: WaitingPayment -> WaitingPharmacy
        encounter.MoveToWaitingPharmacy();
        Assert.Equal(EncounterStatus.WaitingPharmacy, encounter.Status);

        // Step 4: Complete
        encounter.CompleteEncounter();
        Assert.Equal(EncounterStatus.Completed, encounter.Status);
        Assert.True(encounter.IsCompleted);
        Assert.NotNull(encounter.CompletedTime);
    }
}
