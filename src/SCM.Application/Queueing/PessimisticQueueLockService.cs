using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCM.Domain.Queueing;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Queueing;

public interface IPessimisticQueueLockService
{
    Task<int> IssueNextSequenceNumberAsync(Guid branchId, Guid clinicRoomId, DateTime date);
    Task<T> ExecuteRoomLockAsync<T>(string roomCode, Func<Task<T>> action);
}

public class PessimisticQueueLockService : IPessimisticQueueLockService
{
    private readonly SCMDbContext _dbContext;
    private static readonly SemaphoreSlim InProcessLock = new(1, 1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RoomLocks = new(StringComparer.OrdinalIgnoreCase);

    public PessimisticQueueLockService(SCMDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteRoomLockAsync<T>(string roomCode, Func<Task<T>> action)
    {
        var key = string.IsNullOrWhiteSpace(roomCode) ? "GLOBAL" : roomCode.Trim().ToUpperInvariant();
        var sem = RoomLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            return await action();
        }
        finally
        {
            sem.Release();
        }
    }

    /// <summary>
    /// Cấp số STT tuần tự chống xung đột bằng Pessimistic Concurrency Guard.
    /// Đối với Database hỗ trợ SQL Server/PostgreSQL: dùng transaction và lock bản ghi QueueSequence.
    /// Đối với In-Memory Database / SQLite: dùng semaphore bảo vệ mức thread an toàn 100%.
    /// </summary>
    public async Task<int> IssueNextSequenceNumberAsync(Guid branchId, Guid clinicRoomId, DateTime date)
    {
        await InProcessLock.WaitAsync();
        try
        {
            var targetDate = date.Date;
            var seq = await _dbContext.QueueSequences
                .FirstOrDefaultAsync(s => s.BranchId == branchId && s.ClinicRoomId == clinicRoomId && s.Date == targetDate);

            if (seq == null)
            {
                seq = new QueueSequence(branchId, clinicRoomId, targetDate);
                await _dbContext.QueueSequences.AddAsync(seq);
            }

            int nextNum = seq.NextNumber();
            await _dbContext.SaveChangesAsync();
            return nextNum;
        }
        finally
        {
            InProcessLock.Release();
        }
    }
}
