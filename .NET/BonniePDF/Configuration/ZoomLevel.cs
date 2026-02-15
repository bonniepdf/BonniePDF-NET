namespace BonniePDF.Configuration;

/// <summary>
/// Defines the default zoom level when a PDF is opened
/// </summary>
public enum ZoomLevel
{
    /// <summary>
    /// Fit the entire page within the window
    /// </summary>
    FitPage,

    /// <summary>
    /// Fit the page width within the window
    /// </summary>
    FitWidth,

    /// <summary>
    /// Fit the page height within the window
    /// </summary>
    FitHeight,

    /// <summary>
    /// Use a custom zoom level (specified in DocumentSettings)
    /// </summary>
    Custom
}