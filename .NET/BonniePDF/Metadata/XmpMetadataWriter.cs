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
    private static class XmpNamespaces
    {
        public const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public const string Dc = "http://purl.org/dc/elements/1.1/";
        public const string Xmp = "http://ns.adobe.com/xap/1.0/";
        public const string Pdf = "http://ns.adobe.com/pdf/1.3/";
        public const string Pdfaid = "http://www.aiim.org/pdfa/ns/id/";
        public const string Xml = "http://www.w3.org/XML/1998/namespace";
        public const string AdobeMeta = "adobe:ns:meta/";
    }

    private static class XmlPrefixes
    {
        public const string Rdf = "rdf";
        public const string Dc = "dc";
        public const string Xmp = "xmp";
        public const string Pdf = "pdf";
        public const string Pdfaid = "pdfaid";
        public const string Xml = "xml";
        public const string AdobeX = "x";
    }

    private static class XmlElements
    {
        // RDF
        public const string RdfRoot = "RDF";
        public const string Description = "Description";
        public const string Alt = "Alt";
        public const string Seq = "Seq";
        public const string Bag = "Bag";
        public const string ListItem = "li";

        // Dublin Core (DC)
        public const string DcTitle = "title";
        public const string DcCreator = "creator";
        public const string DcSubject = "subject";
        public const string DcDescription = "description";

        // XMP / PDF
        public const string XmpMeta = "xmpmeta";
        public const string Keywords = "Keywords";
        public const string CreatorTool = "CreatorTool";
        public const string CreateDate = "CreateDate";
        public const string ModifyDate = "ModifyDate";
        public const string Producer = "Producer";

        // PDF/A
        public const string PdfaidPart = "part";
        public const string PdfaidConformance = "conformance";
    }

    private static class XmpPacket
    {
        public const string InstructionName = "xpacket";
        // The id is a standard GUID required by the XMP specification
        public const string BeginAttribute = "begin=\"\xEF\xBB\xBF\" id=\"W5M0MpCehiHzreSzNTczkc9d\"";
        public const string EndAttribute = "end=\"w\"";
    }

    private const string Iso8601Format = "yyyy-MM-ddTHH:mm:sszzz";
    private const string DefaultLanguage = "x-default";

    /// <summary>
    /// Generates the XMP XML string from document metadata.
    /// </summary>
    public static string GenerateXmpXml(DocumentMetadata metadata, bool pdfA = false)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n",
            OmitXmlDeclaration = true
        };

        using var sw = new StringWriter();
        using (var xw = XmlWriter.Create(sw, settings))
        {
            xw.WriteProcessingInstruction(XmpPacket.InstructionName, XmpPacket.BeginAttribute);

            xw.WriteStartElement(XmlPrefixes.AdobeX, XmlElements.XmpMeta, XmpNamespaces.AdobeMeta);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.RdfRoot, XmpNamespaces.Rdf);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.Description, XmpNamespaces.Rdf);
            xw.WriteAttributeString(XmlPrefixes.Rdf, "about", XmpNamespaces.Rdf, string.Empty);

            WriteMetadataElements(xw, metadata, pdfA);

            xw.WriteEndElement(); // rdf:Description
            xw.WriteEndElement(); // rdf:RDF
            xw.WriteEndElement(); // x:xmpmeta

            xw.WriteProcessingInstruction(XmpPacket.InstructionName, XmpPacket.EndAttribute);
        }

        return sw.ToString();
    }

    private static void WriteMetadataElements(XmlWriter xw, DocumentMetadata metadata, bool pdfA)
    {
        if (!string.IsNullOrEmpty(metadata.Title))
        {
            xw.WriteStartElement(XmlPrefixes.Dc, XmlElements.DcTitle, XmpNamespaces.Dc);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.Alt, XmpNamespaces.Rdf);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.ListItem, XmpNamespaces.Rdf);
            xw.WriteAttributeString(XmlPrefixes.Xml, "lang", XmpNamespaces.Xml, DefaultLanguage);
            xw.WriteString(metadata.Title);
            xw.WriteEndElement(); // rdf:li
            xw.WriteEndElement(); // rdf:Alt
            xw.WriteEndElement(); // dc:title
        }

        if (!string.IsNullOrEmpty(metadata.Author))
        {
            xw.WriteStartElement(XmlPrefixes.Dc, XmlElements.DcCreator, XmpNamespaces.Dc);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.Seq, XmpNamespaces.Rdf);
            xw.WriteElementString(XmlPrefixes.Rdf, XmlElements.ListItem, XmpNamespaces.Rdf, metadata.Author);
            xw.WriteEndElement(); // rdf:Seq
            xw.WriteEndElement(); // dc:creator
        }

        if (!string.IsNullOrEmpty(metadata.Keywords))
        {
            xw.WriteStartElement(XmlPrefixes.Dc, XmlElements.DcSubject, XmpNamespaces.Dc);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.Bag, XmpNamespaces.Rdf);

            var keywords = metadata.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string keyword in keywords)
            {
                xw.WriteElementString(XmlPrefixes.Rdf, XmlElements.ListItem, XmpNamespaces.Rdf, keyword);
            }

            xw.WriteEndElement(); // rdf:Bag
            xw.WriteEndElement(); // dc:subject

            xw.WriteElementString(XmlPrefixes.Pdf, XmlElements.Keywords, XmpNamespaces.Pdf, metadata.Keywords);
        }

        if (!string.IsNullOrEmpty(metadata.Subject))
        {
            xw.WriteStartElement(XmlPrefixes.Dc, XmlElements.DcDescription, XmpNamespaces.Dc);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.Alt, XmpNamespaces.Rdf);
            xw.WriteStartElement(XmlPrefixes.Rdf, XmlElements.ListItem, XmpNamespaces.Rdf);
            xw.WriteAttributeString(XmlPrefixes.Xml, "lang", XmpNamespaces.Xml, DefaultLanguage);
            xw.WriteString(metadata.Subject);
            xw.WriteEndElement(); // rdf:li
            xw.WriteEndElement(); // rdf:Alt
            xw.WriteEndElement(); // dc:description
        }

        if (!string.IsNullOrEmpty(metadata.Creator))
            xw.WriteElementString(XmlPrefixes.Xmp, XmlElements.CreatorTool, XmpNamespaces.Xmp, metadata.Creator);

        if (metadata.CreationDate.HasValue)
            xw.WriteElementString(XmlPrefixes.Xmp, XmlElements.CreateDate, XmpNamespaces.Xmp, FormatIso8601(metadata.CreationDate.Value));

        if (metadata.ModifiedDate.HasValue)
            xw.WriteElementString(XmlPrefixes.Xmp, XmlElements.ModifyDate, XmpNamespaces.Xmp, FormatIso8601(metadata.ModifiedDate.Value));

        if (!string.IsNullOrEmpty(metadata.Producer))
            xw.WriteElementString(XmlPrefixes.Pdf, XmlElements.Producer, XmpNamespaces.Pdf, metadata.Producer);

        if (pdfA)
        {
            xw.WriteElementString(XmlPrefixes.Pdfaid, XmlElements.PdfaidPart, XmpNamespaces.Pdfaid, "1");
            xw.WriteElementString(XmlPrefixes.Pdfaid, XmlElements.PdfaidConformance, XmpNamespaces.Pdfaid, "B");
        }
    }

    /// <summary>
    /// Formats a DateTimeOffset as ISO 8601 string.
    /// </summary>
    private static string FormatIso8601(DateTimeOffset date)
    {
        return date.ToString(Iso8601Format);
    }
}