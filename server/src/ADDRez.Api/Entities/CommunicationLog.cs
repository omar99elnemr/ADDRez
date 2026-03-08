using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class CommunicationLog : TenantEntity
{
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public int? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public CommunicationChannel Channel { get; set; }
    public CommunicationType Type { get; set; }
    public string? Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string Status { get; set; } = "pending"; // pending, sent, delivered, failed
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
}
