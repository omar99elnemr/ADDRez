using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class Reservation : OutletEntity
{
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; }

    public int? TableId { get; set; }
    public Table? Table { get; set; }

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public string? GuestName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public DateOnly? GuestDateOfBirth { get; set; }
    public string? GuestGender { get; set; }
    public string? MembershipNo { get; set; }
    public string? RoomNo { get; set; }
    public int Covers { get; set; } = 2;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public int DurationMinutes { get; set; } = 90;
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public ReservationType Type { get; set; } = ReservationType.DineIn;
    public SeatingType SeatingType { get; set; } = SeatingType.Seated;
    public ReservationMethod Method { get; set; } = ReservationMethod.Phone;
    public string? Notes { get; set; }
    public string? SpecialRequests { get; set; }
    public decimal? DepositAmount { get; set; }
    public bool DepositPaid { get; set; } = false;
    public decimal? DiscountPercent { get; set; }
    public string? DiscountReason { get; set; }
    public string? QrCode { get; set; }
    public string? ConfirmationCode { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? SeatedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<ReservationStatusHistory> StatusHistory { get; set; } = [];
    public GuestList? GuestList { get; set; }
    public ICollection<ChangesLog> ChangesLogs { get; set; } = [];
}
