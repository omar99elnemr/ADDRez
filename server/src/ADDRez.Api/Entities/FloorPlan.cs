namespace ADDRez.Api.Entities;

public class FloorPlan : BaseEntity
{
    public int LayoutId { get; set; }
    public Layout Layout { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
    public string? BackgroundColor { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Table> Tables { get; set; } = [];
    public ICollection<Landmark> Landmarks { get; set; } = [];
}
