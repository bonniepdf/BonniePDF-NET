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

    // Explicit-set tracking for this feature DefaultTextStyle cascade
    private bool _fontExplicit;
    private float _fontSize = 12f;
    private bool _fontSizeExplicit;
    private Color _textColor = Color.Black;
    private bool _textColorExplicit;
    private float _lineHeightMultiplier = 1.2f;
    private bool _lineHeightExplicit;

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
            if (value != null) { _fontExplicit = true; ResolveFontName(); }
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
            _fontExplicit = true;
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
            _fontExplicit = true;
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
            _fontExplicit = true;
        }
    }

    /// <summary>
    /// Font size for text rendering
    /// </summary>
    public float FontSize
    {
        get => _fontSize;
        set { _fontSize = value; _fontSizeExplicit = true; }
    }

    /// <summary>
    /// TextBlock color for rendering
    /// </summary>
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; _textColorExplicit = true; }
    }

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
    public float LineHeightMultiplier
    {
        get => _lineHeightMultiplier;
        set { _lineHeightMultiplier = value; _lineHeightExplicit = true; }
    }

    /// <summary>
    /// Line height calculated from font size and LineHeightMultiplier
    /// </summary>
    protected float LineHeight => FontSize * LineHeightMultiplier;

    /// <summary>
    /// Applies style properties from <paramref name="style"/> only for properties
    /// that have not been explicitly set by user code. Called by the document
    /// tree-walk before rendering when DefaultTextStyle is configured.
    /// </summary>
    public void ApplyDefaultStyleIfNotSet(TextStyle style)
    {
        if (style == null) return;

        if (!_fontExplicit)
        {
            if (style.FontFamily != null)
            {
                _fontFamily = style.FontFamily;
                if (style.Weight != FontWeight.Normal) _weight = style.Weight;
                if (style.Italic) _isItalic = true;
                ResolveFontName();
            }
        }
        if (!_fontSizeExplicit && style.FontSize.HasValue)
            _fontSize = style.FontSize.Value;
        if (!_textColorExplicit && style.Color.HasValue)
            _textColor = style.Color.Value;
        if (!_lineHeightExplicit && style.LineHeightMultiplier.HasValue)
            _lineHeightMultiplier = style.LineHeightMultiplier.Value;
    }

    /// <summary>
    /// Computes wrapped lines for text.
    /// Explicit line breaks (<c>\n</c>, <c>\r\n</c>, or a bare <c>\r</c>) are
    /// honoured first and split the text into segments; each segment is then
    /// word-wrapped to <paramref name="maxWidth"/>. An empty segment (from
    /// consecutive breaks, or a leading/trailing break) yields one blank line,
    /// so <c>"a\n\nb"</c> produces <c>["a", "", "b"]</c>. Text containing no
    /// break characters wraps exactly as before.
    /// </summary>
    /// <param name="text">The text to wrap</param>
    /// <param name="maxWidth">The maximum width for a line</param>
    /// <returns>A list of lines, each fitting within the maximum width</returns>
    protected IList<string> ComputeLines(string text, float maxWidth)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        // Normalise CRLF / CR to LF, then split on hard breaks.
        string[] segments = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (string segment in segments)
        {
            List<string> wrapped = WrapSegment(segment, maxWidth);
            if (wrapped.Count == 0)
                // Preserve the blank line represented by an empty segment.
                lines.Add("");
            else
                lines.AddRange(wrapped);
        }

        return lines;
    }

    /// <summary>
    /// Word-wraps a single break-free segment to <paramref name="maxWidth"/>.
    /// </summary>
    private List<string> WrapSegment(string text, float maxWidth)
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