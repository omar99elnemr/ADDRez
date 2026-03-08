using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Reservations;
using ADDRez.Api.Entities.Enums;
using ADDRez.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GateCheckerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ReservationHub> _hub;

    public GateCheckerController(AppDbContext db, IHubContext<ReservationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Code is required" });

        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var reservation = await _db.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Table)
            .Include(r => r.TimeSlot)
            .Where(r => r.OutletId == outletId && r.Date == today &&
                   (r.ConfirmationCode == code || r.QrCode == code))
            .FirstOrDefaultAsync();

        if (reservation == null)
            return NotFound(new { message = "No reservation found for this code today" });

        return Ok(new
        {
            reservation.Id,
            GuestName = reservation.GuestName ?? reservation.Customer?.FullName,
            reservation.Covers,
            Time = reservation.Time.ToString("HH:mm"),
            Status = reservation.Status.ToString(),
            SeatingType = reservation.SeatingType.ToString(),
            TableName = reservation.Table?.Name,
            TimeSlotName = reservation.TimeSlot?.Name,
            reservation.ConfirmationCode,
            reservation.Notes,
            reservation.SpecialRequests,
            CanCheckIn = reservation.Status == ReservationStatus.Confirmed,
            IsAlreadyIn = reservation.Status == ReservationStatus.CheckedIn || reservation.Status == ReservationStatus.Seated
        });
    }

    [HttpPost("{id}/checkin")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var reservation = await _db.Reservations
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id && r.OutletId == outletId);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found" });

        if (reservation.Status != ReservationStatus.Confirmed)
            return BadRequest(new { message = $"Cannot check in — current status is {reservation.Status}" });

        reservation.Status = ReservationStatus.CheckedIn;
        reservation.CheckedInAt = DateTime.UtcNow;

        _db.ReservationStatusHistories.Add(new Entities.ReservationStatusHistory
        {
            ReservationId = id,
            FromStatus = ReservationStatus.Confirmed,
            ToStatus = ReservationStatus.CheckedIn,
            Notes = "Checked in via Gate Checker"
        });

        await _db.SaveChangesAsync();
        await _hub.NotifyStatusChanged(outletId.Value, id, "CheckedIn");

        return Ok(new
        {
            message = "Guest checked in successfully",
            guestName = reservation.GuestName ?? reservation.Customer?.FullName,
            reservation.Covers
        });
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;
}
