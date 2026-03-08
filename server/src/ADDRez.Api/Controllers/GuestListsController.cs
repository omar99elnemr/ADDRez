using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Settings;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/guest-lists")]
[Authorize]
public class GuestListsController : ControllerBase
{
    private readonly AppDbContext _db;
    public GuestListsController(AppDbContext db) => _db = db;

    [HttpGet("reservation/{reservationId}")]
    [Permission("guest_lists.view")]
    public async Task<IActionResult> GetByReservation(int reservationId)
    {
        var guestList = await _db.GuestLists
            .Include(gl => gl.Items)
            .FirstOrDefaultAsync(gl => gl.ReservationId == reservationId);

        if (guestList == null)
        {
            // Auto-create guest list for this reservation
            guestList = new GuestList { ReservationId = reservationId, MaxCapacity = 100 };
            _db.GuestLists.Add(guestList);
            await _db.SaveChangesAsync();
            guestList.Items = [];
        }

        return Ok(new GuestListDto(
            guestList.Id, guestList.ReservationId, guestList.Name, guestList.MaxCapacity,
            guestList.Items.Sum(i => i.Covers),
            guestList.Items.Where(i => i.Status == GuestListItemStatus.CheckedIn).Sum(i => i.Covers),
            guestList.Items.Select(i => new GuestListItemDto(
                i.Id, i.Name, i.Email, i.Phone, i.Covers, i.Status, i.Notes, i.CheckedInAt
            ))
        ));
    }

    [HttpPost("{guestListId}/items")]
    [Permission("guest_lists.manage")]
    public async Task<IActionResult> AddItem(int guestListId, [FromBody] CreateGuestListItemRequest request)
    {
        var guestList = await _db.GuestLists.FindAsync(guestListId);
        if (guestList == null) return NotFound(new { message = "Guest list not found" });

        var item = new GuestListItem
        {
            GuestListId = guestListId, Name = request.Name, Email = request.Email,
            Phone = request.Phone, Covers = request.Covers, Notes = request.Notes,
            Status = GuestListItemStatus.Invited
        };

        _db.GuestListItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(new { id = item.Id });
    }

    [HttpPut("items/{itemId}")]
    [Permission("guest_lists.manage")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateGuestListItemRequest request)
    {
        var item = await _db.GuestListItems.FindAsync(itemId);
        if (item == null) return NotFound(new { message = "Guest list item not found" });

        item.Name = request.Name; item.Email = request.Email;
        item.Phone = request.Phone; item.Covers = request.Covers; item.Notes = request.Notes;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Item updated" });
    }

    [HttpPost("items/{itemId}/checkin")]
    [Permission("guest_lists.checkin")]
    public async Task<IActionResult> CheckIn(int itemId)
    {
        var item = await _db.GuestListItems.FindAsync(itemId);
        if (item == null) return NotFound(new { message = "Guest list item not found" });

        item.Status = item.Status == GuestListItemStatus.CheckedIn
            ? GuestListItemStatus.Confirmed
            : GuestListItemStatus.CheckedIn;

        item.CheckedInAt = item.Status == GuestListItemStatus.CheckedIn ? DateTime.UtcNow : null;
        await _db.SaveChangesAsync();
        return Ok(new { status = item.Status, checked_in_at = item.CheckedInAt });
    }

    [HttpDelete("items/{itemId}")]
    [Permission("guest_lists.manage")]
    public async Task<IActionResult> DeleteItem(int itemId)
    {
        var item = await _db.GuestListItems.FindAsync(itemId);
        if (item == null) return NotFound(new { message = "Guest list item not found" });
        _db.GuestListItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Item removed" });
    }
}
