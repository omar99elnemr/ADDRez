using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ADDRez.Api.Data.Configurations;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.ToTable("time_slots");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.DepositAmountPerPerson).HasPrecision(18, 2);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany(b => b.TimeSlots)
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Layout).WithMany(l => l.TimeSlots)
            .HasForeignKey(e => e.LayoutId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class TimeSlotCategoryExclusionConfiguration : IEntityTypeConfiguration<TimeSlotCategoryExclusion>
{
    public void Configure(EntityTypeBuilder<TimeSlotCategoryExclusion> builder)
    {
        builder.ToTable("time_slot_category_exclusions");
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.TimeSlot).WithMany(t => t.CategoryExclusions)
            .HasForeignKey(e => e.TimeSlotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.ClientCategory).WithMany(c => c.TimeSlotExclusions)
            .HasForeignKey(e => e.ClientCategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GuestName).HasMaxLength(200);
        builder.Property(e => e.GuestEmail).HasMaxLength(200);
        builder.Property(e => e.GuestPhone).HasMaxLength(50);
        builder.Property(e => e.GuestGender).HasMaxLength(20);
        builder.Property(e => e.MembershipNo).HasMaxLength(100);
        builder.Property(e => e.RoomNo).HasMaxLength(50);
        builder.Property(e => e.DiscountPercent).HasPrecision(5, 2);
        builder.Property(e => e.DiscountReason).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Method).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.SpecialRequests).HasMaxLength(2000);
        builder.Property(e => e.DepositAmount).HasPrecision(18, 2);
        builder.Property(e => e.QrCode).HasMaxLength(500);
        builder.Property(e => e.ConfirmationCode).HasMaxLength(50);
        builder.Property(e => e.CancellationReason).HasMaxLength(500);

        builder.HasIndex(e => new { e.OutletId, e.Date });
        builder.HasIndex(e => new { e.OutletId, e.Status });
        builder.HasIndex(e => e.ConfirmationCode);

        builder.HasOne(e => e.Company).WithMany()
            .HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Outlet).WithMany(b => b.Reservations)
            .HasForeignKey(e => e.OutletId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Customer).WithMany(c => c.Reservations)
            .HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.TimeSlot).WithMany(t => t.Reservations)
            .HasForeignKey(e => e.TimeSlotId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.Table).WithMany(t => t.Reservations)
            .HasForeignKey(e => e.TableId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(e => e.CreatedByUser).WithMany(u => u.CreatedReservations)
            .HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Tags).WithMany(t => t.Reservations)
            .UsingEntity("reservation_tags",
                l => l.HasOne(typeof(Tag)).WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne(typeof(Reservation)).WithMany().HasForeignKey("ReservationId").OnDelete(DeleteBehavior.Cascade));
    }
}

public class ReservationStatusHistoryConfiguration : IEntityTypeConfiguration<ReservationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ReservationStatusHistory> builder)
    {
        builder.ToTable("reservation_status_history");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.Reservation).WithMany(r => r.StatusHistory)
            .HasForeignKey(e => e.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.User).WithMany()
            .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class GuestListConfiguration : IEntityTypeConfiguration<GuestList>
{
    public void Configure(EntityTypeBuilder<GuestList> builder)
    {
        builder.ToTable("guest_lists");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200);

        builder.HasOne(e => e.Reservation).WithOne(r => r.GuestList)
            .HasForeignKey<GuestList>(e => e.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GuestListItemConfiguration : IEntityTypeConfiguration<GuestListItem>
{
    public void Configure(EntityTypeBuilder<GuestListItem> builder)
    {
        builder.ToTable("guest_list_items");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne(e => e.GuestList).WithMany(g => g.Items)
            .HasForeignKey(e => e.GuestListId).OnDelete(DeleteBehavior.Cascade);
    }
}
