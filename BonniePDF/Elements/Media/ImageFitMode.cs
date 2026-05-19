namespace BonniePDF.Elements.Media;

/// <summary>
/// How to fit an image within its bounds
/// </summary>
public enum ImageFitMode
{
    /// <summary>Scale to fit within bounds, maintain aspect ratio</summary>
    Contain,

    /// <summary>Scale to fill bounds, maintain aspect ratio, crop if needed</summary>
    Cover,

    /// <summary>Stretch to fill bounds, ignore aspect ratio</summary>
    Fill,

    /// <summary>Scale to fit height, maintain aspect ratio</summary>
    FitHeight,

    /// <summary>Use actual image size (1 pixel = 1 point)</summary>
    ActualSize
}