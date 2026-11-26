using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SCM.Domain.Branches;

namespace SCM.Application.Branches;

public class BranchContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BranchContextMiddleware> _logger;

    public const string BranchHeaderKey = "X-Branch-Code";
    public const string DefaultBranchCode = "CNQ1";

    public BranchContextMiddleware(RequestDelegate next, ILogger<BranchContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentBranch currentBranch)
    {
        string? branchCode = null;

        // 1. Check HTTP Header
        if (context.Request.Headers.TryGetValue(BranchHeaderKey, out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            branchCode = headerVal.ToString().Trim().ToUpperInvariant();
        }

        // 2. Fallback to Claim in JWT if present
        if (string.IsNullOrEmpty(branchCode) && context.User?.Identity?.IsAuthenticated == true)
        {
            branchCode = context.User.FindFirst("branch_code")?.Value?.Trim().ToUpperInvariant();
        }

        // 3. Fallback to Default Branch
        if (string.IsNullOrEmpty(branchCode))
        {
            branchCode = DefaultBranchCode;
        }

        // Extract role claim if present, otherwise header or default
        var role = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? context.Request.Headers["X-User-Role"].FirstOrDefault()
            ?? "admin_chain";

        bool isAll = string.Equals(branchCode, "ALL", StringComparison.OrdinalIgnoreCase);
        currentBranch.Change(null, branchCode, isAllBranches: isAll, role: role);

        _logger.LogDebug("BranchContext initialized: Code={BranchCode}, Role={Role}, IsAll={IsAll}", branchCode, role, isAll);

        await _next(context);
    }
}
