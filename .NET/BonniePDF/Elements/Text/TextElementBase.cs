using System.Collections.Generic;
using System.Threading;
using BonniePDF.Core;
using BonniePDF.Typography;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Base class for text-based PDF elements that provides common functionality for
/// font handling, text encoding, and line wrapping. This reduces code duplication
/// across text elements and ensures consistent text rendering behavior.
/// </summary>
public abstract class TextElementBase : ElementBase
{
    /// <summary>
    /// Ambient default text style that is applied to new text elements during construction.
    /// Set by PdfBuilder/PageDescriptor during fluent API document building.
    /// Uses AsyncLocal for thread safety.
    /// </summary>
    internal static readonly AsyncLocal<TextStyle?> AmbientDefaultStyle = new();

    /// <summary>
    /// Token for injecting total pages count
    /// </summary>
    public const string TotalPagesToken = "{{TotalPages}}";

    /// <summary>
    /// Token for injecting the current page number
    /// </summary>
    public const string CurrentPageToken = "{{CurrentPage}}";

    private string? _fontFamily;
    private FontWeight _weight = FontWeight.Normal;
    private bool _isItalic;
    private string _fontName = StandardFonts.Helvetica;

    protected TextElementBase()
    {
        var defaultStyle = AmbientDefaultStyle.Value;
        if (defaultStyle != null) ApplyDefaultStyle(defaultStyle);
    }

    private void ApplyDefaultStyle(TextStyle style)
    {
        if (style.FontFamily != null)
            FontFamily = style.FontFamily;
        if (style.Weight != FontWeight.Normal)
            Weight = style.Weight;
        if (style.Italic)
            IsItalic = true;
        if (style.FontSize.HasValue)
            FontSize = style.FontSize.Value;
        if (style.Color.HasValue)
            TextColor = style.Color.Value;
        if (style.Decoration != TextDecoration.None)
            TextDecoration = style.Decoration;
    }

    /// <summary>
    /// Font family name (e.g. "Helvetica", "Times", "Arial") for weight/italic resolution.
    /// When set, FontName is automatically resolved via FontFamilyResolver using
    /// the current Weight and IsItalic values.
    /// </summary>
    public string? FontFamily
    {
        get => _fontFamily;
        set
        {
            _fontFamily = value;
            if (value != null) ResolveFontName();
        }
    }

    /// <summary>
    /// Font weight (100-900). Normal = 400, Bold = 700.
    /// When FontFamily is set, triggers automatic resolution.
    /// When FontFamily is not set, uses the current FontName as the base family for resolution.
    /// </summary>
    public FontWeight Weight
    {
        get => _weight;
        set
        {
            _weight = value;
            ResolveFontName();
        }
    }

    /// <summary>
    /// Whether the text should be rendered in italic/oblique style.
    /// When FontFamily is set, triggers automatic resolution.
    /// When FontFamily is not set, uses the current FontName as the base family for resolution.
    /// </summary>
    public bool IsItalic
    {
        get => _isItalic;
        set
        {
            _isItalic = value;
            ResolveFontName();
        }
    }

    /// <summary>
    /// The resolved PDF font name used for rendering.
    /// Can be set directly (e.g. "Helvetica-Bold") for backward compatibility,
    /// or resolved automatically from FontFamily + Weight + IsItalic.
    /// Setting this directly clears FontFamily.
    /// </summary>
    public string FontName
    {
        get => _fontName;
        set
        {
            _fontName = value;
            _fontFamily = null; // Direct font name bypasses family resolution
        }
    }

    /// <summary>
    /// Font size for text rendering
    /// </summary>
    public float FontSize { get; set; } = 12;

    /// <summary>
    /// TextBlock color for rendering
    /// </summary>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>
    /// Text decoration (underline, strikethrough, or both)
    /// </summary>
    public TextDecoration TextDecoration { get; set; } = TextDecoration.None;

    /// <summary>
    /// Resolves FontName from FontFamily + Weight + IsItalic via FontFamilyResolver.
    /// Falls back to the current FontName as the base family when FontFamily is not set,
    /// enabling .Bold() and .Italic() to work without an explicit .Font() call.
    /// </summary>
    private void ResolveFontName()
    {
        string family = _fontFamily ?? _fontName;
        _fontName = FontFamilyResolver.Resolve(family, _weight, _isItalic);
    }

    /// <summary>
    /// Multiplier applied to font size to compute line height. Default is 1.2.
    /// Set to a value like 1.5 for looser leading or 1.0 for tight leading.
    /// </summary>
    public float LineHeightMultiplier { get; set; } = 1.2f;

    /// <summary>
    /// Line height calculated from font size and LineHeightMultiplier
    /// </summary>
    protected float LineHeight => FontSize * LineHeightMultiplier;

    /// <summary>
    /// Computes wrapped lines for text that exceeds the maximum width.
    /// Uses word wrapping to break lines at appropriate positions.
    /// </summary>
    /// <param name="text">The text to wrap</param>
    /// <param name="maxWidth">The maximum width for a line</param>
    /// <returns>A list of lines, each fitting within the maximum width</returns>
    protected IList<string> ComputeLines(string text, float maxWidth)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + ' ' + word;

            // Calculate exact width of the test line (handling tokens)
            float testWidth = MeasureTextWidth(testLine);

            if (testWidth > maxWidth)
            {
                // Current line is full, add it and start new line with current word
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                // Word fits, add it to current line
                currentLine = testLine;
            }
        }

        // Don't forget the last line
        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    /// <summary>
    /// Measures the width of a text string using the current font settings.
    /// Handles substitution tokens like {{TotalPages}} by measuring a placeholder.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <returns>The width of the text in points</returns>
    protected float MeasureTextWidth(string text)
    {
        // Replace tokens with estimated placeholders (2 digits) for measurement
        string measuredText = text.Replace(TotalPagesToken, "00").Replace(CurrentPageToken, "00");
        return FontMetrics.GetStringWidth(measuredText, FontName, FontSize);
    }
}