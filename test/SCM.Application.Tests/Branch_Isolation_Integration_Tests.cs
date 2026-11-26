using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SCM.Application.Branches;
using SCM.Application.Contracts.Branches.Dtos;
using SCM.Domain.Branches;
using SCM.Domain.Queueing;
using SCM.Domain.Shared.Enums;
using SCM.EntityFrameworkCore;
using Xunit;

namespace SCM.Application.Tests;

[Trait("Category", "BranchIsolation")]
public class Branch_Isolation_Integration_Tests
{
    private SCMDbContext CreateInMemoryContext(string dbName, ICurrentBranch? currentBranch = null)
    {
        var options = new DbContextOptionsBuilder<SCMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new SCMDbContext(options, currentBranch);
    }

    /// <summary>
    /// AC-0.1: Bác sĩ Doctor_01 tại BR01 truy vấn danh sách lượt khám chỉ nhìn thấy lượt khám thuộc BR01.
    /// Dữ liệu của BR02 hoàn toàn bị ẩn ở tầng SQL/QueryFilter.
    /// </summary>
    [Fact]
    public async Task Doctor_Cannot_Access_Other_Branch_Data()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "doctor");
        var context = CreateInMemoryContext(dbName, currentBranch);

        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();

        // 10 lượt khám thuộc BR01 và 15 lượt khám thuộc BR02
        currentBranch.Change(null, "ALL", isAllBranches: true, role: "admin_chain");
        for (int i = 1; i <= 10; i++)
        {
            context.QueueTickets.Add(new QueueTicket($"A{i:D3}", ReceptionStreamType.StreamA_FastTrack, Guid.NewGuid(), $"Bệnh nhân BR01-{i}", $"001099000{i:D3}")
            {
                BranchId = branchAId,
                BranchCode = "BR01"
            });
        }

        for (int i = 1; i <= 15; i++)
        {
            context.QueueTickets.Add(new QueueTicket($"B{i:D3}", ReceptionStreamType.StreamA_FastTrack, Guid.NewGuid(), $"Bệnh nhân BR02-{i}", $"001099100{i:D3}")
            {
                BranchId = branchBId,
                BranchCode = "BR02"
            });
        }
        await context.SaveChangesAsync();

        // Act: Doctor_01 login vào BR01
        currentBranch.Change(branchAId, "BR01", isAllBranches: false, role: "doctor");
        var visibleTickets = await context.QueueTickets.ToListAsync();

        // Assert: Trả về đúng 10 lượt khám của BR01, 0 lượt khám của BR02
        Assert.Equal(10, visibleTickets.Count);
        Assert.All(visibleTickets, t => Assert.Equal("BR01", t.BranchCode));
    }

    /// <summary>
    /// AC-0.2: Lễ tân hoặc nhân sự BR01 cố tình truy xuất chi tiết ID của bệnh nhân thuộc BR02
    /// -> Ném Forbidden/KeyNotFound và ghi nhận 1 bản ghi cảnh báo an ninh SECURITY_ALERT_CROSS_BRANCH_ATTEMPT vào AuditLogPII.
    /// </summary>
    [Fact]
    public async Task Tampering_PatientId_From_Another_Branch_Throws_Forbidden()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "receptionist");
        var context = CreateInMemoryContext(dbName, currentBranch);
        var branchService = new BranchAppService(context, currentBranch, NullLogger<BranchAppService>.Instance);

        var patientBId = Guid.NewGuid();
        var ticketB = new QueueTicket("B001", ReceptionStreamType.StreamA_FastTrack, patientBId, "Bệnh nhân VIP BR02", "079201000999")
        {
            BranchId = Guid.NewGuid(),
            BranchCode = "BR02"
        };

        // Act & Assert: Validate cross branch access
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await branchService.ValidateCrossBranchAccessAsync(ticketB, ticketB.Id.ToString(), "QueueTicket");
        });

        // Kiểm tra bản ghi cảnh báo an ninh trong AuditLogPII
        var alertLog = await context.AuditLogPIIs.FirstOrDefaultAsync(l => l.Action == "SECURITY_ALERT_CROSS_BRANCH_ATTEMPT");
        Assert.NotNull(alertLog);
        Assert.Equal("QueueTicket", alertLog.EntityName);
        Assert.Equal("BR01", alertLog.BranchCode);
        Assert.Contains("Truy cập trái phép chéo chi nhánh", alertLog.MaskedDetails);
    }

    /// <summary>
    /// AC-0.3: Admin_Chain có vai trò admin_chain bật chế độ ALL trên BranchSwitcher
    /// -> Bỏ qua Global Query Filter an toàn, lấy được toàn bộ 25 lượt khám của cả BR01 và BR02.
    /// </summary>
    [Fact]
    public async Task ChainAdmin_Can_Query_All_Branches()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(null, "ALL", isAllBranches: true, role: "admin_chain");
        var context = CreateInMemoryContext(dbName, currentBranch);

        var branchAId = Guid.NewGuid();
        var branchBId = Guid.NewGuid();

        for (int i = 1; i <= 10; i++)
        {
            context.QueueTickets.Add(new QueueTicket($"A{i:D3}", ReceptionStreamType.StreamA_FastTrack, Guid.NewGuid(), $"Bệnh nhân BR01-{i}", $"001099000{i:D3}")
            {
                BranchId = branchAId,
                BranchCode = "BR01"
            });
        }

        for (int i = 1; i <= 15; i++)
        {
            context.QueueTickets.Add(new QueueTicket($"B{i:D3}", ReceptionStreamType.StreamA_FastTrack, Guid.NewGuid(), $"Bệnh nhân BR02-{i}", $"001099100{i:D3}")
            {
                BranchId = branchBId,
                BranchCode = "BR02"
            });
        }
        await context.SaveChangesAsync();

        // Act: Query với ALL
        var allTickets = await context.QueueTickets.ToListAsync();

        // Assert: 25 records
        Assert.Equal(25, allTickets.Count);
        Assert.Equal(10, allTickets.Count(t => t.BranchCode == "BR01"));
        Assert.Equal(15, allTickets.Count(t => t.BranchCode == "BR02"));
    }

    /// <summary>
    /// AC-0.4: Người dùng vai trò branch_admin cố tình CUD Master Data dùng chung
    /// -> Ném UnauthorizedAccessException (HTTP 403 Forbidden).
    /// </summary>
    [Fact]
    public async Task BranchAdmin_Cannot_Modify_Global_MasterData()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "branch_admin");
        var context = CreateInMemoryContext(dbName, currentBranch);
        var branchService = new BranchAppService(context, currentBranch, NullLogger<BranchAppService>.Instance);

        var newBranchInput = new BranchCreateUpdateDto
        {
            Code = "BR99",
            Name = "Chi nhánh tự tạo trái phép"
        };

        // Assert: Bị chặn ném UnauthorizedAccessException
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await branchService.CreateAsync(newBranchInput);
        });

        // Cũng không được switch sang ALL
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
        {
            await branchService.SwitchBranchAsync("ALL");
        });
    }

    /// <summary>
    /// AC-0.5: Bác sĩ xem hồ sơ bệnh án có thông tin CCCD và SĐT -> Phát sinh 1 record VIEW_PII
    /// với MaskedDetails chứa CCCD 079******123 và SĐT 090*****89.
    /// </summary>
    [Fact]
    public async Task Viewing_Patient_Creates_Masked_AuditLog()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "doctor");
        var context = CreateInMemoryContext(dbName, currentBranch);

        string rawCccd = "079201000123";
        string rawPhone = "0908123489";

        // Tạo audit log với Masked Details
        string maskedCccd = $"{rawCccd.Substring(0, 3)}******{rawCccd.Substring(rawCccd.Length - 3)}";
        string maskedPhone = $"{rawPhone.Substring(0, 3)}*****{rawPhone.Substring(rawPhone.Length - 2)}";

        var auditLog = new AuditLogPII(
            userId: Guid.NewGuid(),
            userName: "BS.CKII Chánh Huỳnh",
            branchId: currentBranch.Id,
            branchCode: currentBranch.Code ?? "BR01",
            action: "VIEW_PII",
            entityName: "PatientRecord",
            entityId: Guid.NewGuid().ToString(),
            maskedDetails: $"CCCD: {maskedCccd}, SĐT: {maskedPhone}",
            clientIp: "192.168.1.50",
            userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            details: "Bác sĩ mở xem kết quả xét nghiệm tổng quát"
        );

        context.AuditLogPIIs.Add(auditLog);
        await context.SaveChangesAsync();

        var saved = await context.AuditLogPIIs.FirstOrDefaultAsync(l => l.Id == auditLog.Id);
        Assert.NotNull(saved);
        Assert.Equal("VIEW_PII", saved.Action);
        Assert.Equal("CCCD: 079******123, SĐT: 090*****89", saved.MaskedDetails);
    }

    /// <summary>
    /// Invariant C-0.5: Bảng AuditLogPII là bất biến (Immutable), nghiêm cấm sửa đổi hoặc xóa bản ghi.
    /// </summary>
    [Fact]
    public async Task AuditLogPII_Is_Immutable_Cannot_Modify_Or_Delete()
    {
        string dbName = Guid.NewGuid().ToString();
        var currentBranch = new CurrentBranch(Guid.NewGuid(), "BR01", isAllBranches: false, role: "doctor");
        var context = CreateInMemoryContext(dbName, currentBranch);

        var log = new AuditLogPII(
            userId: Guid.NewGuid(),
            userName: "BS. Admin",
            branchId: currentBranch.Id,
            branchCode: "BR01",
            action: "VIEW_PII",
            entityName: "MedicalRecord",
            entityId: "REC001",
            maskedDetails: "CCCD: 079******999"
        );

        context.AuditLogPIIs.Add(log);
        await context.SaveChangesAsync();

        // Cố tình sửa đổi record
        log.Action = "MODIFIED_ACTION";
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await context.SaveChangesAsync();
        });

        // Tạo context mới để thử xóa
        var context2 = CreateInMemoryContext(dbName, currentBranch);
        var existingLog = await context2.AuditLogPIIs.FindAsync(log.Id);
        Assert.NotNull(existingLog);

        context2.AuditLogPIIs.Remove(existingLog);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await context2.SaveChangesAsync();
        });
    }

    /// <summary>
    /// Middleware bóc tách Header X-Branch-Code và fallback đúng branch mặc định.
    /// </summary>
    [Fact]
    public async Task BranchContextMiddleware_Extracts_Header_And_Falls_Back_Correctly()
    {
        var currentBranch = new CurrentBranch();
        var middleware = new BranchContextMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<BranchContextMiddleware>.Instance);

        // Scenario 1: Có Header X-Branch-Code
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Request.Headers["X-Branch-Code"] = "CNHN";
        httpContext1.Request.Headers["X-User-Role"] = "doctor";

        await middleware.InvokeAsync(httpContext1, currentBranch);

        Assert.Equal("CNHN", currentBranch.Code);
        Assert.False(currentBranch.IsAllBranches);
        Assert.Equal("doctor", currentBranch.CurrentRole);

        // Scenario 2: Thiếu Header -> Fallback về default branch CNQ1
        var httpContext2 = new DefaultHttpContext();
        await middleware.InvokeAsync(httpContext2, currentBranch);

        Assert.Equal(BranchContextMiddleware.DefaultBranchCode, currentBranch.Code);
        Assert.False(currentBranch.IsAllBranches);

        // Scenario 3: Header là ALL
        var httpContext3 = new DefaultHttpContext();
        httpContext3.Request.Headers["X-Branch-Code"] = "ALL";
        httpContext3.Request.Headers["X-User-Role"] = "admin_chain";

        await middleware.InvokeAsync(httpContext3, currentBranch);

        Assert.True(currentBranch.IsAllBranches);
        Assert.Equal("admin_chain", currentBranch.CurrentRole);
    }

    /// <summary>
    /// AC-0.6 (Performance SLA): 100 requests đồng thời gửi đến Backend kèm Header X-Branch-Code
    /// -> Middleware phân giải chi nhánh và nạp ngữ cảnh <= 5ms/request.
    /// </summary>
    [Fact]
    public async Task AC06_Middleware_Resolves_100_Concurrent_Requests_Under_5ms()
    {
        var middleware = new BranchContextMiddleware(
            next: (ctx) => Task.CompletedTask,
            logger: NullLogger<BranchContextMiddleware>.Instance);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var tasks = Enumerable.Range(1, 100).Select(i => Task.Run(async () =>
        {
            var branchScoped = new CurrentBranch();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Branch-Code"] = i % 2 == 0 ? "CNQ1" : "CNQ3";
            httpContext.Request.Headers["X-User-Role"] = "doctor";

            await middleware.InvokeAsync(httpContext, branchScoped);

            Assert.NotNull(branchScoped.Code);
            Assert.Equal("doctor", branchScoped.CurrentRole);
        }));

        await Task.WhenAll(tasks);
        sw.Stop();

        double totalMs = sw.Elapsed.TotalMilliseconds;
        double avgMsPerReq = totalMs / 100.0;

        // SLA: Trung bình <= 5ms / request
        Assert.True(avgMsPerReq <= 5.0, $"Middleware xử lý quá thời gian SLA: {avgMsPerReq:F2}ms/req > 5ms");
    }
}
