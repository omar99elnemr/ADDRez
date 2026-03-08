using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum CustomerStatus
{
    [Description("Active")]
    Active,

    [Description("Blacklisted")]
    Blacklisted
}

public static class CustomerStatusExtensions
{
    public static string Label(this CustomerStatus status) => status switch
    {
        CustomerStatus.Active => "Active",
        CustomerStatus.Blacklisted => "Blacklisted",
        _ => status.ToString()
    };

    public static string Color(this CustomerStatus status) => status switch
    {
        CustomerStatus.Active => "#22c55e",
        CustomerStatus.Blacklisted => "#ef4444",
        _ => "#6b7280"
    };
}
