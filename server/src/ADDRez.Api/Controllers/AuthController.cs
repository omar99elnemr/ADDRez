using System.Security.Claims;
using ADDRez.Api.Data;
using ADDRez.Api.DTOs.Auth;
using ADDRez.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (user, token, error) = await _authService.LoginAsync(request.Username, request.Password);
        if (user == null || token == null)
            return Unauthorized(new { message = error });

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Key)
            .Distinct();

        var response = new LoginResponse(
            Token: token,
            User: new UserDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                FullName: user.FullName,
                Phone: user.Phone,
                AvatarUrl: user.AvatarUrl,
                IsActive: user.IsActive,
                CompanyId: user.CompanyId,
                CompanyName: user.Company.Name,
                Roles: user.Roles.Select(r => new RoleDto(r.Id, r.Name, r.Slug)),
                Permissions: permissions,
                Outlets: user.Outlets.Select(b => new OutletSummaryDto(b.Id, b.Name, b.IsActive))
            )
        );

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var companyId = int.Parse(User.FindFirst("company_id")!.Value);

        var user = await _authService.GetUserByIdAsync(userId, companyId);
        if (user == null) return NotFound(new { message = "User not found" });

        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Key)
            .Distinct();

        return Ok(new UserDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            Phone: user.Phone,
            AvatarUrl: user.AvatarUrl,
            IsActive: user.IsActive,
            CompanyId: user.CompanyId,
            CompanyName: user.Company?.Name ?? "",
            Roles: user.Roles.Select(r => new RoleDto(r.Id, r.Name, r.Slug)),
            Permissions: permissions,
            Outlets: user.Outlets.Select(b => new OutletSummaryDto(b.Id, b.Name, b.IsActive))
        ));
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, [FromServices] AppDbContext db)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await db.Users.Include(u => u.Roles).ThenInclude(r => r.Permissions).Include(u => u.Outlets).Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return NotFound();

        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        await db.SaveChangesAsync();

        var permissions = user.Roles.SelectMany(r => r.Permissions).Select(p => p.Key).Distinct();
        return Ok(new UserDto(
            user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.FullName,
            user.Phone, user.AvatarUrl, user.IsActive, user.CompanyId, user.Company?.Name ?? "",
            user.Roles.Select(r => new RoleDto(r.Id, r.Name, r.Slug)),
            permissions,
            user.Outlets.Select(b => new OutletSummaryDto(b.Id, b.Name, b.IsActive))
        ));
    }

    [HttpPost("update-password")]
    [Authorize]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request, [FromServices] AppDbContext db)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully" });
    }
}
