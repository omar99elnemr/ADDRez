namespace ADDRez.Api.Entities;

public class Landmark : BaseEntity
{
    public int FloorPlanId { get; set; }
    public FloorPlan FloorPlan { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Entrance, DJ Booth, Kitchen, Bar, Stage, etc.
    public string? Icon { get; set; }
    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public double Width { get; set; } = 60;
    public double Height { get; set; } = 60;
    public double Rotation { get; set; } = 0;
}
