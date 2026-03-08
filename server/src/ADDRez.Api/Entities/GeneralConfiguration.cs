namespace ADDRez.Api.Entities;

public class GeneralConfiguration : OutletEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
