using System.Security.Claims;
using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Common;
using ADDRez.Api.DTOs.Reservations;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReservationsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Permission("reservations.view")]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery q, [FromQuery] string? status, [FromQuery] string? date, [FromQuery] int? timeSlotId)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var query = _db.Reservations
            .Include(r => r.Customer)
            .Include(r => r.Table)
            .Include(r => r.TimeSlot)
            .Include(r => r.Tags)
            .Where(r => r.OutletId == outletId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, true, out var s))
            query = query.Where(r => r.Status == s);

        if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var d))
            query = query.Where(r => r.Date == d);

        if (timeSlotId.HasValue)
            query = query.Where(r => r.TimeSlotId == timeSlotId);

        if (!string.IsNullOrEmpty(q.Search))
            query = query.Where(r =>
                (r.GuestName != null && r.GuestName.Contains(q.Search)) ||
                (r.Customer != null && (r.Customer.FirstName.Contains(q.Search) || r.Customer.LastName.Contains(q.Search))) ||
                (r.ConfirmationCode != null && r.ConfirmationCode.Contains(q.Search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Date).ThenBy(r => r.Time)
            .Skip((q.Page - 1) * q.PerPage).Take(q.PerPage)
            .Select(r => new ReservationListDto(
                r.Id, r.GuestName ?? (r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName : null),
                r.GuestEmail, r.GuestPhone, r.CustomerId,
                r.Customer != null ? r.Customer.FirstName + " " + r.Customer.LastName : null,
                r.Covers, r.Date.ToString("yyyy-MM-dd"), r.Time.ToString("HH:mm"),
                r.DurationMinutes, r.Status, r.Type, r.SeatingType, r.Method, r.Notes,
                r.TableId, r.Table != null ? r.Table.Name : null,
                r.TimeSlotId, r.TimeSlot != null ? r.TimeSlot.Name : null,
                r.ConfirmationCode,
                r.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)),
                r.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<ReservationListDto>(items, q.Page, (int)Math.Ceiling((double)total / q.PerPage), total, q.PerPage));
    }

    [HttpGet("{id}")]
    [Permission("reservations.view")]
    public async Task<IActionResult> Show(int id)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Customer).Include(r => r.Table).Include(r => r.TimeSlot).Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound(new { message = "Reservation not found" });

        return Ok(MapToDto(reservation));
    }

    [HttpPost]
    [Permission("reservations.create")]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var reservation = new Reservation
        {
            CompanyId = companyId,
            OutletId = outletId.Value,
            CustomerId = request.CustomerId,
            GuestName = request.GuestName,
            GuestEmail = request.GuestEmail,
            GuestPhone = request.GuestPhone,
            GuestDateOfBirth = !string.IsNullOrEmpty(request.GuestDateOfBirth) ? DateOnly.Parse(request.GuestDateOfBirth) : null,
            GuestGender = request.GuestGender,
            MembershipNo = request.MembershipNo,
            RoomNo = request.RoomNo,
            Covers = request.Covers,
            Date = DateOnly.Parse(request.Date),
            Time = TimeOnly.Parse(request.Time),
            DurationMinutes = request.DurationMinutes,
            Type = request.Type,
            SeatingType = request.SeatingType,
            Method = request.Method,
            Notes = request.Notes,
            SpecialRequests = request.SpecialRequests,
            DepositAmount = request.DepositAmount,
            DiscountPercent = request.DiscountPercent,
            DiscountReason = request.DiscountReason,
            TimeSlotId = request.TimeSlotId,
            TableId = request.TableId,
            CreatedByUserId = userId,
            Status = ReservationStatus.Pending,
            ConfirmationCode = GenerateConfirmationCode()
        };

        if (request.TagIds?.Length > 0)
        {
            var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToListAsync();
            reservation.Tags = tags;
        }

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        // Create status history entry
        _db.ReservationStatusHistories.Add(new ReservationStatusHistory
        {
            ReservationId = reservation.Id,
            UserId = userId,
            FromStatus = ReservationStatus.Pending,
            ToStatus = ReservationStatus.Pending,
            Notes = "Reservation created"
        });
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Show), new { id = reservation.Id }, new { id = reservation.Id, confirmation_code = reservation.ConfirmationCode });
    }

    [HttpPut("{id}")]
    [Permission("reservations.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReservationRequest request)
    {
        var reservation = await _db.Reservations.Include(r => r.Tags).FirstOrDefaultAsync(r => r.Id == id);
        if (reservation == null) return NotFound(new { message = "Reservation not found" });

        reservation.CustomerId = request.CustomerId;
        reservation.GuestName = request.GuestName;
        reservation.GuestEmail = request.GuestEmail;
        reservation.GuestPhone = request.GuestPhone;
        reservation.GuestDateOfBirth = !string.IsNullOrEmpty(request.GuestDateOfBirth) ? DateOnly.Parse(request.GuestDateOfBirth) : null;
        reservation.GuestGender = request.GuestGender;
        reservation.MembershipNo = request.MembershipNo;
        reservation.RoomNo = request.RoomNo;
        reservation.Covers = request.Covers;
        reservation.Date = DateOnly.Parse(request.Date);
        reservation.Time = TimeOnly.Parse(request.Time);
        reservation.DurationMinutes = request.DurationMinutes;
        reservation.Type = request.Type;
        reservation.Method = request.Method;
        reservation.Notes = request.Notes;
        reservation.SpecialRequests = request.SpecialRequests;
        reservation.DepositAmount = request.DepositAmount;
        reservation.DiscountPercent = request.DiscountPercent;
        reservation.DiscountReason = request.DiscountReason;
        reservation.TimeSlotId = request.TimeSlotId;
        reservation.TableId = request.TableId;

        if (request.TagIds != null)
        {
            reservation.Tags.Clear();
            var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToListAsync();
            foreach (var tag in tags) reservation.Tags.Add(tag);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Reservation updated" });
    }

    [HttpDelete("{id}")]
    [Permission("reservations.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation == null) return NotFound(new { message = "Reservation not found" });

        _db.Reservations.Remove(reservation);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Reservation deleted" });
    }

    [HttpPost("{id}/status")]
    [Permission("reservations.status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation == null) return NotFound(new { message = "Reservation not found" });

        if (!reservation.Status.CanTransitionTo(request.Status))
            return BadRequest(new { message = $"Cannot transition from {reservation.Status.Label()} to {request.Status.Label()}" });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var oldStatus = reservation.Status;
        reservation.Status = request.Status;

        // Set timestamps
        switch (request.Status)
        {
            case ReservationStatus.CheckedIn:
                reservation.CheckedInAt = DateTime.UtcNow;
                break;
            case ReservationStatus.Seated:
                reservation.SeatedAt = DateTime.UtcNow;
                break;
            case ReservationStatus.CheckedOut:
                reservation.CheckedOutAt = DateTime.UtcNow;
                break;
            case ReservationStatus.Cancelled:
                reservation.CancelledAt = DateTime.UtcNow;
                reservation.CancellationReason = request.Notes;
                break;
        }

        _db.ReservationStatusHistories.Add(new ReservationStatusHistory
        {
            ReservationId = id,
            UserId = userId,
            FromStatus = oldStatus,
            ToStatus = request.Status,
            Notes = request.Notes
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Status changed to {request.Status.Label()}" });
    }

    [HttpPost("{id}/assign-table")]
    [Permission("reservations.assign_table")]
    public async Task<IActionResult> AssignTable(int id, [FromBody] AssignTableRequest request)
    {
        var reservation = await _db.Reservations.FindAsync(id);
        if (reservation == null) return NotFound(new { message = "Reservation not found" });

        reservation.TableId = request.TableId;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Table assigned" });
    }

    [HttpGet("calendar")]
    [Permission("reservations.view")]
    public async Task<IActionResult> Calendar([FromQuery] CalendarQuery q)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var startDate = !string.IsNullOrEmpty(q.StartDate) ? DateOnly.Parse(q.StartDate) : DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = !string.IsNullOrEmpty(q.EndDate) ? DateOnly.Parse(q.EndDate) : startDate.AddDays(7);

        var query = _db.Reservations
            .Include(r => r.Customer).Include(r => r.Table)
            .Where(r => r.OutletId == outletId && r.Date >= startDate && r.Date <= endDate);

        if (q.TimeSlotId.HasValue)
            query = query.Where(r => r.TimeSlotId == q.TimeSlotId);

        var reservations = await query.OrderBy(r => r.Date).ThenBy(r => r.Time).ToListAsync();

        var days = reservations.GroupBy(r => r.Date).Select(g => new CalendarDayDto(
            g.Key.ToString("yyyy-MM-dd"),
            g.Select(r => new CalendarReservationDto(
                r.Id,
                r.Customer?.FullName ?? r.GuestName,
                r.Covers,
                r.Time.ToString("HH:mm"),
                r.Status,
                r.SeatingType,
                r.Table?.Name
            ))
        ));

        return Ok(days);
    }

    [HttpGet("capacity")]
    [Permission("floor_plan.view")]
    public async Task<IActionResult> Capacity([FromQuery] int? timeSlotId)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date == today);

        if (timeSlotId.HasValue)
            query = query.Where(r => r.TimeSlotId == timeSlotId);

        var reservations = await query.ToListAsync();
        var activeStatuses = new[] { ReservationStatus.Confirmed, ReservationStatus.CheckedIn, ReservationStatus.Seated };

        var active = reservations.Where(r => activeStatuses.Contains(r.Status)).ToList();
        var totalTables = await _db.Tables
            .Include(t => t.FloorPlan).ThenInclude(fp => fp.Layout)
            .Where(t => t.FloorPlan.Layout.OutletId == outletId && t.IsActive)
            .CountAsync();

        return Ok(new CapacityDto(
            TotalSeated: active.Where(r => r.SeatingType == Entities.Enums.SeatingType.Seated).Sum(r => r.Covers),
            TotalStanding: active.Where(r => r.SeatingType == Entities.Enums.SeatingType.Standing).Sum(r => r.Covers),
            TablesReserved: active.Count(r => r.TableId != null),
            TotalTables: totalTables,
            AttendedCustomers: reservations.Count(r => r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Seated || r.Status == ReservationStatus.CheckedOut),
            WalkInCustomers: reservations.Count(r => r.Method == Entities.Enums.ReservationMethod.WalkIn),
            TotalCovers: active.Sum(r => r.Covers),
            PendingCount: reservations.Count(r => r.Status == ReservationStatus.Pending)
        ));
    }

    [HttpPost("move")]
    [Permission("reservations.mass_actions")]
    public async Task<IActionResult> MoveReservations([FromBody] MoveReservationsRequest request)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        if (!DateOnly.TryParse(request.FromDate, out var fromDate))
            return BadRequest(new { message = "Invalid date format" });

        var statuses = new List<ReservationStatus> { ReservationStatus.Confirmed };
        if (request.IncludePending) statuses.Add(ReservationStatus.Pending);

        var reservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date == fromDate &&
                   r.TimeSlotId == request.FromTimeSlotId && statuses.Contains(r.Status))
            .ToListAsync();

        if (reservations.Count == 0)
            return BadRequest(new { message = "No reservations found to move" });

        foreach (var r in reservations)
        {
            r.TimeSlotId = request.ToTimeSlotId;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"{reservations.Count} reservations moved", count = reservations.Count });
    }

    [HttpGet("search-guests")]
    [Permission("reservations.create")]
    public async Task<IActionResult> SearchGuests([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());

        var customers = await _db.Customers
            .Where(c => c.FirstName.Contains(q) || c.LastName.Contains(q) ||
                   (c.Phone != null && c.Phone.Contains(q)) ||
                   (c.Email != null && c.Email.Contains(q)))
            .OrderBy(c => c.FirstName)
            .Take(10)
            .Select(c => new { c.Id, c.FirstName, c.LastName, FullName = c.FirstName + " " + c.LastName, c.Phone, c.Email, c.Status })
            .ToListAsync();

        return Ok(customers);
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;

    private static string GenerateConfirmationCode() =>
        $"ADR-{Random.Shared.Next(10000, 99999)}";

    private static ReservationListDto MapToDto(Reservation r) => new(
        r.Id, r.GuestName ?? r.Customer?.FullName,
        r.GuestEmail, r.GuestPhone, r.CustomerId, r.Customer?.FullName,
        r.Covers, r.Date.ToString("yyyy-MM-dd"), r.Time.ToString("HH:mm"),
        r.DurationMinutes, r.Status, r.Type, r.SeatingType, r.Method, r.Notes,
        r.TableId, r.Table?.Name, r.TimeSlotId, r.TimeSlot?.Name,
        r.ConfirmationCode,
        r.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)),
        r.CreatedAt
    );
}
