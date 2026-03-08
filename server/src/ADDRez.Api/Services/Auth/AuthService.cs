using ADDRez.Api.Data;
using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Services.Auth;

public interface IAuthService
{
    Task<(User? user, string? token, string? error)> LoginAsync(string username, string password);
    Task<User?> GetUserByIdAsync(int userId, int companyId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<(User? user, string? token, string? error)> LoginAsync(string username, string password)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .Include(u => u.Outlets)
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return (null, null, "Invalid username or password");

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (null, null, "Invalid username or password");

        if (!user.IsActive)
            return (null, null, "Account is deactivated");

        if (!user.Company.IsActive)
            return (null, null, "Company account is deactivated");

        // Collect all unique permissions from all roles
        var permissions = user.Roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Key)
            .Distinct()
            .ToList();

        var token = _jwtService.GenerateToken(user, permissions);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (user, token, null);
    }

    public async Task<User?> GetUserByIdAsync(int userId, int companyId)
    {
        return await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Roles).ThenInclude(r => r.Permissions)
            .Include(u => u.Outlets)
            .Where(u => u.Id == userId && u.CompanyId == companyId)
            .FirstOrDefaultAsync();
    }
}
