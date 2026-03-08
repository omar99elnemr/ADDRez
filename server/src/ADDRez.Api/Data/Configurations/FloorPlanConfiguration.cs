using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class LayoutConfiguration : IEntityTypeConfiguration<Layout>
{
    public void Configure(EntityTypeBuilder<Layout> builder)
    {
        builder.ToTable("layouts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany(b => b.Layouts)
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FloorPlanConfiguration : IEntityTypeConfiguration<FloorPlan>
{
    public void Configure(EntityTypeBuilder<FloorPlan> builder)
    {
        builder.ToTable("floor_plans");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.BackgroundColor).HasMaxLength(20);

        builder.HasOne(e => e.Layout).WithMany(l => l.FloorPlans)
            .HasForeignKey(e => e.LayoutId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TableTypeConfiguration : IEntityTypeConfiguration<TableType>
{
    public void Configure(EntityTypeBuilder<TableType> builder)
    {
        builder.ToTable("table_types");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("tables");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(50);
        builder.Property(e => e.Shape).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(e => e.FloorPlan).WithMany(f => f.Tables)
            .HasForeignKey(e => e.FloorPlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TableType).WithMany(t => t.Tables)
            .HasForeignKey(e => e.TableTypeId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TableCombinationConfiguration : IEntityTypeConfiguration<TableCombination>
{
    public void Configure(EntityTypeBuilder<TableCombination> builder)
    {
        builder.ToTable("table_combinations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();

        builder.HasOne(e => e.FloorPlan).WithMany()
            .HasForeignKey(e => e.FloorPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TableCombinationItemConfiguration : IEntityTypeConfiguration<TableCombinationItem>
{
    public void Configure(EntityTypeBuilder<TableCombinationItem> builder)
    {
        builder.ToTable("table_combination_items");
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.TableCombination).WithMany(c => c.Items)
            .HasForeignKey(e => e.TableCombinationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Table).WithMany(t => t.CombinationItems)
            .HasForeignKey(e => e.TableId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LandmarkConfiguration : IEntityTypeConfiguration<Landmark>
{
    public void Configure(EntityTypeBuilder<Landmark> builder)
    {
        builder.ToTable("landmarks");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(100);

        builder.HasOne(e => e.FloorPlan).WithMany(f => f.Landmarks)
            .HasForeignKey(e => e.FloorPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}
