namespace ADDRez.Api.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public abstract class TenantEntity : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}

public abstract class OutletEntity : TenantEntity
{
    public int OutletId { get; set; }
    public Outlet Outlet { get; set; } = null!;
}
