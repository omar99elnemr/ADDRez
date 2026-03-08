namespace ADDRez.Api.Entities;

public class TimeSlot : OutletEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? LayoutId { get; set; }
    public Layout? Layout { get; set; }

    // Day-based (Mon-Sun flags) or date-range based
    public bool Monday { get; set; } = true;
    public bool Tuesday { get; set; } = true;
    public bool Wednesday { get; set; } = true;
    public bool Thursday { get; set; } = true;
    public bool Friday { get; set; } = true;
    public bool Saturday { get; set; } = true;
    public bool Sunday { get; set; } = true;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public int MaxCovers { get; set; } = 100;
    public int MaxReservations { get; set; } = 50;
    public int TurnTimeMinutes { get; set; } = 90;
    public int GracePeriodMinutes { get; set; } = 15;
    public bool RequireDeposit { get; set; } = false;
    public decimal DepositAmountPerPerson { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<TimeSlotCategoryExclusion> CategoryExclusions { get; set; } = [];
}
