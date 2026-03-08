namespace ADDRez.Api.Entities;

public class TableCombinationItem : BaseEntity
{
    public int TableCombinationId { get; set; }
    public TableCombination TableCombination { get; set; } = null!;

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;
}
