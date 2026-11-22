using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BonniePDF.Metadata;

/// <summary>
/// Generates XMP metadata XML for PDF documents.
/// Produces the canonical RDF/XML packet layout shared across implementations.
/// </summary>
internal static class XmpMetadataWriter
{
    private const string Iso8601UtcFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    private static readonly char[] KeywordSeparators = [',', ';'];

    /// <summary>
    /// Generates the XMP XML string from document metadata, including
    /// any conformance-namespace blocks the document claims
    /// (PDF/A, PDF/UA, ZUGFeRD/Factur-X).
    /// </summary>
    public static string GenerateXmpXml(
        DocumentMetadata metadata,
        IReadOnlyList<XmpConformanceBlock>? conformanceBlocks = null)
    {
        IReadOnlyList<XmpConformanceBlock> blocks = conformanceBlocks ?? [];
        var sb = new StringBuilder(1024);
        sb.Append("<?xpacket begin=\"\uFEFF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n");
        sb.Append("<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n");
        sb.Append("  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n");

        bool wroteDescription = false;

        // 1. Conformance namespaces (pdfaid, pdfuaid, fx, ...) in one rdf:Description
        if (blocks.Count > 0)
        {
            wroteDescription = true;
            sb.Append("    <rdf:Description rdf:about=\"\"");
            foreach (XmpConformanceBlock block in blocks)
            {
                sb.Append(" xmlns:").Append(block.Prefix).Append("=\"").Append(block.NamespaceUri).Append('"');
            }
            sb.Append(">\n");
            foreach (XmpConformanceBlock block in blocks)
            {
                foreach (KeyValuePair<string, string> element in block.Elements)
                {
                    sb.Append("      <").Append(block.Prefix).Append(':').Append(element.Key).Append('>');
                    AppendEscapedXmlText(sb, element.Value);
                    sb.Append("</").Append(block.Prefix).Append(':').Append(element.Key).Append(">\n");
                }
            }
            sb.Append("    </rdf:Description>\n");
        }

        // 2. Dublin Core - title, creator (author), subject (keywords), description (subject).
        bool hasTitle = !string.IsNullOrEmpty(metadata?.Title);
        bool hasAuthor = !string.IsNullOrEmpty(metadata?.Author);
        bool hasKeywords = !string.IsNullOrEmpty(metadata?.Keywords);
        bool hasSubject = !string.IsNullOrEmpty(metadata?.Subject);

        if (hasTitle || hasAuthor || hasKeywords || hasSubject)
        {
            wroteDescription = true;
            sb.Append("    <rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n");
            if (hasTitle)
            {
                sb.Append("      <dc:title>\n");
                sb.Append("        <rdf:Alt>\n");
                sb.Append("          <rdf:li xml:lang=\"x-default\">");
                AppendEscapedXmlText(sb, metadata!.Title!);
                sb.Append("</rdf:li>\n");
                sb.Append("        </rdf:Alt>\n");
                sb.Append("      </dc:title>\n");
            }
            if (hasAuthor)
            {
                sb.Append("      <dc:creator>\n");
                sb.Append("        <rdf:Seq>\n");
                sb.Append("          <rdf:li>");
                AppendEscapedXmlText(sb, metadata!.Author!);
                sb.Append("</rdf:li>\n");
                sb.Append("        </rdf:Seq>\n");
                sb.Append("      </dc:creator>\n");
            }
            if (hasKeywords)
            {
                string[] tokens = metadata!.Keywords!.Split(
                    KeywordSeparators,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                sb.Append("      <dc:subject>\n");
                sb.Append("        <rdf:Bag>\n");
                foreach (string keyword in tokens)
                {
                    sb.Append("          <rdf:li>");
                    AppendEscapedXmlText(sb, keyword);
                    sb.Append("</rdf:li>\n");
                }
                sb.Append("        </rdf:Bag>\n");
                sb.Append("      </dc:subject>\n");
            }
            if (hasSubject)
            {
                sb.Append("      <dc:description>\n");
                sb.Append("        <rdf:Alt>\n");
                sb.Append("          <rdf:li xml:lang=\"x-default\">");
                AppendEscapedXmlText(sb, metadata!.Subject!);
                sb.Append("</rdf:li>\n");
                sb.Append("        </rdf:Alt>\n");
                sb.Append("      </dc:description>\n");
            }
            sb.Append("    </rdf:Description>\n");
        }

        // 3. PDF schema - Keywords, Producer.
        bool hasProducer = !string.IsNullOrEmpty(metadata?.Producer);
        if (hasKeywords || hasProducer)
        {
            wroteDescription = true;
            sb.Append("    <rdf:Description rdf:about=\"\" xmlns:pdf=\"http://ns.adobe.com/pdf/1.3/\">\n");
            if (hasKeywords)
            {
                sb.Append("      <pdf:Keywords>");
                AppendEscapedXmlText(sb, metadata!.Keywords!);
                sb.Append("</pdf:Keywords>\n");
            }
            if (hasProducer)
            {
                sb.Append("      <pdf:Producer>");
                AppendEscapedXmlText(sb, metadata!.Producer!);
                sb.Append("</pdf:Producer>\n");
            }
            sb.Append("    </rdf:Description>\n");
        }

        // 4. XMP basic schema - CreatorTool, CreateDate, ModifyDate.
        bool hasCreator = !string.IsNullOrEmpty(metadata?.Creator);
        bool hasCreationDate = metadata?.CreationDate.HasValue == true;
        bool hasModifiedDate = metadata?.ModifiedDate.HasValue == true;
        if (hasCreator || hasCreationDate || hasModifiedDate)
        {
            wroteDescription = true;
            sb.Append("    <rdf:Description rdf:about=\"\" xmlns:xmp=\"http://ns.adobe.com/xap/1.0/\">\n");
            if (hasCreator)
            {
                sb.Append("      <xmp:CreatorTool>");
                AppendEscapedXmlText(sb, metadata!.Creator!);
                sb.Append("</xmp:CreatorTool>\n");
            }
            if (hasCreationDate)
            {
                sb.Append("      <xmp:CreateDate>");
                sb.Append(FormatIso8601(metadata!.CreationDate!.Value));
                sb.Append("</xmp:CreateDate>\n");
            }
            if (hasModifiedDate)
            {
                sb.Append("      <xmp:ModifyDate>");
                sb.Append(FormatIso8601(metadata!.ModifiedDate!.Value));
                sb.Append("</xmp:ModifyDate>\n");
            }
            sb.Append("    </rdf:Description>\n");
        }

        // 5. PDF/A extension schema declarations (ISO 19005-2/3 §6.6.2.3).
        bool hasPdfA = false;
        List<XmpExtensionSchema>? extensionSchemas = null;
        foreach (XmpConformanceBlock block in blocks)
        {
            if (string.Equals(block.Prefix, "pdfaid", StringComparison.Ordinal)) hasPdfA = true;
            if (block.ExtensionSchema != null)
            {
                extensionSchemas ??= [];
                extensionSchemas.Add(block.ExtensionSchema);
            }
        }
        if (hasPdfA && extensionSchemas is { Count: > 0 })
        {
            wroteDescription = true;
            sb.Append("    <rdf:Description rdf:about=\"\"\n");
            sb.Append("        xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n");
            sb.Append("        xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n");
            sb.Append("        xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n");
            sb.Append("      <pdfaExtension:schemas>\n");
            sb.Append("        <rdf:Bag>\n");
            foreach (XmpExtensionSchema schema in extensionSchemas)
            {
                sb.Append("          <rdf:li rdf:parseType=\"Resource\">\n");
                sb.Append("            <pdfaSchema:namespaceURI>");
                AppendEscapedXmlText(sb, schema.NamespaceUri);
                sb.Append("</pdfaSchema:namespaceURI>\n");
                sb.Append("            <pdfaSchema:prefix>");
                AppendEscapedXmlText(sb, schema.Prefix);
                sb.Append("</pdfaSchema:prefix>\n");
                sb.Append("            <pdfaSchema:schema>");
                AppendEscapedXmlText(sb, schema.SchemaName);
                sb.Append("</pdfaSchema:schema>\n");
                sb.Append("            <pdfaSchema:property>\n");
                sb.Append("              <rdf:Seq>\n");
                foreach (XmpExtensionSchemaProperty prop in schema.Properties)
                {
                    sb.Append("                <rdf:li rdf:parseType=\"Resource\">\n");
                    sb.Append("                  <pdfaProperty:name>");
                    AppendEscapedXmlText(sb, prop.Name);
                    sb.Append("</pdfaProperty:name>\n");
                    sb.Append("                  <pdfaProperty:valueType>");
                    AppendEscapedXmlText(sb, prop.ValueType);
                    sb.Append("</pdfaProperty:valueType>\n");
                    sb.Append("                  <pdfaProperty:category>");
                    AppendEscapedXmlText(sb, prop.Category);
                    sb.Append("</pdfaProperty:category>\n");
                    sb.Append("                  <pdfaProperty:description>");
                    AppendEscapedXmlText(sb, prop.Description);
                    sb.Append("</pdfaProperty:description>\n");
                    sb.Append("                </rdf:li>\n");
                }
                sb.Append("              </rdf:Seq>\n");
                sb.Append("            </pdfaSchema:property>\n");
                sb.Append("          </rdf:li>\n");
            }
            sb.Append("        </rdf:Bag>\n");
            sb.Append("      </pdfaExtension:schemas>\n");
            sb.Append("    </rdf:Description>\n");
        }

        if (!wroteDescription)
        {
            sb.Append("    <rdf:Description rdf:about=\"\"/>\n");
        }

        sb.Append("  </rdf:RDF>\n");
        sb.Append("</x:xmpmeta>\n");
        sb.Append("<?xpacket end=\"w\"?>");
        return sb.ToString();
    }

    private static void AppendEscapedXmlText(StringBuilder sb, string text)
    {
        foreach (char c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default: sb.Append(c); break;
            }
        }
    }

    /// <summary>Formats a DateTimeOffset as a UTC ISO 8601 string (e.g. 2026-05-24T07:23:11Z).</summary>
    private static string FormatIso8601(DateTimeOffset date)
    {
        return date.ToUniversalTime().ToString(Iso8601UtcFormat, CultureInfo.InvariantCulture);
    }
}
