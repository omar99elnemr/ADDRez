namespace ADDRez.Api.Entities;

public class GuestList : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public string? Name { get; set; }
    public int MaxCapacity { get; set; } = 100;

    // Navigation
    public ICollection<GuestListItem> Items { get; set; } = [];
}
