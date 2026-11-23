using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Queueing;

public interface IReceptionAppService
{
    Task<QueueTicketDto> CheckinAsync(CheckinRequestDto input);
    Task<QueueTicketDto?> GetTicketAsync(Guid ticketId);
    Task<List<QueueTicketDto>> GetActiveTicketsAsync();
    ReceptionStreamType DetermineStream(bool hasBloodTaken, bool hasAdditionalServices);
}
