using System.Security.Claims;
using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Dashboard;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Permission("dashboard.view")]
    public async Task<IActionResult> Get()
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var yesterday = today.AddDays(-1);
        var weekStart = today.AddDays(-6);
        var prevWeekStart = weekStart.AddDays(-7);
        var prevWeekEnd = weekStart.AddDays(-1);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        // ── Today's reservations (with includes for detail) ──
        var todayReservations = await _db.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Table)
            .Include(r => r.TimeSlot)
            .Include(r => r.CreatedByUser)
            .Where(r => r.OutletId == outletId && r.Date == today)
            .OrderBy(r => r.Time)
            .ToListAsync();

        // ── Yesterday's reservations for comparison ──
        var yesterdayCount = await _db.Reservations
            .CountAsync(r => r.OutletId == outletId && r.Date == yesterday);
        var yesterdayCovers = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date == yesterday)
            .SumAsync(r => r.Covers);
        var yesterdayRevenue = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date == yesterday && r.DepositAmount != null)
            .SumAsync(r => r.DepositAmount ?? 0);

        // ── This week data ──
        var thisWeekReservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date >= weekStart && r.Date <= today)
            .ToListAsync();
        var prevWeekReservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date >= prevWeekStart && r.Date <= prevWeekEnd)
            .ToListAsync();

        var weekRevenue = thisWeekReservations.Sum(r => r.DepositAmount ?? 0);
        var prevWeekRevenue = prevWeekReservations.Sum(r => r.DepositAmount ?? 0);

        // ── Monthly revenue ──
        var monthlyRevenue = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date >= monthStart && r.Date <= today && r.DepositAmount != null)
            .SumAsync(r => r.DepositAmount ?? 0);

        // ── New customers this week ──
        var newCustomersThisWeek = await _db.Customers
            .CountAsync(c => c.CreatedAt >= weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                          && c.CreatedAt <= today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
        var newCustomersPrevWeek = await _db.Customers
            .CountAsync(c => c.CreatedAt >= prevWeekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                          && c.CreatedAt <= prevWeekEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));

        // ── Total reservation stats (all time for this outlet) ──
        var totalReservations = await _db.Reservations.CountAsync(r => r.OutletId == outletId);
        var totalCancelled = await _db.Reservations.CountAsync(r => r.OutletId == outletId && r.Status == ReservationStatus.Cancelled);

        // ── Build KPIs ──
        var todayRevenue = todayReservations.Sum(r => r.DepositAmount ?? 0);
        var todayExpectedVisitors = todayReservations
            .Where(r => r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.NoShow)
            .Sum(r => r.Covers);

        var kpis = new DashboardKpis(
            TodayReservations: todayReservations.Count,
            TodayReservationsChange: CalcChange(todayReservations.Count, yesterdayCount),
            TodayExpectedVisitors: todayExpectedVisitors,
            TodayExpectedVisitorsChange: CalcChange(todayExpectedVisitors, yesterdayCovers),
            TodayCovers: todayReservations.Sum(r => r.Covers),
            NewCustomersThisWeek: newCustomersThisWeek,
            NewCustomersChange: CalcChange(newCustomersThisWeek, newCustomersPrevWeek),
            TodayRevenue: todayRevenue,
            TodayRevenueChange: CalcChange((double)todayRevenue, (double)yesterdayRevenue),
            WeekRevenue: weekRevenue,
            WeekRevenueChange: CalcChange((double)weekRevenue, (double)prevWeekRevenue),
            PendingConfirmation: todayReservations.Count(r => r.Status == ReservationStatus.Pending),
            CheckedIn: todayReservations.Count(r => r.Status == ReservationStatus.CheckedIn),
            Seated: todayReservations.Count(r => r.Status == ReservationStatus.Seated),
            NoShows: todayReservations.Count(r => r.Status == ReservationStatus.NoShow),
            Cancelled: todayReservations.Count(r => r.Status == ReservationStatus.Cancelled),
            TotalReservations: totalReservations,
            TotalCancelled: totalCancelled,
            MonthlyRevenue: monthlyRevenue
        );

        // ── 7-day chart with walk-in breakdown ──
        var weekChartRaw = thisWeekReservations
            .GroupBy(r => r.Date)
            .Select(g => new
            {
                Date = g.Key,
                Reservations = g.Count(r => r.Method != ReservationMethod.WalkIn),
                WalkIns = g.Count(r => r.Method == ReservationMethod.WalkIn),
                Covers = g.Sum(r => r.Covers)
            })
            .ToList();

        // Fill all 7 days
        var weekChart = new List<WeekChartPoint>();
        for (int i = 6; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            var entry = weekChartRaw.FirstOrDefault(w => w.Date == d);
            weekChart.Add(new WeekChartPoint(
                d.ToString("yyyy-MM-dd"),
                d.ToString("ddd"),
                entry?.Reservations ?? 0,
                entry?.WalkIns ?? 0,
                entry?.Covers ?? 0
            ));
        }

        // ── Top 5 customers ──
        var topCustomers = await _db.Customers
            .Include(c => c.ClientCategory)
            .OrderByDescending(c => c.TotalSpend)
            .Take(5)
            .Select(c => new TopCustomerDto(
                c.Id, c.FullName, c.TotalVisits, c.TotalSpend,
                c.ClientCategory != null ? c.ClientCategory.Name : null,
                c.ClientCategory != null ? c.ClientCategory.Color : null
            ))
            .ToListAsync();

        // ── Unconfirmed queue (today) ──
        var unconfirmed = todayReservations
            .Where(r => r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.Time)
            .Take(10)
            .Select(r => new ReservationSummaryDto(
                r.Id,
                r.Customer?.FullName ?? r.GuestName,
                r.Covers,
                r.Date.ToString("yyyy-MM-dd"),
                r.Time.ToString("HH:mm"),
                r.Status.Label(),
                r.Table?.Name
            ))
            .ToList();

        // ── Per-outlet breakdown ──
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var userOutlets = await _db.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Outlets)
            .Where(o => o.IsActive)
            .ToListAsync();

        var outletIds = userOutlets.Select(o => o.Id).ToList();
        var outletResCounts = await _db.Reservations
            .Where(r => outletIds.Contains(r.OutletId))
            .GroupBy(r => new { r.OutletId, IsToday = r.Date == today })
            .Select(g => new { g.Key.OutletId, g.Key.IsToday, Count = g.Count() })
            .ToListAsync();

        var outletStats = userOutlets.Select(o => new OutletStatDto(
            o.Id,
            o.Name,
            outletResCounts.Where(x => x.OutletId == o.Id).Sum(x => x.Count),
            outletResCounts.Where(x => x.OutletId == o.Id && x.IsToday).Sum(x => x.Count)
        )).ToList();

        // ── Status distribution (this month) ──
        var monthReservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date >= monthStart && r.Date <= today)
            .ToListAsync();

        var statusDist = new StatusDistribution(
            Confirmed: monthReservations.Count(r => r.Status == ReservationStatus.Confirmed),
            Pending: monthReservations.Count(r => r.Status == ReservationStatus.Pending),
            Seated: monthReservations.Count(r => r.Status == ReservationStatus.Seated),
            CheckedIn: monthReservations.Count(r => r.Status == ReservationStatus.CheckedIn),
            CheckedOut: monthReservations.Count(r => r.Status == ReservationStatus.CheckedOut),
            NoShow: monthReservations.Count(r => r.Status == ReservationStatus.NoShow),
            Cancelled: monthReservations.Count(r => r.Status == ReservationStatus.Cancelled)
        );

        // ── Today's full reservation list ──
        var outletName = userOutlets.FirstOrDefault(o => o.Id == outletId)?.Name ?? "Outlet";
        var todayList = todayReservations.Select(r => new TodayReservationDto(
            r.Id,
            r.ConfirmationCode,
            outletName,
            r.Customer?.FullName ?? r.GuestName,
            r.Customer?.Phone ?? r.GuestPhone,
            r.Covers,
            r.Time.ToString("HH:mm"),
            r.Status.Label(),
            r.Status.Color(),
            r.Table?.Name,
            r.TimeSlot?.Name,
            r.Method.Label()
        )).ToList();

        // ── Future unconfirmed reservations ──
        var futureUnconfirmed = await _db.Reservations
            .Include(r => r.Customer)
            .Include(r => r.TimeSlot)
            .Include(r => r.CreatedByUser)
            .Where(r => r.OutletId == outletId && r.Date > today && r.Status == ReservationStatus.Pending)
            .OrderBy(r => r.Date).ThenBy(r => r.Time)
            .Take(20)
            .ToListAsync();

        var futureList = futureUnconfirmed.Select(r => new FutureUnconfirmedDto(
            r.Id,
            outletName,
            r.Date.ToString("MMM d, yyyy"),
            r.TimeSlot?.Name,
            r.Customer?.FullName ?? r.GuestName,
            r.Customer?.Phone ?? r.GuestPhone,
            r.Covers,
            r.Type.ToString(),
            r.Notes,
            r.CreatedByUser?.FullName ?? "System"
        )).ToList();

        return Ok(new DashboardResponse(kpis, weekChart, topCustomers, unconfirmed, outletStats, statusDist, todayList, futureList));
    }

    private static double CalcChange(double current, double previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return Math.Round((current - previous) / previous * 100, 1);
    }

    private int? GetOutletId()
    {
        if (HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id)
            return id;
        return null;
    }
}
