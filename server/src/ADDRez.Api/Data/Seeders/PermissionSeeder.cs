using ADDRez.Api.Entities;

namespace ADDRez.Api.Data.Seeders;

public static class PermissionSeeder
{
    public static List<Permission> GetPermissions()
    {
        var now = DateTime.UtcNow;
        var id = 1;

        return
        [
            // Dashboard
            P(id++, "dashboard.view", "View Dashboard", "Dashboard", now),

            // Reservations
            P(id++, "reservations.view", "View Reservations", "Reservations", now),
            P(id++, "reservations.create", "Create Reservations", "Reservations", now),
            P(id++, "reservations.edit", "Edit Reservations", "Reservations", now),
            P(id++, "reservations.delete", "Delete Reservations", "Reservations", now),
            P(id++, "reservations.status", "Change Reservation Status", "Reservations", now),
            P(id++, "reservations.assign_table", "Assign Table to Reservation", "Reservations", now),
            P(id++, "reservations.checkin", "Check In Reservation", "Reservations", now),
            P(id++, "reservations.checkout", "Check Out Reservation", "Reservations", now),
            P(id++, "reservations.mass_actions", "Perform Mass Actions", "Reservations", now),

            // Floor Plan
            P(id++, "floor_plan.view", "View Floor Plan", "Floor Plan", now),
            P(id++, "floor_plan.edit", "Edit Floor Plan", "Floor Plan", now),
            P(id++, "floor_plan.manage_layouts", "Manage Layouts", "Floor Plan", now),

            // Customers
            P(id++, "customers.view", "View Customers", "Customers", now),
            P(id++, "customers.create", "Create Customers", "Customers", now),
            P(id++, "customers.edit", "Edit Customers", "Customers", now),
            P(id++, "customers.delete", "Delete Customers", "Customers", now),
            P(id++, "customers.blacklist", "Blacklist/Unblacklist Customers", "Customers", now),
            P(id++, "customers.notes", "Manage Customer Notes", "Customers", now),
            P(id++, "customers.import", "Import Customers", "Customers", now),
            P(id++, "customers.export", "Export Customers", "Customers", now),

            // Guest Lists
            P(id++, "guest_lists.view", "View Guest Lists", "Guest Lists", now),
            P(id++, "guest_lists.manage", "Manage Guest List Items", "Guest Lists", now),
            P(id++, "guest_lists.checkin", "Check In Guests", "Guest Lists", now),

            // Time Slots
            P(id++, "time_slots.view", "View Time Slots", "Time Slots", now),
            P(id++, "time_slots.manage", "Manage Time Slots", "Time Slots", now),

            // Campaigns
            P(id++, "campaigns.view", "View Campaigns", "Campaigns", now),
            P(id++, "campaigns.create", "Create Campaigns", "Campaigns", now),
            P(id++, "campaigns.edit", "Edit Campaigns", "Campaigns", now),
            P(id++, "campaigns.delete", "Delete Campaigns", "Campaigns", now),
            P(id++, "campaigns.send", "Send Campaigns", "Campaigns", now),

            // Reports
            P(id++, "reports.view", "View Reports", "Reports", now),
            P(id++, "reports.export", "Export Reports", "Reports", now),

            // Settings
            P(id++, "settings.company", "Manage Company Settings", "Settings", now),
            P(id++, "settings.branches", "Manage Branches", "Settings", now),
            P(id++, "settings.users", "Manage Users", "Settings", now),
            P(id++, "settings.roles", "Manage Roles & Permissions", "Settings", now),
            P(id++, "settings.tags", "Manage Tags", "Settings", now),
            P(id++, "settings.categories", "Manage Client Categories", "Settings", now),
            P(id++, "settings.templates", "Manage Notification Templates", "Settings", now),
            P(id++, "settings.terms", "Manage Terms & Conditions", "Settings", now),
            P(id++, "settings.general", "Manage General Configuration", "Settings", now),
            P(id++, "settings.pos", "Manage POS Integration", "Settings", now),

            // Operations
            P(id++, "operations.view_log", "View Operations Log", "Operations", now),
            P(id, "operations.view_changes", "View Changes Log", "Operations", now),
        ];
    }

    private static Permission P(int id, string key, string name, string group, DateTime now) => new()
    {
        Id = id,
        Key = key,
        Name = name,
        Group = group,
        CreatedAt = now,
        UpdatedAt = now
    };
}
