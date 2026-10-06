using BonniePDF;
using BonniePDF.Drawing;

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
[System.ComponentModel.DisplayName("Annual - Masked")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A two-page annual report. Page 1 is the classic photographic cover: a full-width hero image clipped to an angled mask cutting into a green title band with a gold seam. Page 2 is the \"inside the report\" spread - KPI band, quarterly-revenue column chart, revenue-by-segment donut, and a financial-summary table.")]
public static class AnnualReportMaskedCoverExample
{
    private const string HeroImageKey = "images/sean-pollock-PhYq704ffdA-unsplash.jpg";
    private const string Company = "Sample Company";
    private const int Year = 2025;
    private static readonly string[] SegmentColors = ["#047857", "#0d9488", "#f59e0b", "#0284c7"];

    private static readonly Color Band = ColorPalette.Emerald[900]; // brand band behind the title
    private static readonly Color Seam = ColorPalette.Amber[400];   // gold seam along the diagonal cut

    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        if (!assets.TryGetValue(HeroImageKey, out byte[]? heroData))
            throw new InvalidOperationException($"Missing required asset: {HeroImageKey}");
        ImageInfo heroImage = ImageLoader.Load(heroData);

        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0, // zero margin - full-bleed cover
            PageColor = Color.White,
            Language = "en-GB",
            Accessibility = AccessibilityMode.PdfUa1,
            // The cover artwork lives in the page-background slot. Slots are painted
            // inside an /Artifact marked-content section, so the band, the photo and
            // the seam stay out of the structure tree where decoration belongs - and
            // the cover type above flows as ordinary tagged content.
            PageBackground = pageNumber => pageNumber == 1
                ? new CanvasElement((canvas, w, h) => DrawCover(canvas, w, h, heroImage))
                {
                    DrawHeight = PdfSize.A4.Height,
                    IsDecorative = true,
                }
                : null,
        };

        Document doc = new(settings);
        doc.Metadata.Title = $"{Company} - Annual Report {Year}";
        doc.Metadata.Author = Company;
        doc.Metadata.Subject = $"{Company} Annual Report {Year} - masked hero-image cover (vector drawing surface)";

        doc.Add(CoverType());

        // Page 2 - the "inside the report" capabilities spread (tables + charts + donut).
        CapabilitiesPage.Add(doc, new ReportBrand(
            Company, Year,
            ColorPalette.Emerald[800], ColorPalette.Amber[600], ColorPalette.Emerald[50],
            SegmentColors));

        return doc;
    }

    private static void DrawCover(ICanvas canvas, float w, float h, ImageInfo heroImage)
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

        canvas.DrawImage(heroImage, 0, 0, w, photoBottom);

        canvas.RestoreState();

        // 3. Gold seam stroked along the diagonal cut.
        canvas.BeginPath();
        canvas.MoveTo(0, cutLeft);
        canvas.LineTo(w, cutRight);
        canvas.StrokeCurrentPath(new StrokeStyle { Paint = Seam, Width = 6f, LineCap = LineCap.Butt });
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
            Color = ColorPalette.Emerald[200],
            Margin = new Thickness(0, 0, 0, 6),
        });

        stack.Add(new Heading(Company)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 46,
            LineHeight = 1f,
            Color = Color.White,
            Margin = new Thickness(0, 0, 0, 18),
        });

        // The gold rule under the title is decoration, so it stays a drawn box.
        stack.Add(new Container
        {
            Width = 120,
            Height = 4,
            BackgroundColor = Seam,
            Margin = new Thickness(2, 0, 0, 14),
        });

        stack.Add(new TextBlock("Building resilient infrastructure across three continents")
        {
            FontSize = 13,
            Color = ColorPalette.Emerald[100],
            Margin = new Thickness(0, 0, 0, 106),
        });

        stack.Add(new TextBlock($"Prepared with BonniePDF · Fiscal Year {Year}")
        {
            FontSize = 10,
            Color = ColorPalette.Emerald[200],
        });

        // Anchored to the foot of the page rather than positioned by a top padding
        // measured against the font's metrics: the block keeps its footing if the
        // title wraps or a face changes.
        return new Container
        {
            Height = PdfSize.A4.Height,
            VerticalAlignment = VerticalAlignment.End,
            Padding = new Thickness(72, 0, 72, 49),
            Child = stack,
        };
    }
}
