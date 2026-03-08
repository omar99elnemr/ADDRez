using ADDRez.Api.Data;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using ADDRez.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublicBookingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ReservationHub> _hub;

    public PublicBookingController(AppDbContext db, IHubContext<ReservationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet("outlets/{outletId}/slots")]
    public async Task<IActionResult> GetAvailableSlots(int outletId, [FromQuery] string? date)
    {
        var targetDate = !string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

        var slots = await _db.TimeSlots
            .IgnoreQueryFilters()
            .Where(ts => ts.OutletId == outletId && ts.IsActive)
            .OrderBy(ts => ts.StartTime)
            .Select(ts => new
            {
                ts.Id,
                ts.Name,
                StartTime = ts.StartTime.ToString("HH:mm"),
                EndTime = ts.EndTime.ToString("HH:mm"),
                ts.MaxReservations,
                CurrentCount = _db.Reservations
                    .IgnoreQueryFilters()
                    .Count(r => r.OutletId == outletId && r.TimeSlotId == ts.Id && r.Date == targetDate &&
                           r.Status != ReservationStatus.Cancelled && r.Status != ReservationStatus.NoShow)
            })
            .ToListAsync();

        return Ok(slots.Select(s => new
        {
            s.Id, s.Name, s.StartTime, s.EndTime,
            Available = s.MaxReservations - s.CurrentCount,
            IsFull = s.CurrentCount >= s.MaxReservations
        }));
    }

    [HttpPost("outlets/{outletId}/reserve")]
    public async Task<IActionResult> CreateReservation(int outletId, [FromBody] PublicReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { message = "Guest name is required" });
        if (string.IsNullOrWhiteSpace(request.Date) || !DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new { message = "Valid date is required" });
        if (string.IsNullOrWhiteSpace(request.Time) || !TimeOnly.TryParse(request.Time, out var time))
            return BadRequest(new { message = "Valid time is required" });
        if (request.Covers < 1)
            return BadRequest(new { message = "At least 1 cover is required" });

        var outlet = await _db.Outlets.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == outletId);
        if (outlet == null)
            return NotFound(new { message = "Outlet not found" });

        var reservation = new Reservation
        {
            CompanyId = outlet.CompanyId,
            OutletId = outletId,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            GuestPhone = request.GuestPhone,
            Covers = request.Covers,
            Date = date,
            Time = time,
            DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 90,
            Type = ReservationType.DineIn,
            SeatingType = SeatingType.Seated,
            Method = ReservationMethod.Online,
            Notes = request.Notes,
            SpecialRequests = request.SpecialRequests,
            TimeSlotId = request.TimeSlotId,
            Status = ReservationStatus.Pending,
            ConfirmationCode = $"ADR-{Random.Shared.Next(10000, 99999)}"
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        await _hub.NotifyReservationCreated(outletId, new
        {
            reservation.Id,
            reservation.GuestName,
            reservation.Covers,
            Date = reservation.Date.ToString("yyyy-MM-dd"),
            Time = reservation.Time.ToString("HH:mm"),
            Status = reservation.Status.ToString(),
            reservation.ConfirmationCode
        });

        return Ok(new
        {
            message = "Reservation created successfully!",
            reservation.ConfirmationCode,
            reservation.Id
        });
    }
}

public record PublicReservationRequest
{
    public string GuestName { get; init; } = string.Empty;
    public string? GuestEmail { get; init; }
    public string? GuestPhone { get; init; }
    public int Covers { get; init; } = 2;
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public int DurationMinutes { get; init; } = 90;
    public string? Notes { get; init; }
    public string? SpecialRequests { get; init; }
    public int? TimeSlotId { get; init; }
}
