using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Core;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;

namespace BonniePDF.Elements.Text;

/// <summary>
/// A rich text element that supports mixed styling within a single flowing paragraph.
/// Text spans with different fonts, sizes, colors, weights, and decorations flow together
/// with automatic word wrapping across span boundaries.
/// </summary>
public class RichText : ElementBase, IStatefulElement, IRoleProvider
{
    /// <summary>
    /// RichText renders one flowing paragraph, so it tags as /P. Without a role
    /// the pipeline would emit its text as untagged content (PDF/UA 7.1).
    /// </summary>
    public SemanticRole SemanticRole => SemanticRole.P;

    private readonly List<TextSpan> _spans = new();
    private float _cachedContentWidth = -1;
    private List<RichTextLine>? _cachedLines;

    // State tracking for multipage rendering
    private int _lastLineRendered = -1;

    /// <summary>
    /// Text alignment for the paragraph.
    /// </summary>
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Default style inherited by all spans. Span-level styles override these values.
    /// </summary>
    public TextStyle DefaultStyle { get; set; } = new()
    {
        FontFamily = "Helvetica",
        FontSize = 12,
        Color = Color.Black
    };

    /// <summary>
    /// Minimum number of lines to render before a page break (orphan control).
    /// </summary>
    public int MinLinesBeforeBreak { get; set; } = 2;

    /// <summary>
    /// Minimum number of lines that must appear after a page break (widow control).
    /// </summary>
    public int MinLinesAfterBreak { get; set; } = 2;

    // --- Builder API ---

    /// <summary>
    /// Adds a text span with an explicit style.
    /// </summary>
    public RichText Span(string text, TextStyle? style = null)
    {
        _spans.Add(new TextSpan
        {
            Text = text,
            // Snapshot the style: the fluent builders mutate and return `this`,
            // so a style shared across spans and mutated between them would
            // otherwise leak its final state onto every span that references it.
            Style = style?.Clone()
        });
        InvalidateCache();
        return this;
    }

    /// <summary>
    /// Adds a text span with a style configured via callback.
    /// </summary>
    public RichText Span(string text, Action<TextStyle> configure)
    {
        TextStyle style = new();
        configure(style);
        return Span(text, style);
    }

    /// <summary>
    /// Adds a text span followed by a line break.
    /// </summary>
    public RichText Line(string text, TextStyle? style = null)
    {
        _spans.Add(new TextSpan
        {
            Text = text,
            // Snapshot the style - see Span() for why.
            Style = style?.Clone(),
            IsLineBreak = true
        });
        InvalidateCache();
        return this;
    }

    /// <summary>
    /// Adds a line break (empty line).
    /// </summary>
    public RichText LineBreak()
    {
        _spans.Add(new TextSpan
        {
            Text = "",
            IsLineBreak = true
        });
        InvalidateCache();
        return this;
    }

    // --- Rendering ---

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="width"></param>
    /// <returns></returns>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Apply top margin only on first render
        if (_lastLineRendered < 0) ctx.CurrentY += Margin.Top;

        float contentWidth = width - Margin.Left - Margin.Right;
        List<RichTextLine> lines = GetOrComputeLines(contentWidth);

        int startLine = _lastLineRendered + 1;
        if (startLine >= lines.Count)
        {
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
            if (testY + lines[i].Height > ctx.MaxY) break;
            linesThatFit++;
            testY += lines[i].Height;
        }

        // If all remaining lines fit, render them all
        if (linesThatFit >= remainingLines)
            return RenderLines(ctx, lines, startLine, lines.Count, contentWidth, localY);

        bool notAtTop = ctx.CurrentY > ctx.Margin;

        // Orphan control
        if (startLine == 0 && linesThatFit < MinLinesBeforeBreak && notAtTop)
            return RenderStatus.WrapRequired;

        // Widow control
        int linesAfterBreak = remainingLines - linesThatFit;
        if (linesThatFit > 0 && linesAfterBreak > 0 && linesAfterBreak < MinLinesAfterBreak)
        {
            int targetFit = remainingLines - MinLinesAfterBreak;
            if (startLine == 0 && targetFit < MinLinesBeforeBreak && notAtTop)
                return RenderStatus.WrapRequired;
            linesThatFit = Math.Max(0, targetFit);
        }

        if (linesThatFit <= 0)
            return startLine == 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;

        int endLine = startLine + linesThatFit;
        return RenderLines(ctx, lines, startLine, endLine, contentWidth, localY);
    }

    private RenderStatus RenderLines(RenderContext ctx, List<RichTextLine> lines,
        int startLine, int endLine, float contentWidth, float localY)
    {
        for (int i = startLine; i < endLine; i++)
        {
            RichTextLine line = lines[i];
            bool isLastLine = i == lines.Count - 1;
            DrawRichLine(ctx, line, contentWidth, localY, ctx.CurrentX + Margin.Left, isLastLine);
            localY += line.Height;
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
    /// 
    /// </summary>
    public void ResetState()
    {
        _lastLineRendered = -1;
        InvalidateCache();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected override float GetPreferredContentWidth()
    {
        // Measure the total width of all spans without wrapping
        float totalWidth = 0;
        foreach (TextSpan span in _spans)
        {
            TextStyle resolved = ResolveStyle(span.Style);
            totalWidth += FontMetrics.GetStringWidth(
                span.Text ?? "", resolved.EffectiveFontName, resolved.EffectiveFontSize)
                + StyleSpacing(span.Text ?? "", resolved);
        }

        return totalWidth;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        List<RichTextLine> lines = GetOrComputeLines(contentWidth);
        return lines.Sum(line => line.Height);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="contentWidth"></param>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        List<RichTextLine> lines = GetOrComputeLines(contentWidth);
        float localY = ctx.CurrentY;

        for (int i = 0; i < lines.Count; i++)
        {
            bool isLastLine = i == lines.Count - 1;
            DrawRichLine(ctx, lines[i], contentWidth, localY, ctx.CurrentX, isLastLine);
            localY += lines[i].Height;
        }

        ctx.CurrentY = localY;
    }

    // --- Line Building ---

    /// <summary>
    /// Builds word-wrapped lines from the spans. Each line contains a list of styled runs.
    /// Words from different spans can coexist on the same line.
    /// </summary>
    private List<RichTextLine> ComputeLines(float maxWidth)
    {
        // 1. Break spans into styled words (word = text + style)
        List<StyledWord> words = BuildStyledWords();

        // 2. Flow words into lines, wrapping when width exceeds maxWidth
        List<RichTextLine> lines = new();
        RichTextLine currentLine = new();
        float currentWidth = 0;

        foreach (StyledWord word in words)
        {
            if (word.IsLineBreak)
            {
                // Add any text before the break
                if (!string.IsNullOrEmpty(word.Text))
                {
                    currentLine.Runs.Add(new StyledRun { Text = word.Text, Style = word.Style });
                    currentWidth += word.Width;
                }

                currentLine.TotalWidth = currentWidth;
                UpdateLineHeight(currentLine);
                lines.Add(currentLine);
                currentLine = new RichTextLine();
                currentWidth = 0;
                continue;
            }

            float wordWidth = word.Width;
            float spaceWidth = word.LeadingSpaceWidth;
            float totalNeeded = currentWidth > 0 ? spaceWidth + wordWidth : wordWidth;

            if (currentWidth + totalNeeded > maxWidth && currentWidth > 0)
            {
                // Line is full - wrap to next line
                currentLine.TotalWidth = currentWidth;
                UpdateLineHeight(currentLine);
                lines.Add(currentLine);
                currentLine = new RichTextLine();
                currentWidth = 0;
                // Don't add leading space at start of new line
                spaceWidth = 0;
                totalNeeded = wordWidth;
            }

            // Add leading space with its own style (may differ from the word's style at span
            // boundaries).  For example the space before a highlighted span should carry the
            // *preceding* span's style (no background), not the highlighted span's style.
            if (currentWidth > 0 && word.HasLeadingSpace)
            {
                TextStyle spaceStyle = word.LeadingSpaceStyle ?? word.Style;
                if (currentLine.Runs.Count > 0 && StylesEqual(currentLine.Runs[^1].Style, spaceStyle))
                    currentLine.Runs[^1].Text += " ";
                else
                    currentLine.Runs.Add(new StyledRun { Text = " ", Style = spaceStyle });
                currentWidth += word.LeadingSpaceWidth;
            }

            // Add the word text with its own style
            if (currentLine.Runs.Count > 0 && StylesEqual(currentLine.Runs[^1].Style, word.Style))
                currentLine.Runs[^1].Text += word.Text;
            else
                currentLine.Runs.Add(new StyledRun { Text = word.Text!, Style = word.Style });

            currentWidth += word.Width;
        }

        // Add the final line. A paragraph that ends immediately after a raw
        // text break ("a\n") keeps its trailing blank line, matching the
        // TextBlock string rule; a structural Line()/LineBreak() terminal
        // break does not create one.
        StyledWord? lastWord = words.Count > 0 ? words[^1] : null;
        if (currentLine.Runs.Count > 0 || (lastWord is { IsLineBreak: true, IsTextBreak: true }))
        {
            currentLine.TotalWidth = currentWidth;
            UpdateLineHeight(currentLine);
            lines.Add(currentLine);
        }

        // If no content at all, add one empty line with default height
        if (lines.Count == 0)
        {
            RichTextLine emptyLine = new();
            emptyLine.Height = DefaultStyle.EffectiveFontSize * 1.2f;
            lines.Add(emptyLine);
        }

        return lines;
    }

    /// <summary>
    /// Breaks all spans into a flat list of styled words. Each word carries the style
    /// of the span it came from. Spaces between words are tracked for accurate measurement.
    /// Raw break characters in span text (<c>\n</c>, with <c>\r\n</c>/<c>\r</c> normalised)
    /// split the span into segments; each break becomes a text-break word and each
    /// segment flows like a mini-span.
    /// </summary>
    private List<StyledWord> BuildStyledWords()
    {
        List<StyledWord> words = new();
        bool prevSpanEndedWithSpace = false;
        TextStyle? prevResolvedStyle = null;

        foreach (TextSpan span in _spans)
        {
            TextStyle resolved = ResolveStyle(span.Style);
            string rawText = span.Text ?? "";

            if (string.IsNullOrEmpty(rawText))
            {
                if (span.IsLineBreak)
                {
                    words.Add(new StyledWord { Style = resolved, IsLineBreak = true });
                    prevSpanEndedWithSpace = false;
                }
                continue;
            }

            string[] segments = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int s = 0; s < segments.Length; s++)
            {
                if (s > 0)
                {
                    words.Add(new StyledWord { Style = resolved, IsLineBreak = true, IsTextBreak = true });
                    // A break consumes any pending trailing space; the next word
                    // starts its line flush (the flow loop also drops leading spaces
                    // at line starts).
                    prevSpanEndedWithSpace = false;
                }

                string text = segments[s];
                if (string.IsNullOrEmpty(text)) continue;

                // Split by spaces, preserving awareness of leading/trailing spaces
                string[] parts = text.Split(' ');

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;

                    // A word has a leading space if:
                    // 1. It's not the first token in its segment (i > 0 means there was a space between parts)
                    // 2. It IS the first token in its segment (i == 0), and either:
                    //    a. The segment itself starts with a space (e.g. " mixed with"), or
                    //    b. The previous span ended with a space (e.g. "contains " → "bold")
                    bool hasLeadingSpace = i > 0 ||
                        (words.Count > 0 && (text.StartsWith(' ') || prevSpanEndedWithSpace));

                    // Determine which style *owns* the leading space:
                    // - Inter-span space: the space came from the previous span's trailing space.
                    //   It should use the previous span's style (e.g. no highlight background),
                    //   NOT the current word's style.  This prevents the highlight background of
                    //   "highlight important text" bleeding into the space before it.
                    // - Intra-span space (i > 0) or span-leading space (text.StartsWith(' ')):
                    //   the space belongs to this span's style.
                    bool isInterSpanSpace = hasLeadingSpace
                        && i == 0
                        && prevSpanEndedWithSpace
                        && !text.StartsWith(' ');

                    TextStyle spaceStyle = isInterSpanSpace && prevResolvedStyle != null
                        ? prevResolvedStyle
                        : resolved;

                    float partWidth = FontMetrics.GetStringWidth(
                        part, resolved.EffectiveFontName, resolved.EffectiveFontSize)
                        + StyleSpacing(part, resolved);
                    float spaceWidth = hasLeadingSpace
                        ? FontMetrics.GetStringWidth(" ", spaceStyle.EffectiveFontName, spaceStyle.EffectiveFontSize)
                          + StyleSpacing(" ", spaceStyle)
                        : 0;

                    words.Add(new StyledWord
                    {
                        Text = part,
                        Style = resolved,
                        Width = partWidth,
                        HasLeadingSpace = hasLeadingSpace,
                        LeadingSpaceWidth = spaceWidth,
                        LeadingSpaceStyle = hasLeadingSpace ? spaceStyle : null
                    });
                }

                prevSpanEndedWithSpace = text.EndsWith(' ');
            }

            // Handle trailing line break from the span
            if (span.IsLineBreak)
            {
                words.Add(new StyledWord { Style = resolved, IsLineBreak = true });
                prevSpanEndedWithSpace = false;
            }

            prevResolvedStyle = resolved;
        }

        return words;
    }

    /// <summary>
    /// Sets the line height to the maximum font size across all runs * 1.2.
    /// </summary>
    private void UpdateLineHeight(RichTextLine line)
    {
        float maxFontSize = DefaultStyle.EffectiveFontSize;
        foreach (StyledRun run in line.Runs)
        {
            float fontSize = run.Style.EffectiveFontSize;
            if (fontSize > maxFontSize) maxFontSize = fontSize;
        }

        line.Height = maxFontSize * 1.2f;
    }

    // --- Drawing ---

    /// <summary>
    /// Draws a single rich text line by iterating its styled runs.
    /// Each run is drawn at the current X offset with its own font/size/color.
    /// </summary>
    private void DrawRichLine(RenderContext ctx, RichTextLine line, float contentWidth,
        float y, float baseX, bool isLastLine)
    {
        if (line.Runs.Count == 0) return;

        // Calculate starting X based on alignment
        float startX = baseX;
        switch (Alignment)
        {
            case TextAlignment.Center:
                startX += (contentWidth - line.TotalWidth) / 2;
                break;
            case TextAlignment.Right:
                startX += contentWidth - line.TotalWidth;
                break;
        }

        // For justify, calculate extra space per space character
        float extraWordSpacing = 0;
        bool useJustify = Alignment == TextAlignment.Justify && !isLastLine;
        if (useJustify)
        {
            int spaceCount = 0;
            foreach (StyledRun run in line.Runs)
                foreach (char c in run.Text)
                    if (c == ' ') spaceCount++;

            if (spaceCount > 0)
                extraWordSpacing = (contentWidth - line.TotalWidth) / spaceCount;
        }

        float currentX = startX;

        foreach (StyledRun run in line.Runs)
        {
            if (string.IsNullOrEmpty(run.Text)) continue;

            TextStyle style = run.Style;
            string fontName = style.EffectiveFontName;
            float fontSize = style.EffectiveFontSize;
            Color color = style.EffectiveColor;

            // Draw background highlight if specified
            if (style.BackgroundColor.HasValue)
            {
                float runWidth = MeasureRunWidth(run, extraWordSpacing);
                ctx.Canvas.FillRect(currentX, y, runWidth, line.Height, style.BackgroundColor.Value);
            }

            // Baseline shift (super/subscript) moves the glyphs vertically without
            // affecting the run's advance width, so currentX and measurement are
            // untouched. Positive shift = up, so subtract from the top-down y.
            float shift = BaselineShiftResolver.Resolve(style.BaselineAlignment, style.BaselineShift, fontSize);
            float runY = y - shift;

            // letter/word spacing as canvas state for this run.
            float ls = style.LetterSpacing ?? 0;
            float ws = style.WordSpacing ?? 0;
            bool spaced = ls != 0 || ws != 0;
            if (spaced) ctx.Canvas.SetTextSpacing(ls, ws);

            // Draw each word segment separately when justified (to apply extra spacing)
            if (useJustify && extraWordSpacing > 0 && run.Text.Contains(' '))
            {
                currentX = DrawJustifiedRun(ctx, run, currentX, runY, line.Height, extraWordSpacing);
            }
            else
            {
                // Draw the run as a single text call
                ctx.Canvas.DrawText(currentX, runY, run.Text, color, fontSize, fontName, TextAlignment.Left);

                float runWidth = FontMetrics.GetStringWidth(run.Text, fontName, fontSize) + StyleSpacing(run.Text, style);

                // Draw decorations
                if (style.Decoration != TextDecoration.None)
                    DrawRunDecorations(ctx, style, currentX, runY, runWidth, fontSize);

                currentX += runWidth;
            }

            if (spaced) ctx.Canvas.SetTextSpacing(0, 0);
        }
    }

    /// <summary>
    /// Draws a justified run by splitting at spaces and adding extra spacing.
    /// </summary>
    private static float DrawJustifiedRun(RenderContext ctx, StyledRun run,
        float startX, float y, float lineHeight, float extraWordSpacing)
    {
        TextStyle style = run.Style;
        string fontName = style.EffectiveFontName;
        float fontSize = style.EffectiveFontSize;
        Color color = style.EffectiveColor;

        float currentX = startX;
        string[] words = run.Text.Split(' ');

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];

            // Add space before word (except first). The space glyph carries its own
            // letter/word spacing in addition to the justification spread.
            if (i > 0)
            {
                float spaceWidth = FontMetrics.GetStringWidth(" ", fontName, fontSize) + StyleSpacing(" ", style);
                currentX += spaceWidth + extraWordSpacing;
            }

            if (!string.IsNullOrEmpty(word))
            {
                ctx.Canvas.DrawText(currentX, y, word, color, fontSize, fontName, TextAlignment.Left);
                float wordWidth = FontMetrics.GetStringWidth(word, fontName, fontSize) + StyleSpacing(word, style);

                if (style.Decoration != TextDecoration.None)
                    DrawRunDecorations(ctx, style, currentX, y, wordWidth, fontSize);

                currentX += wordWidth;
            }
        }

        return currentX;
    }

    /// <summary>
    /// Draws underline, strikethrough, and/or overline decorations for a styled run.
    /// </summary>
    private static void DrawRunDecorations(RenderContext ctx, TextStyle style,
        float x, float y, float width, float fontSize)
    {
        float thickness = style.DecorationThickness ?? Math.Max(0.5f, fontSize / 18f);
        Color color = style.DecorationColor ?? style.EffectiveColor;
        DecorationStyle lineStyle = style.DecorationStyle;

        if (style.Decoration.HasFlag(TextDecoration.Overline))
            DecorationPainter.Paint(ctx.Canvas, x, y + fontSize * 0.15f, width, color, thickness, lineStyle);

        if (style.Decoration.HasFlag(TextDecoration.Underline))
            DecorationPainter.Paint(ctx.Canvas, x, y + fontSize + fontSize / 7f, width, color, thickness, lineStyle);

        if (style.Decoration.HasFlag(TextDecoration.Strikethrough))
            DecorationPainter.Paint(ctx.Canvas, x, y + fontSize * 0.55f, width, color, thickness, lineStyle);
    }

    /// <summary>
    /// Extra advance from a style's <see cref="TextStyle.LetterSpacing"/>/
    /// <see cref="TextStyle.WordSpacing"/> for a string: letter spacing after
    /// every glyph plus word spacing after every space. Zero when neither is set.
    /// </summary>
    private static float StyleSpacing(string text, TextStyle style)
    {
        float ls = style.LetterSpacing ?? 0;
        float ws = style.WordSpacing ?? 0;
        if (ls == 0 && ws == 0) return 0;
        int glyphs = 0;
        int spaces = 0;
        foreach (System.Text.Rune rune in text.EnumerateRunes())
        {
            if (rune.Value < 32) continue;
            glyphs++;
            if (rune.Value == ' ') spaces++;
        }
        return glyphs * ls + spaces * ws;
    }

    // --- Helpers ---

    private TextStyle ResolveStyle(TextStyle? spanStyle)
    {
        if (spanStyle == null) return DefaultStyle ?? new TextStyle { FontFamily = "Helvetica", FontSize = 12 };
        return spanStyle.MergeWith(DefaultStyle);
    }

    private static float MeasureRunWidth(StyledRun run, float extraWordSpacing)
    {
        float baseWidth = FontMetrics.GetStringWidth(
            run.Text, run.Style.EffectiveFontName, run.Style.EffectiveFontSize)
            + StyleSpacing(run.Text, run.Style);

        if (extraWordSpacing > 0)
        {
            int spaceCount = 0;
            foreach (char c in run.Text)
                if (c == ' ') spaceCount++;
            baseWidth += spaceCount * extraWordSpacing;
        }

        return baseWidth;
    }

    private static bool StylesEqual(TextStyle a, TextStyle b)
    {
        return string.Equals(a.EffectiveFontName, b.EffectiveFontName, StringComparison.Ordinal) &&
               Math.Abs(a.EffectiveFontSize - b.EffectiveFontSize) < 0.01f &&
               a.EffectiveColor.Equals(b.EffectiveColor) &&
               a.Decoration == b.Decoration &&
               a.BackgroundColor.Equals(b.BackgroundColor);
    }

    private List<RichTextLine> GetOrComputeLines(float contentWidth)
    {
        if (_cachedLines != null && Math.Abs(_cachedContentWidth - contentWidth) < 0.001f)
            return _cachedLines;

        _cachedLines = ComputeLines(contentWidth);
        _cachedContentWidth = contentWidth;
        return _cachedLines;
    }

    private void InvalidateCache()
    {
        _cachedLines = null;
        _cachedContentWidth = -1;
    }
}

/// <summary>
/// A single line of rich text, composed of multiple styled runs.
/// </summary>
internal class RichTextLine
{
    /// <summary>The styled runs that make up this line</summary>
    public List<StyledRun> Runs { get; set; } = new();

    /// <summary>The total width of all runs in this line</summary>
    public float TotalWidth { get; set; }

    /// <summary>The height of this line (determined by the tallest font)</summary>
    public float Height { get; set; }
}

/// <summary>
/// A contiguous piece of text with a single style, used for rendering.
/// </summary>
internal class StyledRun
{
    /// <summary>The text content</summary>
    public required string Text { get; set; }

    /// <summary>The resolved style for this run</summary>
    public required TextStyle Style { get; set; }
}

/// <summary>
/// An intermediate word representation used during line building.
/// </summary>
internal class StyledWord
{
    // Not required: line-break placeholder words (IsLineBreak = true) are
    // constructed without Text.
    public string? Text { get; set; }
    public required TextStyle Style { get; set; }
    public float Width { get; set; }
    public bool HasLeadingSpace { get; set; }
    public float LeadingSpaceWidth { get; set; }
    /// <summary>
    /// The style to use when rendering the leading space character.
    /// For intra-span spaces (space between words within the same span) this equals Style.
    /// For inter-span spaces (trailing space from the preceding span) this is the preceding
    /// span's resolved style - so the space inherits that span's background/color, not the
    /// following word's background/color.
    /// </summary>
    public TextStyle? LeadingSpaceStyle { get; set; }
    public bool IsLineBreak { get; set; }

    /// <summary>
    /// True when this line break came from a raw break character (<c>\n</c>) inside
    /// span text rather than the structural <c>Line()</c>/<c>LineBreak()</c> API. A
    /// paragraph-final text break yields a trailing blank line (matching the
    /// TextBlock string rule); a paragraph-final structural break does not.
    /// </summary>
    public bool IsTextBreak { get; set; }
}
