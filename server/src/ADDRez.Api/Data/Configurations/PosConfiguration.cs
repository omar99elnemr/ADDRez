using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class PosConfigurationEntityConfiguration : IEntityTypeConfiguration<Entities.PosConfiguration>
{
    public void Configure(EntityTypeBuilder<Entities.PosConfiguration> builder)
    {
        builder.ToTable("pos_configurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PosType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ApiUrl).HasMaxLength(500);
        builder.Property(e => e.ApiKey).HasMaxLength(500);
        builder.Property(e => e.Username).HasMaxLength(200);
        builder.Property(e => e.PasswordEncrypted).HasMaxLength(500);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany()
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PosTableMappingConfiguration : IEntityTypeConfiguration<PosTableMapping>
{
    public void Configure(EntityTypeBuilder<PosTableMapping> builder)
    {
        builder.ToTable("pos_table_mappings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PosTableId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PosTableName).HasMaxLength(200);

        builder.HasOne(e => e.PosConfiguration).WithMany()
            .HasForeignKey(e => e.PosConfigurationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Table).WithMany()
            .HasForeignKey(e => e.TableId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PosTransactionConfiguration : IEntityTypeConfiguration<PosTransaction>
{
    public void Configure(EntityTypeBuilder<PosTransaction> builder)
    {
        builder.ToTable("pos_transactions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PosCheckId).HasMaxLength(100).IsRequired();
        builder.Property(e => e.PosTableId).HasMaxLength(100);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.TipAmount).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany()
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Reservation).WithMany()
            .HasForeignKey(e => e.ReservationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Table).WithMany()
            .HasForeignKey(e => e.TableId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class GeneralConfigurationEntityConfiguration : IEntityTypeConfiguration<GeneralConfiguration>
{
    public void Configure(EntityTypeBuilder<GeneralConfiguration> builder)
    {
        builder.ToTable("general_configurations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Value).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => new { e.OutletId, e.Key }).IsUnique();

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany(b => b.GeneralConfigurations)
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
    }
}
