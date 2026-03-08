using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum ReservationType
{
    [Description("Dine In")]
    DineIn,

    [Description("Event")]
    Event,

    [Description("Birthday")]
    Birthday,

    [Description("Corporate")]
    Corporate,

    [Description("Group")]
    Group,

    [Description("Private Dining")]
    PrivateDining,

    [Description("Lounge")]
    Lounge,

    [Description("Takeaway")]
    Takeaway,

    [Description("Inhouse")]
    Inhouse
}

public static class ReservationTypeExtensions
{
    public static string Label(this ReservationType type) => type switch
    {
        ReservationType.DineIn => "Dine In",
        ReservationType.Event => "Event",
        ReservationType.Birthday => "Birthday",
        ReservationType.Corporate => "Corporate",
        ReservationType.Group => "Group",
        ReservationType.PrivateDining => "Private Dining",
        ReservationType.Lounge => "Lounge",
        ReservationType.Takeaway => "Takeaway",
        ReservationType.Inhouse => "Inhouse",
        _ => type.ToString()
    };
}
