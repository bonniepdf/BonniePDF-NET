using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #3 - "Geometric badge".
///
/// A light, modern cover built from this feature primitives: a radial "sun" bleeding off
/// the top-left corner, an abstract cluster of translucent rotated polygons and a
/// star placed with affine transforms, and a circular masked image badge (a photo
/// cropped to a circle - the most common "team / hero" cover crop) ringed in gold.
///
/// As in the masked-cover example the badge image is a vector placeholder; replace
/// the PLACEHOLDER block with <c>canvas.DrawImage(...)</c> to crop a real photo to
/// the circle. The clip is already in force.
/// </summary>
[System.ComponentModel.DisplayName("Annual Report Cover - Geometric")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A two-page annual report. Page 1 is a light, modern cover built from vector primitives (radial sun, translucent rotated polygons and a star via affine transforms, a circular masked image badge ringed in gold). Page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportGeometricCoverExample
{
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#0369a1", "#4f46e5", "#f59e0b", "#059669"];

    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Accent = ColorPalette.Amber[500];

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
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - geometric cover with a circular masked image (vector drawing surface)";

        CanvasElement cover = new(DrawCover)
        {
            CanvasHeight = PdfSize.A4.Height,
            AltText = $"{Company} Annual Report {Year} cover: a geometric layout with a circular cropped photo badge and the company name.",
        };
        doc.Add(cover);

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Sky[700], ColorPalette.Amber[500], ColorPalette.Sky[50],
            SegmentColors));

        return doc;
    }

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

    /// <summary>Regular polygon centred at (cx, cy), built in local coordinates.</summary>
    private static void AddPolygon(ICanvas canvas, float cx, float cy, float radius, int sides)
    {
        for (int i = 0; i < sides; i++)
        {
            float a = -MathF.PI / 2f + 2f * MathF.PI * i / sides;
            float x = cx + radius * MathF.Cos(a);
            float y = cy + radius * MathF.Sin(a);
            if (i == 0) canvas.MoveTo(x, y);
            else canvas.LineTo(x, y);
        }
        canvas.ClosePath();
    }

    /// <summary>Star centred at (cx, cy).</summary>
    private static void AddStar(ICanvas canvas, float cx, float cy, float outer, float inner, int points)
    {
        int total = points * 2;
        for (int i = 0; i < total; i++)
        {
            float a = -MathF.PI / 2f + MathF.PI * i / points;
            float r = i % 2 == 0 ? outer : inner;
            float x = cx + r * MathF.Cos(a);
            float y = cy + r * MathF.Sin(a);
            if (i == 0) canvas.MoveTo(x, y);
            else canvas.LineTo(x, y);
        }
        canvas.ClosePath();
    }

    private static void DrawCover(ICanvas canvas, float w, float h)
    {
        // 1. Light backdrop.
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(ColorPalette.Slate[50]);

        // 2. Radial "sun" bleeding off the top-left corner.
        canvas.BeginPath();
        AddCircle(canvas, 30f, 40f, 340f);
        canvas.FillCurrentPath(new RadialGradientPaint(
            new Point(30f, 40f), 340f,
            new[]
            {
                new GradientStop(0f, ColorPalette.Amber[300].WithAlpha(0.7f)),
                new GradientStop(1f, ColorPalette.Amber[300].WithAlpha(0f)),
            }));

        // 3. Abstract geometric cluster, lower-right, placed with transforms + alpha.
        canvas.SaveState();
        canvas.SetGlobalAlpha(0.5f);

        canvas.SaveState();
        canvas.Translate(w * 0.78f, h * 0.7f);
        canvas.Rotate(18f);
        canvas.BeginPath();
        AddPolygon(canvas, 0, 0, 120f, 6);
        canvas.FillCurrentPath(ColorPalette.Sky[400]);
        canvas.RestoreState();

        canvas.SaveState();
        canvas.Translate(w * 0.62f, h * 0.82f);
        canvas.Rotate(-12f);
        canvas.BeginPath();
        AddPolygon(canvas, 0, 0, 90f, 3);
        canvas.FillCurrentPath(ColorPalette.Violet[400]);
        canvas.RestoreState();

        canvas.SaveState();
        canvas.Translate(w * 0.86f, h * 0.86f);
        canvas.Rotate(8f);
        canvas.BeginPath();
        AddStar(canvas, 0, 0, 70f, 30f, 5);
        canvas.FillCurrentPath(Accent);
        canvas.RestoreState();

        canvas.RestoreState();

        // 4. Circular masked image badge, upper-centre.
        float bx = w * 0.5f;
        float by = h * 0.36f;
        float br = 96f;

        canvas.SaveState();
        canvas.BeginPath();
        AddCircle(canvas, bx, by, br);
        canvas.ClipToCurrentPath();

        // -- PLACEHOLDER --------------------------------------------------------
        // Replace with `canvas.DrawImage(image, bx - br, by - br, br * 2, br * 2)`
        // to crop a real photo to the circle; the circular clip above still applies.
        canvas.BeginPath();
        canvas.AddRect(bx - br, by - br, br * 2f, br * 2f);
        canvas.FillCurrentPath(new LinearGradientPaint(
            new Point(bx - br, by - br), new Point(bx + br, by + br),
            new GradientStop(0f, ColorPalette.Indigo[500]),
            new GradientStop(1f, ColorPalette.Sky[400])));
        canvas.DrawText(bx, by - 6f, "PHOTO", Color.White.WithAlpha(0.85f), 13f, StandardFonts.HelveticaBold, TextAlignment.Center);
        // -----------------------------------------------------------------------

        canvas.RestoreState();

        // Gold ring around the badge.
        canvas.BeginPath();
        AddCircle(canvas, bx, by, br);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = Accent, Width = 5f });

        // 5. Cover type, centred under the badge.
        canvas.DrawText(bx, by + br + 40f, $"ANNUAL REPORT {Year}", ColorPalette.Amber[600], 15f, StandardFonts.HelveticaBold, TextAlignment.Center);
        canvas.DrawText(bx, by + br + 64f, Company, Ink, 44f, StandardFonts.HelveticaBold, TextAlignment.Center);
        canvas.DrawText(bx, by + br + 124f, "Stewardship, growth, and long-term value", ColorPalette.Slate[500], 13f, StandardFonts.Helvetica, TextAlignment.Center);

        // 6. Footer credit.
        canvas.DrawText(bx, h * 0.94f, $"Prepared with BonniePDF - Fiscal Year {Year}", ColorPalette.Slate[400], 10f, StandardFonts.Helvetica, TextAlignment.Center);
    }
}
