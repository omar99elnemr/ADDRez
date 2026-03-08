using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum CommunicationChannel
{
    [Description("Email")]
    Email,

    [Description("SMS")]
    Sms,

    [Description("WhatsApp")]
    WhatsApp,

    [Description("Push Notification")]
    Push
}

public static class CommunicationChannelExtensions
{
    public static string Label(this CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.Email => "Email",
        CommunicationChannel.Sms => "SMS",
        CommunicationChannel.WhatsApp => "WhatsApp",
        CommunicationChannel.Push => "Push Notification",
        _ => channel.ToString()
    };
}
