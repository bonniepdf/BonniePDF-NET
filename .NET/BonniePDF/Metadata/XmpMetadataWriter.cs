using System;
using System.IO;
using System.Xml;
using BonniePDF.Configuration;

namespace BonniePDF.Metadata;

/// <summary>
/// Generates XMP metadata XML for PDF documents.
/// XMP (Extensible Metadata Platform) provides standardized metadata
/// that is required for PDF/A compliance and optional for PDF 1.7.
/// </summary>
internal static class XmpMetadataWriter
{
    private const string NsRdf    = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private const string NsDc     = "http://purl.org/dc/elements/1.1/";
    private const string NsXmp    = "http://ns.adobe.com/xap/1.0/";
    private const string NsPdf    = "http://ns.adobe.com/pdf/1.3/";
    private const string NsPdfaid = "http://www.aiim.org/pdfa/ns/id/";
    private const string NsXml    = "http://www.w3.org/XML/1998/namespace";

    /// <summary>
    /// Generates the XMP XML string from document metadata.
    /// </summary>
    public static string GenerateXmpXml(DocumentMetadata metadata, bool pdfA = false)
    {
        var settings = new XmlWriterSettings
        {
            Indent             = true,
            IndentChars        = "  ",
            NewLineChars       = "\n",
            OmitXmlDeclaration = true
        };

        using var sw = new StringWriter();
        using (var xw = XmlWriter.Create(sw, settings))
        {
            xw.WriteProcessingInstruction("xpacket", "begin=\"\xEF\xBB\xBF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"");

            xw.WriteStartElement("x",   "xmpmeta",     "adobe:ns:meta/");
            xw.WriteStartElement("rdf", "RDF",         NsRdf);
            xw.WriteStartElement("rdf", "Description", NsRdf);
            xw.WriteAttributeString("rdf", "about", NsRdf, "");

            WriteMetadataElements(xw, metadata, pdfA);

            xw.WriteEndElement(); // rdf:Description
            xw.WriteEndElement(); // rdf:RDF
            xw.WriteEndElement(); // x:xmpmeta

            xw.WriteProcessingInstruction("xpacket", "end=\"w\"");
        }

        return sw.ToString();
    }

    private static void WriteMetadataElements(XmlWriter xw, DocumentMetadata metadata, bool pdfA)
    {
        if (!string.IsNullOrEmpty(metadata.Title))
        {
            xw.WriteStartElement("dc", "title", NsDc);
            xw.WriteStartElement("rdf", "Alt", NsRdf);
            xw.WriteStartElement("rdf", "li", NsRdf);
            xw.WriteAttributeString("xml", "lang", NsXml, "x-default");
            xw.WriteString(metadata.Title);
            xw.WriteEndElement(); // rdf:li
            xw.WriteEndElement(); // rdf:Alt
            xw.WriteEndElement(); // dc:title
        }

        if (!string.IsNullOrEmpty(metadata.Author))
        {
            xw.WriteStartElement("dc", "creator", NsDc);
            xw.WriteStartElement("rdf", "Seq", NsRdf);
            xw.WriteElementString("rdf", "li", NsRdf, metadata.Author);
            xw.WriteEndElement(); // rdf:Seq
            xw.WriteEndElement(); // dc:creator
        }

        if (!string.IsNullOrEmpty(metadata.Keywords))
        {
            xw.WriteStartElement("dc", "subject", NsDc);
            xw.WriteStartElement("rdf", "Bag", NsRdf);
            foreach (string keyword in metadata.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                xw.WriteElementString("rdf", "li", NsRdf, keyword);
            xw.WriteEndElement(); // rdf:Bag
            xw.WriteEndElement(); // dc:subject

            xw.WriteElementString("pdf", "Keywords", NsPdf, metadata.Keywords);
        }

        if (!string.IsNullOrEmpty(metadata.Subject))
        {
            xw.WriteStartElement("dc", "description", NsDc);
            xw.WriteStartElement("rdf", "Alt", NsRdf);
            xw.WriteStartElement("rdf", "li", NsRdf);
            xw.WriteAttributeString("xml", "lang", NsXml, "x-default");
            xw.WriteString(metadata.Subject);
            xw.WriteEndElement(); // rdf:li
            xw.WriteEndElement(); // rdf:Alt
            xw.WriteEndElement(); // dc:description
        }

        if (!string.IsNullOrEmpty(metadata.Creator))
            xw.WriteElementString("xmp", "CreatorTool", NsXmp, metadata.Creator);

        if (metadata.CreationDate.HasValue)
            xw.WriteElementString("xmp", "CreateDate", NsXmp, FormatIso8601(metadata.CreationDate.Value));

        if (metadata.ModifiedDate.HasValue)
            xw.WriteElementString("xmp", "ModifyDate", NsXmp, FormatIso8601(metadata.ModifiedDate.Value));

        if (!string.IsNullOrEmpty(metadata.Producer))
            xw.WriteElementString("pdf", "Producer", NsPdf, metadata.Producer);

        if (pdfA)
        {
            xw.WriteElementString("pdfaid", "part",        NsPdfaid, "1");
            xw.WriteElementString("pdfaid", "conformance", NsPdfaid, "B");
        }
    }

    /// <summary>
    /// Formats a DateTimeOffset as ISO 8601 string.
    /// </summary>
    private static string FormatIso8601(DateTimeOffset date)
    {
        return date.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }
}
