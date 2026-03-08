using ADDRez.Api.Entities.Enums;

namespace ADDRez.Api.Entities;

public class GuestListItem : BaseEntity
{
    public int GuestListId { get; set; }
    public GuestList GuestList { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Covers { get; set; } = 1;
    public GuestListItemStatus Status { get; set; } = GuestListItemStatus.Invited;
    public string? Notes { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
