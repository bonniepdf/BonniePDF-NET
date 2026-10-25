using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BonniePDF.Drawing;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// A rendered SVG document: one standalone SVG string per page, plus
/// <c>@font-face</c> CSS that embeds (as subsetted data URIs) the fonts the
/// document used, so a host can render the pages with the exact fonts BonniePDF
/// laid them out with.
/// </summary>
/// <remarks>
/// <see cref="ToJson"/> writes the export in the form a previewer reads, so a
/// document rendered on a server can be shown in a browser. The TypeScript
/// implementation produces the same JSON for the same document.
/// </remarks>
public sealed class SvgDocumentExport
{
    /// <summary>
    /// The version of the export format, written as <c>version</c>. It changes
    /// only for a breaking change to the export's shape; the embedded snapshot
    /// carries its own version.
    /// </summary>
    public const int CurrentVersion = 1;

    public SvgDocumentExport(
        IReadOnlyList<string> pages,
        string fontFaceCss,
        LayoutSnapshot? snapshot = null,
        ComplianceReport? report = null)
    {
        Pages = pages;
        FontFaceCss = fontFaceCss;
        Snapshot = snapshot;
        Report = report;
    }

    /// <summary>The export format version; see <see cref="CurrentVersion"/>.</summary>
    public int Version { get; } = CurrentVersion;

    /// <summary>One standalone SVG string per page, in page order. Each root
    /// carries <c>width</c>, <c>height</c> and <c>viewBox</c> in PDF points.</summary>
    public IReadOnlyList<string> Pages { get; }

    /// <summary><c>&lt;style&gt;</c>-ready <c>@font-face</c> rules, or <c>""</c> when the
    /// document uses no embeddable fonts (e.g. only the viewer-supplied standard-14).</summary>
    public string FontFaceCss { get; }

    /// <summary>
    /// The captured layout tree, present only when
    /// <see cref="SvgPageExportOptions.CaptureLayout"/> was set. Its pages and
    /// rectangles are index-aligned with <see cref="Pages"/> and use the same
    /// top-left, points coordinate space as the SVG.
    /// </summary>
    public LayoutSnapshot? Snapshot { get; }

    /// <summary>
    /// The compliance report for <see cref="Snapshot"/>, present exactly when the
    /// snapshot is. Equal to <see cref="CompliancePreflight.Evaluate(LayoutSnapshot)"/>
    /// with default options, so every rule family runs, WCAG included; filter by
    /// <see cref="ComplianceFinding.Standard"/> to hide one. Attached here so the
    /// report always comes from the same engine as the snapshot it describes,
    /// even when it is read in another process.
    /// </summary>
    public ComplianceReport? Report { get; }

    /// <summary>
    /// Writes the export as JSON: camelCase keys, unset values omitted rather
    /// than written as <c>null</c>, severities as <c>"error"</c> /
    /// <c>"warning"</c> / <c>"info"</c>. A browser previewer loads the result
    /// directly, for example from an HTTP response.
    /// </summary>
    /// <remarks>
    /// Like JavaScript's <c>JSON.stringify</c>, the output does not escape
    /// <c>&lt;</c>, <c>&gt;</c> or <c>&amp;</c>, which keeps SVG markup compact.
    /// It is therefore JSON for a response body or a file. Do not paste it
    /// unescaped into an HTML <c>&lt;script&gt;</c> element.
    /// </remarks>
    public string ToJson() => SvgDocumentExportJson.Write(this);
}

/// <summary>
/// Renders the laid-out pages of a <see cref="Document"/> to standalone SVG
/// strings, reusing the same layout/pagination pipeline as PDF output so the
/// page count and visual layout match the generated PDF.
/// </summary>
internal static class SvgPageExporter
{
    public static IReadOnlyList<string> Export(Document document, SvgPageExportOptions? options)
        => ExportDocument(document, options).Pages;

    /// <summary>
    /// Like <see cref="Export"/>, but also returns <c>@font-face</c> CSS embedding a
    /// minimal subset (only the glyphs actually drawn) of each embedded font.
    /// </summary>
    public static SvgDocumentExport ExportDocument(Document document, SvgPageExportOptions? options)
    {
        options ??= new SvgPageExportOptions();
        DocumentSettings settings = document.Settings;
        bool embedStandardFonts = settings.Conformance != PdfAConformance.None ||
                                  settings.Accessibility == AccessibilityMode.PdfUa1;
        using IDisposable fontScope = FontMetrics.BeginDocumentScope(embedStandardFonts, document.Fonts);

        var svgOptions = new SvgRenderOptions
        {
            Precision = options.Precision,
            EmbedImages = options.EmbedImages,
            PdfBaseline = true
        };

        PdfSize oriented = settings.GetOrientedPageSize();
        RenderContext ctx = new(oriented, settings.Margin, isSvgMode: true, svgOptions, svgContentOnly: true)
        {
            Conformance = settings.Conformance,
            EmojiRendering = settings.EmojiRendering
        };
        // Mirror PdfDocumentSerializer: PDF/UA-1 substitutes embedded Liberation for
        // the standard-14 references, so the preview must resolve them the same way.
        // Without this the SVG falls back to a generic "Helvetica, Arial, sans-serif"
        // stack and the page previews in whatever face the viewer happens to have -
        // not the face the PDF actually carries, and not what the TypeScript
        // exporter emits for the same document.
        ctx.Resources.EmbedStandardFonts = settings.Accessibility == AccessibilityMode.PdfUa1;
        ctx.Resources.MissingGlyphs = settings.MissingGlyphs;

        // Two-pass TOC resolution. A VisualToc renders before the headings it
        // references, so on a single pass its page numbers resolve to "XX" and
        // its links target page 1. Mirror PdfDocumentSerializer: run a throw-away
        // layout pass first (through the same pipeline, so pagination - and thus
        // the resolved page numbers - is identical) to populate TocEntries, then
        // seed the real context. Only paid for when the document contains a TOC.
        if (TocLayout.RequiresTwoPass(document))
        {
            RenderContext dryRunCtx = new(oriented, settings.Margin, isSvgMode: true, svgOptions, svgContentOnly: true)
            {
                Conformance = settings.Conformance,
                TocEntries = new Dictionary<string, int>()
            };
            dryRunCtx.Resources.EmbedStandardFonts = ctx.Resources.EmbedStandardFonts;
            dryRunCtx.Resources.MissingGlyphs = settings.MissingGlyphs;
            foreach (PageRenderOutput _ in PdfRenderPipeline.RenderPagesStreaming(document.Pages, settings, dryRunCtx))
            {
                // intentionally empty - discard the rendered output of the dry run
            }
            ctx.TocEntries = dryRunCtx.TocEntries;
        }

        // Opt-in layout capture. Attached before rendering so the pipeline's
        // OpenNode/CloseNode/PageBreak hooks populate it; left null otherwise so
        // capture is a guarded no-op and output is unchanged.
        LayoutRecorder? recorder = options.CaptureLayout ? new LayoutRecorder() : null;
        if (recorder != null) ctx.LayoutRecorder = recorder;

        string title = ResolveTitle(document, options.Title);

        var renderedPages = new List<PageRenderOutput>();
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(document.Pages, settings, ctx))
            renderedPages.Add(pageOutput);

        int totalPages = renderedPages.Count;
        int limit = options.MaxPages is int max && max >= 0 ? Math.Min(max, totalPages) : totalPages;

        var results = new List<string>(limit);
        for (int i = 0; i < limit; i++)
        {
            PageRenderOutput pageOutput = renderedPages[i];
            string visual = SvgCanvas.ResolveTotalPages(pageOutput.ContentStream, totalPages);
            // Link overlays are kept OUT of the page body and passed separately so
            // the assembler can place them above the painted layers while keeping
            // them outside the page's atomic-image group — an ARIA role="img"
            // prunes its subtree, so a live <a> nested inside it is invalid.
            string overlays = BuildLinkOverlays(pageOutput.Hyperlinks, pageOutput.Size.Height);
            results.Add(Assemble(visual, overlays, i + 1, pageOutput.Size, options, title));
        }

        LayoutSnapshot? snapshot = null;
        if (recorder != null)
        {
            // Page sizes align with the emitted SVG pages.
            recorder.SetPages(renderedPages.Take(limit).Select(p => new LayoutSnapshot.PageSize
            {
                Width = p.Size.Width,
                Height = p.Size.Height,
                Color = p.PageColor.HasValue ? LayoutRecorder.ColorToHex(p.PageColor.Value) : null,
                BackdropUnknown = p.PageBackdropUnknown
            }));
            // Document-level accessibility/compliance context for the evaluator.
            string? metaTitle = document.Metadata?.Title;
            recorder.SetMeta(new SnapshotMeta
            {
                AccessibilityMode = (int)settings.Accessibility,
                Conformance = settings.Conformance.ToString(),
                Language = string.IsNullOrEmpty(settings.Language) ? null : settings.Language,
                Title = string.IsNullOrEmpty(metaTitle) ? null : metaTitle
            });
            snapshot = recorder.Snapshot();

            // The Document is represented by a synthetic root, and its constructor-created first
            // Page has no separate declaration, so both navigate to the Document's own source.
            // Explicitly added pages keep their own (or no) attribution.
            string? documentSourceId = DebugSource.Of(document);
            if (documentSourceId is not null)
            {
                snapshot.Root.SourceId = documentSourceId;
                LayoutNode? automaticPage = snapshot.Root.Children.FirstOrDefault(n => n.Type == "Page");
                if (automaticPage is not null && automaticPage.SourceId is null)
                    automaticPage.SourceId = documentSourceId;
            }
        }

        // Evaluated over the finished snapshot. The evaluator is pure, and neither
        // the snapshot nor any page changes.
        ComplianceReport? report = snapshot is null ? null : CompliancePreflight.Evaluate(snapshot);
        return new SvgDocumentExport(results, BuildFontFaceCss(ctx.SvgFontUsage), snapshot, report);
    }

    /// <summary>
    /// Builds <c>@font-face</c> rules that embed a minimal subset (only the used
    /// glyphs) of each font as a base64 data URI, under its private
    /// <see cref="SvgCanvas.EmbeddedFontFamily"/> name. Falls back to the full font
    /// if subsetting fails, and skips fonts that yield no bytes.
    /// </summary>
    private static string BuildFontFaceCss(
        IReadOnlyDictionary<string, (TtfFont Font, HashSet<int> CodePoints)> usage)
    {
        var sb = new StringBuilder();
        foreach (KeyValuePair<string, (TtfFont Font, HashSet<int> CodePoints)> kvp in usage)
        {
            (TtfFont font, HashSet<int> codePoints) = kvp.Value;
            if (codePoints.Count == 0) continue;

            byte[] bytes;
            try
            {
                var subsetter = new TtfSubsetter(font);
                var glyphIds = new HashSet<ushort>();
                var charToGlyph = new Dictionary<int, ushort>();
                foreach (int cp in codePoints)
                {
                    ushort gid = font.GetGlyphId(cp);
                    glyphIds.Add(gid);
                    charToGlyph[cp] = gid;
                }
                bytes = subsetter.CreateSubset(glyphIds, charToGlyph);
            }
            catch
            {
                // Subsetting failed - fall back to the full font so the face still renders.
                bytes = font.GetRawFontData();
            }
            if (bytes == null || bytes.Length == 0) continue;

            sb.Append("@font-face{font-family:'")
              .Append(SvgCanvas.EmbeddedFontFamily(kvp.Key))
              .Append("';font-display:swap;src:url(\"data:font/ttf;base64,")
              .Append(Convert.ToBase64String(bytes))
              .Append("\") format(\"truetype\");}");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Builds clickable link overlays for the hyperlinks registered on a page,
    /// mirroring the /Link annotations the PDF serializer writes. External links
    /// (<see cref="Hyperlink.Uri"/>) become SVG <c>&lt;a&gt;</c> elements, so they
    /// are live links in any SVG host; internal links
    /// (<see cref="Hyperlink.TargetPageIndex"/>, e.g. VisualToc entries) become
    /// rects carrying <c>data-bp-link-page="N"</c> (1-based) that a host such as
    /// @bonniepdf/previewer wires to page navigation. The rects paint nothing
    /// (<c>fill="none"</c>) but hit-test (<c>pointer-events="all"</c>). Hyperlink
    /// render bounds are PDF coordinates (bottom-left origin), so Y is flipped.
    /// <para>
    /// Every external <c>&lt;a&gt;</c> carries an <c>aria-label</c> giving it a
    /// discernible accessible name — the link's visible text, the same value the
    /// PDF pipeline writes into the annotation <c>/Contents</c> (PDF/UA 7-18) — so
    /// the link reads identically across the PDF and its SVG preview. Without a
    /// name an anchor wrapping only an invisible hit-rect is an unnamed link
    /// (WCAG 2.4.4 / axe <c>link-name</c>).
    /// </para>
    /// </summary>
    private static string BuildLinkOverlays(IList<Hyperlink>? links, float pageHeight)
    {
        if (links == null || links.Count == 0) return "";

        StringBuilder sb = new();
        foreach (Hyperlink link in links)
        {
            float w = link.RenderWidth;
            float h = link.RenderHeight;
            if (w <= 0 || h <= 0) continue;

            string rect =
                $"x=\"{FmtNum(link.RenderX)}\" y=\"{FmtNum(pageHeight - link.RenderY - h)}\" " +
                $"width=\"{FmtNum(w)}\" height=\"{FmtNum(h)}\" fill=\"none\" pointer-events=\"all\"";

            if (!string.IsNullOrEmpty(link.Uri))
            {
                // Prefer the visible link text; fall back to the URI so the anchor
                // is never nameless even for links created without display text.
                string name = string.IsNullOrEmpty(link.Text) ? link.Uri : link.Text;
                sb.Append($"<a href=\"{XmlEscape.EscapeAttr(link.Uri)}\" target=\"_blank\" rel=\"noopener\" " +
                          $"aria-label=\"{XmlEscape.EscapeAttr(name)}\"><rect {rect}/></a>");
            }
            else if (link.TargetPageIndex is int page && page >= 0)
            {
                sb.Append($"<rect {rect} data-bp-link-page=\"{page + 1}\" style=\"cursor:pointer\"/>");
            }
        }
        return sb.ToString();
    }

    /// <summary>Formats a coordinate for link-overlay markup (2 dp, no trailing zeros).</summary>
    private static string FmtNum(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return "0";
        return ((double)Math.Round(value, 2)).ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveTitle(Document document, string? explicitTitle)
    {
        if (!string.IsNullOrEmpty(explicitTitle)) return explicitTitle;
        string? metaTitle = document.Metadata?.Title;
        return string.IsNullOrEmpty(metaTitle) ? "BonniePDF document" : metaTitle;
    }

    private static string Assemble(
        string body, string overlays, int pageNumber, PdfSize size, SvgPageExportOptions options, string title)
    {
        string w = Round(size.Width, options.Precision);
        string h = Round(size.Height, options.Precision);

        // Only external anchors are focusable interactive controls; internal
        // navigation rects are inert and don't force the model switch below.
        bool hasInteractiveLinks = overlays.Contains("<a ", StringComparison.Ordinal);
        string pageLabel = $"{XmlEscape.EscapeAttr(title)} - page {pageNumber}";

        StringBuilder sb = new();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\"");
        if (options.ShapeRendering != SvgShapeRendering.Auto)
        {
            string token = options.ShapeRendering switch
            {
                SvgShapeRendering.OptimizeSpeed => "optimizeSpeed",
                SvgShapeRendering.CrispEdges => "crispEdges",
                _ => "geometricPrecision",
            };
            sb.Append($" shape-rendering=\"{token}\"");
        }

        string groupAttrs = "";
        if (options.IncludeAccessibilityMetadata)
        {
            if (hasInteractiveLinks)
            {
                // Model: "labelled image + named links". A page carrying live links
                // cannot be a single atomic image — ARIA role="img" prunes its
                // subtree, so a focusable <a> inside it is both invisible to AT and
                // an invalid nesting (axe nested-interactive). Instead the root
                // stays an unpruned graphics container named via aria-label; the
                // painted page becomes an aria-hidden visual layer (its accessible
                // name is the root's label, exactly as the atomic-image model
                // presented it); and the link overlays ride above it as real,
                // individually-named <a> siblings.
                sb.Append($" aria-label=\"{pageLabel}\"");
                groupAttrs = " aria-hidden=\"true\"";
            }
            else
            {
                // No interactive links: keep the whole page as one atomic labelled image.
                sb.Append($" role=\"img\" aria-label=\"{pageLabel}\"");
            }
        }
        sb.Append('>');

        if (options.IncludeAccessibilityMetadata)
        {
            sb.Append($"<title>{XmlEscape.EscapeXml(title + " - page " + pageNumber)}</title>");
        }

        sb.Append($"<g id=\"page-{pageNumber}\" data-bonniepdf-page=\"{pageNumber}\"{groupAttrs}>");
        sb.Append(body);
        sb.Append("</g>");
        // Overlays sit after (above) the visual group and, crucially, outside it —
        // so external anchors are valid, focusable, named siblings of the page
        // image rather than pruned descendants of it.
        sb.Append(overlays);
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Round(float value, int precision)
        => System.Math.Round((double)value, precision).ToString(CultureInfo.InvariantCulture);
}
