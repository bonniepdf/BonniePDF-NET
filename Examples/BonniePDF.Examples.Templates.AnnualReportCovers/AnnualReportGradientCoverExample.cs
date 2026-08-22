using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #1 - "Gradient field".
///
/// A full-bleed annual-report cover painted entirely on the this feature vector drawing
/// surface. The whole page is one <see cref="CanvasElement"/>: a vertical
/// linear-gradient backdrop, a soft radial "glow" that fades to transparent, a
/// set of fine diagonal accent rules, a rotated brand mark, and the cover type
/// drawn directly with canvas text.
///
/// Because the cover is one canvas the document uses a zero margin and a single
/// full-page element, so the artwork bleeds to every paper edge - exactly how a
/// printed report cover is laid out.
/// </summary>
[System.ComponentModel.DisplayName("Annual - Gradient")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A two-page annual report. Page 1 is a full-bleed cover drawn on the vector surface (indigo-to-fuchsia gradient, radial glow, diagonal accent rules, rotated brand mark, canvas-drawn type); page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportGradientCoverExample
{
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#4f46e5", "#7c3aed", "#c026d3", "#f59e0b"];

    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            Language = "en-GB",
            Accessibility = AccessibilityMode.PdfUa1,
            // The cover artwork lives in the page-background slot. Slots are painted
            // inside an /Artifact marked-content section, so the gradient, the glow
            // and the accent rules stay out of the structure tree where decoration
            // belongs - and the cover type above flows as ordinary tagged content.
            PageBackground = pageNumber => pageNumber == 1
                ? new CanvasElement(DrawCover)
                {
                    CanvasHeight = PdfSize.A4.Height,
                    IsDecorative = true,
                }
                : null,
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - gradient cover (vector drawing surface)";

        doc.Add(CoverType());

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Indigo[700], ColorPalette.Fuchsia[600], ColorPalette.Indigo[50],
            SegmentColors));

        return doc;
    }

    private static void DrawCover(ICanvas canvas, float w, float h)
    {
        // 1. Full-bleed vertical gradient backdrop (deep indigo -> violet -> fuchsia).
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(new LinearGradientPaint(
            new Point(0, 0), new Point(0, h),
            new GradientStop(0f, ColorPalette.Indigo[950]),
            new GradientStop(0.55f, ColorPalette.Violet[800]),
            new GradientStop(1f, ColorPalette.Fuchsia[700])));

        // 2. Soft radial glow, upper-right, fading to transparent so it reads as
        //    light bleeding across the gradient rather than a hard disc.
        canvas.SaveState();
        canvas.BeginPath();
        canvas.AddCircle(w * 0.84f, h * 0.16f, 260f);
        canvas.FillCurrentPath(new RadialGradientPaint(
            new Point(w * 0.84f, h * 0.16f), 260f,
            new[]
            {
                new GradientStop(0f, ColorPalette.Amber[200].WithAlpha(0.55f)),
                new GradientStop(1f, ColorPalette.Amber[200].WithAlpha(0f)),
            }));
        canvas.RestoreState();

        // 3. Fine diagonal accent rules across the lower third, drawn at low alpha.
        canvas.SaveState();
        canvas.SetGlobalAlpha(0.35f);
        for (int i = 0; i < 5; i++)
        {
            float y = h * 0.62f + i * 16f;
            canvas.BeginPath();
            canvas.MoveTo(0, y);
            canvas.LineTo(w, y - 60f);
            canvas.StrokeCurrentPath(new StrokeStyle { Paint = ColorPalette.Fuchsia[300], Width = 1.5f, LineCap = LineCap.Round });
        }
        canvas.RestoreState();

        // 4. Brand mark - a single gold accent square rotated 45 degrees (a transform demo).
        canvas.SaveState();
        canvas.Translate(72f, h * 0.2f);
        canvas.Rotate(45f);
        canvas.BeginPath();
        canvas.AddRect(-9f, -9f, 18f, 18f);
        canvas.FillCurrentPath(ColorPalette.Amber[400]);
        canvas.RestoreState();

        // The gold rule under the title is decoration, so it stays a drawn box.
        canvas.BeginPath();
        canvas.AddRect(74f, h * 0.49f, 120f, 5f);
        canvas.FillCurrentPath(ColorPalette.Amber[400]);
    }

    /// <summary>
    /// The cover type as real text elements rather than canvas glyphs: the company
    /// name is the document's H1 and everything here lands in the structure tree,
    /// so a screen reader reads the cover in order instead of meeting one opaque
    /// figure.
    /// </summary>
    private static Container CoverType()
    {
        VerticalStack stack = new();

        stack.Add(new TextBlock("SAMPLE")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            TextColor = Color.White,
            Margin = new Thickness(36, 0, 0, 108),
        });

        stack.Add(new TextBlock($"ANNUAL REPORT {Year}")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = ColorPalette.Fuchsia[200],
            Margin = new Thickness(0, 0, 0, 8),
        });

        stack.Add(new Heading(Company)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 52,
            LineHeight = 1f,
            TextColor = Color.White,
            Margin = new Thickness(0, 0, 0, 70),
        });

        stack.Add(new TextBlock("Designing the systems that power tomorrow")
        {
            FontSize = 14,
            TextColor = ColorPalette.Violet[100],
            Margin = new Thickness(0, 0, 0, 324),
        });

        stack.Add(new TextBlock("Prepared with BonniePDF")
        {
            FontSize = 10,
            TextColor = ColorPalette.Violet[200],
            Margin = new Thickness(0, 0, 0, 7),
        });

        stack.Add(new TextBlock($"Fiscal Year {Year} · Confidential")
        {
            FontSize = 10,
            TextColor = ColorPalette.Violet[300],
        });

        // Anchored to the foot of the page rather than positioned by a top padding
        // measured against the font's metrics: the block keeps its footing if the
        // title wraps or a face changes.
        return new Container
        {
            Height = PdfSize.A4.Height,
            VerticalAlignment = VerticalAlignment.End,
            Padding = new Thickness(72, 0, 72, 51),
            Child = stack,
        };
    }
}
