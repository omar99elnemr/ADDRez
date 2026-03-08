namespace ADDRez.Api.Entities;

public class PosConfiguration : OutletEntity
{
    public string PosType { get; set; } = "micros"; // micros, other
    public string? ApiUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? PasswordEncrypted { get; set; }
    public bool IsActive { get; set; } = false;
    public DateTime? LastSyncAt { get; set; }
}
