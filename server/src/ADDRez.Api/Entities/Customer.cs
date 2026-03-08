using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class Customer : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PhoneCountryCode { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Instagram { get; set; }
    public int? ClientCategoryId { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public int TotalVisits { get; set; } = 0;
    public decimal TotalSpend { get; set; } = 0;
    public int NoShowCount { get; set; } = 0;
    public int CancellationCount { get; set; } = 0;
    public DateTime? LastVisitAt { get; set; }
    public string? BlacklistReason { get; set; }
    public DateTime? BlacklistedAt { get; set; }
    public string? CompanyName { get; set; }
    public string? Position { get; set; }
    public string? FacebookUrl { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
    public decimal AverageSpend => TotalVisits > 0 ? TotalSpend / TotalVisits : 0;

    // Navigation
    public ClientCategory? ClientCategory { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
    public ICollection<CustomerNote> Notes { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<CampaignRecipient> CampaignRecipients { get; set; } = [];
}
