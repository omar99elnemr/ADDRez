namespace ADDRez.Api.Entities;

public class TableType : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Table> Tables { get; set; } = [];
}
