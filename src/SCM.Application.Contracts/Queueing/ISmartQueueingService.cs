using SCM.Application.Contracts.Queueing.Dtos;

namespace SCM.Application.Contracts.Queueing;

public interface ISmartQueueingService
{
    Task<CallNextResultDto> CallNextPatientAsync(string roomCode);
    Task<QueueAssignmentDto> CompleteAssignmentAsync(Guid assignmentId, CompleteAssignmentInputDto? input = null);
    Task<QueueBoardDto> GetQueueBoardAsync();
    Task<List<ClinicRoomLoadDto>> GetRoomLoadsAsync();
    Task<QueueAssignmentDto?> AutoAdvanceNextRoomAsync(Guid ticketId);
    Task RefreshRoomLoadsAsync();
    double CalculateRoomCostScore(ClinicRoomLoadDto room, int currentPatientFloor = 1, bool isUrgent = false);
    Task InitializeRoomLoadsAsync();
}
