using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    [Permission("reports.view")]
    public async Task<IActionResult> Summary([FromQuery] string? dateFrom, [FromQuery] string? dateTo)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var from = !string.IsNullOrEmpty(dateFrom) ? DateOnly.Parse(dateFrom) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = !string.IsNullOrEmpty(dateTo) ? DateOnly.Parse(dateTo) : DateOnly.FromDateTime(DateTime.UtcNow);

        var reservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date >= from && r.Date <= to)
            .ToListAsync();

        var totalReservations = reservations.Count;
        var totalCovers = reservations.Sum(r => r.Covers);
        var confirmed = reservations.Count(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Seated || r.Status == ReservationStatus.CheckedOut);
        var noShows = reservations.Count(r => r.Status == ReservationStatus.NoShow);
        var cancelled = reservations.Count(r => r.Status == ReservationStatus.Cancelled);
        var avgCovers = totalReservations > 0 ? (double)totalCovers / totalReservations : 0;

        var dailyBreakdown = reservations.GroupBy(r => r.Date).OrderBy(g => g.Key)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), reservations = g.Count(), covers = g.Sum(r => r.Covers) });

        var statusBreakdown = reservations.GroupBy(r => r.Status)
            .Select(g => new { status = g.Key.Label(), count = g.Count(), color = g.Key.Color() });

        var typeBreakdown = reservations.GroupBy(r => r.Type)
            .Select(g => new { type = g.Key.Label(), count = g.Count() });

        return Ok(new
        {
            period = new { from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd") },
            kpis = new { totalReservations, totalCovers, confirmed, noShows, cancelled, avgCovers = Math.Round(avgCovers, 1) },
            daily_breakdown = dailyBreakdown,
            status_breakdown = statusBreakdown,
            type_breakdown = typeBreakdown
        });
    }

    [HttpGet("reservations")]
    [Permission("reports.view")]
    public async Task<IActionResult> Reservations([FromQuery] string? dateFrom, [FromQuery] string? dateTo, [FromQuery] string? status)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var from = !string.IsNullOrEmpty(dateFrom) ? DateOnly.Parse(dateFrom) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = !string.IsNullOrEmpty(dateTo) ? DateOnly.Parse(dateTo) : DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _db.Reservations
            .Include(r => r.Customer).Include(r => r.Table).Include(r => r.TimeSlot)
            .Where(r => r.OutletId == outletId && r.Date >= from && r.Date <= to);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        var data = await query.OrderBy(r => r.Date).ThenBy(r => r.Time)
            .Select(r => new
            {
                r.Id, guest_name = r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName : r.GuestName,
                r.Covers, date = r.Date.ToString("yyyy-MM-dd"), time = r.Time.ToString("HH:mm"),
                status = r.Status.Label(), type = r.Type.Label(), method = r.Method.Label(),
                table = r.Table != null ? r.Table.Name : (string?)null,
                time_slot = r.TimeSlot != null ? r.TimeSlot.Name : (string?)null
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("customers")]
    [Permission("reports.view")]
    public async Task<IActionResult> Customers([FromQuery] int? categoryId, [FromQuery] int top = 50)
    {
        var query = _db.Customers.Include(c => c.ClientCategory).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(c => c.ClientCategoryId == categoryId);

        var data = await query.OrderByDescending(c => c.TotalSpend).Take(top)
            .Select(c => new
            {
                c.Id, name = c.FirstName + " " + c.LastName, c.Email, c.Phone,
                category = c.ClientCategory != null ? c.ClientCategory.Name : (string?)null,
                c.TotalVisits, c.TotalSpend, c.NoShowCount, c.CancellationCount,
                last_visit = c.LastVisitAt
            })
            .ToListAsync();

        return Ok(data);
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;
}
