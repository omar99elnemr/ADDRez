using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Subject).HasMaxLength(500);
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.TargetAudience).HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany(c => c.Campaigns)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.CreatedByUser).WithMany(u => u.CreatedCampaigns)
            .HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
    public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
    {
        builder.ToTable("campaign_recipients");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);

        builder.HasOne(e => e.Campaign).WithMany(c => c.Recipients)
            .HasForeignKey(e => e.CampaignId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Customer).WithMany(c => c.CampaignRecipients)
            .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BirthdayReminderConfigConfiguration : IEntityTypeConfiguration<BirthdayReminderConfig>
{
    public void Configure(EntityTypeBuilder<BirthdayReminderConfig> builder)
    {
        builder.ToTable("birthday_reminder_configs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Template).WithMany()
            .HasForeignKey(e => e.TemplateId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CommunicationLogConfiguration : IEntityTypeConfiguration<CommunicationLog>
{
    public void Configure(EntityTypeBuilder<CommunicationLog> builder)
    {
        builder.ToTable("communication_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Recipient).HasMaxLength(200);
        builder.Property(e => e.Subject).HasMaxLength(500);
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.ErrorMessage).HasMaxLength(1000);

        builder.HasOne(e => e.Company).WithMany(c => c.CommunicationLogs)
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Customer).WithMany()
            .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Reservation).WithMany()
            .HasForeignKey(e => e.ReservationId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Campaign).WithMany()
            .HasForeignKey(e => e.CampaignId).OnDelete(DeleteBehavior.SetNull);
    }
}
