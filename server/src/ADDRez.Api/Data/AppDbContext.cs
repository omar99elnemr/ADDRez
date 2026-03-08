using ADDRez.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Data;

public class AppDbContext : DbContext
{
    private int? _currentCompanyId;
    private int? _currentOutletId;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Tenant & Auth
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Outlet> Outlets => Set<Outlet>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    // CRM & Settings
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerNote> CustomerNotes => Set<CustomerNote>();
    public DbSet<ClientCategory> ClientCategories => Set<ClientCategory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagCategory> TagCategories => Set<TagCategory>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<TermsCondition> TermsConditions => Set<TermsCondition>();

    // Floor Plan
    public DbSet<Layout> Layouts => Set<Layout>();
    public DbSet<FloorPlan> FloorPlans => Set<FloorPlan>();
    public DbSet<TableType> TableTypes => Set<TableType>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<TableCombination> TableCombinations => Set<TableCombination>();
    public DbSet<TableCombinationItem> TableCombinationItems => Set<TableCombinationItem>();
    public DbSet<Landmark> Landmarks => Set<Landmark>();

    // Time Slots
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<TimeSlotCategoryExclusion> TimeSlotCategoryExclusions => Set<TimeSlotCategoryExclusion>();

    // Reservations
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationStatusHistory> ReservationStatusHistories => Set<ReservationStatusHistory>();

    // Guest Lists
    public DbSet<GuestList> GuestLists => Set<GuestList>();
    public DbSet<GuestListItem> GuestListItems => Set<GuestListItem>();

    // Campaigns & Communication
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<BirthdayReminderConfig> BirthdayReminderConfigs => Set<BirthdayReminderConfig>();
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();

    // Logs & Audit
    public DbSet<OperationsLog> OperationsLogs => Set<OperationsLog>();
    public DbSet<ChangesLog> ChangesLogs => Set<ChangesLog>();
    public DbSet<ImportHistory> ImportHistories => Set<ImportHistory>();
    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();

    // POS
    public DbSet<PosConfiguration> PosConfigurations => Set<PosConfiguration>();
    public DbSet<PosTableMapping> PosTableMappings => Set<PosTableMapping>();
    public DbSet<PosTransaction> PosTransactions => Set<PosTransaction>();

    // Configuration
    public DbSet<GeneralConfiguration> GeneralConfigurations => Set<GeneralConfiguration>();

    public void SetTenantId(int companyId) => _currentCompanyId = companyId;
    public void SetOutletId(int outletId) => _currentOutletId = outletId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters for multi-tenancy
        modelBuilder.Entity<Venue>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Outlet>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<User>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Role>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<ClientCategory>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Tag>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<TagCategory>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<NotificationTemplate>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<TermsCondition>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Layout>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<TableType>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<TimeSlot>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Reservation>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<Campaign>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<CommunicationLog>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<OperationsLog>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<ImportHistory>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<GeneratedReport>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<BirthdayReminderConfig>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<PosConfiguration>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<PosTransaction>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
        modelBuilder.Entity<GeneralConfiguration>().HasQueryFilter(e => _currentCompanyId == null || e.CompanyId == _currentCompanyId);
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
