using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class OperationsLogConfiguration : IEntityTypeConfiguration<OperationsLog>
{
    public void Configure(EntityTypeBuilder<OperationsLog> builder)
    {
        builder.ToTable("operations_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Metadata).HasColumnType("text");
        builder.Property(e => e.IpAddress).HasMaxLength(50);

        builder.HasIndex(e => new { e.OutletId, e.CreatedAt });

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany(b => b.OperationsLogs)
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ChangesLogConfiguration : IEntityTypeConfiguration<ChangesLog>
{
    public void Configure(EntityTypeBuilder<ChangesLog> builder)
    {
        builder.ToTable("changes_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FieldName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.OldValue).HasMaxLength(2000);
        builder.Property(e => e.NewValue).HasMaxLength(2000);

        builder.HasOne(e => e.Reservation).WithMany(r => r.ChangesLogs)
            .HasForeignKey(e => e.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ImportHistoryConfiguration : IEntityTypeConfiguration<ImportHistory>
{
    public void Configure(EntityTypeBuilder<ImportHistory> builder)
    {
        builder.ToTable("import_histories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.ErrorLog).HasColumnType("text");

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class GeneratedReportConfiguration : IEntityTypeConfiguration<GeneratedReport>
{
    public void Configure(EntityTypeBuilder<GeneratedReport> builder)
    {
        builder.ToTable("generated_reports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ReportType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Format).HasMaxLength(10).IsRequired();
        builder.Property(e => e.FilePath).HasMaxLength(500);
        builder.Property(e => e.FileName).HasMaxLength(200);
        builder.Property(e => e.Status).HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}
