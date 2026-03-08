using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class BirthdayReminderConfig : TenantEntity
{
    public int DaysBefore { get; set; } = 7;
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Email;
    public int? TemplateId { get; set; }
    public NotificationTemplate? Template { get; set; }
    public bool IsActive { get; set; } = true;
}
