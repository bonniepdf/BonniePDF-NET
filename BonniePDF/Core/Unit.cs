namespace BonniePDF;

/// <summary>
/// Measurement units for lengths and dimensions.
/// Following PDF specification, points (1/72 inch) are the default unit.
/// </summary>
public enum Unit
{
    /// <summary>Points - 1/72 inch (PDF default)</summary>
    Point,

    /// <summary>Inches - 72 points</summary>
    Inch,

    /// <summary>Centimeters - 28.35 points</summary>
    Centimeter,

    /// <summary>Millimeters - 2.835 points</summary>
    Millimeter
}