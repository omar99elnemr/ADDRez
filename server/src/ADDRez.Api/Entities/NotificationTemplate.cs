using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class NotificationTemplate : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public CommunicationType Type { get; set; }
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Email;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
