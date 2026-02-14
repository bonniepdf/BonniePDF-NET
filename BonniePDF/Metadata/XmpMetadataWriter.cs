using System;
using System.Text;

namespace BonniePDF.Metadata;

/// <summary>
/// Generates XMP metadata XML for PDF documents.
/// XMP (Extensible Metadata Platform) provides standardized metadata
/// that is required for PDF/A compliance and optional for PDF 1.7.
/// </summary>
internal static class XmpMetadataWriter
{
    /// <summary>
    /// Generates the XMP XML string from document metadata.
    /// </summary>
    public static string GenerateXmpXml(DocumentMetadata metadata, bool pdfA = false)
    {
        StringBuilder sb = new();

        sb.Append("<?xpacket begin=\"\xEF\xBB\xBF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.Append("  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");
        sb.Append("    <rdf:Description rdf:about=\"\"\n");
        sb.Append("      xmlns:dc=\"http://purl.org/dc/elements/1.1/\"\n");
        sb.Append("      xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\"\n");
        sb.Append("      xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\"\n");
        sb.Append("      xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\">\n");

        // dc:title
        if (!string.IsNullOrEmpty(metadata.Title))
        {
            sb.Append("      <dc:title>\n");
            sb.Append("        <rdf:Alt>\n");
            sb.Append($"          <rdf:li xml:lang=\"x-default\">{EscapeXml(metadata.Title)}</rdf:li>\n");
            sb.Append("        </rdf:Alt>\n");
            sb.Append("      </dc:title>\n");
        }

        // dc:creator
        if (!string.IsNullOrEmpty(metadata.Author))
        {
            sb.Append("      <dc:creator>\n");
            sb.Append("        <rdf:Seq>\n");
            sb.Append($"          <rdf:li>{EscapeXml(metadata.Author)}</rdf:li>\n");
            sb.Append("        </rdf:Seq>\n");
            sb.Append("      </dc:creator>\n");
        }

        // dc:subject (keywords)
        if (!string.IsNullOrEmpty(metadata.Keywords))
        {
            sb.Append("      <dc:subject>\n");
            sb.Append("        <rdf:Bag>\n");
            foreach (string keyword in metadata.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                sb.Append($"          <rdf:li>{EscapeXml(keyword)}</rdf:li>\n");
            }
            sb.Append("        </rdf:Bag>\n");
            sb.Append("      </dc:subject>\n");
            
            // PDF/A requires pdf:Keywords to match the Info dictionary Keywords exactly
            sb.Append($"      <pdf:Keywords>{EscapeXml(metadata.Keywords)}</pdf:Keywords>\n");
        }

        // dc:description
        if (!string.IsNullOrEmpty(metadata.Subject))
        {
            sb.Append("      <dc:description>\n");
            sb.Append("        <rdf:Alt>\n");
            sb.Append($"          <rdf:li xml:lang=\"x-default\">{EscapeXml(metadata.Subject)}</rdf:li>\n");
            sb.Append("        </rdf:Alt>\n");
            sb.Append("      </dc:description>\n");
        }

        // xmp:CreatorTool
        if (!string.IsNullOrEmpty(metadata.Creator))
            sb.Append($"      <xmp:CreatorTool>{EscapeXml(metadata.Creator)}</xmp:CreatorTool>\n");

        // xmp:CreateDate
        if (metadata.CreationDate.HasValue)
            sb.Append($"      <xmp:CreateDate>{FormatIso8601(metadata.CreationDate.Value)}</xmp:CreateDate>\n");

        // xmp:ModifyDate
        if (metadata.ModifiedDate.HasValue)
            sb.Append($"      <xmp:ModifyDate>{FormatIso8601(metadata.ModifiedDate.Value)}</xmp:ModifyDate>\n");

        // pdf:Producer
        if (!string.IsNullOrEmpty(metadata.Producer))
            sb.Append($"      <pdf:Producer>{EscapeXml(metadata.Producer)}</pdf:Producer>\n");

        // PDF/A identification
        if (pdfA)
        {
            sb.Append("      <pdfaid:part>1</pdfaid:part>\n");
            sb.Append("      <pdfaid:conformance>B</pdfaid:conformance>\n");
        }

        sb.Append("    </rdf:Description>\n");
        sb.Append("  </rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"w\"?>");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a DateTime as ISO 8601 string.
    /// </summary>
    private static string FormatIso8601(DateTime date)
    {
        if (date.Kind == DateTimeKind.Utc)
            return date.ToString("yyyy-MM-ddTHH:mm:ss+00:00");
        return date.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    /// <summary>
    /// Escapes special characters for XML content.
    /// </summary>
    private static string EscapeXml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
