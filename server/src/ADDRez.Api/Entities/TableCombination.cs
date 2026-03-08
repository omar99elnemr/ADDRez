namespace ADDRez.Api.Entities;

public class TableCombination : BaseEntity
{
    public int FloorPlanId { get; set; }
    public FloorPlan FloorPlan { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int MinCovers { get; set; } = 1;
    public int MaxCovers { get; set; } = 10;

    // Navigation
    public ICollection<TableCombinationItem> Items { get; set; } = [];
}
