using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Username).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200).IsRequired();
        builder.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.AvatarUrl).HasMaxLength(500);

        builder.HasIndex(e => new { e.CompanyId, e.Username }).IsUnique();
        builder.HasIndex(e => new { e.CompanyId, e.Email }).IsUnique();

        builder.HasOne(e => e.Company).WithMany(c => c.Users)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Roles).WithMany(r => r.Users)
            .UsingEntity("user_roles",
                l => l.HasOne(typeof(Role)).WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));

        builder.HasMany(e => e.Outlets).WithMany(b => b.Users)
            .UsingEntity("user_outlets",
                l => l.HasOne(typeof(Outlet)).WithMany().HasForeignKey("OutletId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(User)).WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade));
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => new { e.CompanyId, e.Slug }).IsUnique();

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Permissions).WithMany(p => p.Roles)
            .UsingEntity("role_permissions",
                l => l.HasOne(typeof(Permission)).WithMany().HasForeignKey("PermissionId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(Role)).WithMany().HasForeignKey("RoleId").OnDelete(DeleteBehavior.Cascade));
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Group).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder.HasIndex(e => e.Key).IsUnique();
    }
}
