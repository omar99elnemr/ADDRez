namespace ADDRez.Api.Entities;

public class Venue : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Outlet> Outlets { get; set; } = [];
}
