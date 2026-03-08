namespace ADDRez.Api.Entities;

public class OperationsLog : OutletEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; } // JSON string for extra data
    public string? IpAddress { get; set; }
}
