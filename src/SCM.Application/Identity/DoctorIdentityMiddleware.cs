using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SCM.Application.Contracts.Identity;
using SCM.Domain.Branches;
using SCM.EntityFrameworkCore;

namespace SCM.Application.Identity;

public class DoctorIdentityMiddleware
{
    public const string UserIdHeader = "X-User-Id";
    public const string UserNameHeader = "X-User-Name";

    private readonly RequestDelegate _next;

    public DoctorIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext httpContext,
        SCMDbContext db,
        ICurrentBranch currentBranch,
        ICurrentDoctorContext currentDoctor,
        RbacPermissionService rbacService)
    {
        if (!currentBranch.Id.HasValue &&
            !string.IsNullOrWhiteSpace(currentBranch.Code) &&
            !currentBranch.IsAllBranches)
        {
            var branch = await db.Branches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == currentBranch.Code);
            if (branch != null)
            {
                currentBranch.Change(branch.Id, branch.Code, currentBranch.IsAllBranches, currentBranch.CurrentRole);
            }
        }

        Guid? userId = null;
        if (httpContext.Request.Headers.TryGetValue(UserIdHeader, out var uidHeader) &&
            Guid.TryParse(uidHeader.FirstOrDefault(), out var parsedUid))
        {
            userId = parsedUid;
        }
        else if (httpContext.User?.Identity?.IsAuthenticated == true &&
                 Guid.TryParse(httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.FindFirst("user_id")?.Value, out var claimUid))
        {
            userId = claimUid;
        }

        var userName = httpContext.Request.Headers[UserNameHeader].FirstOrDefault()?.Trim()
            ?? httpContext.User?.Identity?.Name;

        if (userId.HasValue || !string.IsNullOrWhiteSpace(userName))
        {
            await rbacService.HydrateCurrentContextAsync(userId, userName, currentBranch, currentDoctor);
        }

        await _next(httpContext);
    }
}
