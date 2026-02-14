namespace BonniePDF.Models;

/// <summary>
/// Provides semantic border width presets for common use cases.
/// All values are in points (1/72 inch).
/// </summary>
public static class BorderStyle
{
    public const float None = 0f;

    /// <summary>
    /// Hairline border - the thinnest practical line (0.25 points).
    /// May not render on all screens but prints well.
    /// </summary>
    public const float Hairline = 0.25f;

    /// <summary>
    /// Thin border (0.5 points) - subtle but visible on most displays.
    /// Good for table cell borders and subtle separators.
    /// </summary>
    public const float Thin = 0.5f;

    /// <summary>
    /// Normal border (1 point) - standard visible border.
    /// The PDF default, reliable across all renderers.
    /// </summary>
    public const float Normal = 1f;

    /// <summary>
    /// Medium border (1.5 points) - slightly emphasized.
    /// Good for section separators or highlighted elements.
    /// </summary>
    public const float Medium = 1.5f;

    /// <summary>
    /// Thick border (2 points) - prominent border.
    /// Good for emphasis or outer container borders.
    /// </summary>
    public const float Thick = 2f;

    /// <summary>
    /// Heavy border (3 points) - very prominent.
    /// Use sparingly for strong visual emphasis.
    /// </summary>
    public const float Heavy = 3f;
}