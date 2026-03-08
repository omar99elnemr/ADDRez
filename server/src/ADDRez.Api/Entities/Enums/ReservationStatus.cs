using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum ReservationStatus
{
    [Description("Pending")]
    Pending,

    [Description("Confirmed")]
    Confirmed,

    [Description("Deposit Pending")]
    DepositPending,

    [Description("Checked In")]
    CheckedIn,

    [Description("Seated")]
    Seated,

    [Description("Checked Out")]
    CheckedOut,

    [Description("No Show")]
    NoShow,

    [Description("Cancelled")]
    Cancelled
}

public static class ReservationStatusExtensions
{
    public static string Label(this ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "Pending",
        ReservationStatus.Confirmed => "Confirmed",
        ReservationStatus.DepositPending => "Deposit Pending",
        ReservationStatus.CheckedIn => "Checked In",
        ReservationStatus.Seated => "Seated",
        ReservationStatus.CheckedOut => "Checked Out",
        ReservationStatus.NoShow => "No Show",
        ReservationStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public static string Color(this ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => "#f59e0b",
        ReservationStatus.Confirmed => "#22c55e",
        ReservationStatus.DepositPending => "#f97316",
        ReservationStatus.CheckedIn => "#3b82f6",
        ReservationStatus.Seated => "#8b5cf6",
        ReservationStatus.CheckedOut => "#6b7280",
        ReservationStatus.NoShow => "#ef4444",
        ReservationStatus.Cancelled => "#dc2626",
        _ => "#6b7280"
    };

    public static ReservationStatus[] AllowedTransitions(this ReservationStatus status) => status switch
    {
        ReservationStatus.Pending => [ReservationStatus.Confirmed, ReservationStatus.DepositPending, ReservationStatus.Cancelled],
        ReservationStatus.DepositPending => [ReservationStatus.Confirmed, ReservationStatus.Cancelled],
        ReservationStatus.Confirmed => [ReservationStatus.CheckedIn, ReservationStatus.Seated, ReservationStatus.NoShow, ReservationStatus.Cancelled],
        ReservationStatus.CheckedIn => [ReservationStatus.Seated, ReservationStatus.CheckedOut, ReservationStatus.Cancelled],
        ReservationStatus.Seated => [ReservationStatus.CheckedOut],
        ReservationStatus.CheckedOut => [],
        ReservationStatus.NoShow => [ReservationStatus.Confirmed],
        ReservationStatus.Cancelled => [ReservationStatus.Pending],
        _ => []
    };

    public static bool CanTransitionTo(this ReservationStatus current, ReservationStatus target)
        => current.AllowedTransitions().Contains(target);
}
