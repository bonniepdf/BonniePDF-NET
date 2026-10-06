using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.Templates.AnnualReportCovers;

/// <summary>
/// Annual-report front cover #3 - "Geometric badge".
///
/// A light, modern cover built from vector drawing primitives: a radial "sun" bleeding off
/// the top-left corner, an abstract cluster of translucent rotated polygons and a
/// star placed with affine transforms, and a circular masked image badge (a photo
/// cropped to a circle - the most common "team / hero" cover crop) ringed in gold.
///
/// The badge is a real photograph: the circular path is pushed as a clip and the
/// image drawn over its bounding square, so the circle does the cropping.
/// </summary>
[System.ComponentModel.DisplayName("Annual - Geometric")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A two-page annual report. Page 1 is a light, modern cover built from vector primitives (radial sun, translucent rotated polygons and a star via affine transforms, a photo cropped to a circular badge ringed in gold). Page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportGeometricCoverExample
{
    private const string BadgeImageKey = "images/sean-pollock-PhYq704ffdA-unsplash.jpg";
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#0369a1", "#4f46e5", "#f59e0b", "#059669"];

    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Accent = ColorPalette.Amber[500];

    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.TryGetValue(BadgeImageKey, out byte[]? badgeData))
            throw new InvalidOperationException($"Missing required asset: {BadgeImageKey}");
        ImageInfo badgeImage = ImageLoader.Load(badgeData);

        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            Language = "en-GB",
            Accessibility = AccessibilityMode.PdfUa1,
            // The cover artwork lives in the page-background slot. Slots are painted
            // inside an /Artifact marked-content section, so the sun, the polygons
            // and the photo badge stay out of the structure tree where decoration
            // belongs - and the cover type above flows as ordinary tagged content.
            PageBackground = pageNumber => pageNumber == 1
                ? new CanvasElement((canvas, w, h) => DrawCover(canvas, w, h, badgeImage))
                {
                    DrawHeight = PdfSize.A4.Height,
                    IsDecorative = true,
                }
                : null,
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - geometric cover with a circular masked image (vector drawing surface)";

        doc.Add(CoverType());

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        // Amber[700], not [500]: the KPI card paints this as 24pt bold text on the
        // Sky-50 tile, where [500] only reaches 2.00:1 against the 3:1 large-text
        // threshold. The cover's own Accent (canvas fills above) is unaffected.
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Sky[700], ColorPalette.Amber[700], ColorPalette.Sky[50],
            SegmentColors));

        return doc;
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

    private static void DrawCover(ICanvas canvas, float w, float h, ImageInfo badgeImage)
    {
        // 1. Light backdrop.
        canvas.BeginPath();
        canvas.AddRect(0, 0, w, h);
        canvas.FillCurrentPath(ColorPalette.Slate[50]);

        // 2. Radial "sun" bleeding off the top-left corner.
        canvas.BeginPath();
        canvas.AddCircle(30f, 40f, 340f);
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
        canvas.AddCircle(bx, by, br);
        canvas.ClipToCurrentPath();

        // The photo is drawn over the badge's bounding square; the circular clip
        // above is what crops it to the circle.
        canvas.DrawImage(badgeImage, bx - br, by - br, br * 2f, br * 2f);

        canvas.RestoreState();

        // Gold ring around the badge.
        canvas.BeginPath();
        canvas.AddCircle(bx, by, br);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = Accent, Width = 5f });

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

        stack.Add(new TextBlock($"ANNUAL REPORT {Year}")
        {
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15,
            Color = ColorPalette.Amber[600],
            Alignment = TextAlignment.Center,
        });

        stack.Add(new Heading(Company)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 44,
            LineHeight = 1f,
            Color = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 48),
        });

        stack.Add(new TextBlock("Stewardship, growth, and long-term value")
        {
            FontSize = 13,
            Color = ColorPalette.Slate[500],
            Alignment = TextAlignment.Center,
            // The block is anchored to the foot of the page, so this gap is what
            // sets how far the type sits below the badge: the eyebrow was clearing
            // the gold ring by about 3pt, which read as a collision.
            Margin = new Thickness(0, 0, 0, 240),
        });

        // Slate[600], not [400]: this footer sits on the flat page background,
        // where Slate[400] is 2.51:1 against the 4.5:1 WCAG AA threshold.
        stack.Add(new TextBlock($"Prepared with BonniePDF · Fiscal Year {Year}")
        {
            FontSize = 10,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Center,
        });

        // Anchored to the foot of the page rather than positioned by a top padding
        // measured against the font's metrics: the block keeps its footing if the
        // title wraps or a face changes.
        return new Container
        {
            Height = PdfSize.A4.Height,
            VerticalAlignment = VerticalAlignment.End,
            Padding = new Thickness(56, 0, 56, 48),
            Child = stack,
        };
    }
}
