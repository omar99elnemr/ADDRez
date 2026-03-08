using ADDRez.Api.DTOs.Reservations;
using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.DTOs.Customers;

public record CustomerListDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    CustomerStatus Status,
    string? CategoryName,
    string? CategoryColor,
    int TotalVisits,
    decimal TotalSpend,
    DateTime? LastVisitAt,
    IEnumerable<TagDto> Tags,
    DateTime CreatedAt
);

public record CustomerDetailDto(
    int Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    string? PhoneCountryCode,
    DateTime? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? Address,
    string? City,
    string? Country,
    string? Instagram,
    string? FacebookUrl,
    string? Company,
    string? Position,
    int? ClientCategoryId,
    string? CategoryName,
    CustomerStatus Status,
    int TotalVisits,
    decimal TotalSpend,
    decimal AverageSpend,
    int NoShowCount,
    int CancellationCount,
    DateTime? LastVisitAt,
    string? BlacklistReason,
    DateTime? BlacklistedAt,
    IEnumerable<TagDto> Tags,
    IEnumerable<CustomerNoteDto> Notes,
    DateTime CreatedAt
);

public record BirthdayDto(
    int Id,
    string FullName,
    int Day,
    int Month,
    string? Phone,
    string? CategoryName,
    string? CategoryColor,
    int TotalVisits
);

public record CustomerNoteDto(int Id, string Note, string? UserName, DateTime CreatedAt);

public record CreateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? PhoneCountryCode { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Instagram { get; init; }
    public int? ClientCategoryId { get; init; }
    public int[]? TagIds { get; init; }
}

public record UpdateCustomerRequest : CreateCustomerRequest;

public record AddNoteRequest(string Note);

public record BlacklistRequest(string? Reason);

public record CustomerReservationDto(
    int Id,
    string? GuestName,
    int Covers,
    string Date,
    string Time,
    int DurationMinutes,
    ReservationStatus Status,
    ReservationType Type,
    SeatingType SeatingType,
    string? TableName,
    string? TimeSlotName,
    string? OutletName,
    string? Notes,
    decimal? AmountSpent,
    string? ClosingComments,
    DateTime CreatedAt
);

public record CustomerActivityLogDto(
    int Id,
    string Action,
    string? Description,
    string? DoneBy,
    DateTime CreatedAt
);
