using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Queueing;
using SCM.Domain.Queueing;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

public class Queueing_Concurrency_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options);
    }

    /// <summary>
    /// AC-4.3: Concurrency Stress Test on Queue Ticket Issuance
    /// Given: 50 Kiosk đồng thời bấm cấp STT cho cùng Buồng khám ROOM_01 tại Chi nhánh BR01 trong vòng 500ms.
    /// When: 50 luồng chạy song song gọi IssueNextSequenceNumberAsync().
    /// Then: 100% 50 requests hoàn thành thành công (0 Deadlock, 0 Timeout).
    ///       50 số STT nhận được là tập hợp liên tục từ 1 đến 50 (0 số bị trùng lặp, 0 số bị nhảy cóc).
    /// </summary>
    [Fact]
    public async Task Fifty_Concurrent_Requests_Produce_Unique_Sequential_Tickets()
    {
        var dbName = nameof(Fifty_Concurrent_Requests_Produce_Unique_Sequential_Tickets) + Guid.NewGuid();
        var context = CreateInMemoryContext(dbName);
        var lockService = new PessimisticQueueLockService(context);

        var branchId = Guid.NewGuid();
        var clinicRoomId = Guid.NewGuid();
        var examDate = DateTime.UtcNow.Date;

        const int concurrentCount = 50;
        var generatedTickets = new ConcurrentBag<int>();

        var tasks = Enumerable.Range(0, concurrentCount).Select(async _ =>
        {
            int number = await lockService.IssueNextSequenceNumberAsync(branchId, clinicRoomId, examDate);
            generatedTickets.Add(number);
        });

        await Task.WhenAll(tasks);

        // Verify: Đúng 50 số được tạo
        Assert.Equal(concurrentCount, generatedTickets.Count);

        // Verify: Không có số trùng lặp
        var distinctCount = generatedTickets.Distinct().Count();
        Assert.Equal(concurrentCount, distinctCount);

        // Verify: Tập hợp số là liên tục từ 1 đến 50
        var orderedList = generatedTickets.OrderBy(x => x).ToList();
        for (int i = 0; i < concurrentCount; i++)
        {
            Assert.Equal(i + 1, orderedList[i]);
        }
    }
}
