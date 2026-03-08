using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum CommunicationType
{
    [Description("Confirmation")]
    Confirmation,

    [Description("Cancellation")]
    Cancellation,

    [Description("Reminder")]
    Reminder,

    [Description("Update")]
    Update,

    [Description("Campaign")]
    Campaign,

    [Description("Birthday")]
    Birthday
}

public static class CommunicationTypeExtensions
{
    public static string Label(this CommunicationType type) => type switch
    {
        CommunicationType.Confirmation => "Confirmation",
        CommunicationType.Cancellation => "Cancellation",
        CommunicationType.Reminder => "Reminder",
        CommunicationType.Update => "Update",
        CommunicationType.Campaign => "Campaign",
        CommunicationType.Birthday => "Birthday",
        _ => type.ToString()
    };
}
