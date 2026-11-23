using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SCM.Application.Contracts.Queueing;
using SCM.Application.Contracts.Queueing.Dtos;
using SCM.Domain.MasterData;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Queueing;

public class SmartQueueingService : ISmartQueueingService
{
    private readonly SCMDbContext _dbContext;
    private readonly ILogger<SmartQueueingService> _logger;

    // Weight coefficients for Cost Function
    private const double W1_Wait = 0.5;
    private const double W2_Dist = 0.2;
    private const double W3_Urgency = 0.3;

    public SmartQueueingService(SCMDbContext dbContext, ILogger<SmartQueueingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task InitializeRoomLoadsAsync()
    {
        var existingLoads = await _dbContext.ClinicRoomLoads.ToListAsync();
        if (existingLoads.Count == 0)
        {
            var defaultRooms = new List<ClinicRoomLoad>
            {
                new(Guid.NewGuid(), "P.101", "Phòng Khám Nội Tổng Quát & Thể Lực", RoomType.GeneralClinical, maxCapacity: 25, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "P.102", "Phòng Khám Ngoại - Chấn Thương", RoomType.Surgery, maxCapacity: 15, avgConsultationMinutes: 4, floorNumber: 1),
                new(Guid.NewGuid(), "P.103", "Phòng Khám Sản Phụ Khoa", RoomType.ObGyn, maxCapacity: 15, avgConsultationMinutes: 5, floorNumber: 1),
                new(Guid.NewGuid(), "P.104", "Phòng Khám Mắt (Thị Lực & Khúc Xạ)", RoomType.Ophthalmology, maxCapacity: 20, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "P.105", "Phòng Khám Tai Mũi Họng (Nội Soi)", RoomType.ENT, maxCapacity: 20, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "P.106", "Phòng Khám Răng Hàm Mặt", RoomType.Dental, maxCapacity: 15, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "P.107", "Phòng Khám Da Liễu", RoomType.Dermatology, maxCapacity: 15, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "P.SA01", "Phòng Siêu Âm 4D Số 1", RoomType.Ultrasound, maxCapacity: 15, avgConsultationMinutes: 7, floorNumber: 2),
                new(Guid.NewGuid(), "P.SA02", "Phòng Siêu Âm Tổng Quát Số 2", RoomType.Ultrasound, maxCapacity: 15, avgConsultationMinutes: 7, floorNumber: 2),
                new(Guid.NewGuid(), "P.XQ01", "Phòng Chụp X-Quang Kỹ Thuật Số", RoomType.XRay, maxCapacity: 25, avgConsultationMinutes: 4, floorNumber: 2),
                new(Guid.NewGuid(), "P.ECG", "Phòng Đo Điện Tim (ECG)", RoomType.ECG, maxCapacity: 20, avgConsultationMinutes: 3, floorNumber: 2),
                new(Guid.NewGuid(), "P.XN01", "Phòng Lấy Mẫu Xét Nghiệm (Phlebotomy)", RoomType.Phlebotomy, maxCapacity: 30, avgConsultationMinutes: 3, floorNumber: 1),
                new(Guid.NewGuid(), "Q.TN01", "Quầy Thu Ngân 01 - Quyết Toán Viện Phí", RoomType.Cashier, maxCapacity: 40, avgConsultationMinutes: 2, floorNumber: 1),
                new(Guid.NewGuid(), "P.TK01", "Phòng Bác Sĩ Trưởng Đoàn Tổng Kết & Ký Số", RoomType.GeneralClinical, maxCapacity: 20, avgConsultationMinutes: 5, floorNumber: 1)
            };

            await _dbContext.ClinicRoomLoads.AddRangeAsync(defaultRooms);
            await _dbContext.SaveChangesAsync();
        }
    }

    public double CalculateRoomCostScore(ClinicRoomLoadDto room, int currentPatientFloor = 1, bool isUrgent = false)
    {
        // 1. Estimated wait time cost
        double tWait = room.CurrentQueueLength * (room.AvgConsultationMinutes > 0 ? room.AvgConsultationMinutes : 5);

        // 2. Physical distance cost (between floors)
        double dist = Math.Abs(currentPatientFloor - room.FloorNumber) * 5.0;

        // 3. Urgency reduction (e.g. fasting phlebotomy or urgent triage)
        double urgency = isUrgent ? 20.0 : 0.0;

        // Base Cost Score
        double cost = (W1_Wait * tWait) + (W2_Dist * dist) - (W3_Urgency * urgency);

        // 4. Bottleneck Breaker: Congestion penalty
        // If Ultrasound or Ophthalmology has >= 12 waiting or >= 35 mins wait, add penalty to disperse patients to other rooms
        if ((room.RoomType == RoomType.Ultrasound || room.RoomType == RoomType.Ophthalmology) && room.IsCongested)
        {
            cost += 150.0;
        }

        return cost;
    }

    public async Task RefreshRoomLoadsAsync()
    {
        var rooms = await _dbContext.ClinicRoomLoads.ToListAsync();
        if (rooms.Count == 0)
        {
            await InitializeRoomLoadsAsync();
            rooms = await _dbContext.ClinicRoomLoads.ToListAsync();
        }

        var activeAssignments = await _dbContext.QueueAssignments
            .Where(a => a.Status == AssignmentStatus.Waiting || a.Status == AssignmentStatus.InConsultation)
            .ToListAsync();

        foreach (var room in rooms)
        {
            int queueLength = activeAssignments.Count(a => a.RoomCode.Equals(room.RoomCode, StringComparison.OrdinalIgnoreCase));
            room.UpdateLoad(queueLength);

            var serving = activeAssignments.FirstOrDefault(a => a.RoomCode.Equals(room.RoomCode, StringComparison.OrdinalIgnoreCase) && a.Status == AssignmentStatus.InConsultation);
            if (serving != null)
            {
                var ticket = await _dbContext.QueueTickets.FirstOrDefaultAsync(t => t.Id == serving.TicketId);
                room.SetServing(ticket?.TicketNumber, ticket?.PatientName);
            }
            else
            {
                room.ClearServing();
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<ClinicRoomLoadDto>> GetRoomLoadsAsync()
    {
        var rooms = await _dbContext.ClinicRoomLoads.AsNoTracking().ToListAsync();
        if (rooms.Count == 0)
        {
            await InitializeRoomLoadsAsync();
            rooms = await _dbContext.ClinicRoomLoads.AsNoTracking().ToListAsync();
        }

        var waitingCounts = await _dbContext.QueueAssignments
            .Where(a => a.Status == AssignmentStatus.Waiting)
            .GroupBy(a => a.RoomCode)
            .Select(g => new { RoomCode = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoomCode.ToUpperInvariant(), x => x.Count);

        return rooms.Select(r => new ClinicRoomLoadDto
        {
            Id = r.Id,
            RoomId = r.RoomId,
            RoomCode = r.RoomCode,
            RoomName = r.RoomName,
            RoomType = r.RoomType,
            MaxCapacity = r.MaxCapacity,
            CurrentQueueLength = r.CurrentQueueLength,
            EstimatedWaitMinutes = r.EstimatedWaitMinutes,
            AvgConsultationMinutes = r.AvgConsultationMinutes,
            FloorNumber = r.FloorNumber,
            IsActive = r.IsActive,
            IsCongested = r.IsCongested(12, 35),
            CurrentCallingTicketNumber = r.CurrentCallingTicketNumber,
            CurrentServingPatientName = r.CurrentServingPatientName,
            WaitingCount = waitingCounts.GetValueOrDefault(r.RoomCode.ToUpperInvariant(), 0)
        }).OrderBy(r => r.FloorNumber).ThenBy(r => r.RoomCode).ToList();
    }

    public async Task<CallNextResultDto> CallNextPatientAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            throw new ArgumentException("Mã phòng không được để trống.", nameof(roomCode));

        roomCode = roomCode.Trim().ToUpperInvariant();
        var room = await _dbContext.ClinicRoomLoads.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
        if (room == null)
        {
            await InitializeRoomLoadsAsync();
            room = await _dbContext.ClinicRoomLoads.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
            if (room == null)
            {
                return new CallNextResultDto { HasPatient = false, Message = $"Không tìm thấy phòng {roomCode}." };
            }
        }

        // Get waiting assignments for this room, ordered by Ticket Priority DESC, then AssignedAt ASC
        var candidates = await _dbContext.QueueAssignments
            .Include(a => a.Ticket)
            .Where(a => a.RoomCode == roomCode && a.Status == AssignmentStatus.Waiting)
            .OrderByDescending(a => a.Ticket!.PriorityLevel)
            .ThenBy(a => a.AssignedAt)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            return new CallNextResultDto
            {
                HasPatient = false,
                Message = $"Hàng đợi phòng {roomCode} hiện không có bệnh nhân chờ.",
                RoomLoad = (await GetRoomLoadsAsync()).FirstOrDefault(r => r.RoomCode == roomCode)
            };
        }

        // Check each candidate for business invariants
        QueueAssignment? selectedAssignment = null;
        foreach (var candidate in candidates)
        {
            var ticket = candidate.Ticket;
            if (ticket == null) continue;

            // Invariant 1: Atomic Status Transition
            // A patient cannot be called if they are currently InConsultation in another room!
            bool isInConsultationElsewhere = await _dbContext.QueueAssignments
                .AnyAsync(a => a.TicketId == ticket.Id && a.Id != candidate.Id && a.Status == AssignmentStatus.InConsultation);

            if (isInConsultationElsewhere)
            {
                _logger.LogInformation("Bỏ qua bệnh nhân {PatientName} ({TicketNumber}) vì đang khám ở phòng khác.", ticket.PatientName, ticket.TicketNumber);
                continue;
            }

            // Invariant 2: Final Review Guard (Bác sĩ Trưởng đoàn)
            // Room P.TK01 only calls when 100% other assignments are Finished or Skipped!
            if (roomCode == "P.TK01")
            {
                bool hasUnfinishedOtherAssignments = await _dbContext.QueueAssignments
                    .AnyAsync(a => a.TicketId == ticket.Id && a.Id != candidate.Id && a.Status != AssignmentStatus.Finished && a.Status != AssignmentStatus.Skipped);

                if (hasUnfinishedOtherAssignments)
                {
                    _logger.LogInformation("Phòng Tổng kết P.TK01 từ chối gọi {TicketNumber} do chưa hoàn tất các chuyên khoa.", ticket.TicketNumber);
                    continue;
                }
            }

            selectedAssignment = candidate;
            break;
        }

        if (selectedAssignment == null)
        {
            return new CallNextResultDto
            {
                HasPatient = false,
                Message = $"Các bệnh nhân trong hàng đợi phòng {roomCode} hiện đang được khám ở phòng khác hoặc chưa đủ điều kiện gọi.",
                RoomLoad = (await GetRoomLoadsAsync()).FirstOrDefault(r => r.RoomCode == roomCode)
            };
        }

        // Start consultation for this assignment
        selectedAssignment.StartConsultation();
        var currentTicket = selectedAssignment.Ticket!;
        currentTicket.MarkServing();

        // Update room load
        room.SetServing(currentTicket.TicketNumber, currentTicket.PatientName);

        await _dbContext.SaveChangesAsync();
        await RefreshRoomLoadsAsync();

        var roomLoads = await GetRoomLoadsAsync();
        var updatedRoomLoad = roomLoads.FirstOrDefault(r => r.RoomCode == roomCode);

        return new CallNextResultDto
        {
            HasPatient = true,
            Assignment = new QueueAssignmentDto
            {
                Id = selectedAssignment.Id,
                TicketId = selectedAssignment.TicketId,
                TicketNumber = currentTicket.TicketNumber,
                PatientName = currentTicket.PatientName,
                RoomId = selectedAssignment.RoomId,
                RoomCode = selectedAssignment.RoomCode,
                RoomName = selectedAssignment.RoomName,
                RoomType = selectedAssignment.RoomType,
                OrderIndex = selectedAssignment.OrderIndex,
                AssignedAt = selectedAssignment.AssignedAt,
                CalledAt = selectedAssignment.CalledAt,
                CompletedAt = selectedAssignment.CompletedAt,
                Status = selectedAssignment.Status,
                DoctorStaffId = selectedAssignment.DoctorStaffId,
                DoctorStaffName = selectedAssignment.DoctorStaffName,
                Notes = selectedAssignment.Notes
            },
            RoomLoad = updatedRoomLoad,
            Message = $"Đã gọi thành công bệnh nhân {currentTicket.PatientName} ({currentTicket.TicketNumber}) vào phòng {room.RoomName}."
        };
    }

    public async Task<QueueAssignmentDto> CompleteAssignmentAsync(Guid assignmentId, CompleteAssignmentInputDto? input = null)
    {
        var assignment = await _dbContext.QueueAssignments
            .Include(a => a.Ticket)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
            throw new ArgumentException($"Không tìm thấy lượt khám với Id: {assignmentId}");

        assignment.Complete(input?.Notes);
        if (input?.DoctorStaffId != null) assignment.DoctorStaffId = input.DoctorStaffId;
        if (!string.IsNullOrWhiteSpace(input?.DoctorStaffName)) assignment.DoctorStaffName = input.DoctorStaffName;

        var room = await _dbContext.ClinicRoomLoads.FirstOrDefaultAsync(r => r.RoomCode == assignment.RoomCode);
        if (room != null && room.CurrentCallingTicketNumber == assignment.Ticket?.TicketNumber)
        {
            room.ClearServing();
        }

        await _dbContext.SaveChangesAsync();

        // Auto-advance patient to next best room
        await AutoAdvanceNextRoomAsync(assignment.TicketId);

        // Check if all assignments are finished
        var allAssignments = await _dbContext.QueueAssignments
            .Where(a => a.TicketId == assignment.TicketId)
            .ToListAsync();

        bool allFinished = allAssignments.All(a => a.Status == AssignmentStatus.Finished || a.Status == AssignmentStatus.Skipped);
        if (allFinished && assignment.Ticket != null)
        {
            assignment.Ticket.MarkCompleted();
            await _dbContext.SaveChangesAsync();
        }

        await RefreshRoomLoadsAsync();

        return new QueueAssignmentDto
        {
            Id = assignment.Id,
            TicketId = assignment.TicketId,
            TicketNumber = assignment.Ticket?.TicketNumber ?? string.Empty,
            PatientName = assignment.Ticket?.PatientName ?? string.Empty,
            RoomId = assignment.RoomId,
            RoomCode = assignment.RoomCode,
            RoomName = assignment.RoomName,
            RoomType = assignment.RoomType,
            OrderIndex = assignment.OrderIndex,
            AssignedAt = assignment.AssignedAt,
            CalledAt = assignment.CalledAt,
            CompletedAt = assignment.CompletedAt,
            Status = assignment.Status,
            DoctorStaffId = assignment.DoctorStaffId,
            DoctorStaffName = assignment.DoctorStaffName,
            Notes = assignment.Notes
        };
    }

    public async Task<QueueAssignmentDto?> AutoAdvanceNextRoomAsync(Guid ticketId)
    {
        var ticket = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null) return null;

        var waitingAssignments = ticket.Assignments
            .Where(a => a.Status == AssignmentStatus.Waiting)
            .ToList();

        if (waitingAssignments.Count == 0)
        {
            return null;
        }

        // Dynamic Room Balancing: Sort waiting assignments using Cost Function
        var roomLoads = await GetRoomLoadsAsync();
        var roomLoadsDict = roomLoads.ToDictionary(r => r.RoomCode.ToUpperInvariant());

        var scoredAssignments = waitingAssignments.Select(a =>
        {
            double score = 100.0;
            if (roomLoadsDict.TryGetValue(a.RoomCode.ToUpperInvariant(), out var rLoad))
            {
                score = CalculateRoomCostScore(rLoad, currentPatientFloor: 1, isUrgent: ticket.PriorityLevel > 1);
            }
            return new { Assignment = a, Score = score };
        }).OrderBy(x => x.Score).ToList();

        // Reorder order indexes so the lowest score room is next
        int idx = 1;
        foreach (var item in scoredAssignments)
        {
            item.Assignment.OrderIndex = idx++;
        }

        await _dbContext.SaveChangesAsync();

        var nextBest = scoredAssignments.First().Assignment;
        return new QueueAssignmentDto
        {
            Id = nextBest.Id,
            TicketId = nextBest.TicketId,
            TicketNumber = ticket.TicketNumber,
            PatientName = ticket.PatientName,
            RoomId = nextBest.RoomId,
            RoomCode = nextBest.RoomCode,
            RoomName = nextBest.RoomName,
            RoomType = nextBest.RoomType,
            OrderIndex = nextBest.OrderIndex,
            AssignedAt = nextBest.AssignedAt,
            CalledAt = nextBest.CalledAt,
            CompletedAt = nextBest.CompletedAt,
            Status = nextBest.Status
        };
    }

    public async Task<QueueBoardDto> GetQueueBoardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var tickets = await _dbContext.QueueTickets
            .Include(t => t.Assignments)
            .Where(t => t.CheckinTime >= today)
            .ToListAsync();

        var roomLoads = await GetRoomLoadsAsync();

        var recentCalls = await _dbContext.QueueAssignments
            .Include(a => a.Ticket)
            .Where(a => a.CalledAt != null && a.Status == AssignmentStatus.InConsultation)
            .OrderByDescending(a => a.CalledAt)
            .Take(10)
            .Select(a => new QueueAssignmentDto
            {
                Id = a.Id,
                TicketId = a.TicketId,
                TicketNumber = a.Ticket != null ? a.Ticket.TicketNumber : string.Empty,
                PatientName = a.Ticket != null ? a.Ticket.PatientName : string.Empty,
                RoomId = a.RoomId,
                RoomCode = a.RoomCode,
                RoomName = a.RoomName,
                RoomType = a.RoomType,
                OrderIndex = a.OrderIndex,
                AssignedAt = a.AssignedAt,
                CalledAt = a.CalledAt,
                CompletedAt = a.CompletedAt,
                Status = a.Status
            })
            .ToListAsync();

        return new QueueBoardDto
        {
            TotalWaiting = tickets.Count(t => t.Status == TicketStatus.Issued || t.Status == TicketStatus.Calling),
            TotalServing = tickets.Count(t => t.Status == TicketStatus.Serving),
            TotalCompleted = tickets.Count(t => t.Status == TicketStatus.Completed),
            StreamACount = tickets.Count(t => t.StreamType == ReceptionStreamType.StreamA_FastTrack),
            StreamBCount = tickets.Count(t => t.StreamType == ReceptionStreamType.StreamB_Compensatory),
            StreamCCount = tickets.Count(t => t.StreamType == ReceptionStreamType.StreamC_Additional),
            Rooms = roomLoads,
            RecentCallingTickets = recentCalls
        };
    }
}
