using BonniePDF;
using BonniePDF.Examples.Templates.AnnualReportCovers;
using BonniePDF.Examples.Templates.BusinessInvoice;
using BonniePDF.Examples.Templates.FinancialDashboard;
using BonniePDF.Examples.Templates.LabReport;
using BonniePDF.Examples.Templates.PublicLibraryMembershipLetter;
using BonniePDF.Examples.Templates.ShippingLabel;

namespace BonniePDF.Examples.ShowcaseAssets;

/// <summary>
/// Regenerates the showcase assets the root README displays: a first-page SVG
/// preview and the full PDF behind it, for each document in <see cref="Showcase"/>.
///
/// Run with: dotnet run --project Examples/BonniePDF.Examples.ShowcaseAssets
///
/// These assets used to be produced by hand, which meant nothing failed when
/// they went missing. Keep them reproducible: change a showcase example, re-run
/// this, commit both outputs.
///
/// The PDFs are committed binaries and do not delta-compress, so every run adds
/// a full copy to history. Regenerate deliberately, not on every build - and
/// keep an eye on the size column this prints.
///
/// The TypeScript repository carries the equivalent script and the same six
/// documents. Each implementation generates its own assets: a PDF copied across
/// would stop being evidence that this one produces it.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// The README tiles, in display order. The asset name is the README's, not
    /// the example's - they diverge where the README wants a plainer noun
    /// ("pathology-lab-report") than the type does. Adding a tile here is not
    /// enough on its own; the README grid lists them explicitly, so update it too.
    /// </summary>
    private static readonly (string Asset, Func<Document> Generate)[] Showcase =
    [
        ("annual-report", AnnualReportGradientCoverExample.Generate),
        ("financial-dashboard", FinancialDashboardExample.Generate),
        ("business-invoice", BusinessInvoiceExample.Generate),
        ("shipping-label", ShippingLabelExample.Generate),
        ("pathology-lab-report", LabReportExample.Generate),
        ("public-library-membership", PublicLibraryMembershipLetterExample.Generate),
    ];

    private static int Main()
    {
        string repoRoot = FindRepositoryRoot();
        string svgDir = Path.Combine(repoRoot, "docs", "images", "showcase");
        string pdfDir = Path.Combine(repoRoot, "docs", "pdf");
        Directory.CreateDirectory(svgDir);
        Directory.CreateDirectory(pdfDir);

        int failures = 0;

        foreach ((string asset, Func<Document> generate) in Showcase)
        {
            try
            {
                using Document doc = generate();

                // The PDF first: it is the artefact the README links to, and
                // generating it is what proves the example still builds.
                byte[] pdf = doc.GenerateBytes();
                File.WriteAllBytes(Path.Combine(pdfDir, asset + ".pdf"), pdf);

                // Then page 1 as the inline preview. Images embed as data URIs
                // by default, so the SVG is standalone - which is also why an
                // image-heavy cover runs orders of magnitude larger than a
                // vector one.
                SvgDocumentExport export = doc.GenerateSvgDocument(new SvgPageExportOptions { MaxPages = 1 });
                if (export.Pages.Count == 0)
                    throw new InvalidOperationException("exported no pages");

                string svg = InlineFonts(export.Pages[0], export.FontFaceCss);
                File.WriteAllText(Path.Combine(svgDir, asset + ".svg"), svg);

                Console.WriteLine(
                    $"  + {asset,-28} pdf {Kb(pdf.Length)}   svg {Kb(System.Text.Encoding.UTF8.GetByteCount(svg))}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  x {asset}: {ex.Message}");
                failures++;
            }
        }

        if (failures > 0)
        {
            Console.Error.WriteLine($"{failures} showcase asset(s) failed to generate.");
            return 1;
        }

        Console.WriteLine("Showcase assets written to docs/images/showcase/ and docs/pdf/.");
        return 0;
    }

    /// <summary>
    /// Inlines the subsetted @font-face rules into the SVG so it renders with
    /// the fonts the layout was measured in. Without this the file still opens,
    /// but a viewer with no 'bpf-*' family substitutes its own font and the
    /// glyphs no longer match the positions the engine computed for them.
    /// Standard-14 text carries a real family name instead and needs nothing.
    /// </summary>
    private static string InlineFonts(string svg, string fontFaceCss)
    {
        if (string.IsNullOrEmpty(fontFaceCss)) return svg;

        string style = $"<style>{fontFaceCss}</style>";
        int afterTitle = svg.IndexOf("</title>", StringComparison.Ordinal);
        int at = afterTitle >= 0
            ? afterTitle + "</title>".Length
            : svg.IndexOf('>') + 1;
        return svg[..at] + style + svg[at..];
    }

    private static string Kb(int bytes) => $"{bytes / 1024.0,5:F0} KB";

    /// <summary>
    /// Walks up from the build output to the repository root, identified by the
    /// solution file, so the script writes to the working tree rather than to
    /// bin/ wherever it happens to be run from.
    /// </summary>
    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BonniePDF.slnx")))
            dir = dir.Parent;

        return dir?.FullName
               ?? throw new InvalidOperationException(
                   "Could not locate the repository root (no BonniePDF.slnx found above " + AppContext.BaseDirectory +
                   ").");
    }
}