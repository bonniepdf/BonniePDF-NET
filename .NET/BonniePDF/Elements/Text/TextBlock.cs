using System;
using System.Collections.Generic;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Text;

/// <summary>
/// A text block element that supports word wrapping, alignment, font customization,
/// and automatic pagination across page boundaries.
/// </summary>
public class TextBlock : TextElementBase, IStatefulElement
{
    private float _cachedContentWidth = -1;
    private List<string>? _cachedLines;

    // State tracking for multipage rendering
    private int _lastLineRendered = -1;
    public required string Text { get; set; }
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

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

    public TextBlock(string text)
    {
        this.Text = text;
    }

    /// <summary>
    /// Renders this text block with state tracking for pagination.
    /// Text that doesn't fit on the current page will be continued on the next page.
    /// Respects MinLinesBeforeBreak (orphan) and MinLinesAfterBreak (widow) settings.
    /// </summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Apply top margin only on first render (not on continuation pages)
        if (_lastLineRendered < 0) ctx.CurrentY += Margin.Top;

        float contentWidth = width - Margin.Left - Margin.Right;
        List<string> lines = GetOrComputeLines(contentWidth);

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

        // Calculate how many lines fit on this page
        int linesThatFit = 0;
        float testY = localY;
        for (int i = startLine; i < lines.Count; i++)
        {
            if (testY + LineHeight > ctx.MaxY) break;
            linesThatFit++;
            testY += LineHeight;
        }

        // If all remaining lines fit, just render them all (no break needed)
        if (linesThatFit >= remainingLines)
        {
            return RenderLines(ctx, lines, startLine, lines.Count, contentWidth, localY);
        }

        bool notAtTop = ctx.CurrentY > ctx.Margin;

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
                // Can't satisfy both constraints — move the entire block
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
    private RenderStatus RenderLines(RenderContext ctx, List<string> lines, int startLine, int endLine,
        float contentWidth, float localY)
    {
        for (int i = startLine; i < endLine; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX + Margin.Left, isLastLine);
            localY += LineHeight;
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
    public void ResetState()
    {
        _lastLineRendered = -1;
        _cachedLines = null;
        _cachedContentWidth = -1;
    }

    protected override float GetPreferredContentWidth()
    {
        return MeasureTextWidth(Text);
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        List<string> lines = GetOrComputeLines(contentWidth);
        return lines.Count * LineHeight;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        List<string> lines = GetOrComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        for (int i = 0; i < lines.Count; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            DrawSingleLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX, isLastLine);
            localY += LineHeight;
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
        if (line.Contains(TotalPagesToken))
        {
            DrawLineWithTokens(ctx, line, contentWidth, y, baseX);
            // Decorations for token lines use the measured width
            if (TextDecoration != TextDecoration.None)
            {
                float lineWidth = MeasureTextWidth(line);
                float decoX = baseX;
                switch (Alignment)
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
        bool useJustify = Alignment == TextAlignment.Justify && !isLastLine && line.Contains(' ');

        if (useJustify)
        {
            ctx.Canvas.DrawTextJustified(baseX, y, line, TextColor, FontSize, FontName, contentWidth);

            if (TextDecoration != TextDecoration.None)
                DrawDecorations(ctx, baseX, y, contentWidth);
        }
        else
        {
            // For justified last lines, fall back to left alignment
            TextAlignment effectiveAlign = Alignment == TextAlignment.Justify ? TextAlignment.Left : Alignment;

            float x = baseX;
            if (effectiveAlign == TextAlignment.Center)
                x += contentWidth / 2;
            else if (effectiveAlign == TextAlignment.Right)
                x += contentWidth;

            ctx.Canvas.DrawText(x, y, line, TextColor, FontSize, FontName, effectiveAlign);

            if (TextDecoration != TextDecoration.None)
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
        float thickness = Math.Max(0.5f, FontSize / 18f);

        if (TextDecoration.HasFlag(TextDecoration.Underline))
        {
            // Underline: drawn below the baseline.
            // Baseline is approximately at y + fontSize. Underline offset below baseline ~fontSize/7.
            float underlineY = y + FontSize + FontSize / 7f;
            ctx.Canvas.DrawLine(x, underlineY, x + width, underlineY, TextColor, thickness);
        }

        if (TextDecoration.HasFlag(TextDecoration.Strikethrough))
        {
            // Strikethrough: drawn at approximately half the cap height.
            // Cap height is roughly 0.7 * fontSize, so midpoint is ~0.35 * fontSize from top,
            // offset by the ascent baseline positioning (~0.5 * fontSize from line top).
            float strikeY = y + FontSize * 0.55f;
            ctx.Canvas.DrawLine(x, strikeY, x + width, strikeY, TextColor, thickness);
        }
    }

    private void DrawLineWithTokens(RenderContext ctx, string line, float contentWidth, float y, float baseX)
    {
        // 1. Calculate total width of the line (using placeholder for tokens)
        // MeasureTextWidth in TextElementBase now handles substitution
        float totalLineWidth = MeasureTextWidth(line);

        // 2. Determine Start X
        float startX = baseX;
        if (Alignment == TextAlignment.Center)
            startX += (contentWidth - totalLineWidth) / 2;
        else if (Alignment == TextAlignment.Right) startX += contentWidth - totalLineWidth;

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
                ctx.Canvas.DrawText(currentX, y, part, TextColor, FontSize, FontName, TextAlignment.Left);
                currentX += MeasureTextWidth(part);
            }

            // Draw token if not the last part
            if (i < parts.Length - 1)
            {
                // Width of the placeholder
                float tokenWidth = MeasureTextWidth("00"); // Matches TextElementBase substitution

                if (ctx.IsSvgMode)
                {
                    ctx.Canvas.DrawText(currentX, y, "??", TextColor, FontSize, FontName, TextAlignment.Left);
                }
                else
                {
                    string resourceName = ctx.RegisterDeferredXObject(FontName, FontSize, TextColor);
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
    private List<string> GetOrComputeLines(float contentWidth)
    {
        // Use tolerance for floating point comparison
        if (_cachedLines != null && Math.Abs(_cachedContentWidth - contentWidth) < 0.001f) return _cachedLines;

        _cachedLines = ComputeLines(Text, contentWidth);
        _cachedContentWidth = contentWidth;
        return _cachedLines;
    }
}