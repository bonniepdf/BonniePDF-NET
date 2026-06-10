using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Drawing;
using BonniePDF.Rendering;

namespace BonniePDF.Export;

/// <summary>
/// Renders the laid-out pages of a <see cref="Document"/> to standalone SVG
/// strings, reusing the same layout/pagination pipeline as PDF output so the
/// page count and visual layout match the generated PDF.
/// </summary>
internal static class SvgPageExporter
{
    public static IReadOnlyList<string> Export(Document document, SvgPageExportOptions options)
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

        string title = ResolveTitle(document, options.Title);

        var results = new List<string>();
        int pageNumber = 1;
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(document.Pages, settings, ctx))
        {
            results.Add(Assemble(pageOutput.ContentStream, pageNumber, pageOutput.Size, options, title));
            pageNumber++;
            // Stop after the requested page count. Earlier pages are already laid
            // out; we simply stop consuming the streaming pipeline.
            if (options.MaxPages is int max && results.Count >= max) break;
        }

        return results;
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
            sb.Append($" role=\"img\" aria-label=\"{EscapeAttr(title)} - page {pageNumber}\"");
        }
        sb.Append('>');

        if (options.IncludeAccessibilityMetadata)
        {
            sb.Append($"<title>{EscapeXml(title + " - page " + pageNumber)}</title>");
        }

        sb.Append($"<g id=\"page-{pageNumber}\" data-bonniepdf-page=\"{pageNumber}\">");
        sb.Append(body);
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    private static string Round(float value, int precision)
        => System.Math.Round((double)value, precision).ToString(CultureInfo.InvariantCulture);

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string EscapeAttr(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");
    }
}
