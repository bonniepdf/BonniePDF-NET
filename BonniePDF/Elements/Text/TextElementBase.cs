using System.Collections.Generic;
using System.Text;
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

    // Explicit-set tracking for DefaultTextStyle cascade
    private bool _fontExplicit;
    private float _fontSize = 12f;
    private bool _fontSizeExplicit;
    private Color _textColor = Color.Black;
    private bool _textColorExplicit;
    private float _lineHeight = 1.2f;
    private bool _lineHeightExplicit;

    protected TextElementBase()
    {
        var defaultStyle = AmbientDefaultStyle.Value;
        if (defaultStyle != null) ApplyDefaultStyle(defaultStyle);
    }

    // What a definition writer needs to know: which values were set, rather than defaulted or supplied by
    // DefaultTextStyle, because a default written as if it had been chosen would stop the cascade.
    internal bool FontIsExplicit => _fontExplicit;
    internal bool FontIsChosenByFamily => _fontFamily != null;
    internal bool FontSizeIsExplicit => _fontSizeExplicit;
    internal bool TextColorIsExplicit => _textColorExplicit;
    internal bool LineHeightIsExplicit => _lineHeightExplicit;

    private void ApplyDefaultStyle(TextStyle style)
    {
        if (style.FontFamily != null)
            FontFamily = style.FontFamily;
        if (style.FontWeight != FontWeight.Normal)
            FontWeight = style.FontWeight;
        if (style.Italic)
            Italic = true;
        if (style.FontSize.HasValue)
            FontSize = style.FontSize.Value;
        if (style.Color.HasValue)
            Color = style.Color.Value;
        if (style.Decoration != TextDecoration.None)
            Decoration = style.Decoration;
    }

    /// <summary>
    /// Font family name (e.g. "Helvetica", "Times", "Arial") for weight/italic resolution.
    /// When set, FontName is automatically resolved via FontFamilyResolver using
    /// the current FontWeight and Italic values.
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
    public FontWeight FontWeight
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
    public bool Italic
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
    /// or resolved automatically from FontFamily + FontWeight + Italic.
    /// Setting this directly clears FontFamily.
    /// </summary>
    public string FontName
    {
        get
        {
            // While a document with its own fonts is being generated, a family is resolved against
            // them (spec 16 §4). Otherwise the name resolved when the family was set stands.
            if (_fontFamily != null && FontMetrics.ActiveDocumentFonts != null)
                return FontFamilyResolver.Resolve(_fontFamily, _weight, _isItalic);
            return _fontName;
        }
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
    public Color Color
    {
        get => _textColor;
        set { _textColor = value; _textColorExplicit = true; }
    }

    /// <summary>
    /// Text decoration (underline, strikethrough, overline, or a combination)
    /// </summary>
    public TextDecoration Decoration { get; set; } = TextDecoration.None;

    /// <summary>
    /// Colour of the decoration lines. Null = use <see cref="Color"/>, the default.
    /// </summary>
    public Color? DecorationColor { get; set; }

    /// <summary>
    /// Thickness of the decoration lines in points. Null = the default derived from
    /// font size (<c>max(0.5, fontSize / 18)</c>).
    /// </summary>
    public float? DecorationThickness { get; set; }

    /// <summary>Line style for decorations (solid/wavy/dashed/dotted). Default solid.</summary>
    public DecorationStyle DecorationStyle { get; set; } = DecorationStyle.Solid;

    /// <summary>
    /// Letter spacing (tracking) in points, added after every glyph.
    /// Default 0. Reflected in measurement and both PDF and SVG output.
    /// </summary>
    public float LetterSpacing { get; set; }

    /// <summary>Word spacing in points, added after every space. Default 0.</summary>
    public float WordSpacing { get; set; }

    /// <summary>
    /// Resolves FontName from FontFamily + FontWeight + Italic via FontFamilyResolver.
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
    public float LineHeight
    {
        get => _lineHeight;
        set { _lineHeight = value; _lineHeightExplicit = true; }
    }

    /// <summary>
    /// Line height in points, computed from the font size and the
    /// <see cref="LineHeight"/> multiplier. This is the actual vertical advance
    /// used by layout; <see cref="LineHeight"/> is the multiplier that scales it.
    /// </summary>
    protected float ComputedLineHeight => FontSize * LineHeight;

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
                if (style.FontWeight != FontWeight.Normal) _weight = style.FontWeight;
                if (style.Italic) _isItalic = true;
                ResolveFontName();
            }
        }
        if (!_fontSizeExplicit && style.FontSize.HasValue)
            _fontSize = style.FontSize.Value;
        if (!_textColorExplicit && style.Color.HasValue)
            _textColor = style.Color.Value;
        if (!_lineHeightExplicit && style.LineHeight.HasValue)
            _lineHeight = style.LineHeight.Value;
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
    /// Wraps a single break-free segment, keeping every space (spec 03 "Preserved whitespace").
    /// </summary>
    /// <remarks>
    /// Works by Unicode code point and never splits a surrogate pair. A line is the longest prefix
    /// that fits: <paramref name="firstWidth"/> for a segment's first line, <paramref name="width"/>
    /// for the rest. It breaks at the last space that has a non-space before it, trimming the line's
    /// trailing spaces and dropping the spaces at the break; failing that, it breaks at the last code
    /// point that fits. At least one code point is placed on every line, so it always terminates.
    /// </remarks>
    protected List<string> WrapPreservingWhitespace(string text, float firstWidth, float width)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        List<string> rest = [];
        foreach (Rune rune in text.EnumerateRunes()) rest.Add(rune.ToString());
        float limit = firstWidth;

        bool Fits(int count) => MeasureTextWidth(string.Concat(rest.GetRange(0, count))) <= limit;
        bool HasNonSpace(int count)
        {
            for (int i = 0; i < count; i++)
                if (rest[i] != " ") return true;
            return false;
        }

        while (rest.Count > 0)
        {
            if (Fits(rest.Count))
            {
                lines.Add(string.Concat(rest));
                break;
            }

            // One code point is always placed, even when it alone is wider than the line.
            if (rest.Count == 1)
            {
                lines.Add(rest[0]);
                break;
            }

            // The longest prefix that fits, never fewer than one code point.
            int lo = 1;
            int hi = rest.Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (Fits(mid)) lo = mid;
                else hi = mid - 1;
            }

            int count = lo;
            int cut = -1;
            if (rest[count] == " " && HasNonSpace(count))
            {
                cut = count;
            }
            else
            {
                for (int i = count - 1; i > 0; i--)
                {
                    if (rest[i] == " " && HasNonSpace(i))
                    {
                        cut = i;
                        break;
                    }
                }
            }

            if (cut >= 0)
            {
                lines.Add(string.Concat(rest.GetRange(0, cut)).TrimEnd(' '));
                rest.RemoveRange(0, cut);
                while (rest.Count > 0 && rest[0] == " ") rest.RemoveAt(0);
            }
            else
            {
                lines.Add(string.Concat(rest.GetRange(0, count)));
                rest.RemoveRange(0, count);
            }

            limit = width;
        }

        return lines;
    }

    /// <summary>
    /// Word-wraps a single break-free segment to <paramref name="maxWidth"/>.
    /// </summary>
    protected virtual List<string> WrapSegment(string text, float maxWidth)
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
        return FontMetrics.GetStringWidth(measuredText, FontName, FontSize) + SpacingWidth(measuredText);
    }

    /// <summary>
    /// Extra advance contributed by <see cref="LetterSpacing"/>/<see cref="WordSpacing"/>
    /// for a string: letter spacing after every glyph plus word spacing after every
    /// space. Matches the PDF/SVG painters and the reserved layout width.
    /// Zero when no spacing is set.
    /// </summary>
    protected float SpacingWidth(string text)
    {
        return TextSpacing.GetWidth(text, LetterSpacing, WordSpacing);
    }
}