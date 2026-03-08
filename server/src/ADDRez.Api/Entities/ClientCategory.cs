namespace ADDRez.Api.Entities;

public class ClientCategory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6b7280";
    public int Priority { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<TimeSlotCategoryExclusion> TimeSlotExclusions { get; set; } = [];
}
