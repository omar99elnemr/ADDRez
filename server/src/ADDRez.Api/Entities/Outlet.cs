namespace ADDRez.Api.Entities;

public class Outlet : TenantEntity
{
    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int DefaultGracePeriodMinutes { get; set; } = 15;
    public int DefaultTurnTimeMinutes { get; set; } = 90;
    public bool AutoConfirmOnline { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Layout> Layouts { get; set; } = [];
    public ICollection<TimeSlot> TimeSlots { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<OperationsLog> OperationsLogs { get; set; } = [];
    public ICollection<GeneralConfiguration> GeneralConfigurations { get; set; } = [];
}
