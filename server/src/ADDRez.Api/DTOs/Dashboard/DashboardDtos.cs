namespace ADDRez.Api.DTOs.Dashboard;

public record DashboardResponse(
    DashboardKpis Kpis,
    IEnumerable<WeekChartPoint> WeekChart,
    IEnumerable<TopCustomerDto> TopCustomers,
    IEnumerable<ReservationSummaryDto> UnconfirmedQueue,
    IEnumerable<OutletStatDto> OutletStats,
    StatusDistribution StatusDistribution,
    IEnumerable<TodayReservationDto> TodayReservations,
    IEnumerable<FutureUnconfirmedDto> FutureUnconfirmed
);

public record DashboardKpis(
    int TodayReservations,
    double TodayReservationsChange,
    int TodayExpectedVisitors,
    double TodayExpectedVisitorsChange,
    int TodayCovers,
    int NewCustomersThisWeek,
    double NewCustomersChange,
    decimal TodayRevenue,
    double TodayRevenueChange,
    decimal WeekRevenue,
    double WeekRevenueChange,
    int PendingConfirmation,
    int CheckedIn,
    int Seated,
    int NoShows,
    int Cancelled,
    int TotalReservations,
    int TotalCancelled,
    decimal MonthlyRevenue
);

public record WeekChartPoint(string Date, string DayLabel, int Reservations, int WalkIns, int Covers);

public record TopCustomerDto(int Id, string Name, int TotalVisits, decimal TotalSpend, string? Category, string? CategoryColor);

public record ReservationSummaryDto(
    int Id,
    string? GuestName,
    int Covers,
    string Date,
    string Time,
    string Status,
    string? TableName
);

public record OutletStatDto(int Id, string Name, int TotalReservations, int TodayReservations);

public record StatusDistribution(
    int Confirmed,
    int Pending,
    int Seated,
    int CheckedIn,
    int CheckedOut,
    int NoShow,
    int Cancelled
);

public record TodayReservationDto(
    int Id,
    string? ConfirmationCode,
    string OutletName,
    string? GuestName,
    string? GuestPhone,
    int Covers,
    string Time,
    string Status,
    string StatusColor,
    string? TableName,
    string? TimeSlotName,
    string Method
);

public record FutureUnconfirmedDto(
    int Id,
    string OutletName,
    string Date,
    string? TimeSlotName,
    string? GuestName,
    string? GuestPhone,
    int Covers,
    string Type,
    string? Notes,
    string CreatedBy
);
