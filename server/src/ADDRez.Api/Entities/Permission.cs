namespace ADDRez.Api.Entities;

public class Permission : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<Role> Roles { get; set; } = [];
}
