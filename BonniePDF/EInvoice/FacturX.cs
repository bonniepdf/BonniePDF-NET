using System;

namespace BonniePDF;

/// <summary>
/// Binds caller-supplied invoice XML to a generated document to produce a
/// ZUGFeRD / Factur-X e-invoice. It is a thin, dependency-free binding over
/// capabilities the engine already has: it forces PDF/A-3, embeds the XML as
/// an associated file with the <c>Data</c> relationship, and registers the
/// <c>fx:</c> XMP metadata block.
///
/// <para>Before changing the document it checks only what the Factur-X claim
/// depends on: the XML is well-formed UTF-8 CII (no DOCTYPE) and declares a
/// recognised profile identifier (BT-24) that matches the stated profile. Any
/// failure throws a <see cref="FacturXException"/> and leaves the document
/// unchanged.</para>
///
/// <para><b>BonniePDF does not generate or validate the invoice XML.</b> The
/// XML is a caller input — produced by an ERP or a dedicated e-invoicing
/// toolchain (Mustangproject, the Factur-X reference libraries, …). The check
/// above does not read the CII schema, totals or VAT rules. Validate generated
/// files against veraPDF (PDF/A-3) <i>and</i> an independent ZUGFeRD validator
/// before claiming conformance.</para>
/// </summary>
public static class FacturX
{
    /// <summary>The Factur-X-mandated file name for the embedded invoice XML.</summary>
    public const string DefaultFileName = "factur-x.xml";

    /// <summary>
    /// Applies Factur-X to <paramref name="document"/> in place and returns it
    /// for chaining. Forces PDF/A-3 (keeping a PDF/A-3a choice if already set),
    /// attaches <paramref name="invoiceXml"/>, and sets the <c>fx:</c> XMP.
    /// </summary>
    /// <param name="document">The document to turn into an e-invoice.</param>
    /// <param name="invoiceXml">The caller-supplied invoice XML bytes (CII / UN-CEFACT).</param>
    /// <param name="profile">
    /// The semantic profile the XML conforms to. When <c>null</c>, it is read from the XML's profile
    /// identifier (BT-24). When given, it must match that identifier, or a
    /// <see cref="FacturXException"/> with code <c>profile-mismatch</c> is thrown.
    /// </param>
    /// <exception cref="FacturXException">The XML is not well-formed UTF-8 CII, or its profile identifier is missing, unrecognised or differs from <paramref name="profile"/>.</exception>
    /// <param name="fileName">Embedded file name. Default <c>factur-x.xml</c>; must match the XMP.</param>
    /// <param name="documentType">The <c>fx:DocumentType</c> value. Default <c>"INVOICE"</c>.</param>
    public static Document Apply(
        Document document,
        byte[] invoiceXml,
        FacturXProfile? profile = null,
        string fileName = DefaultFileName,
        string documentType = "INVOICE")
    {
        ArgumentNullException.ThrowIfNull(document);
        if (invoiceXml is not { Length: > 0 })
            throw new ArgumentException("Invoice XML cannot be null or empty.", nameof(invoiceXml));
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        var declared = FacturXXmlCheck.Check(invoiceXml);
        if (profile is { } stated && stated != declared.Profile)
        {
            throw new FacturXException(
                "profile-mismatch",
                $"Factur-X profile {stated.ToConformanceLevel()} was requested, but the invoice XML declares " +
                $"{declared.Profile.ToConformanceLevel()} (\"{declared.Guideline}\")");
        }

        // Factur-X is defined on PDF/A-3. Force the "b" level unless the caller
        // already selected a PDF/A-3 level (so an explicit tagged 3a choice
        // survives).
        if (document.Settings.Conformance is not (PdfAConformance.PdfA3b or PdfAConformance.PdfA3a))
            document.Settings.Conformance = PdfAConformance.PdfA3b;

        document.Attach(new FileAttachment
        {
            FileName = fileName,
            Content = invoiceXml,
            Relationship = AttachmentRelationship.Data,
            MediaType = "text/xml",
            Description = "Factur-X invoice",
        });

        document.Settings.FacturX = new FacturXMetadata
        {
            DocumentType = documentType,
            DocumentFileName = fileName,
            ConformanceLevel = declared.Profile.ToConformanceLevel(),
        };

        return document;
    }
}
