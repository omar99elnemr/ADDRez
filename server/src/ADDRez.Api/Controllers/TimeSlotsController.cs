using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Settings;
using ADDRez.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/time-slots")]
[Authorize]
public class TimeSlotsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TimeSlotsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Permission("time_slots.view")]
    public async Task<IActionResult> Index()
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var slots = await _db.TimeSlots
            .Where(ts => ts.OutletId == outletId)
            .Include(ts => ts.Layout)
            .Include(ts => ts.CategoryExclusions)
            .OrderBy(ts => ts.StartTime)
            .Select(ts => new TimeSlotDto(
                ts.Id, ts.Name, ts.StartTime.ToString("HH:mm"), ts.EndTime.ToString("HH:mm"),
                ts.LayoutId, ts.Layout != null ? ts.Layout.Name : null,
                ts.Monday, ts.Tuesday, ts.Wednesday, ts.Thursday, ts.Friday, ts.Saturday, ts.Sunday,
                ts.StartDate.HasValue ? ts.StartDate.Value.ToString("yyyy-MM-dd") : null,
                ts.EndDate.HasValue ? ts.EndDate.Value.ToString("yyyy-MM-dd") : null,
                ts.MaxCovers, ts.MaxReservations, ts.TurnTimeMinutes, ts.GracePeriodMinutes,
                ts.RequireDeposit, ts.DepositAmountPerPerson, ts.IsActive,
                ts.CategoryExclusions.Select(ce => ce.ClientCategoryId)
            ))
            .ToListAsync();

        return Ok(slots);
    }

    [HttpGet("all")]
    [Permission("time_slots.view")]
    public async Task<IActionResult> All([FromQuery] int? outletId, [FromQuery] string? sort)
    {
        var query = _db.TimeSlots
            .Include(ts => ts.Layout)
            .Include(ts => ts.CategoryExclusions)
            .Include(ts => ts.Outlet)
            .AsQueryable();

        if (outletId.HasValue) query = query.Where(ts => ts.OutletId == outletId);

        query = sort switch
        {
            "name" => query.OrderBy(ts => ts.Name),
            "outlet" => query.OrderBy(ts => ts.Outlet!.Name).ThenBy(ts => ts.StartTime),
            _ => query.OrderBy(ts => ts.StartTime)
        };

        var slots = await query.Select(ts => new TimeSlotFullDto(
            ts.Id, ts.Name, ts.StartTime.ToString("HH:mm"), ts.EndTime.ToString("HH:mm"),
            ts.LayoutId, ts.Layout != null ? ts.Layout.Name : null,
            ts.Monday, ts.Tuesday, ts.Wednesday, ts.Thursday, ts.Friday, ts.Saturday, ts.Sunday,
            ts.StartDate.HasValue ? ts.StartDate.Value.ToString("yyyy-MM-dd") : null,
            ts.EndDate.HasValue ? ts.EndDate.Value.ToString("yyyy-MM-dd") : null,
            ts.MaxCovers, ts.MaxReservations, ts.TurnTimeMinutes, ts.GracePeriodMinutes,
            ts.RequireDeposit, ts.DepositAmountPerPerson, ts.IsActive,
            ts.CategoryExclusions.Select(ce => ce.ClientCategoryId),
            ts.OutletId, ts.Outlet!.Name
        )).ToListAsync();

        return Ok(slots);
    }

    [HttpPost]
    [Permission("time_slots.manage")]
    public async Task<IActionResult> Create([FromBody] CreateTimeSlotRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);

        // Determine target outlet IDs
        int[] targetOutletIds;
        if (request.OutletIds?.Length > 0)
        {
            targetOutletIds = request.OutletIds;
        }
        else
        {
            var outletId = GetOutletId();
            if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header or outlet_ids required" });
            targetOutletIds = [outletId.Value];
        }

        var createdIds = new List<int>();
        foreach (var oid in targetOutletIds)
        {
            var slot = new TimeSlot
            {
                CompanyId = companyId, OutletId = oid,
                Name = request.Name,
                StartTime = TimeOnly.Parse(request.StartTime),
                EndTime = TimeOnly.Parse(request.EndTime),
                LayoutId = request.LayoutId,
                Monday = request.Monday, Tuesday = request.Tuesday, Wednesday = request.Wednesday,
                Thursday = request.Thursday, Friday = request.Friday, Saturday = request.Saturday, Sunday = request.Sunday,
                StartDate = !string.IsNullOrEmpty(request.StartDate) ? DateOnly.Parse(request.StartDate) : null,
                EndDate = !string.IsNullOrEmpty(request.EndDate) ? DateOnly.Parse(request.EndDate) : null,
                MaxCovers = request.MaxCovers, MaxReservations = request.MaxReservations,
                TurnTimeMinutes = request.TurnTimeMinutes, GracePeriodMinutes = request.GracePeriodMinutes,
                RequireDeposit = request.RequireDeposit, DepositAmountPerPerson = request.DepositAmountPerPerson
            };
            _db.TimeSlots.Add(slot);
            await _db.SaveChangesAsync();

            if (request.ExcludedCategoryIds?.Length > 0)
            {
                foreach (var catId in request.ExcludedCategoryIds)
                    _db.TimeSlotCategoryExclusions.Add(new TimeSlotCategoryExclusion { TimeSlotId = slot.Id, ClientCategoryId = catId });
                await _db.SaveChangesAsync();
            }
            createdIds.Add(slot.Id);
        }

        return Ok(new { ids = createdIds });
    }

    [HttpPut("{id}")]
    [Permission("time_slots.manage")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeSlotRequest request)
    {
        var slot = await _db.TimeSlots.Include(ts => ts.CategoryExclusions).FirstOrDefaultAsync(ts => ts.Id == id);
        if (slot == null) return NotFound(new { message = "Time slot not found" });

        slot.Name = request.Name;
        slot.StartTime = TimeOnly.Parse(request.StartTime);
        slot.EndTime = TimeOnly.Parse(request.EndTime);
        slot.LayoutId = request.LayoutId;
        slot.Monday = request.Monday; slot.Tuesday = request.Tuesday; slot.Wednesday = request.Wednesday;
        slot.Thursday = request.Thursday; slot.Friday = request.Friday; slot.Saturday = request.Saturday; slot.Sunday = request.Sunday;
        slot.StartDate = !string.IsNullOrEmpty(request.StartDate) ? DateOnly.Parse(request.StartDate) : null;
        slot.EndDate = !string.IsNullOrEmpty(request.EndDate) ? DateOnly.Parse(request.EndDate) : null;
        slot.MaxCovers = request.MaxCovers; slot.MaxReservations = request.MaxReservations;
        slot.TurnTimeMinutes = request.TurnTimeMinutes; slot.GracePeriodMinutes = request.GracePeriodMinutes;
        slot.RequireDeposit = request.RequireDeposit; slot.DepositAmountPerPerson = request.DepositAmountPerPerson;
        slot.IsActive = request.IsActive;

        _db.TimeSlotCategoryExclusions.RemoveRange(slot.CategoryExclusions);
        if (request.ExcludedCategoryIds?.Length > 0)
        {
            foreach (var catId in request.ExcludedCategoryIds)
                _db.TimeSlotCategoryExclusions.Add(new TimeSlotCategoryExclusion { TimeSlotId = id, ClientCategoryId = catId });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Time slot updated" });
    }

    [HttpDelete("{id}")]
    [Permission("time_slots.manage")]
    public async Task<IActionResult> Delete(int id)
    {
        var slot = await _db.TimeSlots.FindAsync(id);
        if (slot == null) return NotFound(new { message = "Time slot not found" });
        _db.TimeSlots.Remove(slot);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Time slot deleted" });
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;
}
