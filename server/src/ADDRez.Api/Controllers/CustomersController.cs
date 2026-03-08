using System.Security.Claims;
using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Common;
using ADDRez.Api.DTOs.Customers;
using ADDRez.Api.DTOs.Reservations;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    [Permission("customers.view")]
    public async Task<IActionResult> Index([FromQuery] PaginationQuery q, [FromQuery] int? categoryId, [FromQuery] string? status, [FromQuery] int[]? tagIds)
    {
        var query = _db.Customers
            .Include(c => c.ClientCategory)
            .Include(c => c.Tags)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(c => c.ClientCategoryId == categoryId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<CustomerStatus>(status, true, out var s))
            query = query.Where(c => c.Status == s);

        if (tagIds?.Length > 0)
            query = query.Where(c => c.Tags.Any(t => tagIds.Contains(t.Id)));

        if (!string.IsNullOrEmpty(q.Search))
            query = query.Where(c =>
                c.FirstName.Contains(q.Search) || c.LastName.Contains(q.Search) ||
                (c.Email != null && c.Email.Contains(q.Search)) ||
                (c.Phone != null && c.Phone.Contains(q.Search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((q.Page - 1) * q.PerPage).Take(q.PerPage)
            .Select(c => new CustomerListDto(
                c.Id, c.FirstName, c.LastName, c.FirstName + " " + c.LastName,
                c.Email, c.Phone, c.Status,
                c.ClientCategory != null ? c.ClientCategory.Name : null,
                c.ClientCategory != null ? c.ClientCategory.Color : null,
                c.TotalVisits, c.TotalSpend, c.LastVisitAt,
                c.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)),
                c.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PaginatedResponse<CustomerListDto>(items, q.Page, (int)Math.Ceiling((double)total / q.PerPage), total, q.PerPage));
    }

    [HttpGet("{id}")]
    [Permission("customers.view")]
    public async Task<IActionResult> Show(int id)
    {
        var c = await _db.Customers
            .Include(c => c.ClientCategory).Include(c => c.Tags)
            .Include(c => c.Notes).ThenInclude(n => n.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return NotFound(new { message = "Customer not found" });

        return Ok(new CustomerDetailDto(
            c.Id, c.FirstName, c.LastName, c.FullName, c.Email, c.Phone,
            c.PhoneCountryCode, c.DateOfBirth, c.Gender, c.Nationality,
            c.Address, c.City, c.Country, c.Instagram, c.FacebookUrl,
            c.CompanyName, c.Position, c.ClientCategoryId,
            c.ClientCategory?.Name, c.Status, c.TotalVisits, c.TotalSpend,
            c.AverageSpend, c.NoShowCount, c.CancellationCount, c.LastVisitAt,
            c.BlacklistReason, c.BlacklistedAt,
            c.Tags.Select(t => new TagDto(t.Id, t.Name, t.Color)),
            c.Notes.OrderByDescending(n => n.CreatedAt).Select(n => new CustomerNoteDto(n.Id, n.Note, n.User?.FullName, n.CreatedAt)),
            c.CreatedAt
        ));
    }

    [HttpPost]
    [Permission("customers.create")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);

        var customer = new Customer
        {
            CompanyId = companyId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            PhoneCountryCode = request.PhoneCountryCode,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Nationality = request.Nationality,
            Address = request.Address,
            City = request.City,
            Country = request.Country,
            Instagram = request.Instagram,
            ClientCategoryId = request.ClientCategoryId,
            Status = CustomerStatus.Active
        };

        if (request.TagIds?.Length > 0)
        {
            var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToListAsync();
            customer.Tags = tags;
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Show), new { id = customer.Id }, new { id = customer.Id });
    }

    [HttpPut("{id}")]
    [Permission("customers.edit")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.Include(c => c.Tags).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound(new { message = "Customer not found" });

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.PhoneCountryCode = request.PhoneCountryCode;
        customer.DateOfBirth = request.DateOfBirth;
        customer.Gender = request.Gender;
        customer.Nationality = request.Nationality;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.Country = request.Country;
        customer.Instagram = request.Instagram;
        customer.ClientCategoryId = request.ClientCategoryId;

        if (request.TagIds != null)
        {
            customer.Tags.Clear();
            var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.Id)).ToListAsync();
            foreach (var tag in tags) customer.Tags.Add(tag);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Customer updated" });
    }

    [HttpDelete("{id}")]
    [Permission("customers.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found" });

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Customer deleted" });
    }

    [HttpPost("{id}/blacklist")]
    [Permission("customers.blacklist")]
    public async Task<IActionResult> ToggleBlacklist(int id, [FromBody] BlacklistRequest? request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found" });

        if (customer.Status == CustomerStatus.Blacklisted)
        {
            customer.Status = CustomerStatus.Active;
            customer.BlacklistReason = null;
            customer.BlacklistedAt = null;
        }
        else
        {
            customer.Status = CustomerStatus.Blacklisted;
            customer.BlacklistReason = request?.Reason;
            customer.BlacklistedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Customer {customer.Status.Label()}", status = customer.Status });
    }

    [HttpGet("birthdays")]
    [Permission("customers.view")]
    public async Task<IActionResult> Birthdays([FromQuery] int? month, [FromQuery] int? year)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var customers = await _db.Customers
            .Include(c => c.ClientCategory)
            .Where(c => c.DateOfBirth != null && c.DateOfBirth.Value.Month == targetMonth)
            .OrderBy(c => c.DateOfBirth!.Value.Day)
            .Select(c => new BirthdayDto(
                c.Id, c.FirstName + " " + c.LastName,
                c.DateOfBirth!.Value.Day, c.DateOfBirth.Value.Month,
                c.Phone,
                c.ClientCategory != null ? c.ClientCategory.Name : null,
                c.ClientCategory != null ? c.ClientCategory.Color : null,
                c.TotalVisits
            ))
            .ToListAsync();

        return Ok(new { year = targetYear, month = targetMonth, birthdays = customers });
    }

    [HttpGet("blacklist")]
    [Permission("customers.blacklist")]
    public async Task<IActionResult> BlacklistedCustomers()
    {
        var customers = await _db.Customers
            .Where(c => c.Status == CustomerStatus.Blacklisted)
            .OrderByDescending(c => c.BlacklistedAt)
            .Select(c => new {
                c.Id,
                FullName = c.FirstName + " " + c.LastName,
                c.Phone,
                c.Email,
                c.BlacklistReason,
                c.BlacklistedAt
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpPost("{id}/notes")]
    [Permission("customers.notes")]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest request)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return NotFound(new { message = "Customer not found" });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var note = new CustomerNote { CustomerId = id, UserId = userId, Note = request.Note };
        _db.CustomerNotes.Add(note);
        await _db.SaveChangesAsync();
        return Ok(new { id = note.Id });
    }

    [HttpDelete("{customerId}/notes/{noteId}")]
    [Permission("customers.notes")]
    public async Task<IActionResult> DeleteNote(int customerId, int noteId)
    {
        var note = await _db.CustomerNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == customerId);
        if (note == null) return NotFound(new { message = "Note not found" });

        _db.CustomerNotes.Remove(note);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Note deleted" });
    }

    [HttpGet("{id}/reservations")]
    [Permission("customers.view")]
    public async Task<IActionResult> CustomerReservations(int id)
    {
        var exists = await _db.Customers.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound(new { message = "Customer not found" });

        var reservations = await _db.Reservations
            .Include(r => r.Table)
            .Include(r => r.TimeSlot)
            .Include(r => r.Outlet)
            .Where(r => r.CustomerId == id)
            .OrderByDescending(r => r.Date).ThenByDescending(r => r.Time)
            .Select(r => new CustomerReservationDto(
                r.Id,
                r.GuestName,
                r.Covers,
                r.Date.ToString("yyyy-MM-dd"),
                r.Time.ToString("HH:mm"),
                r.DurationMinutes,
                r.Status,
                r.Type,
                r.SeatingType,
                r.Table != null ? r.Table.Name : null,
                r.TimeSlot != null ? r.TimeSlot.Name : null,
                r.Outlet != null ? r.Outlet.Name : null,
                r.Notes,
                r.DepositAmount,
                r.CancellationReason,
                r.CreatedAt
            ))
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpGet("{id}/activity-log")]
    [Permission("customers.view")]
    public async Task<IActionResult> CustomerActivityLog(int id)
    {
        var exists = await _db.Customers.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound(new { message = "Customer not found" });

        var logs = await _db.OperationsLogs
            .Include(l => l.User)
            .Where(l => l.EntityType == "Customer" && l.EntityId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => new CustomerActivityLogDto(
                l.Id,
                l.Action,
                l.Description,
                l.User != null ? l.User.FirstName + " " + l.User.LastName : null,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(logs);
    }
}
