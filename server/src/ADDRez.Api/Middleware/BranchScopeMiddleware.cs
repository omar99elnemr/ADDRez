using System.Security.Claims;
using ADDRez.Api.Data;

namespace ADDRez.Api.Middleware;

public class OutletScopeMiddleware
{
    private readonly RequestDelegate _next;

    public OutletScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.Request.Headers.TryGetValue("X-Outlet-Id", out var outletIdHeader)
            && int.TryParse(outletIdHeader.FirstOrDefault(), out var outletId))
        {
            // Verify user has access to this outlet
            var outletClaims = context.User.FindAll("outlet_id").Select(c => c.Value).ToList();
            if (outletClaims.Contains(outletId.ToString()))
            {
                db.SetOutletId(outletId);
                context.Items["OutletId"] = outletId;
            }
            else
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = "Access denied to this outlet" });
                return;
            }
        }

        await _next(context);
    }
}
