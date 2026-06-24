using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Drawing;
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
[System.ComponentModel.DisplayName("Annual Report Cover - Gradient")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A full-bleed annual-report front cover drawn entirely on the vector surface: a vertical indigo-to-fuchsia linear gradient, a radial glow that fades to transparent, diagonal accent rules, a rotated brand mark, and canvas-drawn cover type.")]
public static class AnnualReportGradientCoverExample
{
    private const string Company = "Aurora Dynamics";
    private const int Year = 2025;

    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            Language = "en-GB",
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - gradient cover (vector drawing surface)";

        CanvasElement cover = new(DrawCover)
        {
            CanvasHeight = PdfSize.A4.Height,
            AltText = $"{Company} Annual Report {Year} cover: a deep indigo-to-fuchsia gradient with the company name and report year.",
        };
        doc.Add(cover);

        return doc;
    }

    /// <summary>A reusable circle sub-path built from four cubic-bezier quadrants.</summary>
    private static void AddCircle(ICanvas canvas, float cx, float cy, float r)
    {
        float k = r * 0.5522847498f;
        canvas.MoveTo(cx + r, cy);
        canvas.CubicBezierTo(cx + r, cy + k, cx + k, cy + r, cx, cy + r);
        canvas.CubicBezierTo(cx - k, cy + r, cx - r, cy + k, cx - r, cy);
        canvas.CubicBezierTo(cx - r, cy - k, cx - k, cy - r, cx, cy - r);
        canvas.CubicBezierTo(cx + k, cy - r, cx + r, cy - k, cx + r, cy);
        canvas.ClosePath();
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
        AddCircle(canvas, w * 0.84f, h * 0.16f, 260f);
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

        // 5. Cover type, drawn directly on the canvas.
        canvas.DrawText(108f, h * 0.2f - 9f, "AURORA", Color.White, 15f, StandardFonts.HelveticaBold, TextAlignment.Left);

        canvas.DrawText(72f, h * 0.34f, $"ANNUAL REPORT {Year}", ColorPalette.Fuchsia[200], 16f, StandardFonts.HelveticaBold, TextAlignment.Left);
        canvas.DrawText(72f, h * 0.4f, Company, Color.White, 52f, StandardFonts.HelveticaBold, TextAlignment.Left);

        // A short gold rule under the title.
        canvas.BeginPath();
        canvas.AddRect(74f, h * 0.49f, 120f, 5f);
        canvas.FillCurrentPath(ColorPalette.Amber[400]);

        canvas.DrawText(72f, h * 0.52f, "Designing the systems that power tomorrow", ColorPalette.Violet[100], 14f, StandardFonts.Helvetica, TextAlignment.Left);

        // 6. Footer credit, lower-left.
        canvas.DrawText(72f, h * 0.92f, "Prepared with BonniePDF", ColorPalette.Violet[200], 10f, StandardFonts.Helvetica, TextAlignment.Left);
        canvas.DrawText(72f, h * 0.94f, $"Fiscal Year {Year} - Confidential", ColorPalette.Violet[300], 10f, StandardFonts.Helvetica, TextAlignment.Left);
    }
}
