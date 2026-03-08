using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum SeatingType
{
    [Description("Seated")]
    Seated,

    [Description("Standing")]
    Standing
}

public static class SeatingTypeExtensions
{
    public static string Label(this SeatingType type) => type switch
    {
        SeatingType.Seated => "Seated",
        SeatingType.Standing => "Standing",
        _ => type.ToString()
    };
}
