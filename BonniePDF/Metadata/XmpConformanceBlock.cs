using System.Collections.Generic;

namespace BonniePDF.Metadata;

/// <summary>
/// A conformance-namespace block emitted inside the document-level
/// <c>rdf:Description</c> in the XMP packet. One block per claim
/// (PDF/A, PDF/UA, ZUGFeRD/Factur-X, …). Multiple blocks compose:
/// the writer emits them all into the same RDF description, each
/// with its own XML namespace prefix.
///
/// Per the this feature design (<c>the specification</c>),
/// XMP injection must be additive - every conformance feature
/// (current PDF/A, PDF/UA from this feature, ZUGFeRD's <c>fx</c> later)
/// registers through this same hook so no one feature has to rewrite
/// the packet.
/// </summary>
internal sealed class XmpConformanceBlock
{
    /// <summary>XML namespace prefix (e.g. <c>"pdfuaid"</c>, <c>"pdfaid"</c>, <c>"fx"</c>).</summary>
    public string Prefix { get; }

    /// <summary>XML namespace URI for the block's elements.</summary>
    public string NamespaceUri { get; }

    /// <summary>Element name + value pairs written inside the namespace.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Elements { get; }

    /// <summary>
    /// Optional PDF/A extension-schema descriptor. Set when this block's
    /// namespace is NOT one of the schemas predefined by ISO 19005 - in
    /// which case PDF/A-2/3 §6.6.2.3.1 requires the namespace to be
    /// declared in a <c>pdfaExtension:schemas</c> bag. <c>null</c> when
    /// the namespace is already predefined (e.g. <c>pdfaid</c>) and no
    /// extension declaration is needed.
    /// </summary>
    public XmpExtensionSchema ExtensionSchema { get; }

    /// <summary>Creates a new conformance block.</summary>
    public XmpConformanceBlock(
        string prefix,
        string namespaceUri,
        IReadOnlyList<KeyValuePair<string, string>> elements,
        XmpExtensionSchema extensionSchema = null)
    {
        Prefix = prefix;
        NamespaceUri = namespaceUri;
        Elements = elements;
        ExtensionSchema = extensionSchema;
    }

    private const string PdfAidNamespace = "http://www.aiim.org/pdfa/ns/id/";

    private static XmpConformanceBlock PdfAid(string part, string conformance = null, string rev = null)
    {
        var elements = new List<KeyValuePair<string, string>>
        {
            new("part", part),
        };
        if (conformance != null) elements.Add(new("conformance", conformance));
        if (rev != null) elements.Add(new("rev", rev));
        return new XmpConformanceBlock(
            prefix: "pdfaid",
            namespaceUri: PdfAidNamespace,
            elements: elements);
    }

    /// <summary>PDF/A-1b: ISO 19005-1, part 1, conformance B.</summary>
    public static XmpConformanceBlock PdfA1B() => PdfAid("1", "B");

    /// <summary>PDF/A-1a: ISO 19005-1, part 1, conformance A (tagged).</summary>
    public static XmpConformanceBlock PdfA1A() => PdfAid("1", "A");

    /// <summary>PDF/A-2b: ISO 19005-2, part 2, conformance B.</summary>
    public static XmpConformanceBlock PdfA2B() => PdfAid("2", "B");

    /// <summary>PDF/A-2a: ISO 19005-2, part 2, conformance A (tagged).</summary>
    public static XmpConformanceBlock PdfA2A() => PdfAid("2", "A");

    /// <summary>PDF/A-3b: ISO 19005-3, part 3, conformance B (allows embedded files).</summary>
    public static XmpConformanceBlock PdfA3B() => PdfAid("3", "B");

    /// <summary>PDF/A-3a: ISO 19005-3, part 3, conformance A (tagged; allows embedded files).</summary>
    public static XmpConformanceBlock PdfA3A() => PdfAid("3", "A");

    /// <summary>PDF/A-4: ISO 19005-4:2020 baseline. No legacy A/B/U conformance suffix.</summary>
    public static XmpConformanceBlock PdfA4() => PdfAid("4", rev: "2020");

    /// <summary>
    /// Returns the conformance block matching <paramref name="conformance"/>,
    /// or <c>null</c> if no PDF/A claim should be emitted.
    /// </summary>
    public static XmpConformanceBlock ForPdfA(Configuration.PdfAConformance conformance) => conformance switch
    {
        Configuration.PdfAConformance.PdfA1b => PdfA1B(),
        Configuration.PdfAConformance.PdfA1a => PdfA1A(),
        Configuration.PdfAConformance.PdfA2b => PdfA2B(),
        Configuration.PdfAConformance.PdfA2a => PdfA2A(),
        Configuration.PdfAConformance.PdfA3b => PdfA3B(),
        Configuration.PdfAConformance.PdfA3a => PdfA3A(),
        Configuration.PdfAConformance.PdfA4 => PdfA4(),
        _ => null,
    };

    /// <summary>
    /// PDF/UA-1 conformance claim (ISO 14289-1). One <c>pdfuaid:part</c>
    /// element with value <c>"1"</c>. Required for VeraPDF profile
    /// <c>ua1</c> metadata rules (6-2-1, 6-2-2).
    ///
    /// <para>The <c>pdfuaid</c> namespace is not predefined by ISO 19005,
    /// so the block carries an <see cref="XmpExtensionSchema"/> descriptor
    /// that the writer emits as a <c>pdfaExtension:schemas</c> entry
    /// whenever this block coexists with a PDF/A claim.</para>
    /// </summary>
    public static XmpConformanceBlock PdfUa1() => new(
        prefix: "pdfuaid",
        namespaceUri: "http://www.aiim.org/pdfua/ns/id/",
        elements: new[]
        {
            new KeyValuePair<string, string>("part", "1"),
        },
        extensionSchema: new XmpExtensionSchema(
            namespaceUri: "http://www.aiim.org/pdfua/ns/id/",
            prefix: "pdfuaid",
            schemaName: "PDF/UA identification schema",
            properties: new[]
            {
                new XmpExtensionSchemaProperty(
                    name: "part",
                    valueType: "Integer",
                    category: "internal",
                    description: "Indicates, which part of ISO 14289 standard is followed"),
            }));

    /// <summary>
    /// ZUGFeRD / Factur-X 2.x conformance claim. Emits the four
    /// <c>fx:*</c> properties (DocumentType, DocumentFileName, Version,
    /// ConformanceLevel) the Factur-X technical specification requires in the
    /// XMP, plus the <c>pdfaExtension:schemas</c> descriptor for the
    /// non-predefined <c>fx</c> namespace (ISO 19005-3 §6.6.2.3).
    ///
    /// <para>Factur-X is only meaningful on a PDF/A-3 document; the caller
    /// (<see cref="EInvoice.FacturX"/>) enforces that, and the XMP writer only
    /// includes this block when a PDF/A claim survives the honest-claim
    /// guard.</para>
    /// </summary>
    public static XmpConformanceBlock FacturX(EInvoice.FacturXMetadata meta) => new(
        prefix: EInvoice.FacturXMetadata.Prefix,
        namespaceUri: EInvoice.FacturXMetadata.NamespaceUri,
        elements: new[]
        {
            new KeyValuePair<string, string>("DocumentType", meta.DocumentType),
            new KeyValuePair<string, string>("DocumentFileName", meta.DocumentFileName),
            new KeyValuePair<string, string>("Version", meta.Version),
            new KeyValuePair<string, string>("ConformanceLevel", meta.ConformanceLevel),
        },
        extensionSchema: new XmpExtensionSchema(
            namespaceUri: EInvoice.FacturXMetadata.NamespaceUri,
            prefix: EInvoice.FacturXMetadata.Prefix,
            schemaName: "Factur-X PDFA Extension Schema",
            properties: new[]
            {
                new XmpExtensionSchemaProperty("DocumentType", "Text", "external", "INVOICE"),
                new XmpExtensionSchemaProperty("DocumentFileName", "Text", "external", "Factur-X, ZUGFeRD"),
                new XmpExtensionSchemaProperty("Version", "Text", "external", "The actual version of the Factur-X XML schema"),
                new XmpExtensionSchemaProperty("ConformanceLevel", "Text", "external", "The conformance level of the embedded Factur-X data"),
            }));
}
