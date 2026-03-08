using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class TagCategory : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public TagType Type { get; set; }
    public int SortOrder { get; set; } = 0;

    // Navigation
    public ICollection<Tag> Tags { get; set; } = [];
}
