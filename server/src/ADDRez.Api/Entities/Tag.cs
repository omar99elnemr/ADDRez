using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class Tag : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6b7280";
    public TagType Type { get; set; }
    public int? TagCategoryId { get; set; }

    // Navigation
    public TagCategory? TagCategory { get; set; }
    public ICollection<Customer> Customers { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
}
