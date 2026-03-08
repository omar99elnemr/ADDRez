namespace ADDRez.Api.Entities;

public class PosTransaction : OutletEntity
{
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public int? TableId { get; set; }
    public Table? Table { get; set; }

    public string PosCheckId { get; set; } = string.Empty;
    public string? PosTableId { get; set; }
    public decimal Amount { get; set; } = 0;
    public decimal? TipAmount { get; set; }
    public int? GuestCount { get; set; }
    public string Status { get; set; } = "open"; // open, closed, void
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
