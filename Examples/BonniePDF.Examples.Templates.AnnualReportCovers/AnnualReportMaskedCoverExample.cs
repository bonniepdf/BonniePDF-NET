using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Images;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #2 - "Masked hero image".
///
/// The classic photographic report cover: a full-width hero image occupies the
/// top of the page, clipped to an angled mask so its lower edge cuts diagonally
/// into a solid brand band that carries the title. The mask is an arbitrary
/// polygon path fed to <see cref="ICanvas.ClipToCurrentPath"/>.
///
/// </summary>
[System.ComponentModel.DisplayName("Annual Report Cover - Masked Image")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A two-page annual report. Page 1 is the classic photographic cover: a full-width hero image clipped to an angled mask cutting into a green title band with a gold seam. Page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportMaskedCoverExample
{
    private const string HeroImageKey = "images/sean-pollock-PhYq704ffdA-unsplash.jpg";
    private const string Company = "Meridian Group";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#047857", "#0d9488", "#f59e0b", "#0284c7"];

    private static readonly Color Band = ColorPalette.Emerald[900]; // brand band behind the title
    private static readonly Color Seam = ColorPalette.Amber[400];   // gold seam along the diagonal cut

    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
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
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - masked hero-image cover (vector drawing surface)";

        ImageInfo? heroImage = null;
        if (assets != null && assets.TryGetValue(HeroImageKey, out byte[]? heroData))
            heroImage = ImageLoader.Load(heroData);

        CanvasElement cover = new((canvas, w, h) => DrawCover(canvas, w, h, heroImage))
        {
            CanvasHeight = PdfSize.A4.Height,
            AltText = $"{Company} Annual Report {Year} cover: a hero landscape image clipped to an angled mask above a green title band.",
        };
        doc.Add(cover);

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Emerald[800], ColorPalette.Amber[600], ColorPalette.Emerald[50],
            SegmentColors));

        return doc;
    }

    /// <summary>Draws the vector stand-in for the hero photograph (sky, sun, hills, label).</summary>
    private static void DrawPlaceholderImage(ICanvas canvas, float w, float photoBottom)
    {
        // Sky gradient.
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, photoBottom);
        canvas.FillCurrentPath(new LinearGradientPaint(
            new Point(0, 0), new Point(0, photoBottom),
            new GradientStop(0f, ColorPalette.Sky[400]),
            new GradientStop(1f, ColorPalette.Sky[100])));

        // Sun.
        float sx = w * 0.74f;
        float sy = photoBottom * 0.32f;
        float r = 46f;
        float k = r * 0.5522847498f;
        canvas.BeginPath();
        canvas.MoveTo(sx + r, sy);
        canvas.CubicBezierTo(sx + r, sy + k, sx + k, sy + r, sx, sy + r);
        canvas.CubicBezierTo(sx - k, sy + r, sx - r, sy + k, sx - r, sy);
        canvas.CubicBezierTo(sx - r, sy - k, sx - k, sy - r, sx, sy - r);
        canvas.CubicBezierTo(sx + k, sy - r, sx + r, sy - k, sx + r, sy);
        canvas.ClosePath();
        canvas.FillCurrentPath(ColorPalette.Amber[200]);

        // Back hill.
        canvas.BeginPath();
        canvas.MoveTo(0, photoBottom);
        canvas.LineTo(0, photoBottom * 0.72f);
        canvas.LineTo(w * 0.42f, photoBottom * 0.52f);
        canvas.LineTo(w * 0.78f, photoBottom * 0.78f);
        canvas.LineTo(w, photoBottom * 0.66f);
        canvas.LineTo(w, photoBottom);
        canvas.ClosePath();
        canvas.FillCurrentPath(ColorPalette.Emerald[400]);

        // Front hill.
        canvas.BeginPath();
        canvas.MoveTo(0, photoBottom);
        canvas.LineTo(0, photoBottom * 0.86f);
        canvas.LineTo(w * 0.55f, photoBottom * 0.66f);
        canvas.LineTo(w, photoBottom * 0.84f);
        canvas.LineTo(w, photoBottom);
        canvas.ClosePath();
        canvas.FillCurrentPath(ColorPalette.Emerald[600]);

        // Centred "placeholder" caption so the swap-in point is obvious.
        canvas.DrawText(w / 2f, photoBottom * 0.14f, "PLACEHOLDER IMAGE - swap in canvas.DrawImage(...)",
            Color.White.WithAlpha(0.85f), 11f, StandardFonts.HelveticaBold, TextAlignment.Center);
    }

    private static void DrawCover(ICanvas canvas, float w, float h, ImageInfo? heroImage)
    {
        // The diagonal seam: the hero image's lower edge runs from 0.52*H on the
        // left down to 0.62*H on the right. The mask polygon is everything above it.
        float cutLeft = h * 0.52f;
        float cutRight = h * 0.62f;
        float photoBottom = h * 0.66f;

        // 1. Brand band fills the whole page; the masked image overpaints the top.
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(Band);

        // 2. Clip to the angled hero-image mask, then paint the image inside it.
        canvas.SaveState();
        canvas.BeginPath();
        canvas.MoveTo(0, 0);
        canvas.LineTo(w, 0);
        canvas.LineTo(w, cutRight);
        canvas.LineTo(0, cutLeft);
        canvas.ClosePath();
        canvas.ClipToCurrentPath();

        if (heroImage != null)
            canvas.DrawImage(heroImage, 0, 0, w, photoBottom);
        else
            DrawPlaceholderImage(canvas, w, photoBottom);

        canvas.RestoreState();

        // 3. Gold seam stroked along the diagonal cut.
        canvas.BeginPath();
        canvas.MoveTo(0, cutLeft);
        canvas.LineTo(w, cutRight);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = Seam, Width = 6f, LineCap = LineCap.Butt });

        // 4. Title block in the band below the seam.
        float titleY = h * 0.74f;
        canvas.DrawText(72f, titleY - 30f, $"ANNUAL REPORT {Year}", ColorPalette.Emerald[200], 15f, StandardFonts.HelveticaBold, TextAlignment.Left);
        canvas.DrawText(72f, titleY, Company, Color.White, 46f, StandardFonts.HelveticaBold, TextAlignment.Left);

        // Gold rule + tagline.
        canvas.BeginPath();
        canvas.AddRect(74f, titleY + 58f, 120f, 4f);
        canvas.FillCurrentPath(Seam);
        canvas.DrawText(72f, titleY + 72f, "Building resilient infrastructure across three continents", ColorPalette.Emerald[100], 13f, StandardFonts.Helvetica, TextAlignment.Left);

        // 5. Footer credit.
        canvas.DrawText(72f, h * 0.94f, $"Prepared with BonniePDF - Fiscal Year {Year}", ColorPalette.Emerald[200], 10f, StandardFonts.Helvetica, TextAlignment.Left);
    }
}
