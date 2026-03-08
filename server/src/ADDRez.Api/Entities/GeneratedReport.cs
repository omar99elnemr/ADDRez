namespace ADDRez.Api.Entities;

public class GeneratedReport : TenantEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string ReportType { get; set; } = string.Empty; // summary, reservations, customers
    public string Format { get; set; } = "xlsx"; // xlsx, pdf
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public int? OutletId { get; set; }
    public string Status { get; set; } = "pending"; // pending, generating, completed, failed
}
