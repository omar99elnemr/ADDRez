using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class Campaign : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Email;
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    // Target audience: all, category, tag, manual
    public string TargetAudience { get; set; } = "all";
    public int? TargetCategoryId { get; set; }
    public int? TargetTagId { get; set; }

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public int TotalRecipients { get; set; } = 0;
    public int SentCount { get; set; } = 0;
    public int OpenCount { get; set; } = 0;
    public int FailedCount { get; set; } = 0;

    // Navigation
    public ICollection<CampaignRecipient> Recipients { get; set; } = [];
}
