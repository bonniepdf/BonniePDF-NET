using System;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>The claims one generation makes, after the honest-claim guard.</summary>
internal sealed record ConformancePlan(PdfAConformance Conformance, bool PdfUa, bool NeedsOutputIntent)
{
    public bool PdfA => Conformance != PdfAConformance.None;

    /// <summary>
    /// The honest-claim guard. PDF/A and PDF/UA-1 both require every
    /// font to be embedded, so when a standard-14 reference could not be, both
    /// claims are withheld for this generation and each is reported as a
    /// <see cref="DiagnosticCategory.Compliance"/> warning. Tagging is kept. The
    /// document's settings are not modified, so a later generation with the fonts
    /// available carries the full claims.
    /// </summary>
    internal static ConformancePlan Evaluate(DocumentSettings settings, PdfResourceRegistry resources, DiagnosticsCollector diagnostics)
    {
        IReadOnlyList<string> unembedded = resources.GetNonEmbeddedStandardFonts();
        bool fontsUnembedded = unembedded.Count > 0;
        bool pdfUaRequested = settings.Accessibility == AccessibilityMode.PdfUa1;
        bool pdfA = settings.PdfAEnabled && !fontsUnembedded;
        bool pdfUa = pdfUaRequested && !fontsUnembedded;

        string fonts = fontsUnembedded ? string.Join(", ", unembedded) : "standard-14 fonts";
        const string remedy =
            "Register the Liberation fonts (or use embeddable fonts) and generate again to keep the claim. " +
            "The rest of the document is unchanged.";
        if (settings.PdfAEnabled && !pdfA)
        {
            string token = PdfAToken(settings.Conformance);
            diagnostics.AddWarning(DiagnosticCategory.Compliance,
                $"The {token} claim was withheld: these fonts could not be embedded: {fonts}. {remedy}", token);
        }
        if (pdfUaRequested && !pdfUa)
        {
            diagnostics.AddWarning(DiagnosticCategory.Compliance,
                $"The pdf/ua-1 claim was withheld: these fonts could not be embedded: {fonts}. " +
                $"The document is still tagged. {remedy}", "pdf/ua-1");
        }

        // Under PDF/A the OutputIntent goes with the claim; a PDF/UA-only
        // document always carries one, for colour management rather than as a claim.
        bool needsOutputIntent = pdfA || (pdfUaRequested && !settings.PdfAEnabled);
        return new ConformancePlan(pdfA ? settings.Conformance : PdfAConformance.None, pdfUa, needsOutputIntent);
    }

    /// <summary><c>PdfA2a</c> → <c>pdf/a-2a</c>, <c>PdfA4</c> → <c>pdf/a-4</c>: the claim's spec token.</summary>
    internal static string PdfAToken(PdfAConformance conformance)
    {
        string name = conformance.ToString();
        return name.StartsWith("PdfA", StringComparison.Ordinal)
            ? "pdf/a-" + name.Substring(4).ToLowerInvariant()
            : "none";
    }
}
