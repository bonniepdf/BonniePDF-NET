using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Export;

/// <summary>
/// A rendered SVG document: one standalone SVG string per page, plus
/// <c>@font-face</c> CSS that embeds (as subsetted data URIs) the fonts the
/// document used, so a host can render the pages with the exact fonts BonniePDF
/// laid them out with.
/// </summary>
public sealed class SvgDocumentExport
{
    public SvgDocumentExport(IReadOnlyList<string> pages, string fontFaceCss, Rendering.LayoutSnapshot snapshot = null)
    {
        Pages = pages;
        FontFaceCss = fontFaceCss;
        Snapshot = snapshot;
    }

    /// <summary>One standalone SVG string per page.</summary>
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
    public Rendering.LayoutSnapshot Snapshot { get; }
}

/// <summary>
/// Renders the laid-out pages of a <see cref="Document"/> to standalone SVG
/// strings, reusing the same layout/pagination pipeline as PDF output so the
/// page count and visual layout match the generated PDF.
/// </summary>
internal static class SvgPageExporter
{
    public static IReadOnlyList<string> Export(Document document, SvgPageExportOptions options)
        => ExportDocument(document, options).Pages;

    /// <summary>
    /// Like <see cref="Export"/>, but also returns <c>@font-face</c> CSS embedding a
    /// minimal subset (only the glyphs actually drawn) of each embedded font.
    /// </summary>
    public static SvgDocumentExport ExportDocument(Document document, SvgPageExportOptions options)
    {
        options ??= new SvgPageExportOptions();
        DocumentSettings settings = document.Settings;

        var svgOptions = new SvgRenderOptions
        {
            Precision = options.Precision,
            EmbedImages = options.EmbedImages,
            PdfBaseline = true
        };

        PdfSize oriented = settings.GetOrientedPageSize();
        RenderContext ctx = new(oriented, settings.Margin, isSvgMode: true, svgOptions, svgContentOnly: true)
        {
            Conformance = settings.Conformance
        };

        // Opt-in layout capture. Attached before rendering so the pipeline's
        // OpenNode/CloseNode/PageBreak hooks populate it; left null otherwise so
        // capture is a guarded no-op and output is unchanged.
        LayoutRecorder recorder = options.CaptureLayout ? new LayoutRecorder() : null;
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
            string body = SvgCanvas.ResolveTotalPages(pageOutput.ContentStream, totalPages);
            results.Add(Assemble(body, i + 1, pageOutput.Size, options, title));
        }

        LayoutSnapshot snapshot = null;
        if (recorder != null)
        {
            // Page sizes align with the emitted SVG pages.
            recorder.SetPages(renderedPages.Take(limit).Select(p => (p.Size.Width, p.Size.Height)));
            snapshot = recorder.Snapshot();
        }

        return new SvgDocumentExport(results, BuildFontFaceCss(ctx.SvgFontUsage), snapshot);
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

    private static string ResolveTitle(Document document, string explicitTitle)
    {
        if (!string.IsNullOrEmpty(explicitTitle)) return explicitTitle;
        string metaTitle = document.Metadata?.Title;
        return string.IsNullOrEmpty(metaTitle) ? "BonniePDF document" : metaTitle;
    }

    private static string Assemble(string body, int pageNumber, PdfSize size, SvgPageExportOptions options, string title)
    {
        string w = Round(size.Width, options.Precision);
        string h = Round(size.Height, options.Precision);

        StringBuilder sb = new();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\"");

        if (options.IncludeAccessibilityMetadata)
        {
            sb.Append($" role=\"img\" aria-label=\"{XmlEscape.EscapeAttr(title)} - page {pageNumber}\"");
        }
        sb.Append('>');

        if (options.IncludeAccessibilityMetadata)
        {
            sb.Append($"<title>{XmlEscape.EscapeXml(title + " - page " + pageNumber)}</title>");
        }

        sb.Append($"<g id=\"page-{pageNumber}\" data-bonniepdf-page=\"{pageNumber}\">");
        sb.Append(body);
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    private static string Round(float value, int precision)
        => System.Math.Round((double)value, precision).ToString(CultureInfo.InvariantCulture);
}
