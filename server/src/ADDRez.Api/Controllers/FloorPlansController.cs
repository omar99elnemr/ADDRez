using ADDRez.Api.Authorization;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.FloorPlans;
using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/floor-plans")]
[Authorize]
public class FloorPlansController : ControllerBase
{
    private readonly AppDbContext _db;
    public FloorPlansController(AppDbContext db) => _db = db;

    [HttpGet("layouts")]
    [Permission("floor_plan.view")]
    public async Task<IActionResult> GetLayouts([FromQuery] int? outletId = null)
    {
        outletId ??= GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var layouts = await _db.Layouts
            .Where(l => l.OutletId == outletId)
            .Include(l => l.FloorPlans).ThenInclude(fp => fp.Tables).ThenInclude(t => t.TableType)
            .Include(l => l.FloorPlans).ThenInclude(fp => fp.Landmarks)
            .OrderBy(l => l.Name)
            .ToListAsync();

        // Get today's reservations for table status
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeReservations = await _db.Reservations
            .Where(r => r.OutletId == outletId && r.Date == today && r.TableId != null &&
                (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn || r.Status == ReservationStatus.Seated))
            .Include(r => r.Customer)
            .ToListAsync();

        var result = layouts.Select(l => new LayoutDto(
            l.Id, l.Name, l.Description, l.IsDefault, l.IsActive,
            l.FloorPlans.OrderBy(fp => fp.SortOrder).Select(fp => new FloorPlanDto(
                fp.Id, fp.Name, fp.SortOrder, fp.Width, fp.Height, fp.BackgroundColor, fp.IsActive,
                fp.Tables.Where(t => t.IsActive).Select(t =>
                {
                    var res = activeReservations.FirstOrDefault(r => r.TableId == t.Id);
                    return new TableDto(
                        t.Id, t.Name, t.Label, t.MinCovers, t.MaxCovers, t.Shape,
                        t.X, t.Y, t.Width, t.Height, t.Rotation, t.IsCombinable, t.IsActive,
                        t.TableTypeId, t.TableType?.Name,
                        res?.Status.ToString(), res?.Customer?.FullName ?? res?.GuestName
                    );
                }),
                fp.Landmarks.Select(lm => new LandmarkDto(lm.Id, lm.Name, lm.Type, lm.Icon, lm.X, lm.Y, lm.Width, lm.Height, lm.Rotation))
            ))
        ));

        return Ok(result);
    }

    [HttpPost("layouts")]
    [Permission("floor_plan.manage_layouts")]
    public async Task<IActionResult> CreateLayout([FromBody] CreateLayoutRequest request)
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);

        var layout = new Layout
        {
            CompanyId = companyId, OutletId = outletId.Value,
            Name = request.Name, Description = request.Description, IsDefault = request.IsDefault
        };
        _db.Layouts.Add(layout);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLayouts), new { id = layout.Id }, new { id = layout.Id });
    }

    [HttpPut("layouts/{id}")]
    [Permission("floor_plan.manage_layouts")]
    public async Task<IActionResult> UpdateLayout(int id, [FromBody] UpdateLayoutRequest request)
    {
        var layout = await _db.Layouts.FindAsync(id);
        if (layout == null) return NotFound(new { message = "Layout not found" });

        layout.Name = request.Name;
        layout.Description = request.Description;
        layout.IsDefault = request.IsDefault;
        layout.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Layout updated" });
    }

    [HttpPost("layouts/{layoutId}/zones")]
    [Permission("floor_plan.edit")]
    public async Task<IActionResult> CreateFloorPlan(int layoutId, [FromBody] CreateFloorPlanRequest request)
    {
        var layout = await _db.Layouts.FindAsync(layoutId);
        if (layout == null) return NotFound(new { message = "Layout not found" });

        var fp = new FloorPlan
        {
            LayoutId = layoutId, Name = request.Name, SortOrder = request.SortOrder,
            Width = request.Width, Height = request.Height, BackgroundColor = request.BackgroundColor
        };
        _db.FloorPlans.Add(fp);
        await _db.SaveChangesAsync();
        return Ok(new { id = fp.Id });
    }

    [HttpPut("zones/{zoneId}/save")]
    [Permission("floor_plan.edit")]
    public async Task<IActionResult> SaveFloorPlanLayout(int zoneId, [FromBody] SaveFloorPlanLayoutRequest request)
    {
        var floorPlan = await _db.FloorPlans
            .Include(fp => fp.Tables).Include(fp => fp.Landmarks)
            .FirstOrDefaultAsync(fp => fp.Id == zoneId);

        if (floorPlan == null) return NotFound(new { message = "Floor plan zone not found" });

        // Upsert tables
        var existingTableIds = floorPlan.Tables.Select(t => t.Id).ToHashSet();
        var incomingTableIds = request.Tables.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        // Remove deleted tables
        var tablesToRemove = floorPlan.Tables.Where(t => !incomingTableIds.Contains(t.Id)).ToList();
        _db.Tables.RemoveRange(tablesToRemove);

        foreach (var req in request.Tables)
        {
            if (req.Id.HasValue && existingTableIds.Contains(req.Id.Value))
            {
                var table = floorPlan.Tables.First(t => t.Id == req.Id.Value);
                table.Name = req.Name; table.Label = req.Label;
                table.MinCovers = req.MinCovers; table.MaxCovers = req.MaxCovers;
                table.Shape = req.Shape; table.X = req.X; table.Y = req.Y;
                table.Width = req.Width; table.Height = req.Height;
                table.Rotation = req.Rotation; table.IsCombinable = req.IsCombinable;
                table.TableTypeId = req.TableTypeId;
            }
            else
            {
                _db.Tables.Add(new Table
                {
                    FloorPlanId = zoneId, Name = req.Name, Label = req.Label,
                    MinCovers = req.MinCovers, MaxCovers = req.MaxCovers,
                    Shape = req.Shape, X = req.X, Y = req.Y,
                    Width = req.Width, Height = req.Height,
                    Rotation = req.Rotation, IsCombinable = req.IsCombinable,
                    TableTypeId = req.TableTypeId
                });
            }
        }

        // Upsert landmarks
        var existingLandmarkIds = floorPlan.Landmarks.Select(l => l.Id).ToHashSet();
        var incomingLandmarkIds = request.Landmarks.Where(l => l.Id.HasValue).Select(l => l.Id!.Value).ToHashSet();
        var landmarksToRemove = floorPlan.Landmarks.Where(l => !incomingLandmarkIds.Contains(l.Id)).ToList();
        _db.Landmarks.RemoveRange(landmarksToRemove);

        foreach (var req in request.Landmarks)
        {
            if (req.Id.HasValue && existingLandmarkIds.Contains(req.Id.Value))
            {
                var lm = floorPlan.Landmarks.First(l => l.Id == req.Id.Value);
                lm.Name = req.Name; lm.Type = req.Type; lm.Icon = req.Icon;
                lm.X = req.X; lm.Y = req.Y; lm.Width = req.Width;
                lm.Height = req.Height; lm.Rotation = req.Rotation;
            }
            else
            {
                _db.Landmarks.Add(new Landmark
                {
                    FloorPlanId = zoneId, Name = req.Name, Type = req.Type, Icon = req.Icon,
                    X = req.X, Y = req.Y, Width = req.Width, Height = req.Height, Rotation = req.Rotation
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Floor plan saved" });
    }

    [HttpGet("tables")]
    [Permission("floor_plan.view")]
    public async Task<IActionResult> AllTables()
    {
        var outletId = GetOutletId();
        if (outletId == null) return BadRequest(new { message = "X-Outlet-Id header required" });

        var tables = await _db.Tables
            .Include(t => t.TableType)
            .Include(t => t.FloorPlan).ThenInclude(fp => fp.Layout)
            .Where(t => t.FloorPlan.Layout.OutletId == outletId && t.IsActive)
            .Select(t => new { t.Id, t.Name, t.Label, t.MinCovers, t.MaxCovers, t.Shape, TypeName = t.TableType != null ? t.TableType.Name : null })
            .ToListAsync();

        return Ok(tables);
    }

    private int? GetOutletId() =>
        HttpContext.Items.TryGetValue("OutletId", out var val) && val is int id ? id : null;
}
