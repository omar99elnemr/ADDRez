namespace ADDRez.Api.Entities;

public class PosTableMapping : BaseEntity
{
    public int PosConfigurationId { get; set; }
    public PosConfiguration PosConfiguration { get; set; } = null!;

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public string PosTableId { get; set; } = string.Empty;
    public string? PosTableName { get; set; }
}
