using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum CampaignStatus
{
    [Description("Draft")]
    Draft,

    [Description("Scheduled")]
    Scheduled,

    [Description("Sending")]
    Sending,

    [Description("Sent")]
    Sent,

    [Description("Failed")]
    Failed
}

public static class CampaignStatusExtensions
{
    public static string Label(this CampaignStatus status) => status switch
    {
        CampaignStatus.Draft => "Draft",
        CampaignStatus.Scheduled => "Scheduled",
        CampaignStatus.Sending => "Sending",
        CampaignStatus.Sent => "Sent",
        CampaignStatus.Failed => "Failed",
        _ => status.ToString()
    };

    public static string Color(this CampaignStatus status) => status switch
    {
        CampaignStatus.Draft => "#6b7280",
        CampaignStatus.Scheduled => "#f59e0b",
        CampaignStatus.Sending => "#3b82f6",
        CampaignStatus.Sent => "#22c55e",
        CampaignStatus.Failed => "#ef4444",
        _ => "#6b7280"
    };
}
