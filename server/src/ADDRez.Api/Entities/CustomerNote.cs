namespace ADDRez.Api.Entities;

public class CustomerNote : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Note { get; set; } = string.Empty;
}
