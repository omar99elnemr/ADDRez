using System.Security.Claims;
using ADDRez.Api.Data;

namespace ADDRez.Api.Middleware;

public class TenantScopeMiddleware
{
    private readonly RequestDelegate _next;

    public TenantScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var companyIdClaim = context.User.FindFirst("company_id");
        if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out var companyId))
        {
            db.SetTenantId(companyId);
        }

        await _next(context);
    }
}
