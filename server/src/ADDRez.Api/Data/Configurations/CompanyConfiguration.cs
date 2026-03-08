using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Website).HasMaxLength(500);
        builder.Property(e => e.LogoUrl).HasMaxLength(500);
        builder.Property(e => e.Timezone).HasMaxLength(100).HasDefaultValue("UTC");
        builder.Property(e => e.DefaultCurrency).HasMaxLength(10).HasDefaultValue("USD");
        builder.Property(e => e.DefaultLocale).HasMaxLength(10).HasDefaultValue("en");
    }
}

public class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.Country).HasMaxLength(100);

        builder.HasOne(e => e.Company).WithMany(c => c.Venues)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("outlets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Email).HasMaxLength(200);

        builder.HasOne(e => e.Company).WithMany(c => c.Outlets)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Venue).WithMany(v => v.Outlets)
            .HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Cascade);
    }
}
