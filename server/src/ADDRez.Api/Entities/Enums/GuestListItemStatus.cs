using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum GuestListItemStatus
{
    [Description("Invited")]
    Invited,

    [Description("Confirmed")]
    Confirmed,

    [Description("Checked In")]
    CheckedIn,

    [Description("Cancelled")]
    Cancelled,

    [Description("No Show")]
    NoShow
}

public static class GuestListItemStatusExtensions
{
    public static string Label(this GuestListItemStatus status) => status switch
    {
        GuestListItemStatus.Invited => "Invited",
        GuestListItemStatus.Confirmed => "Confirmed",
        GuestListItemStatus.CheckedIn => "Checked In",
        GuestListItemStatus.Cancelled => "Cancelled",
        GuestListItemStatus.NoShow => "No Show",
        _ => status.ToString()
    };

    public static string Color(this GuestListItemStatus status) => status switch
    {
        GuestListItemStatus.Invited => "#f59e0b",
        GuestListItemStatus.Confirmed => "#22c55e",
        GuestListItemStatus.CheckedIn => "#3b82f6",
        GuestListItemStatus.Cancelled => "#dc2626",
        GuestListItemStatus.NoShow => "#ef4444",
        _ => "#6b7280"
    };
}
