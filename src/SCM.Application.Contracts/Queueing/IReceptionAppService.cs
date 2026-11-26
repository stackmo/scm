using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Domain.Shared.Enums;

namespace SCM.Application.Contracts.Queueing;

public interface IReceptionAppService
{
    Task<QueueTicketDto> CheckinAsync(CheckinRequestDto input);
    Task<QueueTicketDto?> GetTicketAsync(Guid ticketId);
    Task<List<QueueTicketDto>> GetActiveTicketsAsync();
    Task<List<ReceptionRosterItemDto>> GetReceptionRosterAsync(Guid? contractId = null, string? keyword = null);
    Task<QueueTicketDto> RegisterWalkinPatientAsync(WalkinPatientRegistrationDto input);
    Task<List<ReceptionRosterItemDto>> ReceiveB2bHandoffAsync(B2bHandoffRequestDto input);
    ReceptionStreamType DetermineStream(bool hasBloodTaken, bool hasAdditionalServices);
}
