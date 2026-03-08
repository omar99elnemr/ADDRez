namespace ADDRez.Api.DTOs.Auth;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    UserDto User
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? Phone,
    string? AvatarUrl,
    bool IsActive,
    int CompanyId,
    string CompanyName,
    IEnumerable<RoleDto> Roles,
    IEnumerable<string> Permissions,
    IEnumerable<OutletSummaryDto> Outlets
);

public record RoleDto(int Id, string Name, string Slug);

public record OutletSummaryDto(int Id, string Name, bool IsActive);

public record UpdatePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string Email, string FirstName, string LastName, string? Phone);
