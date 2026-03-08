using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.PhoneCountryCode).HasMaxLength(10);
        builder.Property(e => e.Gender).HasMaxLength(20);
        builder.Property(e => e.Nationality).HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.Country).HasMaxLength(100);
        builder.Property(e => e.Instagram).HasMaxLength(200);
        builder.Property(e => e.TotalSpend).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(e => new { e.CompanyId, e.Email });
        builder.HasIndex(e => new { e.CompanyId, e.Phone });

        builder.HasOne(e => e.Company).WithMany(c => c.Customers)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ClientCategory).WithMany(c => c.Customers)
            .HasForeignKey(e => e.ClientCategoryId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Tags).WithMany(t => t.Customers)
            .UsingEntity("customer_tags",
                l => l.HasOne(typeof(Tag)).WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(Customer)).WithMany().HasForeignKey("CustomerId").OnDelete(DeleteBehavior.Cascade));
    }
}

public class CustomerNoteConfiguration : IEntityTypeConfiguration<CustomerNote>
{
    public void Configure(EntityTypeBuilder<CustomerNote> builder)
    {
        builder.ToTable("customer_notes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Note).HasMaxLength(2000).IsRequired();

        builder.HasOne(e => e.Customer).WithMany(c => c.Notes)
            .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ClientCategoryConfiguration : IEntityTypeConfiguration<ClientCategory>
{
    public void Configure(EntityTypeBuilder<ClientCategory> builder)
    {
        builder.ToTable("client_categories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.Color).HasMaxLength(20);

        builder.HasOne(e => e.Company).WithMany(c => c.ClientCategories)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagCategoryConfiguration : IEntityTypeConfiguration<TagCategory>
{
    public void Configure(EntityTypeBuilder<TagCategory> builder)
    {
        builder.ToTable("tag_categories");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Icon).HasMaxLength(20);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany(c => c.TagCategories)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany(c => c.Tags)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.TagCategory).WithMany(tc => tc.Tags)
            .HasForeignKey(e => e.TagCategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Subject).HasMaxLength(500);
        builder.Property(e => e.Body).IsRequired();

        builder.HasOne(e => e.Company).WithMany(c => c.NotificationTemplates)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class TermsConditionConfiguration : IEntityTypeConfiguration<TermsCondition>
{
    public void Configure(EntityTypeBuilder<TermsCondition> builder)
    {
        builder.ToTable("terms_conditions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Content).IsRequired();

        builder.HasOne(e => e.Company).WithMany(c => c.TermsConditions)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
