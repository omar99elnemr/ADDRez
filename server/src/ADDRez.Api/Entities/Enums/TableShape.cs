using System.ComponentModel;

namespace ADDRez.Api.Entities.Enums;

public enum TableShape
{
    [Description("Round")]
    Round,

    [Description("Rectangular")]
    Rectangular,

    [Description("Square")]
    Square,

    [Description("Booth")]
    Booth,

    [Description("Bar")]
    Bar
}

public static class TableShapeExtensions
{
    public static string Label(this TableShape shape) => shape switch
    {
        TableShape.Round => "Round",
        TableShape.Rectangular => "Rectangular",
        TableShape.Square => "Square",
        TableShape.Booth => "Booth",
        TableShape.Bar => "Bar",
        _ => shape.ToString()
    };
}
