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
/// XMP injection must be additive — every conformance feature
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

    /// <summary>Creates a new conformance block.</summary>
    public XmpConformanceBlock(
        string prefix,
        string namespaceUri,
        IReadOnlyList<KeyValuePair<string, string>> elements)
    {
        Prefix = prefix;
        NamespaceUri = namespaceUri;
        Elements = elements;
    }

    /// <summary>
    /// PDF/A-1B conformance claim. The historic behaviour of BonniePDF's
    /// PdfAEnabled flag — claimed as "part 1, conformance B". See
    /// <c>the specification</c> for the open
    /// question of which PDF/A level the writer actually produces.
    /// </summary>
    public static XmpConformanceBlock PdfA1B() => new(
        prefix: "pdfaid",
        namespaceUri: "http://www.aiim.org/pdfa/ns/id/",
        elements: new[]
        {
            new KeyValuePair<string, string>("part", "1"),
            new KeyValuePair<string, string>("conformance", "B"),
        });

    /// <summary>
    /// PDF/UA-1 conformance claim (ISO 14289-1). One <c>pdfuaid:part</c>
    /// element with value <c>"1"</c>. Required for VeraPDF profile
    /// <c>ua1</c> metadata rules (6-2-1, 6-2-2).
    /// </summary>
    public static XmpConformanceBlock PdfUa1() => new(
        prefix: "pdfuaid",
        namespaceUri: "http://www.aiim.org/pdfua/ns/id/",
        elements: new[]
        {
            new KeyValuePair<string, string>("part", "1"),
        });
}
