using ADDRez.Api.Entities;
using ADDRez.Api.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace ADDRez.Api.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Companies.AnyAsync()) return;

        var now = DateTime.UtcNow;

        // 1. Permissions
        var permissions = PermissionSeeder.GetPermissions();
        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();

        // 2. Company
        var company = new Company
        {
            Id = 1,
            Name = "ADDRez Demo",
            Email = "info@addrez.demo",
            Phone = "+20-2-2345-6789",
            Website = "https://addrez.demo",
            Timezone = "Africa/Cairo",
            DefaultCurrency = "EGP",
            DefaultLocale = "en",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        // 3. Venue
        var venue = new Venue
        {
            Id = 1,
            CompanyId = 1,
            Name = "The Grand Lounge",
            Description = "Premium dining and events venue",
            Address = "26th of July Corridor, Sheikh Zayed",
            City = "Cairo",
            Country = "Egypt",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();

        // 4. Outlets
        var outlets = new[]
        {
            new Outlet { Id = 1, CompanyId = 1, VenueId = 1, Name = "Parkfood", Address = "Arkan Plaza, Sheikh Zayed", Phone = "+20-2-2345-6790", Email = "parkfood@addrez.demo", DefaultGracePeriodMinutes = 15, DefaultTurnTimeMinutes = 90, AutoConfirmOnline = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 2, CompanyId = 1, VenueId = 1, Name = "Barranco", Address = "Galleria 40, Sheikh Zayed", Phone = "+20-2-2345-6791", Email = "barranco@addrez.demo", DefaultGracePeriodMinutes = 15, DefaultTurnTimeMinutes = 120, AutoConfirmOnline = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 3, CompanyId = 1, VenueId = 1, Name = "Plaza", Address = "Mall of Arabia, 6th of October", Phone = "+20-2-2345-6792", Email = "plaza@addrez.demo", DefaultGracePeriodMinutes = 10, DefaultTurnTimeMinutes = 90, AutoConfirmOnline = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 4, CompanyId = 1, VenueId = 1, Name = "C-West", Address = "Cairo West, Sheikh Zayed", Phone = "+20-2-2345-6793", Email = "cwest@addrez.demo", DefaultGracePeriodMinutes = 15, DefaultTurnTimeMinutes = 90, AutoConfirmOnline = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 5, CompanyId = 1, VenueId = 1, Name = "The Place", Address = "Designia, New Cairo", Phone = "+20-2-2345-6794", Email = "theplace@addrez.demo", DefaultGracePeriodMinutes = 15, DefaultTurnTimeMinutes = 120, AutoConfirmOnline = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 6, CompanyId = 1, VenueId = 1, Name = "ViVa Oro", Address = "Open Air Mall, Maadi", Phone = "+20-2-2345-6795", Email = "vivaoro@addrez.demo", DefaultGracePeriodMinutes = 10, DefaultTurnTimeMinutes = 90, AutoConfirmOnline = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 7, CompanyId = 1, VenueId = 1, Name = "The Muse", Address = "CFC, New Cairo", Phone = "+20-2-2345-6796", Email = "themuse@addrez.demo", DefaultGracePeriodMinutes = 15, DefaultTurnTimeMinutes = 120, AutoConfirmOnline = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Outlet { Id = 8, CompanyId = 1, VenueId = 1, Name = "Teppanyaki", Address = "Katameya Heights, New Cairo", Phone = "+20-2-2345-6797", Email = "teppanyaki@addrez.demo", DefaultGracePeriodMinutes = 10, DefaultTurnTimeMinutes = 90, AutoConfirmOnline = false, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.Outlets.AddRange(outlets);
        await db.SaveChangesAsync();

        // 5. Roles
        var allPermissionIds = permissions.Select(p => p.Id).ToList();

        var superAdmin = new Role
        {
            Id = 1, CompanyId = 1, Name = "Super Admin", Slug = "superadmin",
            Description = "Full system access", IsSystem = true, CreatedAt = now, UpdatedAt = now
        };
        var manager = new Role
        {
            Id = 2, CompanyId = 1, Name = "Manager", Slug = "manager",
            Description = "Branch management access", IsSystem = true, CreatedAt = now, UpdatedAt = now
        };
        var host = new Role
        {
            Id = 3, CompanyId = 1, Name = "Host", Slug = "host",
            Description = "Front desk operations", IsSystem = true, CreatedAt = now, UpdatedAt = now
        };
        var gateChecker = new Role
        {
            Id = 4, CompanyId = 1, Name = "Gate Checker", Slug = "gate-checker",
            Description = "Gate arrival management", IsSystem = true, CreatedAt = now, UpdatedAt = now
        };
        var viewer = new Role
        {
            Id = 5, CompanyId = 1, Name = "Viewer", Slug = "viewer",
            Description = "Read-only access", IsSystem = true, CreatedAt = now, UpdatedAt = now
        };

        db.Roles.AddRange(superAdmin, manager, host, gateChecker, viewer);
        await db.SaveChangesAsync();

        // Assign permissions to roles
        superAdmin.Permissions = [.. permissions];

        var managerExclude = new[] { "settings.company", "settings.roles", "settings.pos" };
        manager.Permissions = [.. permissions.Where(p => !managerExclude.Contains(p.Key))];

        var hostKeys = new[]
        {
            "dashboard.view", "reservations.view", "reservations.create", "reservations.edit",
            "reservations.status", "reservations.checkin", "reservations.checkout", "reservations.assign_table",
            "floor_plan.view", "customers.view", "customers.create", "customers.edit", "customers.notes",
            "guest_lists.view", "guest_lists.manage", "guest_lists.checkin", "time_slots.view"
        };
        host.Permissions = [.. permissions.Where(p => hostKeys.Contains(p.Key))];

        var gateKeys = new[]
        {
            "guest_lists.view", "guest_lists.checkin", "reservations.view", "reservations.checkin"
        };
        gateChecker.Permissions = [.. permissions.Where(p => gateKeys.Contains(p.Key))];

        var viewerKeys = new[]
        {
            "dashboard.view", "reservations.view", "floor_plan.view", "customers.view",
            "guest_lists.view", "time_slots.view", "reports.view"
        };
        viewer.Permissions = [.. permissions.Where(p => viewerKeys.Contains(p.Key))];

        await db.SaveChangesAsync();

        // 6. Users
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password");
        var users = new[]
        {
            new User
            {
                Id = 1, CompanyId = 1, Username = "admin", Email = "admin@addrez.demo",
                PasswordHash = passwordHash, FirstName = "Omar", LastName = "El-Nemr",
                Phone = "+20-100-000-0001", IsActive = true, CreatedAt = now, UpdatedAt = now,
                Roles = [superAdmin], Outlets = [outlets[0], outlets[1], outlets[2], outlets[3], outlets[4], outlets[5], outlets[6], outlets[7]]
            },
            new User
            {
                Id = 2, CompanyId = 1, Username = "sarah.mgr", Email = "manager@addrez.demo",
                PasswordHash = passwordHash, FirstName = "Sarah", LastName = "Hassan",
                Phone = "+20-100-000-0002", IsActive = true, CreatedAt = now, UpdatedAt = now,
                Roles = [manager], Outlets = [outlets[0], outlets[1], outlets[2]]
            },
            new User
            {
                Id = 3, CompanyId = 1, Username = "ahmed.host", Email = "host@addrez.demo",
                PasswordHash = passwordHash, FirstName = "Ahmed", LastName = "Khalil",
                Phone = "+20-100-000-0003", IsActive = true, CreatedAt = now, UpdatedAt = now,
                Roles = [host], Outlets = [outlets[0], outlets[1]]
            },
            new User
            {
                Id = 4, CompanyId = 1, Username = "fatima.gate", Email = "gate@addrez.demo",
                PasswordHash = passwordHash, FirstName = "Fatima", LastName = "Nour",
                Phone = "+20-100-000-0004", IsActive = true, CreatedAt = now, UpdatedAt = now,
                Roles = [gateChecker], Outlets = [outlets[0]]
            }
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // 7. Client Categories
        var categories = new[]
        {
            new ClientCategory { Id = 1, CompanyId = 1, Name = "VIP", Description = "Very Important Person", Color = "#d4a853", Priority = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new ClientCategory { Id = 2, CompanyId = 1, Name = "Regular", Description = "Regular customer", Color = "#22c55e", Priority = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new ClientCategory { Id = 3, CompanyId = 1, Name = "Corporate", Description = "Corporate account", Color = "#3b82f6", Priority = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new ClientCategory { Id = 4, CompanyId = 1, Name = "Influencer", Description = "Social media influencer", Color = "#a855f7", Priority = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new ClientCategory { Id = 5, CompanyId = 1, Name = "Friends & Family", Description = "Friends and family of staff", Color = "#ec4899", Priority = 5, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.ClientCategories.AddRange(categories);
        await db.SaveChangesAsync();

        // 8. Tags
        var tags = new[]
        {
            new Tag { Id = 1, CompanyId = 1, Name = "Allergies", Color = "#ef4444", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 2, CompanyId = 1, Name = "Birthday", Color = "#f59e0b", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 3, CompanyId = 1, Name = "Wheelchair", Color = "#3b82f6", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 4, CompanyId = 1, Name = "Window Seat", Color = "#22c55e", Type = TagType.Reservation, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 5, CompanyId = 1, Name = "Quiet Area", Color = "#8b5cf6", Type = TagType.Reservation, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 6, CompanyId = 1, Name = "Celebration", Color = "#ec4899", Type = TagType.Reservation, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 7, CompanyId = 1, Name = "Frequent Visitor", Color = "#06b6d4", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 8, CompanyId = 1, Name = "No Show Risk", Color = "#dc2626", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 9, CompanyId = 1, Name = "High Spender", Color = "#d4a853", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 10, CompanyId = 1, Name = "Vegetarian", Color = "#16a34a", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 11, CompanyId = 1, Name = "Influencer/Media", Color = "#7c3aed", Type = TagType.Customer, CreatedAt = now, UpdatedAt = now },
            new Tag { Id = 12, CompanyId = 1, Name = "Private Event", Color = "#0891b2", Type = TagType.Reservation, CreatedAt = now, UpdatedAt = now }
        };
        db.Tags.AddRange(tags);
        await db.SaveChangesAsync();

        // 9. Notification Templates
        var templates = new[]
        {
            new NotificationTemplate
            {
                Id = 1, CompanyId = 1, Name = "Reservation Confirmation",
                Type = CommunicationType.Confirmation, Channel = CommunicationChannel.Email,
                Subject = "Reservation Confirmed - {{venue_name}}",
                Body = "Dear {{guest_name}},\n\nYour reservation has been confirmed.\n\nDate: {{date}}\nTime: {{time}}\nParty Size: {{covers}}\nConfirmation Code: {{confirmation_code}}\n\nThank you,\n{{venue_name}}",
                IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = 2, CompanyId = 1, Name = "Reservation Cancellation",
                Type = CommunicationType.Cancellation, Channel = CommunicationChannel.Email,
                Subject = "Reservation Cancelled - {{venue_name}}",
                Body = "Dear {{guest_name}},\n\nYour reservation for {{date}} at {{time}} has been cancelled.\n\nIf this was a mistake, please contact us.\n\nThank you,\n{{venue_name}}",
                IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new NotificationTemplate
            {
                Id = 3, CompanyId = 1, Name = "Reservation Reminder",
                Type = CommunicationType.Reminder, Channel = CommunicationChannel.Email,
                Subject = "Reminder: Your Reservation Tomorrow - {{venue_name}}",
                Body = "Dear {{guest_name}},\n\nThis is a reminder for your reservation tomorrow.\n\nDate: {{date}}\nTime: {{time}}\nParty Size: {{covers}}\n\nWe look forward to seeing you!\n\n{{venue_name}}",
                IsActive = true, CreatedAt = now, UpdatedAt = now
            }
        };
        db.NotificationTemplates.AddRange(templates);
        await db.SaveChangesAsync();

        // 10. Terms & Conditions
        var terms = new[]
        {
            new TermsCondition { Id = 1, CompanyId = 1, Title = "Cancellation Policy", Content = "Reservations must be cancelled at least 4 hours before the scheduled time. Late cancellations may incur a fee.", SortOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TermsCondition { Id = 2, CompanyId = 1, Title = "No-Show Policy", Content = "Guests who do not arrive within the grace period will be marked as a no-show. Repeated no-shows may result in booking restrictions.", SortOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TermsCondition { Id = 3, CompanyId = 1, Title = "Deposit Policy", Content = "Certain time slots may require a deposit per person. Deposits are non-refundable for late cancellations or no-shows.", SortOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.TermsConditions.AddRange(terms);
        await db.SaveChangesAsync();

        // 11. Table Types
        var tableTypes = new[]
        {
            new TableType { Id = 1, CompanyId = 1, Name = "Standard", Description = "Standard dining table", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TableType { Id = 2, CompanyId = 1, Name = "Booth", Description = "Booth seating", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TableType { Id = 3, CompanyId = 1, Name = "Bar", Description = "Bar counter seating", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TableType { Id = 4, CompanyId = 1, Name = "Outdoor", Description = "Outdoor patio table", IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.TableTypes.AddRange(tableTypes);
        await db.SaveChangesAsync();

        // 12. Layouts & Floor Plans — each outlet gets a layout, some have multiple areas (floor plans)
        var layoutParkfood = new Layout { Id = 1, CompanyId = 1, OutletId = 1, Name = "Parkfood", Description = "Parkfood layout", IsDefault = true, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var layoutBarranco = new Layout { Id = 2, CompanyId = 1, OutletId = 2, Name = "Barranco", Description = "Barranco layout", IsDefault = true, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var layoutPlaza    = new Layout { Id = 3, CompanyId = 1, OutletId = 3, Name = "Plaza", Description = "Plaza layout", IsDefault = true, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var layoutCWest    = new Layout { Id = 4, CompanyId = 1, OutletId = 4, Name = "C-West", Description = "C-West layout", IsDefault = true, IsActive = true, CreatedAt = now, UpdatedAt = now };
        db.Layouts.AddRange(layoutParkfood, layoutBarranco, layoutPlaza, layoutCWest);
        await db.SaveChangesAsync();

        // Floor plans (areas) — Barranco has Indoor + Outdoor, others have single area
        var floorPlans = new[]
        {
            new FloorPlan { Id = 1, LayoutId = 1, Name = "Main Hall", SortOrder = 1, Width = 1200, Height = 800, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new FloorPlan { Id = 2, LayoutId = 2, Name = "Indoor", SortOrder = 1, Width = 1200, Height = 800, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new FloorPlan { Id = 3, LayoutId = 2, Name = "Outdoor", SortOrder = 2, Width = 1000, Height = 600, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new FloorPlan { Id = 4, LayoutId = 3, Name = "Main Floor", SortOrder = 1, Width = 1200, Height = 800, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new FloorPlan { Id = 5, LayoutId = 4, Name = "Ground Floor", SortOrder = 1, Width = 1200, Height = 800, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.FloorPlans.AddRange(floorPlans);
        await db.SaveChangesAsync();

        // 13. Tables — Parkfood (fp1), Barranco Indoor (fp2), Barranco Outdoor (fp3)
        var tables = new[]
        {
            // Parkfood — Main Hall
            new Table { Id = 1, FloorPlanId = 1, TableTypeId = 1, Name = "T1", Label = "1", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 100, Y = 100, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 2, FloorPlanId = 1, TableTypeId = 1, Name = "T2", Label = "2", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 250, Y = 100, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 3, FloorPlanId = 1, TableTypeId = 1, Name = "T3", Label = "3", MinCovers = 2, MaxCovers = 6, Shape = TableShape.Rectangular, X = 400, Y = 100, Width = 120, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 4, FloorPlanId = 1, TableTypeId = 2, Name = "B1", Label = "B1", MinCovers = 2, MaxCovers = 6, Shape = TableShape.Booth, X = 100, Y = 300, Width = 140, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 5, FloorPlanId = 1, TableTypeId = 3, Name = "Bar1", Label = "Bar", MinCovers = 1, MaxCovers = 2, Shape = TableShape.Bar, X = 600, Y = 100, Width = 60, Height = 60, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            // Barranco — Indoor
            new Table { Id = 6, FloorPlanId = 2, TableTypeId = 1, Name = "IN-1", Label = "1", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 100, Y = 100, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 7, FloorPlanId = 2, TableTypeId = 1, Name = "IN-2", Label = "2", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 250, Y = 100, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 8, FloorPlanId = 2, TableTypeId = 2, Name = "IN-B1", Label = "B1", MinCovers = 4, MaxCovers = 8, Shape = TableShape.Booth, X = 100, Y = 300, Width = 140, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 9, FloorPlanId = 2, TableTypeId = 1, Name = "IN-3", Label = "3", MinCovers = 4, MaxCovers = 10, Shape = TableShape.Rectangular, X = 400, Y = 100, Width = 160, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            // Barranco — Outdoor
            new Table { Id = 10, FloorPlanId = 3, TableTypeId = 4, Name = "OUT-1", Label = "O1", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 80, Y = 80, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 11, FloorPlanId = 3, TableTypeId = 4, Name = "OUT-2", Label = "O2", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 230, Y = 80, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 12, FloorPlanId = 3, TableTypeId = 4, Name = "OUT-3", Label = "O3", MinCovers = 4, MaxCovers = 6, Shape = TableShape.Rectangular, X = 380, Y = 80, Width = 120, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now },
            // Plaza — Main Floor
            new Table { Id = 13, FloorPlanId = 4, TableTypeId = 1, Name = "P1", Label = "1", MinCovers = 2, MaxCovers = 4, Shape = TableShape.Round, X = 100, Y = 100, Width = 80, Height = 80, IsCombinable = true, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Table { Id = 14, FloorPlanId = 4, TableTypeId = 1, Name = "P2", Label = "2", MinCovers = 4, MaxCovers = 8, Shape = TableShape.Rectangular, X = 300, Y = 100, Width = 140, Height = 80, IsCombinable = false, IsActive = true, CreatedAt = now, UpdatedAt = now }
        };
        db.Tables.AddRange(tables);
        await db.SaveChangesAsync();

        // 14. Landmarks
        var landmarks = new[]
        {
            new Landmark { Id = 1, FloorPlanId = 1, Name = "Entrance", Type = "Entrance", X = 50, Y = 700, Width = 80, Height = 40, CreatedAt = now, UpdatedAt = now },
            new Landmark { Id = 2, FloorPlanId = 1, Name = "Kitchen", Type = "Kitchen", X = 900, Y = 400, Width = 100, Height = 80, CreatedAt = now, UpdatedAt = now },
            new Landmark { Id = 3, FloorPlanId = 2, Name = "Entrance", Type = "Entrance", X = 50, Y = 700, Width = 80, Height = 40, CreatedAt = now, UpdatedAt = now },
            new Landmark { Id = 4, FloorPlanId = 2, Name = "Bar Counter", Type = "Bar", X = 550, Y = 50, Width = 200, Height = 40, CreatedAt = now, UpdatedAt = now },
            new Landmark { Id = 5, FloorPlanId = 3, Name = "Garden Gate", Type = "Entrance", X = 50, Y = 500, Width = 80, Height = 40, CreatedAt = now, UpdatedAt = now }
        };
        db.Landmarks.AddRange(landmarks);
        await db.SaveChangesAsync();

        // 15. Time Slots
        var timeSlots = new[]
        {
            new TimeSlot
            {
                Id = 1, CompanyId = 1, OutletId = 1, Name = "Lunch", LayoutId = 1,
                StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(15, 0),
                Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true, Sunday = true,
                MaxCovers = 50, MaxReservations = 25, TurnTimeMinutes = 90, GracePeriodMinutes = 15,
                RequireDeposit = false, IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new TimeSlot
            {
                Id = 2, CompanyId = 1, OutletId = 1, Name = "Dinner", LayoutId = 1,
                StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(23, 0),
                Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true, Sunday = true,
                MaxCovers = 80, MaxReservations = 40, TurnTimeMinutes = 120, GracePeriodMinutes = 15,
                RequireDeposit = false, IsActive = true, CreatedAt = now, UpdatedAt = now
            },
            new TimeSlot
            {
                Id = 3, CompanyId = 1, OutletId = 1, Name = "Weekend Brunch",
                StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(14, 0),
                Monday = false, Tuesday = false, Wednesday = false, Thursday = false, Friday = true, Saturday = true, Sunday = true,
                MaxCovers = 60, MaxReservations = 30, TurnTimeMinutes = 90, GracePeriodMinutes = 15,
                RequireDeposit = true, DepositAmountPerPerson = 50, IsActive = true, CreatedAt = now, UpdatedAt = now
            }
        };
        db.TimeSlots.AddRange(timeSlots);
        await db.SaveChangesAsync();

        // 16. Demo Customers
        var customers = new[]
        {
            new Customer { Id = 1, CompanyId = 1, FirstName = "Youssef", LastName = "Mansour", Email = "youssef.m@example.com", Phone = "+201001234567", PhoneCountryCode = "+20", ClientCategoryId = 1, Status = CustomerStatus.Active, TotalVisits = 12, TotalSpend = 18500, DateOfBirth = new DateTime(1985, 3, 15, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", City = "Cairo", Country = "Egypt", CompanyName = "Mansour Group", Position = "CEO", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 2, CompanyId = 1, FirstName = "Nour", LastName = "Ibrahim", Email = "nour.i@example.com", Phone = "+201012345678", PhoneCountryCode = "+20", ClientCategoryId = 2, Status = CustomerStatus.Active, TotalVisits = 5, TotalSpend = 4200, DateOfBirth = new DateTime(1992, 7, 22, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", City = "Giza", Country = "Egypt", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 3, CompanyId = 1, FirstName = "Karim", LastName = "El-Sayed", Email = "karim.s@example.com", Phone = "+201023456789", PhoneCountryCode = "+20", ClientCategoryId = 3, Status = CustomerStatus.Active, TotalVisits = 8, TotalSpend = 12800, DateOfBirth = new DateTime(1988, 11, 5, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", City = "Cairo", Country = "Egypt", CompanyName = "TechCorp Egypt", Position = "CTO", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 4, CompanyId = 1, FirstName = "Dalal", LastName = "Younis", Email = "dalal@example.com", Phone = "+201034567890", PhoneCountryCode = "+20", ClientCategoryId = 4, Status = CustomerStatus.Active, TotalVisits = 3, TotalSpend = 2400, DateOfBirth = new DateTime(1995, 2, 3, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", City = "Cairo", Country = "Egypt", Instagram = "@dalal_foodie", FacebookUrl = "https://facebook.com/dalal.younis", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 5, CompanyId = 1, FirstName = "Hassan", LastName = "Morsi", Email = "hassan.m@example.com", Phone = "+201045678901", PhoneCountryCode = "+20", ClientCategoryId = 2, Status = CustomerStatus.Active, TotalVisits = 15, TotalSpend = 24400, DateOfBirth = new DateTime(1980, 9, 18, 0, 0, 0, DateTimeKind.Utc), Gender = "Male", City = "6th of October", Country = "Egypt", CreatedAt = now, UpdatedAt = now },
            new Customer { Id = 6, CompanyId = 1, FirstName = "Mona", LastName = "Adel", Email = "mona.a@example.com", Phone = "+201056789012", PhoneCountryCode = "+20", ClientCategoryId = 5, Status = CustomerStatus.Active, TotalVisits = 20, TotalSpend = 15000, DateOfBirth = new DateTime(1990, 12, 25, 0, 0, 0, DateTimeKind.Utc), Gender = "Female", City = "New Cairo", Country = "Egypt", CreatedAt = now, UpdatedAt = now }
        };
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // 17. Demo Reservations
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reservations = new[]
        {
            new Reservation
            {
                Id = 1, CompanyId = 1, OutletId = 1, CustomerId = 1, TimeSlotId = 2, TableId = 3,
                CreatedByUserId = 1, Covers = 4, Date = today, Time = new TimeOnly(19, 0),
                DurationMinutes = 120, Status = ReservationStatus.Confirmed, Type = ReservationType.DineIn,
                Method = ReservationMethod.Phone, ConfirmationCode = "ADR-001",
                CreatedAt = now, UpdatedAt = now
            },
            new Reservation
            {
                Id = 2, CompanyId = 1, OutletId = 1, CustomerId = 2, TimeSlotId = 2, TableId = 1,
                CreatedByUserId = 1, Covers = 2, Date = today, Time = new TimeOnly(20, 0),
                DurationMinutes = 90, Status = ReservationStatus.Pending, Type = ReservationType.DineIn,
                Method = ReservationMethod.Online, ConfirmationCode = "ADR-002",
                CreatedAt = now, UpdatedAt = now
            },
            new Reservation
            {
                Id = 3, CompanyId = 1, OutletId = 1, CustomerId = 3, TimeSlotId = 1, TableId = 3,
                CreatedByUserId = 2, Covers = 6, Date = today, Time = new TimeOnly(12, 30),
                DurationMinutes = 90, Status = ReservationStatus.Seated, Type = ReservationType.Corporate,
                Method = ReservationMethod.Email, ConfirmationCode = "ADR-003",
                SeatedAt = now.AddHours(-1), CheckedInAt = now.AddHours(-1).AddMinutes(-10),
                CreatedAt = now, UpdatedAt = now
            },
            new Reservation
            {
                Id = 4, CompanyId = 1, OutletId = 1, CustomerId = 5, TimeSlotId = 2, TableId = 4,
                CreatedByUserId = 3, Covers = 3, Date = today.AddDays(1), Time = new TimeOnly(19, 30),
                DurationMinutes = 120, Status = ReservationStatus.Confirmed, Type = ReservationType.Birthday,
                Method = ReservationMethod.Phone, Notes = "Birthday celebration - bring cake at dessert",
                ConfirmationCode = "ADR-004",
                CreatedAt = now, UpdatedAt = now
            }
        };
        db.Reservations.AddRange(reservations);
        await db.SaveChangesAsync();

        // Reset PostgreSQL identity sequences after seeding with explicit IDs
        var sequenceResets = new[]
        {
            "SELECT setval(pg_get_serial_sequence('companies', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM companies))",
            "SELECT setval(pg_get_serial_sequence('venues', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM venues))",
            "SELECT setval(pg_get_serial_sequence('outlets', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM outlets))",
            "SELECT setval(pg_get_serial_sequence('outlet_areas', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM outlet_areas))",
            "SELECT setval(pg_get_serial_sequence('roles', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM roles))",
            "SELECT setval(pg_get_serial_sequence('users', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM users))",
            "SELECT setval(pg_get_serial_sequence('client_categories', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM client_categories))",
            "SELECT setval(pg_get_serial_sequence('tags', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM tags))",
            "SELECT setval(pg_get_serial_sequence('tag_categories', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM tag_categories))",
            "SELECT setval(pg_get_serial_sequence('notification_templates', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM notification_templates))",
            "SELECT setval(pg_get_serial_sequence('terms_conditions', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM terms_conditions))",
            "SELECT setval(pg_get_serial_sequence('layouts', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM layouts))",
            "SELECT setval(pg_get_serial_sequence('table_types', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM table_types))",
            "SELECT setval(pg_get_serial_sequence('tables', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM tables))",
            "SELECT setval(pg_get_serial_sequence('time_slots', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM time_slots))",
            "SELECT setval(pg_get_serial_sequence('customers', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM customers))",
            "SELECT setval(pg_get_serial_sequence('reservations', 'Id'), (SELECT COALESCE(MAX(\"Id\"),0) FROM reservations))",
        };
        foreach (var sql in sequenceResets)
        {
            try { await db.Database.ExecuteSqlRawAsync(sql); } catch { /* ignore if table doesn't exist */ }
        }
    }
}
