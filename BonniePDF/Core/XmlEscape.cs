namespace BonniePDF.Core;

/// <summary>
/// XML/SVG string escaping helpers shared by the SVG canvas, the SVG page
/// exporter, and the render pipelines. Kept separate from
/// <see cref="BonniePDF.Rendering.Pdf.PdfSyntax"/> (which escapes PDF string
/// literals, a different grammar).
/// </summary>
internal static class XmlEscape
{
    /// <summary>
    /// Escapes a string for use as XML text content (&amp;, &lt;, &gt;, ", ').
    /// </summary>
    public static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// Escapes a string for use inside a double-quoted XML attribute value.
    /// </summary>
    public static string EscapeAttr(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;");
    }
}
