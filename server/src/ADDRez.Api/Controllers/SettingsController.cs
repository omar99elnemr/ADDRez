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
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SettingsController(AppDbContext db) => _db = db;

    // ── Company ──
    [HttpGet("company")]
    [Permission("settings.company")]
    public async Task<IActionResult> GetCompany()
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var c = await _db.Companies.FindAsync(companyId);
        if (c == null) return NotFound();
        return Ok(new CompanySettingsDto(c.Id, c.Name, c.Email, c.Phone, c.Website, c.LogoUrl, c.Timezone, c.DefaultCurrency, c.DefaultLocale));
    }

    [HttpPut("company")]
    [Permission("settings.company")]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var c = await _db.Companies.FindAsync(companyId);
        if (c == null) return NotFound();
        c.Name = request.Name; c.Email = request.Email; c.Phone = request.Phone;
        c.Website = request.Website; c.Timezone = request.Timezone;
        c.DefaultCurrency = request.DefaultCurrency; c.DefaultLocale = request.DefaultLocale;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Company settings updated" });
    }

    // ── Outlets ──
    [HttpGet("outlets")]
    [Permission("settings.branches")]
    public async Task<IActionResult> GetOutlets()
    {
        var outlets = await _db.Outlets.OrderBy(b => b.Name)
            .Select(b => new OutletDto(
                b.Id, b.Name, b.Address, b.Phone, b.Email,
                b.DefaultGracePeriodMinutes, b.DefaultTurnTimeMinutes,
                b.AutoConfirmOnline, b.IsActive,
                _db.Layouts.Where(l => l.OutletId == b.Id)
                    .SelectMany(l => l.FloorPlans.OrderBy(fp => fp.SortOrder))
                    .Select(fp => new OutletAreaDto(fp.Id, fp.Name, fp.SortOrder))
                    .ToList()
            ))
            .ToListAsync();
        return Ok(outlets);
    }

    [HttpPost("outlets")]
    [Permission("settings.branches")]
    public async Task<IActionResult> CreateOutlet([FromBody] CreateOutletRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var venueId = await _db.Venues.Where(v => v.CompanyId == companyId).Select(v => v.Id).FirstAsync();
        var outlet = new Outlet
        {
            CompanyId = companyId, VenueId = venueId, Name = request.Name, Address = request.Address,
            Phone = request.Phone, Email = request.Email, DefaultGracePeriodMinutes = request.DefaultGracePeriodMinutes,
            DefaultTurnTimeMinutes = request.DefaultTurnTimeMinutes, AutoConfirmOnline = request.AutoConfirmOnline
        };
        _db.Outlets.Add(outlet);
        await _db.SaveChangesAsync();

        // Auto-create a layout with floor plans for each area
        var areaNames = request.AreaNames?.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        if (areaNames == null || areaNames.Length == 0) areaNames = ["Main"];

        var layout = new Layout
        {
            CompanyId = companyId, OutletId = outlet.Id,
            Name = $"{request.Name} Layout", IsDefault = true
        };
        _db.Layouts.Add(layout);
        await _db.SaveChangesAsync();

        for (var i = 0; i < areaNames.Length; i++)
        {
            _db.FloorPlans.Add(new FloorPlan
            {
                LayoutId = layout.Id, Name = areaNames[i].Trim(),
                SortOrder = i, Width = 1200, Height = 800
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { id = outlet.Id });
    }

    [HttpPut("outlets/{id}")]
    [Permission("settings.branches")]
    public async Task<IActionResult> UpdateOutlet(int id, [FromBody] UpdateOutletRequest request)
    {
        var b = await _db.Outlets.FindAsync(id);
        if (b == null) return NotFound();
        b.Name = request.Name; b.Address = request.Address; b.Phone = request.Phone; b.Email = request.Email;
        b.DefaultGracePeriodMinutes = request.DefaultGracePeriodMinutes; b.DefaultTurnTimeMinutes = request.DefaultTurnTimeMinutes;
        b.AutoConfirmOnline = request.AutoConfirmOnline; b.IsActive = request.IsActive;

        // Sync areas if provided
        if (request.AreaNames != null)
        {
            var companyId = int.Parse(User.FindFirst("company_id")!.Value);
            var layout = await _db.Layouts
                .Include(l => l.FloorPlans)
                .FirstOrDefaultAsync(l => l.OutletId == id);

            if (layout == null)
            {
                layout = new Layout { CompanyId = companyId, OutletId = id, Name = $"{request.Name} Layout", IsDefault = true };
                _db.Layouts.Add(layout);
                await _db.SaveChangesAsync();
            }

            var areaNames = request.AreaNames.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
            if (areaNames.Length == 0) areaNames = ["Main"];

            var existing = layout.FloorPlans.OrderBy(fp => fp.SortOrder).ToList();

            // Remove excess floor plans (only if they have no tables)
            for (var i = areaNames.Length; i < existing.Count; i++)
            {
                var fp = existing[i];
                var hasTables = await _db.Tables.AnyAsync(t => t.FloorPlanId == fp.Id);
                if (!hasTables) _db.FloorPlans.Remove(fp);
            }

            // Update or create floor plans
            for (var i = 0; i < areaNames.Length; i++)
            {
                if (i < existing.Count)
                {
                    existing[i].Name = areaNames[i].Trim();
                    existing[i].SortOrder = i;
                }
                else
                {
                    _db.FloorPlans.Add(new FloorPlan
                    {
                        LayoutId = layout.Id, Name = areaNames[i].Trim(),
                        SortOrder = i, Width = 1200, Height = 800
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Outlet updated" });
    }

    [HttpDelete("outlets/{id}")]
    [Permission("settings.branches")]
    public async Task<IActionResult> DeleteOutlet(int id)
    {
        var b = await _db.Outlets.FindAsync(id);
        if (b == null) return NotFound();
        _db.Outlets.Remove(b);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Outlet deleted" });
    }

    // ── Users ──
    [HttpGet("users")]
    [Permission("settings.users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users.Include(u => u.Roles).Include(u => u.Outlets)
            .OrderBy(u => u.FirstName)
            .Select(u => new UserListDto(u.Id, u.Username, u.Email, u.FirstName, u.LastName, u.FirstName + " " + u.LastName,
                u.Phone, u.IsActive, u.LastLoginAt,
                u.Roles.Select(r => r.Name).ToList(), u.Outlets.Select(b => b.Name).ToList()))
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    [Permission("settings.users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Username already exists" });

        var user = new User
        {
            CompanyId = companyId, Username = request.Username, Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName, LastName = request.LastName, Phone = request.Phone
        };

        var roles = await _db.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
        var outlets = await _db.Outlets.Where(b => request.BranchIds.Contains(b.Id)).ToListAsync();
        user.Roles = roles;
        user.Outlets = outlets;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { id = user.Id });
    }

    [HttpPut("users/{id}")]
    [Permission("settings.users")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _db.Users.Include(u => u.Roles).Include(u => u.Outlets).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.Email = request.Email; user.FirstName = request.FirstName; user.LastName = request.LastName;
        user.Phone = request.Phone; user.IsActive = request.IsActive;

        user.Roles.Clear();
        user.Outlets.Clear();
        var roles = await _db.Roles.Where(r => request.RoleIds.Contains(r.Id)).ToListAsync();
        var outlets = await _db.Outlets.Where(b => request.BranchIds.Contains(b.Id)).ToListAsync();
        foreach (var r in roles) user.Roles.Add(r);
        foreach (var b in outlets) user.Outlets.Add(b);

        await _db.SaveChangesAsync();
        return Ok(new { message = "User updated" });
    }

    [HttpPost("users/{id}/reset-password")]
    [Permission("settings.users")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password reset" });
    }

    [HttpDelete("users/{id}")]
    [Permission("settings.users")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "User deleted" });
    }

    // ── Roles & Permissions ──
    [HttpGet("roles")]
    [Permission("settings.roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles.Include(r => r.Permissions).OrderBy(r => r.Name)
            .Select(r => new RoleDetailDto(r.Id, r.Name, r.Slug, r.Description, r.IsSystem, r.Permissions.Select(p => p.Id).ToList()))
            .ToListAsync();
        return Ok(roles);
    }

    [HttpGet("permissions")]
    [Permission("settings.roles")]
    public async Task<IActionResult> GetPermissions()
    {
        var permissions = await _db.Permissions.OrderBy(p => p.Group).ThenBy(p => p.Key)
            .Select(p => new PermissionDto(p.Id, p.Key, p.Name, p.Group, p.Description))
            .ToListAsync();
        return Ok(permissions);
    }

    [HttpPost("roles")]
    [Permission("settings.roles")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var role = new Role { CompanyId = companyId, Name = request.Name, Slug = request.Slug, Description = request.Description };
        var perms = await _db.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
        role.Permissions = perms;
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return Ok(new { id = role.Id });
    }

    [HttpPut("roles/{id}")]
    [Permission("settings.roles")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var role = await _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();
        if (role.IsSystem) return BadRequest(new { message = "Cannot modify system roles" });

        role.Name = request.Name; role.Description = request.Description;
        role.Permissions.Clear();
        var perms = await _db.Permissions.Where(p => request.PermissionIds.Contains(p.Id)).ToListAsync();
        foreach (var p in perms) role.Permissions.Add(p);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated" });
    }

    [HttpDelete("roles/{id}")]
    [Permission("settings.roles")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();
        if (role.IsSystem) return BadRequest(new { message = "Cannot delete system roles" });
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Role deleted" });
    }

    // ── Tag Categories ──
    [HttpGet("tag-categories")]
    [Permission("settings.tags")]
    public async Task<IActionResult> GetTagCategories([FromQuery] string? type)
    {
        var query = _db.TagCategories.Include(tc => tc.Tags).AsQueryable();

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TagType>(type, true, out var tagType))
            query = query.Where(tc => tc.Type == tagType);

        var categories = await query.OrderBy(tc => tc.SortOrder).ThenBy(tc => tc.Name)
            .Select(tc => new TagCategoryDto(
                tc.Id, tc.Name, tc.Icon, tc.Type, tc.SortOrder,
                tc.Tags.OrderBy(t => t.Name).Select(t => new TagSettingsDto(t.Id, t.Name, t.Color, t.Type, t.TagCategoryId))
            ))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("tag-categories")]
    [Permission("settings.tags")]
    public async Task<IActionResult> CreateTagCategory([FromBody] CreateTagCategoryRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var maxSort = await _db.TagCategories.Where(tc => tc.CompanyId == companyId).MaxAsync(tc => (int?)tc.SortOrder) ?? 0;
        var cat = new TagCategory { CompanyId = companyId, Name = request.Name, Icon = request.Icon, Type = request.Type, SortOrder = maxSort + 1 };
        _db.TagCategories.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(new { id = cat.Id });
    }

    [HttpPut("tag-categories/{id}")]
    [Permission("settings.tags")]
    public async Task<IActionResult> UpdateTagCategory(int id, [FromBody] UpdateTagCategoryRequest request)
    {
        var cat = await _db.TagCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.Name = request.Name; cat.Icon = request.Icon; cat.SortOrder = request.SortOrder;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tag category updated" });
    }

    [HttpDelete("tag-categories/{id}")]
    [Permission("settings.tags")]
    public async Task<IActionResult> DeleteTagCategory(int id)
    {
        var cat = await _db.TagCategories.Include(tc => tc.Tags).FirstOrDefaultAsync(tc => tc.Id == id);
        if (cat == null) return NotFound();
        // Orphan tags (set their category to null) rather than deleting them
        foreach (var tag in cat.Tags) tag.TagCategoryId = null;
        _db.TagCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tag category deleted" });
    }

    // ── Tags ──
    [HttpGet("tags")]
    [Permission("settings.tags")]
    public async Task<IActionResult> GetTags([FromQuery] string? type)
    {
        var query = _db.Tags.AsQueryable();
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TagType>(type, true, out var tagType))
            query = query.Where(t => t.Type == tagType);

        var tags = await query.OrderBy(t => t.Type).ThenBy(t => t.Name)
            .Select(t => new TagSettingsDto(t.Id, t.Name, t.Color, t.Type, t.TagCategoryId))
            .ToListAsync();
        return Ok(tags);
    }

    [HttpPost("tags")]
    [Permission("settings.tags")]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var tag = new Tag { CompanyId = companyId, Name = request.Name, Color = request.Color, Type = request.Type, TagCategoryId = request.TagCategoryId };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        return Ok(new { id = tag.Id });
    }

    [HttpPut("tags/{id}")]
    [Permission("settings.tags")]
    public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound();
        tag.Name = request.Name; tag.Color = request.Color; tag.TagCategoryId = request.TagCategoryId;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tag updated" });
    }

    [HttpDelete("tags/{id}")]
    [Permission("settings.tags")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound();
        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Tag deleted" });
    }

    // ── Client Categories ──
    [HttpGet("categories")]
    [Permission("settings.categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await _db.ClientCategories.OrderBy(c => c.Priority)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.Color, c.Priority, c.IsActive))
            .ToListAsync();
        return Ok(cats);
    }

    [HttpPost("categories")]
    [Permission("settings.categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var cat = new ClientCategory { CompanyId = companyId, Name = request.Name, Description = request.Description, Color = request.Color, Priority = request.Priority };
        _db.ClientCategories.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(new { id = cat.Id });
    }

    [HttpPut("categories/{id}")]
    [Permission("settings.categories")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        var cat = await _db.ClientCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.Name = request.Name; cat.Description = request.Description; cat.Color = request.Color; cat.Priority = request.Priority; cat.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Category updated" });
    }

    [HttpDelete("categories/{id}")]
    [Permission("settings.categories")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await _db.ClientCategories.FindAsync(id);
        if (cat == null) return NotFound();
        _db.ClientCategories.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Category deleted" });
    }

    // ── Notification Templates ──
    [HttpGet("templates")]
    [Permission("settings.templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _db.NotificationTemplates.OrderBy(t => t.Type)
            .Select(t => new TemplateDto(t.Id, t.Name, t.Type, t.Channel, t.Subject, t.Body, t.IsActive))
            .ToListAsync();
        return Ok(templates);
    }

    [HttpPost("templates")]
    [Permission("settings.templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var tmpl = new NotificationTemplate
        {
            CompanyId = companyId, Name = request.Name, Type = request.Type, Channel = request.Channel,
            Subject = request.Subject, Body = request.Body
        };
        _db.NotificationTemplates.Add(tmpl);
        await _db.SaveChangesAsync();
        return Ok(new { id = tmpl.Id });
    }

    [HttpPut("templates/{id}")]
    [Permission("settings.templates")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateTemplateRequest request)
    {
        var tmpl = await _db.NotificationTemplates.FindAsync(id);
        if (tmpl == null) return NotFound();
        tmpl.Name = request.Name; tmpl.Subject = request.Subject; tmpl.Body = request.Body; tmpl.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Template updated" });
    }

    [HttpDelete("templates/{id}")]
    [Permission("settings.templates")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var tmpl = await _db.NotificationTemplates.FindAsync(id);
        if (tmpl == null) return NotFound();
        _db.NotificationTemplates.Remove(tmpl);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Template deleted" });
    }

    // ── Terms & Conditions ──
    [HttpGet("terms")]
    [Permission("settings.terms")]
    public async Task<IActionResult> GetTerms()
    {
        var terms = await _db.TermsConditions.OrderBy(t => t.SortOrder)
            .Select(t => new TermsDto(t.Id, t.Title, t.Content, t.SortOrder, t.IsActive))
            .ToListAsync();
        return Ok(terms);
    }

    [HttpPost("terms")]
    [Permission("settings.terms")]
    public async Task<IActionResult> CreateTerms([FromBody] CreateTermsRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var term = new TermsCondition { CompanyId = companyId, Title = request.Title, Content = request.Content, SortOrder = request.SortOrder };
        _db.TermsConditions.Add(term);
        await _db.SaveChangesAsync();
        return Ok(new { id = term.Id });
    }

    [HttpPut("terms/{id}")]
    [Permission("settings.terms")]
    public async Task<IActionResult> UpdateTerms(int id, [FromBody] UpdateTermsRequest request)
    {
        var term = await _db.TermsConditions.FindAsync(id);
        if (term == null) return NotFound();
        term.Title = request.Title; term.Content = request.Content; term.SortOrder = request.SortOrder; term.IsActive = request.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Term updated" });
    }

    [HttpDelete("terms/{id}")]
    [Permission("settings.terms")]
    public async Task<IActionResult> DeleteTerms(int id)
    {
        var term = await _db.TermsConditions.FindAsync(id);
        if (term == null) return NotFound();
        _db.TermsConditions.Remove(term);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Term deleted" });
    }

    // ── General Configurations ──

    // Global configs are stored with the first outlet but apply company-wide
    [HttpGet("configurations/global")]
    [Permission("settings.general")]
    public async Task<IActionResult> GetGlobalConfigs()
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var firstOutlet = await _db.Outlets.Where(o => o.CompanyId == companyId).OrderBy(o => o.Id).FirstOrDefaultAsync();
        if (firstOutlet == null) return Ok(new Dictionary<string, string>());

        var configs = await _db.GeneralConfigurations
            .Where(c => c.OutletId == firstOutlet.Id && c.Key.StartsWith("global."))
            .ToDictionaryAsync(c => c.Key, c => c.Value);
        return Ok(configs);
    }

    [HttpPut("configurations/global")]
    [Permission("settings.general")]
    public async Task<IActionResult> SaveGlobalConfigs([FromBody] ConfigurationBulkRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var firstOutlet = await _db.Outlets.Where(o => o.CompanyId == companyId).OrderBy(o => o.Id).FirstOrDefaultAsync();
        if (firstOutlet == null) return BadRequest(new { message = "No outlet found" });

        var existing = await _db.GeneralConfigurations
            .Where(c => c.OutletId == firstOutlet.Id && c.Key.StartsWith("global."))
            .ToListAsync();

        foreach (var (key, value) in request.Configs)
        {
            var config = existing.FirstOrDefault(c => c.Key == key);
            if (config != null) { config.Value = value; }
            else { _db.GeneralConfigurations.Add(new GeneralConfiguration { CompanyId = companyId, OutletId = firstOutlet.Id, Key = key, Value = value }); }
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Global configurations saved" });
    }

    // Per-outlet configs (Frequent, Spender, No Show)
    [HttpGet("configurations/outlets")]
    [Permission("settings.general")]
    public async Task<IActionResult> GetOutletConfigs([FromQuery] string prefix)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var outlets = await _db.Outlets.Where(o => o.CompanyId == companyId && o.IsActive).OrderBy(o => o.Name).ToListAsync();
        var allConfigs = await _db.GeneralConfigurations
            .Where(c => c.Key.StartsWith(prefix + "."))
            .ToListAsync();

        var result = outlets.Select(o => new OutletConfigDto(
            o.Id, o.Name,
            allConfigs.Where(c => c.OutletId == o.Id).ToDictionary(c => c.Key, c => c.Value)
        ));
        return Ok(result);
    }

    [HttpPut("configurations/outlets/{outletId}")]
    [Permission("settings.general")]
    public async Task<IActionResult> SaveOutletConfigs(int outletId, [FromBody] ConfigurationBulkRequest request)
    {
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);
        var outlet = await _db.Outlets.FirstOrDefaultAsync(o => o.Id == outletId && o.CompanyId == companyId);
        if (outlet == null) return NotFound(new { message = "Outlet not found" });

        var keys = request.Configs.Keys.ToList();
        var existing = await _db.GeneralConfigurations
            .Where(c => c.OutletId == outletId && keys.Contains(c.Key))
            .ToListAsync();

        foreach (var (key, value) in request.Configs)
        {
            var config = existing.FirstOrDefault(c => c.Key == key);
            if (config != null) { config.Value = value; }
            else { _db.GeneralConfigurations.Add(new GeneralConfiguration { CompanyId = companyId, OutletId = outletId, Key = key, Value = value }); }
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Outlet configurations saved" });
    }

    // ── Table Types ──
    [HttpGet("table-types")]
    [Permission("floor_plan.view")]
    public async Task<IActionResult> GetTableTypes()
    {
        var types = await _db.TableTypes
            .Select(t => new { t.Id, t.Name, t.Description, t.IsActive })
            .ToListAsync();
        return Ok(types);
    }
}
