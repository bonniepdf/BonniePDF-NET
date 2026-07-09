using BonniePDF.Core;

namespace BonniePDF.Configuration;

/// <summary>
/// Configuration for rendering a text watermark over page content.
/// The watermark is rendered at the specified opacity and rotation using
/// PDF ExtGState for transparency and the cm operator for rotation.
/// </summary>
public class WatermarkSettings
{
    /// <summary>
    /// The watermark text to display (e.g. "DRAFT", "CONFIDENTIAL").
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Font family name for the watermark text (default: "Helvetica").
    /// </summary>
    public string FontName { get; set; } = "Helvetica";

    /// <summary>
    /// Font size in points for the watermark text (default: 72).
    /// </summary>
    public float FontSize { get; set; } = 72;

    /// <summary>
    /// Color of the watermark text (default: Gray500).
    /// </summary>
    public Color TextColor { get; set; } = ColorPalette.Gray[500];

    /// <summary>
    /// Opacity of the watermark from 0.0 (invisible) to 1.0 (fully opaque). Default: 0.15.
    /// </summary>
    public float Opacity { get; set; } = 0.15f;

    /// <summary>
    /// Rotation angle in degrees. Negative values rotate counter-clockwise.
    /// Default: -45 (diagonal from bottom-left to top-right).
    /// </summary>
    public float RotationDegrees { get; set; } = -45f;
}
