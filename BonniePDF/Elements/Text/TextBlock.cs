using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// A text block element that supports word wrapping, alignment, font customization,
/// and automatic pagination across page boundaries.
/// </summary>
public class TextBlock : TextElementBase, IStatefulElement, IRoleProvider
{
    private float _cachedContentWidth = -1;
    private IList<string>? _cachedLines;
    // Per-line paragraph metadata parallel to _cachedLines.
    private IList<float>? _cachedIndents;
    private IList<float>? _cachedSpaceBefore;

    // State tracking for multipage rendering
    private int _lastLineRendered = -1;
    public required string Text { get; set; }
    private TextAlignment _alignment = TextAlignment.Left;
    private bool _alignmentExplicit;

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
    private TextAlignment EffectiveAlignment(RenderContext ctx) =>
        _alignmentExplicit ? _alignment : ctx.AmbientHorizontalTextAlignment ?? TextAlignment.Left;

    /// <summary>
    /// Line clamp: maximum number of lines to render. When set and the text
    /// wraps to more lines, the block is truncated to this many lines and
    /// <see cref="Ellipsis"/> is appended to the last visible line. Null = no clamp.
    /// </summary>
    public int? MaxLines { get; set; }

    /// <summary>Ellipsis appended to the last line when <see cref="MaxLines"/> clamps. Default <c>…</c>.</summary>
    public string Ellipsis { get; set; } = "…";

    /// <summary>
    /// Paragraph layout: horizontal indent (points) applied to the first line
    /// of each paragraph (a paragraph = text between hard <c>\n</c> breaks). Default 0.
    /// </summary>
    public float FirstLineIndent { get; set; }

    /// <summary>
    /// Paragraph layout: extra vertical space (points) inserted before each
    /// paragraph after the first. Default 0.
    /// </summary>
    public float ParagraphSpacing { get; set; }

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
    public bool PreserveWhitespace { get; set; }

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
    public int MinLinesBeforeBreak { get; set; } = 2;

    /// <summary>
    /// Minimum number of lines that must appear after a page break (widow control).
    /// If fewer lines remain for the next page, the break point is adjusted
    /// to push more lines forward. Set to 1 to disable widow control. Default is 2.
    /// </summary>
    public int MinLinesAfterBreak { get; set; } = 2;

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
        IList<string> lines = GetOrComputeLines(contentWidth);

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
    private RenderStatus RenderLines(RenderContext ctx, IList<string> lines, int startLine, int endLine,
        float contentWidth, float localY)
    {
        for (int i = startLine; i < endLine; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            localY += SpaceBeforeOf(i); // paragraph leading (0 when unused)
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX + Margin.Left + IndentOf(i), isLastLine);
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
        _cachedLines = null;
        _cachedIndents = null;
        _cachedSpaceBefore = null;
        _cachedContentWidth = -1;
    }

    protected override float GetPreferredContentWidth()
    {
        return MeasureTextWidth(Text) + FirstLineIndent;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        IList<string> lines = GetOrComputeLines(contentWidth);
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
        IList<string> lines = GetOrComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        for (int i = 0; i < lines.Count; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            localY += SpaceBeforeOf(i); // paragraph leading (0 when unused)
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX + IndentOf(i), isLastLine);
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
        bool isLastLine)
    {
        // Apply letter/word spacing as canvas state around this line so the
        // painters advance by the same amount the layout reserved. No-op (and
        // byte-identical) when both are zero.
        bool spaced = LetterSpacing != 0 || WordSpacing != 0;
        if (spaced) ctx.Canvas.SetTextSpacing(LetterSpacing, WordSpacing);
        try
        {
            DrawSingleLineInner(ctx, line, contentWidth, y, baseX, isLastLine);
        }
        finally
        {
            if (spaced) ctx.Canvas.SetTextSpacing(0, 0);
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

        // Determine effective alignment: justified text uses left for the last line
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
            // For justified last lines, fall back to left alignment
            TextAlignment effectiveAlign = alignment == TextAlignment.Justify ? TextAlignment.Left : alignment;

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
        string[] parts = line.Split(new[] { TotalPagesToken }, StringSplitOptions.None);
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

    /// <summary>
    /// Gets cached lines or computes them if needed.
    /// Caching prevents re-computation during multipage rendering.
    /// </summary>
    private IList<string> GetOrComputeLines(float contentWidth)
    {
        // Use tolerance for floating point comparison
        if (_cachedLines != null && Math.Abs(_cachedContentWidth - contentWidth) < 0.001f) return _cachedLines;

        bool paragraphs = FirstLineIndent != 0 || ParagraphSpacing != 0;
        List<string> lines;
        List<float> indents;
        List<float> spaceBefore;

        if (paragraphs)
        {
            ComputeParagraphLayout(Text, contentWidth, out lines, out indents, out spaceBefore);
        }
        else
        {
            lines = new List<string>(ComputeLines(Text, contentWidth));
            indents = Enumerable.Repeat(0f, lines.Count).ToList();
            spaceBefore = Enumerable.Repeat(0f, lines.Count).ToList();
        }

        if (MaxLines is int max && max > 0 && lines.Count > max)
        {
            lines = ClampLines(lines, contentWidth, max);
            indents = indents.GetRange(0, lines.Count);
            spaceBefore = spaceBefore.GetRange(0, lines.Count);
        }

        _cachedLines = lines;
        _cachedIndents = indents;
        _cachedSpaceBefore = spaceBefore;
        _cachedContentWidth = contentWidth;
        return _cachedLines;
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
            last = lastSpace > 0 ? last[..lastSpace].TrimEnd() : last[..^1].TrimEnd();
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
    private void ComputeParagraphLayout(string text, float contentWidth,
        out List<string> lines, out List<float> indents, out List<float> spaceBefore)
    {
        lines = new List<string>();
        indents = new List<float>();
        spaceBefore = new List<float>();

        string[] segments = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int si = 0; si < segments.Length; si++)
        {
            List<string> wrapped = WrapWithFirstIndent(segments[si], contentWidth);
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
    /// and the remainder fits the full <paramref name="contentWidth"/>.
    /// </summary>
    /// <summary>Ordinary word wrapping, or the whitespace-preserving form when <see cref="PreserveWhitespace"/> is set.</summary>
    protected override List<string> WrapSegment(string text, float maxWidth)
        => PreserveWhitespace ? WrapPreservingWhitespace(text, maxWidth, maxWidth) : base.WrapSegment(text, maxWidth);

    private List<string> WrapWithFirstIndent(string segment, float contentWidth)
    {
        var lines = new List<string>();
        if (segment.Length == 0) return lines;
        if (PreserveWhitespace)
            return WrapPreservingWhitespace(segment, contentWidth - FirstLineIndent, contentWidth);

        string[] words = segment.Split(' ');
        string current = string.Empty;
        float widthLimit = contentWidth - FirstLineIndent;

        foreach (string word in words)
        {
            string test = current.Length == 0 ? word : current + ' ' + word;
            if (MeasureTextWidth(test) > widthLimit && current.Length != 0)
            {
                lines.Add(current);
                current = word;
                widthLimit = contentWidth; // subsequent lines are full width
            }
            else
            {
                current = test;
            }
        }
        if (current.Length != 0) lines.Add(current);
        return lines;
    }
}
