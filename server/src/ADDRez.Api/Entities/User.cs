namespace ADDRez.Api.Entities;

public class User : TenantEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Outlet> Outlets { get; set; } = [];
    public ICollection<Reservation> CreatedReservations { get; set; } = [];
    public ICollection<Campaign> CreatedCampaigns { get; set; } = [];
}
