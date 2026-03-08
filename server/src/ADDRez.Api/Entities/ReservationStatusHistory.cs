using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class ReservationStatusHistory : BaseEntity
{
    public int ReservationId { get; set; }
    public Reservation Reservation { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }

    public ReservationStatus FromStatus { get; set; }
    public ReservationStatus ToStatus { get; set; }
    public string? Notes { get; set; }
}
