namespace BonniePDF;

/// <summary>
/// Raised by <see cref="FacturX.Apply"/> when the invoice XML cannot back the Factur-X claim: it is
/// not well-formed UTF-8 CII, or the profile it declares is missing, unknown, or not the one stated.
/// The document is left unchanged.
///
/// <para>This is not invoice validation. Passing the check says nothing about the schema, totals or
/// VAT rules; validate the XML with a dedicated e-invoicing validator as well.</para>
/// </summary>
public sealed class FacturXException : BonniePdfException
{
    /// <summary>
    /// Which check failed:
    /// <list type="bullet">
    /// <item><c>unsupported-encoding</c>: a UTF-16 byte order mark, or a declared encoding other than UTF-8.</item>
    /// <item><c>malformed-xml</c>: invalid UTF-8, or the XML is not well-formed.</item>
    /// <item><c>doctype-forbidden</c>: the XML contains a document type declaration.</item>
    /// <item><c>not-cii</c>: the root is not a UN/CEFACT CII <c>CrossIndustryInvoice</c> (Factur-X carries CII only).</item>
    /// <item><c>guideline-missing</c>: the XML has no profile identifier (BT-24).</item>
    /// <item><c>guideline-unrecognized</c>: the profile identifier is not a Factur-X / ZUGFeRD 2.1+ profile.</item>
    /// <item><c>profile-mismatch</c>: the stated profile differs from the one the XML declares.</item>
    /// </list>
    /// </summary>
    public string Code { get; }

    /// <summary>Creates a new <see cref="FacturXException"/>.</summary>
    public FacturXException(string code, string message) : base($"{message} ({code})")
    {
        Code = code;
    }
}
