using System;

namespace BonniePDF.EInvoice;

/// <summary>
/// Factur-X / ZUGFeRD 2.x semantic profile. Determines the
/// <c>fx:ConformanceLevel</c> written to the XMP and constrains what the
/// embedded invoice XML is expected to contain. BonniePDF does not generate
/// or validate the XML against the profile schema — the profile is metadata
/// about caller-supplied XML (see <see cref="FacturX"/>).
/// </summary>
public enum FacturXProfile
{
    /// <summary>MINIMUM — header + totals only. Not a valid VAT invoice on its own.</summary>
    Minimum,

    /// <summary>BASIC WL (without lines) — header + totals, no line items.</summary>
    BasicWl,

    /// <summary>BASIC — line-level detail, subset of EN 16931.</summary>
    Basic,

    /// <summary>EN 16931 (COMFORT) — the full European semantic standard. The EU baseline.</summary>
    En16931,

    /// <summary>EXTENDED — EN 16931 plus additional business terms.</summary>
    Extended,

    /// <summary>XRECHNUNG — the German CIUS of EN 16931.</summary>
    XRechnung,
}

/// <summary>Maps <see cref="FacturXProfile"/> to its <c>fx:ConformanceLevel</c> string.</summary>
public static class FacturXProfileExtensions
{
    /// <summary>
    /// The <c>fx:ConformanceLevel</c> string for this profile, spelled exactly
    /// as the Factur-X / ZUGFeRD specification requires (note the spaces in
    /// <c>"EN 16931"</c> and <c>"BASIC WL"</c>).
    /// </summary>
    public static string ToConformanceLevel(this FacturXProfile profile) => profile switch
    {
        FacturXProfile.Minimum => "MINIMUM",
        FacturXProfile.BasicWl => "BASIC WL",
        FacturXProfile.Basic => "BASIC",
        FacturXProfile.En16931 => "EN 16931",
        FacturXProfile.Extended => "EXTENDED",
        FacturXProfile.XRechnung => "XRECHNUNG",
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, null),
    };
}
