namespace ADDRez.Api.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public string DefaultCurrency { get; set; } = "USD";
    public string DefaultLocale { get; set; } = "en";
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Venue> Venues { get; set; } = [];
    public ICollection<Outlet> Outlets { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<ClientCategory> ClientCategories { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<TagCategory> TagCategories { get; set; } = [];
    public ICollection<NotificationTemplate> NotificationTemplates { get; set; } = [];
    public ICollection<TermsCondition> TermsConditions { get; set; } = [];
    public ICollection<Campaign> Campaigns { get; set; } = [];
    public ICollection<CommunicationLog> CommunicationLogs { get; set; } = [];
}
