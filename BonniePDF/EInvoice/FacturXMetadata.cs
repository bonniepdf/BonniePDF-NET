namespace BonniePDF.EInvoice;

/// <summary>
/// Resolved Factur-X / ZUGFeRD XMP metadata written into the document-level
/// XMP packet under the <c>fx</c> namespace
/// (<c>urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#</c>). Populated by
/// <see cref="FacturX"/> from a <see cref="FacturXProfile"/>; carried on
/// <c>DocumentSettings.FacturX</c> and consumed by the XMP writer.
///
/// <para>These are the four properties the Factur-X / ZUGFeRD 2.x technical
/// specification requires in the XMP: the values here are the already-resolved
/// strings, not the domain profile enum — the profile→level mapping lives in
/// <see cref="FacturXProfile"/>.</para>
/// </summary>
public sealed class FacturXMetadata
{
    /// <summary>The XMP namespace URI for the Factur-X / ZUGFeRD 2.x block.</summary>
    public const string NamespaceUri = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";

    /// <summary>The XML prefix used for the namespace.</summary>
    public const string Prefix = "fx";

    /// <summary>
    /// <c>fx:DocumentType</c> — the document kind, normally <c>"INVOICE"</c>
    /// (also <c>"ORDER"</c> for Order-X). Default <c>"INVOICE"</c>.
    /// </summary>
    public string DocumentType { get; set; } = "INVOICE";

    /// <summary>
    /// <c>fx:DocumentFileName</c> — the file name of the embedded XML, which
    /// MUST match the attachment's file name. Default <c>"factur-x.xml"</c>.
    /// </summary>
    public string DocumentFileName { get; set; } = "factur-x.xml";

    /// <summary>
    /// <c>fx:Version</c> — the Factur-X specification version. Default
    /// <c>"1.0"</c>.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// <c>fx:ConformanceLevel</c> — the profile name (e.g. <c>"EN 16931"</c>,
    /// <c>"BASIC"</c>, <c>"MINIMUM"</c>, <c>"EXTENDED"</c>). Derived from the
    /// <see cref="FacturXProfile"/>.
    /// </summary>
    public string ConformanceLevel { get; set; } = "EN 16931";
}
