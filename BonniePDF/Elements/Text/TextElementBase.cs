using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace BonniePDF;

/// <summary>
/// Base class for text-based PDF elements that provides common functionality for
/// font handling, text encoding, and line wrapping. This reduces code duplication
/// across text elements and ensures consistent text rendering behavior.
/// </summary>
public abstract class TextElementBase : ElementBase
{
    /// <summary>OpenType feature overrides for embedded text, such as kern=0 or liga=0.</summary>
    public System.Collections.Generic.IReadOnlyDictionary<string,int>? FontFeatures
    {
        get => _text.FontFeatures;
        // TextStyle validates the features and takes a private copy.
        set => _text = _text.With(value is null ? null : new TextStyle { FontFeatures = value }.FontFeatures, static (o, v) => o with { FontFeatures = v });
    }

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

    // Text style and options, shared with every text element that has the same values (see
    // Interned); the members below read and write it.
    private protected TextOptions _text = TextOptions.Default;

    private string? _fontFamily { get => _text.FontFamily; set => _text = _text.With(value, static (o, v) => o with { FontFamily = v }); }
    private FontWeight _weight { get => _text.Weight; set => _text = _text.With(value, static (o, v) => o with { Weight = v }); }
    private bool _isItalic { get => _text.Italic; set => _text = _text.With(value, static (o, v) => o with { Italic = v }); }
    private string _fontName { get => _text.FontName; set => _text = _text.With(value, static (o, v) => o with { FontName = v }); }

    // Explicit-set tracking for DefaultTextStyle cascade
    private bool _fontExplicit { get => _text.FontExplicit; set => _text = _text.With(value, static (o, v) => o with { FontExplicit = v }); }
    // Per-property tracking: the cascade fills each of family, weight and italic independently, so an
    // explicit weight does not stop DefaultTextStyle supplying the family (and the reverse).
    private bool _fontFamilyExplicit { get => _text.FontFamilyExplicit; set => _text = _text.With(value, static (o, v) => o with { FontFamilyExplicit = v }); }
    private bool _weightExplicit { get => _text.WeightExplicit; set => _text = _text.With(value, static (o, v) => o with { WeightExplicit = v }); }
    private bool _italicExplicit { get => _text.ItalicExplicit; set => _text = _text.With(value, static (o, v) => o with { ItalicExplicit = v }); }
    private float _fontSize { get => _text.FontSize; set => _text = _text.With(value, static (o, v) => o with { FontSize = v }); }
    private bool _fontSizeExplicit { get => _text.FontSizeExplicit; set => _text = _text.With(value, static (o, v) => o with { FontSizeExplicit = v }); }
    private Color _textColor { get => _text.TextColor; set => _text = _text.With(value, static (o, v) => o with { TextColor = v }); }
    private bool _textColorExplicit { get => _text.TextColorExplicit; set => _text = _text.With(value, static (o, v) => o with { TextColorExplicit = v }); }
    private bool _decorationExplicit { get => _text.DecorationExplicit; set => _text = _text.With(value, static (o, v) => o with { DecorationExplicit = v }); }
    private float _lineHeight { get => _text.LineHeight; set => _text = _text.With(value, static (o, v) => o with { LineHeight = v }); }
    private bool _lineHeightExplicit { get => _text.LineHeightExplicit; set => _text = _text.With(value, static (o, v) => o with { LineHeightExplicit = v }); }

    protected TextElementBase()
    {
        var defaultStyle = AmbientDefaultStyle.Value;
        if (defaultStyle != null) ApplyDefaultStyle(defaultStyle);
    }

    // What a definition writer needs to know: which values were set, rather than defaulted or supplied by
    // DefaultTextStyle, because a default written as if it had been chosen would stop the cascade.
    internal bool FontIsExplicit => _fontExplicit;
    internal bool FontIsChosenByFamily => _fontFamilyExplicit && _fontFamily != null;
    internal bool FontIsNamed => _fontFamilyExplicit && _fontFamily == null;
    internal bool FontWeightIsExplicit => _weightExplicit;
    internal bool ItalicIsExplicit => _italicExplicit;
    internal bool FontSizeIsExplicit => _fontSizeExplicit;
    internal bool TextColorIsExplicit => _textColorExplicit;
    internal bool LineHeightIsExplicit => _lineHeightExplicit;
    internal bool DecorationIsExplicit => _decorationExplicit;

    private void ApplyDefaultStyle(TextStyle style)
    {
        FontFeatures ??= style.FontFeatures;
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
            if (value != null) { _fontExplicit = true; _fontFamilyExplicit = true; ResolveFontName(); }
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
            _weightExplicit = true;
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
            _italicExplicit = true;
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
            // A family is resolved when the document is generated, against the document's fonts and the fonts
            // registered by then, so the order of registering a font and setting the family does not matter.
            // Outside generation the name resolved when the family was set stands.
            if (_fontFamily != null && FontMetrics.IsGeneratingDocument)
                return FontFamilyResolver.Resolve(_fontFamily, _weight, _isItalic);
            return _fontName;
        }
        set
        {
            _fontName = value;
            _fontFamily = null; // Direct font name bypasses family resolution
            _fontExplicit = true;
            _fontFamilyExplicit = true;
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
    public TextDecoration Decoration
    {
        get => _text.Decoration;
        set { _text = _text.With(value, static (o, v) => o with { Decoration = v }); _decorationExplicit = true; }
    }

    /// <summary>
    /// Colour of the decoration lines. Null = use <see cref="Color"/>, the default.
    /// </summary>
    public Color? DecorationColor
    {
        get => _text.DecorationColor;
        set => _text = _text.With(value, static (o, v) => o with { DecorationColor = v });
    }

    /// <summary>
    /// Thickness of the decoration lines in points. Null = the default derived from
    /// font size (<c>max(0.5, fontSize / 18)</c>).
    /// </summary>
    public float? DecorationThickness
    {
        get => _text.DecorationThickness;
        set => _text = _text.With(value, static (o, v) => o with { DecorationThickness = v });
    }

    /// <summary>Line style for decorations (solid/wavy/dashed/dotted). Default solid.</summary>
    public DecorationStyle DecorationStyle
    {
        get => _text.DecorationStyle;
        set => _text = _text.With(value, static (o, v) => o with { DecorationStyle = v });
    }

    /// <summary>
    /// Letter spacing (tracking) in points, added after every glyph.
    /// Default 0. Reflected in measurement and both PDF and SVG output.
    /// </summary>
    public float LetterSpacing
    {
        get => _text.LetterSpacing;
        set => _text = _text.With(value, static (o, v) => o with { LetterSpacing = v });
    }

    /// <summary>Word spacing in points, added after every space. Default 0.</summary>
    public float WordSpacing
    {
        get => _text.WordSpacing;
        set => _text = _text.With(value, static (o, v) => o with { WordSpacing = v });
    }

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
    public void ApplyDefaultStyleIfNotSet(TextStyle? style)
    {
        if (style == null) return;
        FontFeatures ??= style.FontFeatures;

        // Family, weight and italic cascade independently. Writing the backing fields leaves the explicit
        // flags alone, so a later cascade can still replace what this one supplied.
        // A property the style does not supply returns to its library default, so a default that was
        // changed or removed since the last cascade does not leave its old value behind.
        bool fontChanged = false;
        if (!_fontFamilyExplicit && _fontFamily != style.FontFamily)
        {
            _fontFamily = style.FontFamily;
            fontChanged = true;
        }
        // A font named directly bypasses family resolution, so weight and italic do not apply to it.
        if (!(_fontFamilyExplicit && _fontFamily == null))
        {
            if (!_weightExplicit && _weight != style.FontWeight) { _weight = style.FontWeight; fontChanged = true; }
            if (!_italicExplicit && _isItalic != style.Italic) { _isItalic = style.Italic; fontChanged = true; }
        }
        if (fontChanged)
        {
            // Without a family the name is resolved from the base font, not from an earlier cascade's result.
            if (!_fontFamilyExplicit && _fontFamily == null) _fontName = TextOptions.Default.FontName;
            ResolveFontName();
        }
        if (!_fontSizeExplicit)
            _fontSize = style.FontSize ?? TextOptions.Default.FontSize;
        if (!_textColorExplicit)
            _textColor = style.Color ?? TextOptions.Default.TextColor;
        if (!_lineHeightExplicit)
            _lineHeight = style.LineHeight ?? TextOptions.Default.LineHeight;
        if (!_decorationExplicit)
            _text = _text.With(style.Decoration, static (o, v) => o with { Decoration = v });
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
        using var performanceScope = PerformanceEvents.Measure(PerformanceStage.Measurement);
        if (string.IsNullOrEmpty(text)) return new List<string>();

        // No hard breaks: the text is one segment, so its wrapped lines are the result.
        if (text.AsSpan().IndexOfAny('\r', '\n') < 0)
        {
            IList<string> single = WrapSegment(text, maxWidth);
            return single.Count == 0 ? new List<string> { "" } : single;
        }

        List<string> lines = [];
        // Normalise CRLF / CR to LF, then split on hard breaks.
        string[] segments = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (string segment in segments)
        {
            IList<string> wrapped = WrapSegment(segment, maxWidth);
            if (wrapped.Count == 0)
                // Preserve the blank line represented by an empty segment.
                lines.Add("");
            else
                lines.AddRange(wrapped);
        }

        return lines;
    }

    /// <summary>
    /// Wraps a single break-free segment, keeping every space.
    /// </summary>
    /// <remarks>
    /// Works by Unicode code point and never splits a surrogate pair. A line is the longest prefix
    /// that fits: <paramref name="firstWidth"/> for a segment's first line, <paramref name="width"/>
    /// for the rest. It breaks at the last space that has a non-space before it, trimming the line's
    /// trailing spaces and dropping the spaces at the break; failing that, it breaks at the last code
    /// point that fits. At least one code point is placed on every line, so it always terminates.
    /// </remarks>
    protected IList<string> WrapPreservingWhitespace(string text, float firstWidth, float width)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        List<string> rest = [];
        var bounds=Typography.Engine.Graphemes.Boundaries(text);
        for(int i=0;i+1<bounds.Count;i++)rest.Add(text.Substring(bounds[i],bounds[i+1]-bounds[i]));
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
            var isSafeBreak = FontMetrics.GetTextBreakPredicate(string.Concat(rest), FontName, FontFeatures);
            int breakOffset = string.Concat(rest.GetRange(0, count)).Length;
            for (int safe = count; safe > 0; safe--)
            {
                if (isSafeBreak(breakOffset)) { count = safe; break; }
                breakOffset -= rest[safe - 1].Length;
            }
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
    protected virtual IList<string> WrapSegment(string text, float maxWidth)
    {
        List<string> lines = [];
        if (!string.IsNullOrEmpty(text)) WrapWords(text, maxWidth, lines);
        return lines;
    }

    /// <summary>The word wrapping of <see cref="WrapSegment"/>, appending the lines of a non-empty <paramref name="text"/> to <paramref name="lines"/>.</summary>
    private protected void WrapWords(string text, float maxWidth, List<string> lines)
    {
        // Words are the spans between single spaces; only their ends are needed, so the text is not split.
        ParagraphWidths? widths = text.AsSpan().Count(' ') >= 2 ? ParagraphWidthsFor(text) : null;

        // The current line and each candidate are spans of text: words joined by the single
        // spaces between them are exactly the text between their ends.
        int lineStart = 0, lineEnd = 0, wordStart = 0;
        while (wordStart <= text.Length)
        {
            int space = text.IndexOf(' ', wordStart);
            int wordEnd = space < 0 ? text.Length : space;
            bool lineEmpty = lineStart == lineEnd;
            int testStart = lineEmpty ? wordStart : lineStart;

            // Calculate exact width of the test line (handling tokens)
            float testWidth = MeasureSpan(text, testStart, wordEnd, widths);

            if (testWidth > maxWidth)
            {
                // Current line is full, add it and start new line with current word
                if (!lineEmpty)
                    AddLine(lines, text, lineStart, lineEnd, widths);
                lineStart = wordStart;
            }
            else
            {
                // Word fits, add it to current line
                lineStart = testStart;
            }
            lineEnd = wordEnd;
            wordStart = wordEnd + 1;
        }

        // Don't forget the last line
        if (lineStart != lineEnd)
            AddLine(lines, text, lineStart, lineEnd, widths);
    }

    /// <summary>
    /// Candidate-line widths from one shaping of <paramref name="text"/>, or null when the
    /// element measures in a way one shaping cannot answer (spacing, page-number tokens,
    /// right-to-left text, or what <see cref="ParagraphWidths.TryCreate"/> declines).
    /// </summary>
    private protected ParagraphWidths? ParagraphWidthsFor(string text)
    {
        if (LetterSpacing != 0 || WordSpacing != 0 || IsRightToLeft) return null;
        if (text.Contains(TotalPagesToken, StringComparison.Ordinal) || text.Contains(CurrentPageToken, StringComparison.Ordinal)) return null;
        using var directionScope = TextFlow.Enter(EffectiveContentDirection);
        return ParagraphWidths.TryCreate(text, FontName, FontSize, FontFeatures);
    }

    /// <summary>
    /// Adds a wrapped line, handing its glyphs from the paragraph's shaping to the shaping cache
    /// so drawing the line does not shape it again. Seeded under the text flow drawing uses for
    /// left-to-right text; under any other flow the entry is simply never read.
    /// </summary>
    private protected void AddLine(List<string> lines, string text, int start, int end, ParagraphWidths? widths)
    {
        string line = text.Substring(start, end - start);
        lines.Add(line);
        if (widths?.TryGetGlyphs(start, end) is not { } glyphs) return;
        using var directionScope = TextFlow.Enter(EffectiveContentDirection);
        widths.Font.SeedShaped(line, FontFeatures, glyphs);
    }

    /// <summary>
    /// <see cref="MeasureTextWidth"/> of <c>text[start..end)</c>, read from
    /// <paramref name="widths"/> when it knows the span.
    /// </summary>
    private protected float MeasureSpan(string text, int start, int end, ParagraphWidths? widths) =>
        widths != null && widths.TryGetWidth(start, end, out float width) ? width : MeasureTextWidth(text.Substring(start, end - start));

    /// <summary>
    /// Measures the width of a text string using the current font settings.
    /// Handles substitution tokens like {{TotalPages}} by measuring a placeholder.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <returns>The width of the text in points</returns>
    protected float MeasureTextWidth(string text)
    {
        using var directionScope = TextFlow.Enter(EffectiveContentDirection);
        // Replace tokens with estimated placeholders (2 digits) for measurement
        string measuredText = text.Replace(TotalPagesToken, "00").Replace(CurrentPageToken, "00");
        return FontMetrics.GetStringWidth(measuredText, FontName, FontSize, FontFeatures) + SpacingWidth(measuredText);
    }

    /// <summary>
    /// Extra advance contributed by <see cref="LetterSpacing"/>/<see cref="WordSpacing"/>
    /// for a string: letter spacing after every glyph plus word spacing after every
    /// space. Matches the PDF/SVG painters and the reserved layout width.
    /// Zero when no spacing is set.
    /// </summary>
    protected float SpacingWidth(string text)
    {
        return TextSpacing.GetWidth(text, LetterSpacing, WordSpacing, FontName, FontFeatures);
    }
}
