using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum ReservationMethod
{
    [Description("Phone")]
    Phone,

    [Description("Walk-in")]
    WalkIn,

    [Description("Online")]
    Online,

    [Description("Email")]
    Email,

    [Description("Social Media")]
    SocialMedia,

    [Description("App")]
    App
}

public static class ReservationMethodExtensions
{
    public static string Label(this ReservationMethod method) => method switch
    {
        ReservationMethod.Phone => "Phone",
        ReservationMethod.WalkIn => "Walk-in",
        ReservationMethod.Online => "Online",
        ReservationMethod.Email => "Email",
        ReservationMethod.SocialMedia => "Social Media",
        ReservationMethod.App => "App",
        _ => method.ToString()
    };
}
