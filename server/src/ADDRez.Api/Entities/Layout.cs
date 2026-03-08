namespace ADDRez.Api.Entities;

public class Layout : OutletEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<FloorPlan> FloorPlans { get; set; } = [];
    public ICollection<TimeSlot> TimeSlots { get; set; } = [];
}
