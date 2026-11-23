using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Domain.B2B;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Queueing;

public class ReceptionAppService : IReceptionAppService
{
    private readonly SCMDbContext _dbContext;
    private readonly ISmartQueueingService _smartQueueingService;
    private readonly ILogger<ReceptionAppService> _logger;

    public ReceptionAppService(
        SCMDbContext dbContext,
        ISmartQueueingService smartQueueingService,
        ILogger<ReceptionAppService> logger)
    {
        _dbContext = dbContext;
        _smartQueueingService = smartQueueingService;
        _logger = logger;
    }

    public ReceptionStreamType DetermineStream(bool hasBloodTaken, bool hasAdditionalServices)
    {
        if (hasAdditionalServices)
        {
            return ReceptionStreamType.StreamC_Additional;
        }

        if (hasBloodTaken)
        {
            return ReceptionStreamType.StreamA_FastTrack;
        }

        return ReceptionStreamType.StreamB_Compensatory;
    }

    public async Task<QueueTicketDto> CheckinAsync(CheckinRequestDto input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        await _smartQueueingService.InitializeRoomLoadsAsync();

        // 1. Identify Patient or Employee
        CompanyEmployee? employee = null;
        if (!string.IsNullOrWhiteSpace(input.Cccd))
        {
            employee = await _dbContext.CompanyEmployees
                .FirstOrDefaultAsync(e => e.IdentificationNumber == input.Cccd.Trim());
        }

        if (employee == null && !string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            employee = await _dbContext.CompanyEmployees
                .FirstOrDefaultAsync(e => e.EmployeeCode == input.EmployeeCode.Trim());
        }

        if (employee == null && input.PatientId.HasValue && input.PatientId.Value != Guid.Empty)
        {
            employee = await _dbContext.CompanyEmployees
                .FirstOrDefaultAsync(e => e.Id == input.PatientId.Value);
        }

        Guid patientId = employee?.Id ?? input.PatientId ?? Guid.NewGuid();
        string patientName = !string.IsNullOrWhiteSpace(input.PatientName)
            ? input.PatientName.Trim()
            : employee?.FullName ?? "Bệnh nhân Kiosk";
        string cccd = !string.IsNullOrWhiteSpace(input.Cccd)
            ? input.Cccd.Trim()
            : employee?.IdentificationNumber ?? string.Empty;
        Guid? contractId = employee?.ContractId ?? input.ContractId;

        // 2. Detect if blood has been collected previously
        bool hasBloodTaken = input.HasBloodTaken ?? false;
        if (!input.HasBloodTaken.HasValue && employee != null)
        {
            bool hasCollectedTube = await _dbContext.SpecimenTubes
                .AnyAsync(s => s.EmployeeId == employee.Id && (s.Status == TubeStatus.Received || s.Status == TubeStatus.Collected));
            bool isCollectedBatch = await _dbContext.BatchEmployeeMappings
                .AnyAsync(m => m.EmployeeId == employee.Id && m.IsCollected);

            hasBloodTaken = hasCollectedTube || isCollectedBatch;
        }

        // 3. Determine Stream Type
        var streamType = DetermineStream(hasBloodTaken, input.HasAdditionalServices);

        // 4. Generate Ticket Number
        var today = DateTime.UtcNow.Date;
        int countToday = await _dbContext.QueueTickets
            .CountAsync(t => t.StreamType == streamType && t.CheckinTime >= today);

        string streamPrefix = streamType switch
        {
            ReceptionStreamType.StreamA_FastTrack => "A",
            ReceptionStreamType.StreamB_Compensatory => "B",
            ReceptionStreamType.StreamC_Additional => "C",
            _ => "T"
        };
        string ticketNumber = $"{streamPrefix}{countToday + 1:D3}";

        // Priority level: Stream B is fasting -> give priority level 2
        int priority = streamType == ReceptionStreamType.StreamB_Compensatory ? 2 : 1;

        // 5. Create Ticket
        var ticket = new QueueTicket(
            ticketNumber,
            streamType,
            patientId,
            patientName,
            cccd,
            contractId,
            priority,
            input.IsPregnantOrSuspected,
            input.Notes ?? string.Empty);

        // 6. Build Route (QueueAssignments)
        var roomLoads = await _dbContext.ClinicRoomLoads.ToListAsync();
        var roomLoadsDict = roomLoads.ToDictionary(r => r.RoomCode.ToUpperInvariant());

        var route = new List<QueueAssignment>();

        // Step A: First room depending on Stream
        if (streamType == ReceptionStreamType.StreamB_Compensatory)
        {
            // Fasting Rule: Phlebotomy MUST be first
            if (roomLoadsDict.TryGetValue("P.XN01", out var pRoom))
            {
                route.Add(new QueueAssignment(ticket.Id, pRoom.RoomId, pRoom.RoomCode, pRoom.RoomName, pRoom.RoomType));
            }
        }
        else if (streamType == ReceptionStreamType.StreamC_Additional)
        {
            // Cashier first for payment
            if (roomLoadsDict.TryGetValue("Q.TN01", out var cRoom))
            {
                route.Add(new QueueAssignment(ticket.Id, cRoom.RoomId, cRoom.RoomCode, cRoom.RoomName, cRoom.RoomType));
            }
        }

        // Step B: Physical Examination (Thể lực & Khám Nội)
        if (roomLoadsDict.TryGetValue("P.101", out var physRoom))
        {
            route.Add(new QueueAssignment(ticket.Id, physRoom.RoomId, physRoom.RoomCode, physRoom.RoomName, physRoom.RoomType));
        }

        // Step C: Ultrasound
        if (roomLoadsDict.TryGetValue("P.SA01", out var usRoom))
        {
            route.Add(new QueueAssignment(ticket.Id, usRoom.RoomId, usRoom.RoomCode, usRoom.RoomName, usRoom.RoomType));
        }

        // Step D: X-Ray (Pregnancy Shield: cấm phụ nữ mang thai hoặc nghi ngờ)
        if (!input.IsPregnantOrSuspected && roomLoadsDict.TryGetValue("P.XQ01", out var xrRoom))
        {
            route.Add(new QueueAssignment(ticket.Id, xrRoom.RoomId, xrRoom.RoomCode, xrRoom.RoomName, xrRoom.RoomType));
        }

        // Step E: Clinical Specialties (Ophthalmology, ENT, Dental, Dermatology, Surgery, ECG)
        var specialtyCodes = new[] { "P.104", "P.105", "P.106", "P.107", "P.102", "P.ECG" };
        foreach (var code in specialtyCodes)
        {
            if (roomLoadsDict.TryGetValue(code, out var sRoom))
            {
                route.Add(new QueueAssignment(ticket.Id, sRoom.RoomId, sRoom.RoomCode, sRoom.RoomName, sRoom.RoomType));
            }
        }

        // Step F: Final Review Room (Bác sĩ Trưởng đoàn)
        if (roomLoadsDict.TryGetValue("P.TK01", out var tkRoom))
        {
            route.Add(new QueueAssignment(ticket.Id, tkRoom.RoomId, tkRoom.RoomCode, tkRoom.RoomName, tkRoom.RoomType));
        }

        // Add assignments to ticket with proper order
        for (int i = 0; i < route.Count; i++)
        {
            route[i].OrderIndex = i + 1;
            ticket.Assignments.Add(route[i]);
        }

        await _dbContext.QueueTickets.AddAsync(ticket);
        await _dbContext.SaveChangesAsync();

        // Dynamically optimize route based on cost score
        await _smartQueueingService.AutoAdvanceNextRoomAsync(ticket.Id);
        await _smartQueueingService.RefreshRoomLoadsAsync();

        return await GetTicketAsync(ticket.Id) ?? throw new InvalidOperationException("Không thể lấy thông tin vé vừa tạo.");
    }

    public async Task<QueueTicketDto?> GetTicketAsync(Guid ticketId)
    {
        var ticket = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null) return null;

        var nextOrCurrent = ticket.Assignments
            .OrderBy(a => a.OrderIndex)
            .FirstOrDefault(a => a.Status == AssignmentStatus.InConsultation || a.Status == AssignmentStatus.Waiting);

        return new QueueTicketDto
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            StreamType = ticket.StreamType,
            PatientId = ticket.PatientId,
            PatientName = ticket.PatientName,
            Cccd = ticket.Cccd,
            ContractId = ticket.ContractId,
            CheckinTime = ticket.CheckinTime,
            PriorityLevel = ticket.PriorityLevel,
            Status = ticket.Status,
            CompletedAt = ticket.CompletedAt,
            IsPregnantOrSuspected = ticket.IsPregnantOrSuspected,
            Notes = ticket.Notes,
            CurrentRoomCode = nextOrCurrent?.RoomCode,
            CurrentRoomName = nextOrCurrent?.RoomName,
            Assignments = ticket.Assignments.OrderBy(a => a.OrderIndex).Select(a => new QueueAssignmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                TicketNumber = ticket.TicketNumber,
                PatientName = ticket.PatientName,
                RoomId = a.RoomId,
                RoomCode = a.RoomCode,
                RoomName = a.RoomName,
                RoomType = a.RoomType,
                OrderIndex = a.OrderIndex,
                AssignedAt = a.AssignedAt,
                CalledAt = a.CalledAt,
                CompletedAt = a.CompletedAt,
                Status = a.Status,
                DoctorStaffId = a.DoctorStaffId,
                DoctorStaffName = a.DoctorStaffName,
                Notes = a.Notes
            }).ToList()
        };
    }

    public async Task<List<QueueTicketDto>> GetActiveTicketsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tickets = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .Where(t => t.CheckinTime >= today && t.Status != TicketStatus.Cancelled)
            .OrderBy(t => t.CheckinTime)
            .ToListAsync();

        return tickets.Select(ticket =>
        {
            var nextOrCurrent = ticket.Assignments
                .OrderBy(a => a.OrderIndex)
                .FirstOrDefault(a => a.Status == AssignmentStatus.InConsultation || a.Status == AssignmentStatus.Waiting);

            return new QueueTicketDto
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                StreamType = ticket.StreamType,
                PatientId = ticket.PatientId,
                PatientName = ticket.PatientName,
                Cccd = ticket.Cccd,
                ContractId = ticket.ContractId,
                CheckinTime = ticket.CheckinTime,
                PriorityLevel = ticket.PriorityLevel,
                Status = ticket.Status,
                CompletedAt = ticket.CompletedAt,
                IsPregnantOrSuspected = ticket.IsPregnantOrSuspected,
                Notes = ticket.Notes,
                CurrentRoomCode = nextOrCurrent?.RoomCode,
                CurrentRoomName = nextOrCurrent?.RoomName,
                Assignments = ticket.Assignments.OrderBy(a => a.OrderIndex).Select(a => new QueueAssignmentDto
                {
                    Id = a.Id,
                    TicketId = a.TicketId,
                    TicketNumber = ticket.TicketNumber,
                    PatientName = ticket.PatientName,
                    RoomId = a.RoomId,
                    RoomCode = a.RoomCode,
                    RoomName = a.RoomName,
                    RoomType = a.RoomType,
                    OrderIndex = a.OrderIndex,
                    AssignedAt = a.AssignedAt,
                    CalledAt = a.CalledAt,
                    CompletedAt = a.CompletedAt,
                    Status = a.Status,
                    DoctorStaffId = a.DoctorStaffId,
                    DoctorStaffName = a.DoctorStaffName,
                    Notes = a.Notes
                }).ToList()
            };
        }).ToList();
    }
}
