using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF;

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
    private TextAlignment _alignment = TextAlignment.Left;
    private bool _alignmentExplicit;

    internal bool AlignmentIsExplicit => _alignmentExplicit;

    internal IReadOnlyList<TextSpan> SpanList => _spans;

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
    /// <example>
    /// <code>
    /// var rich = new RichText();
    /// rich.Span("Payment due within ");
    /// rich.Span("30 days", s =&gt; s.Bold());
    /// rich.Span(" of the invoice date, or ", s =&gt; s.Underline());
    /// </code>
    /// </example>
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

        bool notAtTop = ctx.CurrentY > ctx.ContentTop;

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
    /// The width the text takes on one line, with no wrapping. Measured by the same word flow that
    /// breaks lines, so a container that hands this width back as the maximum can never wrap the text
    /// (summing whole span widths instead disagrees by float rounding, and by trailing spaces, and
    /// wrapped a centred "Page 1 of 2" footer onto two lines).
    /// </summary>
    protected override float GetPreferredContentWidth()
    {
        float widest = 0;
        foreach (RichTextLine line in ComputeLines(float.PositiveInfinity))
            widest = Math.Max(widest, line.TotalWidth);

        return widest;
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

        // If no content at all, add one empty line with default height.
        if (lines.Count == 0)
        {
            RichTextLine emptyLine = new();
            emptyLine.Height = GetStyleLineHeight(DefaultStyle);
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

                    string measuredPart = Measured(part);
                    float partWidth = FontMetrics.GetStringWidth(
                        measuredPart, resolved.EffectiveFontName, resolved.EffectiveFontSize)
                        + StyleSpacing(measuredPart, resolved);
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
    /// Sets the line height to the greatest effective line height across all runs.
    /// </summary>
    private void UpdateLineHeight(RichTextLine line)
    {
        float maxLineHeight = GetStyleLineHeight(DefaultStyle);
        foreach (StyledRun run in line.Runs)
        {
            float lineHeight = GetStyleLineHeight(run.Style);
            if (lineHeight > maxLineHeight) maxLineHeight = lineHeight;
        }

        line.Height = maxLineHeight;
    }

    /// <summary>Returns font size multiplied by the style's line-height multiplier (default 1.2).</summary>
    private static float GetStyleLineHeight(TextStyle style) =>
        style.EffectiveFontSize * (style.LineHeight ?? 1.2f);

    // --- Drawing ---

    /// <summary>
    /// Draws a single rich text line by iterating its styled runs.
    /// Each run is drawn at the current X offset with its own font/size/color.
    /// </summary>
    private void DrawRichLine(RenderContext ctx, RichTextLine line, float contentWidth,
        float y, float baseX, bool isLastLine)
    {
        if (line.Runs.Count == 0) return;

        // Layout reserved two digits for each page token; alignment uses the width the line really
        // has on this page (the current page number may be one digit or three). Equal to
        // line.TotalWidth for a line with no tokens.
        float lineWidth = ActualLineWidth(ctx, line);

        // Calculate starting X based on alignment
        TextAlignment alignment = EffectiveAlignment(ctx);
        float startX = baseX;
        switch (alignment)
        {
            case TextAlignment.Center:
                startX += (contentWidth - lineWidth) / 2;
                break;
            case TextAlignment.Right:
                startX += contentWidth - lineWidth;
                break;
        }

        // For justify, calculate extra space per space character
        float extraWordSpacing = 0;
        bool useJustify = alignment == TextAlignment.Justify && !isLastLine;
        if (useJustify)
        {
            int spaceCount = 0;
            foreach (StyledRun run in line.Runs)
                foreach (char c in run.Text)
                    if (c == ' ') spaceCount++;

            if (spaceCount > 0)
                extraWordSpacing = (contentWidth - lineWidth) / spaceCount;
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
                float runWidth = HasPageToken(run.Text)
                    ? ActualRunWidth(ctx, run)
                    : MeasureRunWidth(run, extraWordSpacing);
                ctx.Canvas.FillRect(currentX, y, runWidth, line.Height, style.BackgroundColor.Value);
            }

            // Baseline shift (super/subscript) moves the glyphs vertically without
            // affecting the run's advance width, so currentX and measurement are
            // untouched. Positive shift = up, so subtract from the top-down y.
            float shift = BaselineShiftResolver.Resolve(style.BaselineAlignment, style.BaselineShift, fontSize);
            float runY = y - shift;

            // Letter/word spacing as canvas state for this run.
            float ls = style.LetterSpacing ?? 0;
            float ws = style.WordSpacing ?? 0;
            bool spaced = ls != 0 || ws != 0;
            if (spaced) ctx.Canvas.SetTextSpacing(ls, ws);

            // Draw each word segment separately when justified (to apply extra spacing)
            if (useJustify && extraWordSpacing > 0 && run.Text.Contains(' '))
            {
                currentX = DrawJustifiedRun(ctx, run, currentX, runY, extraWordSpacing);
            }
            else if (HasPageToken(run.Text))
            {
                float runWidth = DrawTokenText(ctx, run.Text, currentX, runY, style);

                if (style.Decoration != TextDecoration.None)
                    DrawRunDecorations(ctx, style, currentX, runY, runWidth, fontSize);

                currentX += runWidth;
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
        float startX, float y, float extraWordSpacing)
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
                float wordWidth;
                if (HasPageToken(word))
                {
                    wordWidth = DrawTokenText(ctx, word, currentX, y, style);
                }
                else
                {
                    ctx.Canvas.DrawText(currentX, y, word, color, fontSize, fontName, TextAlignment.Left);
                    wordWidth = FontMetrics.GetStringWidth(word, fontName, fontSize) + StyleSpacing(word, style);
                }

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
    /// <see cref="TextStyle.WordSpacing"/> for a string: letter spacing after every
    /// glyph plus word spacing after every space. Zero when neither is set.
    /// </summary>
    private static float StyleSpacing(string text, TextStyle style)
    {
        return TextSpacing.GetWidth(text, style.LetterSpacing ?? 0, style.WordSpacing ?? 0);
    }

    // --- Page tokens ---
    //
    // {{CurrentPage}} and {{TotalPages}} in a span's text behave as they do in a TextBlock: the current
    // page number is substituted when the line is drawn, the total page count is drawn as a deferred
    // object that resolves once the whole document has been laid out, and layout reserves the width of
    // two digits for each. Text with no token takes the original code path untouched.

    private static bool HasPageToken(string text) =>
        text.Contains("{{", StringComparison.Ordinal) &&
        (text.Contains(TextElementBase.CurrentPageToken, StringComparison.Ordinal) ||
         text.Contains(TextElementBase.TotalPagesToken, StringComparison.Ordinal));

    /// <summary>The text as layout measures it: each page token counts as two digits.</summary>
    private static string Measured(string text) =>
        HasPageToken(text)
            ? text.Replace(TextElementBase.TotalPagesToken, "00").Replace(TextElementBase.CurrentPageToken, "00")
            : text;

    private static string WithCurrentPage(RenderContext ctx, string text) =>
        text.Replace(TextElementBase.CurrentPageToken,
            (ctx.CurrentPageIndex + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>A run's advance on this page: the real current page number, two digits for the total.</summary>
    private static float ActualRunWidth(RenderContext ctx, StyledRun run)
    {
        string actual = WithCurrentPage(ctx, run.Text).Replace(TextElementBase.TotalPagesToken, "00");
        return FontMetrics.GetStringWidth(actual, run.Style.EffectiveFontName, run.Style.EffectiveFontSize)
            + StyleSpacing(actual, run.Style);
    }

    /// <summary>
    /// The line's width on this page. Layout measured each page token as two digits; the current page
    /// number may be shorter or longer, so alignment corrects by the difference. Exactly
    /// <see cref="RichTextLine.TotalWidth"/> when no run holds a token.
    /// </summary>
    private static float ActualLineWidth(RenderContext ctx, RichTextLine line)
    {
        float width = line.TotalWidth;
        foreach (StyledRun run in line.Runs)
        {
            if (string.IsNullOrEmpty(run.Text) || !HasPageToken(run.Text)) continue;
            width += ActualRunWidth(ctx, run) - MeasureRunWidth(run, 0);
        }

        return width;
    }

    /// <summary>
    /// Draws text that holds page tokens at (<paramref name="x"/>, <paramref name="y"/>) and returns
    /// its advance. The total page count is a deferred object, exactly as in a TextBlock.
    /// </summary>
    private static float DrawTokenText(RenderContext ctx, string text, float x, float y, TextStyle style)
    {
        string fontName = style.EffectiveFontName;
        float fontSize = style.EffectiveFontSize;
        Color color = style.EffectiveColor;

        string[] parts = WithCurrentPage(ctx, text).Split(new[] { TextElementBase.TotalPagesToken }, StringSplitOptions.None);
        float currentX = x;

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            if (part.Length > 0)
            {
                ctx.Canvas.DrawText(currentX, y, part, color, fontSize, fontName, TextAlignment.Left);
                currentX += FontMetrics.GetStringWidth(part, fontName, fontSize) + StyleSpacing(part, style);
            }

            if (i == parts.Length - 1) continue;

            if (ctx.IsSvgMode)
            {
                if (ctx.Canvas is BonniePDF.Drawing.SvgCanvas svgCanvas)
                    svgCanvas.DrawDeferredTotalPages(currentX, y, color, fontSize, fontName);
                else
                    ctx.Canvas.DrawText(currentX, y, "00", color, fontSize, fontName, TextAlignment.Left);
            }
            else
            {
                ctx.Canvas.DrawFormXObject(ctx.RegisterDeferredXObject(fontName, fontSize, color), currentX, y);
            }

            currentX += FontMetrics.GetStringWidth("00", fontName, fontSize) + StyleSpacing("00", style);
        }

        return currentX - x;
    }

    // --- Helpers ---

    private TextStyle ResolveStyle(TextStyle? spanStyle)
    {
        if (spanStyle == null) return DefaultStyle ?? new TextStyle { FontFamily = "Helvetica", FontSize = 12 };
        return spanStyle.MergeWith(DefaultStyle);
    }

    private static float MeasureRunWidth(StyledRun run, float extraWordSpacing)
    {
        string measured = Measured(run.Text);
        float baseWidth = FontMetrics.GetStringWidth(
            measured, run.Style.EffectiveFontName, run.Style.EffectiveFontSize)
            + StyleSpacing(measured, run.Style);

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
