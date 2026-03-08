using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.DTOs.Reservations;

public record ReservationListDto(
    int Id,
    string? GuestName,
    string? GuestEmail,
    string? GuestPhone,
    int? CustomerId,
    string? CustomerName,
    int Covers,
    string Date,
    string Time,
    int DurationMinutes,
    ReservationStatus Status,
    ReservationType Type,
    SeatingType SeatingType,
    ReservationMethod Method,
    string? Notes,
    int? TableId,
    string? TableName,
    int? TimeSlotId,
    string? TimeSlotName,
    string? ConfirmationCode,
    IEnumerable<TagDto> Tags,
    DateTime CreatedAt
);

public record TagDto(int Id, string Name, string Color);

public record CreateReservationRequest
{
    public int? CustomerId { get; init; }
    public string? GuestName { get; init; }
    public string? GuestEmail { get; init; }
    public string? GuestPhone { get; init; }
    public string? GuestDateOfBirth { get; init; }
    public string? GuestGender { get; init; }
    public string? MembershipNo { get; init; }
    public string? RoomNo { get; init; }
    public int Covers { get; init; } = 2;
    public string Date { get; init; } = string.Empty;
    public string Time { get; init; } = string.Empty;
    public int DurationMinutes { get; init; } = 90;
    public ReservationType Type { get; init; } = ReservationType.DineIn;
    public SeatingType SeatingType { get; init; } = SeatingType.Seated;
    public ReservationMethod Method { get; init; } = ReservationMethod.Phone;
    public string? Notes { get; init; }
    public string? SpecialRequests { get; init; }
    public decimal? DepositAmount { get; init; }
    public decimal? DiscountPercent { get; init; }
    public string? DiscountReason { get; init; }
    public int? TimeSlotId { get; init; }
    public int? TableId { get; init; }
    public int[]? TagIds { get; init; }
}

public record UpdateReservationRequest : CreateReservationRequest;

public record ChangeStatusRequest(ReservationStatus Status, string? Notes);

public record AssignTableRequest(int? TableId);

public record CalendarQuery
{
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public int? TimeSlotId { get; init; }
}

public record CalendarDayDto(string Date, IEnumerable<CalendarReservationDto> Reservations);

public record CalendarReservationDto(
    int Id,
    string? GuestName,
    int Covers,
    string Time,
    ReservationStatus Status,
    SeatingType SeatingType,
    string? TableName
);

// ── Live Capacity ──
public record CapacityDto(
    int TotalSeated,
    int TotalStanding,
    int TablesReserved,
    int TotalTables,
    int AttendedCustomers,
    int WalkInCustomers,
    int TotalCovers,
    int PendingCount
);

// ── Move Reservations ──
public record MoveReservationsRequest
{
    public string FromDate { get; init; } = string.Empty;
    public int FromTimeSlotId { get; init; }
    public int ToTimeSlotId { get; init; }
    public bool IncludePending { get; init; } = true;
}
