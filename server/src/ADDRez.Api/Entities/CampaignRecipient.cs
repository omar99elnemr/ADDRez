namespace ADDRez.Api.Entities;

public class CampaignRecipient : BaseEntity
{
    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string? Status { get; set; } // sent, delivered, opened, failed
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
