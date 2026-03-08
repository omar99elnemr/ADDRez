namespace ADDRez.Api.Entities;

public class ImportHistory : TenantEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty; // customer, reservation, guest_list
    public int TotalRows { get; set; } = 0;
    public int SuccessCount { get; set; } = 0;
    public int FailedCount { get; set; } = 0;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public string? ErrorLog { get; set; }
}
