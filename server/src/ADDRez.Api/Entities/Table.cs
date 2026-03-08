using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class Table : BaseEntity
{
    public int FloorPlanId { get; set; }
    public FloorPlan FloorPlan { get; set; } = null!;

    public int? TableTypeId { get; set; }
    public TableType? TableType { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int MinCovers { get; set; } = 1;
    public int MaxCovers { get; set; } = 4;
    public TableShape Shape { get; set; } = TableShape.Round;
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 80;
    public double Height { get; set; } = 80;
    public double Rotation { get; set; } = 0;
    public bool IsCombinable { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<TableCombinationItem> CombinationItems { get; set; } = [];
}
