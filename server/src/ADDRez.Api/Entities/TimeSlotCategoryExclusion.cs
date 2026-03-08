namespace ADDRez.Api.Entities;

public class TimeSlotCategoryExclusion : BaseEntity
{
    public int TimeSlotId { get; set; }
    public TimeSlot TimeSlot { get; set; } = null!;

    public int ClientCategoryId { get; set; }
    public ClientCategory ClientCategory { get; set; } = null!;
}
