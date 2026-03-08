using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Common;
using ADDRez.Api.DTOs.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LogsController(AppDbContext db) => _db = db;

    [HttpGet("operations")]
    [Permission("operations.view_log")]
    public async Task<IActionResult> Operations([FromQuery] PaginationQuery q, [FromQuery] string? action, [FromQuery] string? entityType)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var query = _db.OperationsLogs
            .Include(l => l.User)
            .Where(l => l.OutletId == outletId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action == action);
        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((q.Page - 1) * q.PerPage).Take(q.PerPage)
            .Select(l => new OperationsLogDto(
                l.Id, l.Action, l.EntityType, l.EntityId, l.Description,
                l.User != null ? l.User.FirstName + " " + l.User.LastName : null,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<OperationsLogDto>(items, q.Page, (int)Math.Ceiling((double)total / q.PerPage), total, q.PerPage));
    }

    [HttpGet("changes")]
    [Permission("operations.view_changes")]
    public async Task<IActionResult> Changes([FromQuery] PaginationQuery q, [FromQuery] int? reservationId)
    {
        var query = _db.ChangesLogs.Include(l => l.User).AsQueryable();

        if (reservationId.HasValue)
            query = query.Where(l => l.ReservationId == reservationId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((q.Page - 1) * q.PerPage).Take(q.PerPage)
            .Select(l => new ChangesLogDto(
                l.Id, l.ReservationId, l.FieldName, l.OldValue, l.NewValue,
                l.User != null ? l.User.FirstName + " " + l.User.LastName : null,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<ChangesLogDto>(items, q.Page, (int)Math.Ceiling((double)total / q.PerPage), total, q.PerPage));
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;
}
