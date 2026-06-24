using BonniePDF.Core;
using BonniePDF.Elements.Shapes;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for Data Matrix symbols.
/// </summary>
public static class DataMatrixExtensions
{
    /// <summary>
    /// Sets the colour of the dark modules.
    /// </summary>
    public static DataMatrix ForegroundColor(this DataMatrix dm, Color color)
    {
        dm.ForegroundColor = color;
        return dm;
    }

    /// <summary>
    /// Sets the background colour of the symbol.
    /// </summary>
    public static DataMatrix BackgroundColor(this DataMatrix dm, Color color)
    {
        dm.BackgroundColor = color;
        return dm;
    }

    /// <summary>
    /// Toggles drawing of the symbol background.
    /// </summary>
    public static DataMatrix DrawBackground(this DataMatrix dm, bool draw)
    {
        dm.DrawBackground = draw;
        return dm;
    }

    /// <summary>
    /// Requests a rectangular symbol where one is available.
    /// </summary>
    public static DataMatrix Rectangular(this DataMatrix dm, bool rectangular)
    {
        dm.Rectangular = rectangular;
        return dm;
    }

    /// <summary>
    /// Sets the number of quiet-zone modules around the symbol.
    /// </summary>
    public static DataMatrix QuietZone(this DataMatrix dm, int modules)
    {
        dm.QuietZone = modules;
        return dm;
    }

    /// <summary>Sets the alternate text for accessibility (PDF/UA-1).</summary>
    public static DataMatrix AltText(this DataMatrix dm, string altText)
    {
        dm.AltText = altText;
        return dm;
    }
}
