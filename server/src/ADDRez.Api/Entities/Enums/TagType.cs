using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum TagType
{
    [Description("Customer")]
    Customer,

    [Description("Reservation")]
    Reservation
}

public static class TagTypeExtensions
{
    public static string Label(this TagType type) => type switch
    {
        TagType.Customer => "Customer",
        TagType.Reservation => "Reservation",
        _ => type.ToString()
    };
}
