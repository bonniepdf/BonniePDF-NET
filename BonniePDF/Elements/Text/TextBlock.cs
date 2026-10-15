using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// A text block element that supports word wrapping, alignment, font customization,
/// and automatic pagination across page boundaries.
/// </summary>
public class TextBlock : TextElementBase, IStatefulElement, IRoleProvider
{
    private float _cachedContentWidth = -1;
    // The wrapped lines: null until computed; a string when there is one line and nothing else
    // to record (most table cells), which is then the only allocation; otherwise a LineLayout.
    private object? _layout;

    private sealed class LineLayout(string[] lines, float[]? indents, float[]? spaceBefore, TextFlow.Line?[]? bidi)
    {
        internal readonly string[] Lines = lines;
        internal readonly float[]? Indents = indents;
        internal readonly float[]? SpaceBefore = spaceBefore;
        internal readonly TextFlow.Line?[]? Bidi = bidi;
    }

    // The computed lines, read without copying a single line into an array.
    private readonly struct Lines(object layout)
    {
        internal int Count => layout is string ? 1 : ((LineLayout)layout).Lines.Length;
        internal string this[int index] => layout is string line && index == 0 ? line : ((LineLayout)layout).Lines[index];
    }

    private TextFlow.Line?[]? _bidiLines => (_layout as LineLayout)?.Bidi;
    // Per-line paragraph metadata parallel to the lines.
    private IList<float>? _cachedIndents => (_layout as LineLayout)?.Indents;
    private IList<float>? _cachedSpaceBefore => (_layout as LineLayout)?.SpaceBefore;

    // State tracking for multipage rendering
    private int _lastLineRendered = -1;
    public required string Text { get; set; }
    private TextAlignment _alignment { get => _text.Alignment; set => _text = _text.With(value, static (o, v) => o with { Alignment = v }); }
    private bool _alignmentExplicit { get => _text.AlignmentExplicit; set => _text = _text.With(value, static (o, v) => o with { AlignmentExplicit = v }); }

    internal bool AlignmentIsExplicit => _alignmentExplicit;

    /// <summary>
    /// Paragraph alignment. When not set explicitly, an enclosing <see cref="Container"/> whose
    /// <c>HorizontalAlignment</c> is Start/Center/End supplies an ambient default instead of the plain
    /// Left below (spec 01 "Document Model" §3.2); see <see cref="EffectiveAlignment"/>.
    /// </summary>
    public TextAlignment Alignment
    {
        get => _alignment;
        set { _alignment = value; _alignmentExplicit = true; }
    }

    /// <summary>The alignment this render actually uses: the explicit value, or the render context's ambient.</summary>
    private TextAlignment EffectiveAlignment(RenderContext ctx)
    {
        var alignment = _alignmentExplicit ? _alignment : ctx.AmbientHorizontalTextAlignment ?? TextAlignment.Start;
        return alignment switch
        {
            TextAlignment.Start => IsRightToLeft ? TextAlignment.Right : TextAlignment.Left,
            TextAlignment.End => IsRightToLeft ? TextAlignment.Left : TextAlignment.Right,
            _ => alignment,
        };
    }

    /// <summary>
    /// Line clamp: maximum number of lines to render. When set and the text
    /// wraps to more lines, the block is truncated to this many lines and
    /// <see cref="Ellipsis"/> is appended to the last visible line. Null = no clamp.
    /// </summary>
    public int? MaxLines
    {
        get => _text.MaxLines;
        set => _text = _text.With(value, static (o, v) => o with { MaxLines = v });
    }

    /// <summary>Ellipsis appended to the last line when <see cref="MaxLines"/> clamps. Default <c>…</c>.</summary>
    public string Ellipsis
    {
        get => _text.Ellipsis;
        set => _text = _text.With(value, static (o, v) => o with { Ellipsis = v });
    }

    /// <summary>
    /// Paragraph layout: horizontal indent (points) applied to the first line
    /// of each paragraph (a paragraph = text between hard <c>\n</c> breaks). Default 0.
    /// </summary>
    public float FirstLineIndent
    {
        get => _text.FirstLineIndent;
        set => _text = _text.With(value, static (o, v) => o with { FirstLineIndent = v });
    }

    /// <summary>
    /// Paragraph layout: extra vertical space (points) inserted before each
    /// paragraph after the first. Default 0.
    /// </summary>
    public float ParagraphSpacing
    {
        get => _text.ParagraphSpacing;
        set => _text = _text.With(value, static (o, v) => o with { ParagraphSpacing = v });
    }

    /// <summary>
    /// Keep every space, including leading and repeated ones, for text whose indentation carries
    /// meaning (source code, a fixed-width listing). Default <c>false</c>, which drops a segment's
    /// leading spaces.
    /// </summary>
    /// <remarks>
    /// Lines wrap by code point at the last space that fits, or mid-token when there is none, so a
    /// long line never overflows. Tabs are not expanded. Inherited by <see cref="Heading"/>; list
    /// items and <see cref="RichText"/> do not take it.
    /// </remarks>
    public bool PreserveWhitespace
    {
        get => _text.PreserveWhitespace;
        set => _text = _text.With(value, static (o, v) => o with { PreserveWhitespace = v });
    }

    /// <summary>
    /// PDF/UA structure role for this element. Defaults to
    /// <see cref="SemanticRole.P"/> (a paragraph).
    /// <see cref="Heading"/> overrides this to return one of
    /// <c>H1</c>…<c>H6</c>.
    /// </summary>
    public virtual SemanticRole SemanticRole => SemanticRole.P;

    /// <summary>
    /// Minimum number of lines to render before a page break (orphan control).
    /// If fewer lines fit on the current page, the entire block moves to the next page.
    /// Set to 1 to disable orphan control. Default is 2.
    /// </summary>
    public int MinLinesBeforeBreak
    {
        get => _text.MinLinesBeforeBreak;
        set => _text = _text.With(value, static (o, v) => o with { MinLinesBeforeBreak = v });
    }

    /// <summary>
    /// Minimum number of lines that must appear after a page break (widow control).
    /// If fewer lines remain for the next page, the break point is adjusted
    /// to push more lines forward. Set to 1 to disable widow control. Default is 2.
    /// </summary>
    public int MinLinesAfterBreak
    {
        get => _text.MinLinesAfterBreak;
        set => _text = _text.With(value, static (o, v) => o with { MinLinesAfterBreak = v });
    }

    public TextBlock()
    {

    }

    [SetsRequiredMembers]
    public TextBlock(string text)
    {
        this.Text = text;
    }

    /// <summary>
    /// Renders this text block with state tracking for pagination.
    /// Text that doesn't fit on the current page will be continued on the next page.
    /// Respects MinLinesBeforeBreak (orphan) and MinLinesAfterBreak (widow) settings.
    /// </summary>
    public virtual RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Apply top margin only on first render (not on continuation pages)
        if (_lastLineRendered < 0) ctx.CurrentY += Margin.Top;

        float contentWidth = width - Margin.Left - Margin.Right;
        Lines lines = GetOrComputeLines(contentWidth);

        // Start from the next unrendered line
        int startLine = _lastLineRendered + 1;

        if (startLine >= lines.Count)
        {
            // All lines already rendered
            ctx.CurrentY += Margin.Bottom;
            return RenderStatus.Complete;
        }

        int remainingLines = lines.Count - startLine;
        float localY = ctx.CurrentY;

        // Calculate how many lines fit on this page. Each line's slot is the base
        // line height plus any paragraph leading above it (zero when unused).
        int linesThatFit = 0;
        float testY = localY;
        for (int i = startLine; i < lines.Count; i++)
        {
            if (testY + SpaceBeforeOf(i) + ComputedLineHeight > ctx.MaxY) break;
            linesThatFit++;
            testY += SpaceBeforeOf(i) + ComputedLineHeight;
        }

        // If all remaining lines fit, just render them all (no break needed)
        if (linesThatFit >= remainingLines)
        {
            return RenderLines(ctx, lines, startLine, lines.Count, contentWidth, localY);
        }

        bool notAtTop = ctx.CurrentY > ctx.ContentTop;

        // Orphan control: if this is the first chunk of this block on the current page
        // and too few lines would appear before the break, move the whole block
        if (startLine == 0 && linesThatFit < MinLinesBeforeBreak && notAtTop)
        {
            return RenderStatus.WrapRequired;
        }

        // Widow control: if too few lines would remain after the break,
        // reduce how many we render so at least MinLinesAfterBreak go to the next page
        int linesAfterBreak = remainingLines - linesThatFit;
        if (linesThatFit > 0 && linesAfterBreak > 0 && linesAfterBreak < MinLinesAfterBreak)
        {
            int targetFit = remainingLines - MinLinesAfterBreak;

            // Don't violate orphan control when adjusting for widows
            if (startLine == 0 && targetFit < MinLinesBeforeBreak && notAtTop)
            {
                // Can't satisfy both constraints - move the entire block
                return RenderStatus.WrapRequired;
            }

            linesThatFit = Math.Max(0, targetFit);
        }

        // If we have zero lines to render (widow adjustment pushed everything out)
        if (linesThatFit <= 0)
        {
            return startLine == 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;
        }

        // Render the lines that fit
        int endLine = startLine + linesThatFit;
        return RenderLines(ctx, lines, startLine, endLine, contentWidth, localY);
    }

    /// <summary>
    /// Renders lines from startLine (inclusive) to endLine (exclusive).
    /// </summary>
    private RenderStatus RenderLines(RenderContext ctx, Lines lines, int startLine, int endLine,
        float contentWidth, float localY)
    {
        for (int i = startLine; i < endLine; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            localY += SpaceBeforeOf(i); // paragraph leading (0 when unused)
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX + Margin.Left, isLastLine, i);
            localY += ComputedLineHeight;
        }

        _lastLineRendered = endLine - 1;
        ctx.CurrentY = localY;

        if (endLine >= lines.Count)
        {
            ctx.CurrentY += Margin.Bottom;
            return RenderStatus.Complete;
        }

        return RenderStatus.Overflow;
    }

    /// <summary>
    /// Resets the internal state, preparing the element for a fresh render.
    /// </summary>
    public virtual void ResetState()
    {
        _lastLineRendered = -1;
        _layout = null;
        _cachedContentWidth = -1;
    }

    protected override float GetPreferredContentWidth()
    {
        return MeasureTextWidth(Text) + FirstLineIndent;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        Lines lines = GetOrComputeLines(contentWidth);
        float height = lines.Count * ComputedLineHeight;
        for (int i = 0; i < lines.Count; i++) height += SpaceBeforeOf(i);
        return height;
    }

    /// <summary>Per-line horizontal indent; 0 when paragraph controls are unused.</summary>
    private float IndentOf(int i) => _cachedIndents != null && i < _cachedIndents.Count ? _cachedIndents[i] : 0f;

    /// <summary>Per-line extra leading above the line; 0 when unused.</summary>
    private float SpaceBeforeOf(int i) => _cachedSpaceBefore != null && i < _cachedSpaceBefore.Count ? _cachedSpaceBefore[i] : 0f;

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        Lines lines = GetOrComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        for (int i = 0; i < lines.Count; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            localY += SpaceBeforeOf(i); // paragraph leading (0 when unused)
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX, isLastLine, i);
            localY += ComputedLineHeight;
        }

        // Advance Y by the total content height
        ctx.CurrentY = localY;
    }

    /// <summary>
    /// Draws a single line of text, handling alignment (including justification) and decorations.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="line">The text line to draw</param>
    /// <param name="contentWidth">Available width for the line</param>
    /// <param name="y">The Y position (top of line)</param>
    /// <param name="baseX">The starting X position</param>
    /// <param name="isLastLine">Whether this is the last line of the text block (last lines are not justified)</param>
    private void DrawSingleLine(RenderContext ctx, string line, float contentWidth, float y, float baseX,
        bool isLastLine, int lineIndex)
    {
        using var directionScope = TextFlow.Enter(EffectiveContentDirection, _bidiLines?[lineIndex]);
        float indent = IndentOf(lineIndex);
        contentWidth -= indent;
        if (!IsRightToLeft) baseX += indent;
        // Apply letter/word spacing as canvas state around this line so the
        // painters advance by the same amount the layout reserved. No-op (and
        // byte-identical) when both are zero.
        bool spaced = LetterSpacing != 0 || WordSpacing != 0;
        ctx.Canvas.SetTextFeatures(FontFeatures);
        if (spaced) ctx.Canvas.SetTextSpacing(LetterSpacing, WordSpacing);
        try
        {
            DrawSingleLineInner(ctx, line, contentWidth, y, baseX, isLastLine);
        }
        finally
        {
            if (spaced) ctx.Canvas.SetTextSpacing(0, 0);
            ctx.Canvas.SetTextFeatures(null);
        }
    }

    private void DrawSingleLineInner(RenderContext ctx, string line, float contentWidth, float y, float baseX,
        bool isLastLine)
    {
        // Substitute current page number inline - it is known at render time
        if (line.Contains(CurrentPageToken))
            line = line.Replace(CurrentPageToken, (ctx.CurrentPageIndex + 1).ToString());

        if (line.Contains(TotalPagesToken))
        {
            DrawLineWithTokens(ctx, line, contentWidth, y, baseX);
            // Decorations for token lines use the measured width
            if (Decoration != TextDecoration.None)
            {
                float lineWidth = MeasureTextWidth(line);
                float decoX = baseX;
                switch (EffectiveAlignment(ctx))
                {
                    case TextAlignment.Center:
                        decoX += (contentWidth - lineWidth) / 2;
                        break;
                    case TextAlignment.Right:
                        decoX += contentWidth - lineWidth;
                        break;
                }
                DrawDecorations(ctx, decoX, y, lineWidth);
            }

            return;
        }

        // The last justified line follows the paragraph start.
        TextAlignment alignment = EffectiveAlignment(ctx);
        bool useJustify = alignment == TextAlignment.Justify && !isLastLine && line.Contains(' ');

        if (useJustify)
        {
            ctx.Canvas.DrawTextJustified(baseX, y, line, Color, FontSize, FontName, contentWidth);

            if (Decoration != TextDecoration.None)
                DrawDecorations(ctx, baseX, y, contentWidth);
        }
        else
        {
            // Align the last justified line to the paragraph start.
            TextAlignment effectiveAlign = alignment == TextAlignment.Justify ? (IsRightToLeft ? TextAlignment.Right : TextAlignment.Left) : alignment;

            float x = baseX;
            if (effectiveAlign == TextAlignment.Center)
                x += contentWidth / 2;
            else if (effectiveAlign == TextAlignment.Right)
                x += contentWidth;

            ctx.Canvas.DrawText(x, y, line, Color, FontSize, FontName, effectiveAlign);

            if (Decoration != TextDecoration.None)
            {
                float lineWidth = MeasureTextWidth(line);
                float decoX = baseX;
                switch (effectiveAlign)
                {
                    case TextAlignment.Center:
                        decoX += (contentWidth - lineWidth) / 2;
                        break;
                    case TextAlignment.Right:
                        decoX += contentWidth - lineWidth;
                        break;
                }
                DrawDecorations(ctx, decoX, y, lineWidth);
            }
        }
    }

    /// <summary>
    /// Draws text decorations (underline and/or strikethrough) for a rendered line.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="x">The left X position of the text</param>
    /// <param name="y">The top Y position of the line</param>
    /// <param name="width">The width of the text to decorate</param>
    private void DrawDecorations(RenderContext ctx, float x, float y, float width)
    {
        // Line thickness scales with font size (typical: 1/18th of font size, min 0.5pt)
        // unless an explicit DecorationThickness is set.
        float thickness = DecorationThickness ?? Math.Max(0.5f, FontSize / 18f);
        Color color = DecorationColor ?? Color;

        if (Decoration.HasFlag(TextDecoration.Overline))
        {
            // Overline: drawn above the text, just over the cap height.
            DecorationPainter.Paint(ctx.Canvas, x, y + FontSize * 0.15f, width, color, thickness, DecorationStyle);
        }

        if (Decoration.HasFlag(TextDecoration.Underline))
        {
            // Underline: drawn below the baseline (~fontSize/7 below).
            DecorationPainter.Paint(ctx.Canvas, x, y + FontSize + FontSize / 7f, width, color, thickness, DecorationStyle);
        }

        if (Decoration.HasFlag(TextDecoration.Strikethrough))
        {
            // Strikethrough: drawn at approximately half the cap height.
            DecorationPainter.Paint(ctx.Canvas, x, y + FontSize * 0.55f, width, color, thickness, DecorationStyle);
        }
    }

    private void DrawLineWithTokens(RenderContext ctx, string line, float contentWidth, float y, float baseX)
    {
        // 1. Calculate total width of the line (using placeholder for tokens)
        // MeasureTextWidth in TextElementBase now handles substitution
        float totalLineWidth = MeasureTextWidth(line);

        // 2. Determine Start X
        float startX = baseX;
        TextAlignment alignment = EffectiveAlignment(ctx);
        if (alignment == TextAlignment.Center)
            startX += (contentWidth - totalLineWidth) / 2;
        else if (alignment == TextAlignment.Right) startX += contentWidth - totalLineWidth;

        // 3. Draw Parts
        string[] parts = line.Split([TotalPagesToken], StringSplitOptions.None);
        float currentX = startX;

        for (int i = 0; i < parts.Length; i++)
        {
            // Draw text part
            string part = parts[i];
            if (!string.IsNullOrEmpty(part))
            {
                // Draw normal text
                // Since 'part' has no tokens, MeasureTextWidth(part) gives actual width
                // We draw Left-aligned at currentX
                ctx.Canvas.DrawText(currentX, y, part, Color, FontSize, FontName, TextAlignment.Left);
                currentX += MeasureTextWidth(part);
            }

            // Draw token if not the last part
            if (i < parts.Length - 1)
            {
                // Width of the placeholder
                float tokenWidth = MeasureTextWidth("00"); // Matches TextElementBase substitution

                if (ctx.IsSvgMode)
                {
                    if (ctx.Canvas is SvgCanvas svgCanvas)
                        svgCanvas.DrawDeferredTotalPages(currentX, y, Color, FontSize, FontName);
                    else
                        ctx.Canvas.DrawText(currentX, y, "00", Color, FontSize, FontName, TextAlignment.Left);
                }
                else
                {
                    string resourceName = ctx.RegisterDeferredXObject(FontName, FontSize, Color);
                    // DrawFormXObject draws at x, y (top-left of form)
                    // Our text rendering uses y as top of line (approx baseline logic inside DrawText)
                    // The XObject content uses "0 -FontSize Td", so drawing at y works.
                    ctx.Canvas.DrawFormXObject(resourceName, currentX, y);
                }

                currentX += tokenWidth;
            }
        }
    }

    /// <summary>The wrapped lines at <paramref name="contentWidth"/>, as a list (for tests).</summary>
    internal IList<string> LinesAt(float contentWidth)
    {
        Lines lines = GetOrComputeLines(contentWidth);
        var list = new List<string>(lines.Count);
        for (int i = 0; i < lines.Count; i++) list.Add(lines[i]);
        return list;
    }

    /// <summary>
    /// Gets cached lines or computes them if needed.
    /// Caching prevents re-computation during multipage rendering.
    /// </summary>
    private Lines GetOrComputeLines(float contentWidth)
    {
        // Use tolerance for floating point comparison
        if (_layout != null && Math.Abs(_cachedContentWidth - contentWidth) < 0.001f) return new Lines(_layout);

        bool paragraphs = FirstLineIndent != 0 || ParagraphSpacing != 0;
        List<string> lines;
        List<float>? indents = null;
        List<float>? spaceBefore = null;

        if (paragraphs)
        {
            ComputeParagraphLayout(Text, contentWidth, out lines, out indents, out spaceBefore);
        }
        else if (TryWrapPlainText(contentWidth, out List<string>? wrapped))
        {
            if (wrapped is null) return new Lines(_layout!);
            lines = wrapped;
        }
        else
        {
            // ComputeLines returns a new list, so it is kept rather than copied.
            IList<string> computed = ComputeLines(Text, contentWidth);
            lines = computed as List<string> ?? [.. computed];
        }

        if (MaxLines is int max && max > 0 && lines.Count > max)
        {
            lines = ClampLines(lines, contentWidth, max);
            indents = indents?.GetRange(0, lines.Count);
            spaceBefore = spaceBefore?.GetRange(0, lines.Count);
        }

        var bidi = IsRightToLeft || TextFlow.NeedsBidi(Text) ? TextFlow.MapLines(Text, lines) : null;
        // One plain line equal to the text keeps the text itself, not a copy of it.
        _layout = lines.Count == 1 && bidi is null && indents is null && spaceBefore is null
            ? (string.Equals(lines[0], Text, StringComparison.Ordinal) ? Text : lines[0])
            : new LineLayout(lines.ToArray(), indents?.ToArray(), spaceBefore?.ToArray(), bidi);
        _cachedContentWidth = contentWidth;
        return new Lines(_layout);
    }

    // Lines wrapped by TryWrapPlainText, reused by the next call on the thread.
    [ThreadStatic] private static List<string>? t_wrapped;

    // Whether a type's WrapSegment is TextBlock's own, so wrapping may bypass it.
    private static readonly ConcurrentDictionary<Type, bool> s_ownWrapSegment = new();

    /// <summary>
    /// <see cref="ComputeLines"/> for text that word-wraps as one segment (no breaks, no preserved
    /// whitespace, left-to-right), without a list per call: false when the text is not like that.
    /// One line is stored as the layout at once and <paramref name="lines"/> is null; otherwise
    /// <paramref name="lines"/> holds them.
    /// </summary>
    private bool TryWrapPlainText(float contentWidth, out List<string>? lines)
    {
        lines = null;
        string text = Text;
        if (string.IsNullOrEmpty(text) || PreserveWhitespace || IsRightToLeft || text.AsSpan().IndexOfAny('\r', '\n') >= 0 ||
            TextFlow.NeedsBidi(text) || !s_ownWrapSegment.GetOrAdd(GetType(), static type =>
                type.GetMethod(nameof(WrapSegment), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(string), typeof(float)])!.DeclaringType == typeof(TextBlock)))
            return false;

        using var performanceScope = PerformanceEvents.Measure(PerformanceStage.Measurement);
        List<string> wrapped = t_wrapped ??= [];
        wrapped.Clear();
        WrapWords(text, contentWidth, wrapped);
        // As ComputeLines: a segment that wraps to nothing is one blank line.
        if (wrapped.Count <= 1)
        {
            string line = wrapped.Count == 0 ? "" : wrapped[0];
            _layout = string.Equals(line, text, StringComparison.Ordinal) ? text : line;
            _cachedContentWidth = contentWidth;
            return true;
        }
        lines = [.. wrapped];
        return true;
    }

    /// <summary>
    /// Line clamp: keeps the first <paramref name="max"/> lines and appends
    /// <see cref="Ellipsis"/> to the last visible one, trimming whole words (then
    /// characters) until <c>line + ellipsis</c> fits <paramref name="contentWidth"/>.
    /// Dropped lines are never drawn, so no hidden text reaches the content stream
    /// or the structure tree - PDF/UA-safe by construction.
    /// </summary>
    private List<string> ClampLines(List<string> lines, float contentWidth, int max)
    {
        List<string> kept = lines.GetRange(0, max);
        int lastIndex = kept.Count - 1;
        string last = kept[lastIndex].TrimEnd();

        while (last.Length > 0 && MeasureTextWidth(last + Ellipsis) > contentWidth)
        {
            int lastSpace = last.TrimEnd().LastIndexOf(' ');
            last = lastSpace > 0 ? last[..lastSpace].TrimEnd() : last[..Typography.Engine.Graphemes.Boundaries(last)[^2]].TrimEnd();
        }

        kept[lastIndex] = last + Ellipsis;
        return kept;
    }

    /// <summary>
    /// Paragraph-aware line layout. Splits on hard <c>\n</c> breaks; the first
    /// line of each paragraph wraps to a narrower column (by <see cref="FirstLineIndent"/>)
    /// and carries that indent, and every paragraph after the first carries
    /// <see cref="ParagraphSpacing"/> of leading before its first line.
    /// </summary>
    private void ComputeParagraphLayout(string? text, float contentWidth,
        out List<string> lines, out List<float> indents, out List<float> spaceBefore)
    {
        lines = new List<string>();
        indents = new List<float>();
        spaceBefore = new List<float>();

        string[] segments = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int si = 0; si < segments.Length; si++)
        {
            IList<string> wrapped = WrapWithFirstIndent(segments[si], contentWidth);
            if (wrapped.Count == 0) wrapped.Add(string.Empty);
            for (int li = 0; li < wrapped.Count; li++)
            {
                lines.Add(wrapped[li]);
                indents.Add(li == 0 ? FirstLineIndent : 0f);
                spaceBefore.Add(li == 0 && si > 0 ? ParagraphSpacing : 0f);
            }
        }
    }

    /// <summary>
    /// Greedy word-wrap where the first line fits <c>contentWidth - firstLineIndent</c>
    /// and the remainder fits the full <paramref name="maxWidth"/>.
    /// </summary>
    /// <summary>Ordinary word wrapping, or the whitespace-preserving form when <see cref="PreserveWhitespace"/> is set.</summary>
    protected override IList<string> WrapSegment(string text, float maxWidth)
        => PreserveWhitespace || IsRightToLeft || TextFlow.NeedsBidi(text) ? WrapPreservingWhitespace(text, maxWidth, maxWidth) : base.WrapSegment(text, maxWidth);

    private IList<string> WrapWithFirstIndent(string segment, float contentWidth)
    {
        var lines = new List<string>();
        if (segment.Length == 0) return lines;
        if (PreserveWhitespace || IsRightToLeft || TextFlow.NeedsBidi(segment))
            return WrapPreservingWhitespace(segment, contentWidth - FirstLineIndent, contentWidth);

        // Words are the spans between single spaces; only their ends are needed, so the text is not split.
        ParagraphWidths? widths = segment.AsSpan().Count(' ') >= 2 ? ParagraphWidthsFor(segment) : null;
        float widthLimit = contentWidth - FirstLineIndent;

        // As in WrapSegment, the current line and each candidate are spans of the segment.
        int lineStart = 0, lineEnd = 0, wordStart = 0;
        while (wordStart <= segment.Length)
        {
            int space = segment.IndexOf(' ', wordStart);
            int wordEnd = space < 0 ? segment.Length : space;
            bool lineEmpty = lineStart == lineEnd;
            int testStart = lineEmpty ? wordStart : lineStart;
            if (MeasureSpan(segment, testStart, wordEnd, widths) > widthLimit && !lineEmpty)
            {
                AddLine(lines, segment, lineStart, lineEnd, widths);
                lineStart = wordStart;
                widthLimit = contentWidth; // subsequent lines are full width
            }
            else
            {
                lineStart = testStart;
            }
            lineEnd = wordEnd;
            wordStart = wordEnd + 1;
        }
        if (lineStart != lineEnd) AddLine(lines, segment, lineStart, lineEnd, widths);
        return lines;
    }
}
