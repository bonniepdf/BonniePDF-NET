using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.ProductRoadmap;

/// <summary>
/// A landscape A4 Gantt-style product roadmap: a six-month timeline header over
/// team swimlanes (Development, Design, Marketing) with rounded task bars
/// positioned by month. Each row is a weighted <see cref="HorizontalStack"/> -
/// a left label of weight <c>LabelWeight</c> followed by the six-month timeline
/// expressed as weighted bars and transparent gaps (one weight unit = one month)
/// - so every row's columns line up under the month header automatically. Bars
/// set <see cref="Container.ClipContent"/> so a label longer than its
/// month span is masked to the rounded pill instead of overflowing.
/// </summary>
[System.ComponentModel.DisplayName("Product Roadmap")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Landscape A4 Gantt-style product roadmap: a six-month timeline header over team swimlanes (Development, Design, Marketing), with rounded task bars positioned by month using weighted horizontal stacks.")]
public static class ProductRoadmapExample
{
    // ── Palette (Slate neutrals + per-team accent ramps [dark, light]) ────────
    private static readonly Color Ink = Color.FromHex("#0f172a");      // slate-900  headings
    private static readonly Color BodyCol = Color.FromHex("#334155");  // slate-700  body / month pills
    private static readonly Color Mute = Color.FromHex("#64748b");     // slate-500  captions
    private static readonly Color Grid = Color.FromHex("#cbd5e1");     // slate-300  grid lines
    private static readonly Color Stripe = Color.FromHex("#f1f5f9");   // slate-100  alternating column shade
    private static readonly Color Tint = Color.FromHex("#f8fafc");     // slate-50   page tint

    private static readonly Color[] Dev = { Color.FromHex("#4f46e5"), Color.FromHex("#818cf8") };       // indigo
    private static readonly Color[] Design = { Color.FromHex("#0d9488"), Color.FromHex("#2dd4bf") };    // teal
    private static readonly Color[] Marketing = { Color.FromHex("#059669"), Color.FromHex("#34d399") }; // emerald

    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };

    private const float LabelWeight = 2.4f; // left column width, in "month units"
    private const float RowHeight = 26f;
    private const float BarHeight = 18f;
    private const float Line = 0.6f; // hairline grid-line / bar-border width

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Landscape, 38f)
        {
            Language = "en-GB",
        };

        Document doc = new(settings);
        doc.Metadata.Title = "Product Roadmap";
        doc.Metadata.Author = "Sample Platform";
        doc.Metadata.Subject = "H1 2026 product roadmap - swimlanes by team across a six-month timeline";

        // ── Title ─────────────────────────────────────────────────────────────
        doc.Add(Tb("Product Roadmap", 22, Ink, bold: true));
        TextBlock subtitle = Tb("H1 2026  ·  Sample Platform", 10, Mute, top: 4);
        subtitle.Margin = new Thickness(0, 4, 0, 16);
        doc.Add(subtitle);

        // ── Month header (project block + six month bubbles) ──────────────────
        VerticalStack project = new();
        project.Add(Tb("PROJECT TITLE HERE", 9, Ink, bold: true));
        project.Add(Tb("Sample Text Here", 7, Mute, top: 2));

        HorizontalStack headerRow = new()
        {
            Spacing = 0,
            WidthDistribution = WidthDistribution.Weighted,
            Alignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
        };
        headerRow.Add(new Container { Child = project, Padding = new Thickness(0, 3, 8, 3) }, LabelWeight);
        foreach (string m in Months) headerRow.Add(MonthCell(m), 1);
        doc.Add(headerRow);

        // ── Swimlanes ─────────────────────────────────────────────────────────
        var teams = new (string Name, string Sub, Color Accent, Color[] Shades, (int S, int E, string L)[][] Rows)[]
        {
            ("DEVELOPMENT TEAM", "Engineering & build", Dev[0], Dev, new[]
            {
                Row((1, 2, "Discovery"), (3, 4, "Prototype")),
                Row((3, 3, "Spike"), (4, 6, "Build & QA")),
            }),
            ("DESIGN TEAM", "UX & visual", Design[0], Design, new[]
            {
                Row((2, 3, "Wireframes"), (4, 5, "UI Kit")),
                Row((5, 6, "Handoff")),
            }),
            ("MARKETING TEAM", "GTM & launch", Marketing[0], Marketing, new[]
            {
                Row((3, 4, "Positioning"), (5, 6, "Campaign")),
                Row((4, 4, "Assets"), (6, 6, "Launch")),
            }),
        };

        bool firstRow = true;
        for (int ti = 0; ti < teams.Length; ti++)
        {
            var team = teams[ti];
            if (ti > 0) doc.Add(StripeBand(12)); // continuous striped band between swimlanes

            for (int ri = 0; ri < team.Rows.Length; ri++)
            {
                IElement label = ri == 0 ? TeamLabel(team.Name, team.Sub, team.Accent) : new Container();
                doc.Add(RoadmapRow(label, team.Rows[ri], team.Shades, firstRow));
                firstRow = false;
            }
        }

        // Light page tint flooding the remaining height.
        doc.Add(new Container { BackgroundColor = Tint, Child = new Spacer(8) });

        return doc;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (int S, int E, string L)[] Row(params (int S, int E, string L)[] segs) => segs;

    private static TextBlock Tb(string content, float size, Color color, bool bold = false,
        TextAlignment? align = null, float top = 0)
    {
        TextBlock t = new()
        {
            Text = content,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            TextColor = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (top > 0) t.Margin = new Thickness(0, top, 0, 0);
        return t;
    }

    // A rounded pill bar carrying a short task label, outlined in the team's darker
    // shade and clipped to its rounded shape.
    private static Container Bar(string label, Color fill, Color outline) => new()
    {
        BackgroundColor = fill,
        CornerRadius = 5,
        ClipContent = true, // ← this feature
        Height = BarHeight,
        Border = new Border(Line),
        BorderColors = new BorderColors(outline),
        Padding = new Thickness(8, 4, 8, 4),
        Child = Tb(label, 6.5f, Color.White, bold: true, align: TextAlignment.Center),
    };

    // One cell of the column grid: a fixed-height box that draws crisp hairline grid
    // lines on its right and bottom edges (and optionally its top/left for the outer
    // frame), with an optional column shade.
    private static Container GridCell(float height, bool shade = false, bool top = false, bool left = false)
    {
        Container c = new()
        {
            Height = height,
            Border = new Border(left ? Line : 0, top ? Line : 0, Line, Line),
            BorderColors = new BorderColors(Grid),
        };
        if (shade) c.BackgroundColor = Stripe;
        return c;
    }

    // The alternating-column grid: a white label gutter, then six month columns
    // shaded on alternate months, all wired with hairline grid lines. Used both as
    // the per-row background (behind the bars) and as the slim band drawn between
    // swimlanes, so the columns read as one continuous grid. Same weights as a bar
    // row, so the lines line up under the month header and behind every bar. The
    // whole grid is wrapped in a crisp-edge region so the hairlines snap to
    // the pixel grid on SVG export; the rounded bars (a separate Layers
    // foreground) keep their antialiasing.
    private static Container StripeBand(float height, bool top = false)
    {
        HorizontalStack band = new() { Spacing = 0, WidthDistribution = WidthDistribution.Weighted };
        band.Add(GridCell(height, top: top, left: true), LabelWeight); // label gutter stays white
        for (int m = 1; m <= Months.Length; m++)
            band.Add(GridCell(height, shade: m % 2 == 0, top: top), 1);
        return new Container { CrispEdges = true, Child = band };
    }

    // The bar layer of a row: a left label of weight LabelWeight, then the
    // six-month timeline as weighted bars and transparent gaps (one weight = one
    // month) so the striped grid shows through. Bars are vertically centred.
    private static HorizontalStack BarLayer(IElement labelCell, (int S, int E, string L)[] segs, Color[] shades)
    {
        HorizontalStack row = new()
        {
            Spacing = 0,
            WidthDistribution = WidthDistribution.Weighted,
            Alignment = HorizontalAlignment.Center,
            Height = RowHeight,
        };
        row.Add(labelCell, LabelWeight);

        int cursor = 1;
        for (int i = 0; i < segs.Length; i++)
        {
            (int s, int e, string l) = segs[i];
            if (s > cursor) row.Add(new Spacer(BarHeight), s - cursor);
            row.Add(Bar(l, shades[i % 2], shades[0]), e - s + 1);
            cursor = e + 1;
        }
        if (cursor <= Months.Length) row.Add(new Spacer(BarHeight), Months.Length - cursor + 1);

        return row;
    }

    // One roadmap row: the striped column grid (background) with the bars floating
    // on top (this feature Layers). Both layers share the same weights, so the bars sit over
    // their month columns and the grid runs continuously behind them.
    private static Container RoadmapRow(IElement labelCell, (int S, int E, string L)[] segs, Color[] shades, bool top = false)
    {
        Container c = new();
        c.Layers(l => l.Background(StripeBand(RowHeight, top)).PrimaryLayer(BarLayer(labelCell, segs, shades)));
        return c;
    }

    // Left label for the first row of a team: name + sub with a coloured left rail.
    private static Container TeamLabel(string name, string sub, Color accent)
    {
        VerticalStack v = new();
        v.Add(Tb(name, 9, Ink, bold: true));
        v.Add(Tb(sub, 7, Mute, top: 2));
        return new Container
        {
            Child = v,
            Padding = new Thickness(10, 3, 8, 3),
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(accent),
        };
    }

    // A dark month "bubble" centred in its column.
    private static Container MonthCell(string name)
    {
        Container pill = new()
        {
            BackgroundColor = BodyCol,
            CornerRadius = 9,
            Padding = new Thickness(12, 4, 12, 4),
            Child = Tb(name, 8, Color.White, bold: true, align: TextAlignment.Center),
        };
        return new Container { HorizontalAlignment = HorizontalAlignment.Center, Child = pill };
    }
}
