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
    private static readonly Color Stripe = Color.FromHex("#f1f5f9");   // slate-100  alternating column shade
    private static readonly Color Tint = Color.FromHex("#f8fafc");     // slate-50   page tint

    private static readonly Color[] Dev = { Color.FromHex("#4f46e5"), Color.FromHex("#818cf8") };       // indigo
    private static readonly Color[] Design = { Color.FromHex("#0d9488"), Color.FromHex("#2dd4bf") };    // teal
    private static readonly Color[] Marketing = { Color.FromHex("#059669"), Color.FromHex("#34d399") }; // emerald

    private static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };

    private const float LabelWeight = 2.4f; // left column width, in "month units"
    private const float RowHeight = 26f;
    private const float BarHeight = 18f;

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Landscape, 38f)
        {
            Language = "en-GB",
        };

        Document doc = new(settings);
        doc.Metadata.Title = "Product Roadmap";
        doc.Metadata.Author = "Acme Platform";
        doc.Metadata.Subject = "H1 2026 product roadmap - swimlanes by team across a six-month timeline";

        // ── Title ─────────────────────────────────────────────────────────────
        doc.Add(Tb("Product Roadmap", 22, Ink, bold: true));
        doc.Add(Tb("H1 2026  ·  Acme Platform", 10, Mute, top: 4));
        doc.Add(new Spacer(16));

        // ── Month header (project block + six month bubbles) ──────────────────
        VerticalStack project = new();
        project.AddChild(Tb("PROJECT TITLE HERE", 9, Ink, bold: true));
        project.AddChild(Tb("Sample Text Here", 7, Mute, top: 2));

        HorizontalStack headerRow = new()
        {
            Spacing = 0,
            WidthDistribution = WidthDistribution.Weighted,
            Alignment = HorizontalAlignment.Center,
        };
        headerRow.AddChild(new Container { Child = project, Padding = new Thickness(0, 3, 8, 3) }, LabelWeight);
        foreach (string m in Months) headerRow.AddChild(MonthCell(m), 1);
        doc.Add(headerRow);

        doc.Add(new Spacer(8));

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

        for (int ti = 0; ti < teams.Length; ti++)
        {
            var team = teams[ti];
            if (ti > 0) doc.Add(StripeBand(12)); // continuous striped band between swimlanes

            for (int ri = 0; ri < team.Rows.Length; ri++)
            {
                IElement label = ri == 0 ? TeamLabel(team.Name, team.Sub, team.Accent) : new Container();
                doc.Add(RoadmapRow(label, team.Rows[ri], team.Shades));
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

    // A rounded pill bar carrying a short task label, clipped to its rounded shape.
    private static Container Bar(string label, Color color) => new()
    {
        BackgroundColor = color,
        CornerRadius = 5,
        ClipContent = true, // ← this feature
        Height = BarHeight,
        Padding = new Thickness(8, 4, 8, 4),
        Child = Tb(label, 6.5f, Color.White, bold: true, align: TextAlignment.Center),
    };

    // A fixed-height cell with an optional background, used to build the striped
    // column grid behind the bars.
    private static Container Cell(float height, Color? color = null)
    {
        Container c = new() { Height = height };
        if (color.HasValue) c.BackgroundColor = color.Value;
        return c;
    }

    // The alternating-column grid: a white label gutter, then six month columns
    // shaded on alternate months. Used both as the per-row background (behind the
    // bars) and as the slim band drawn between swimlanes, so the columns read as
    // one continuous grid. Same weights as a bar row, so the stripes line up under
    // the month header and behind every bar.
    private static HorizontalStack StripeBand(float height)
    {
        HorizontalStack band = new() { Spacing = 0, WidthDistribution = WidthDistribution.Weighted };
        band.AddChild(Cell(height), LabelWeight); // label gutter stays white
        for (int m = 1; m <= Months.Length; m++)
            band.AddChild(Cell(height, m % 2 == 0 ? Stripe : (Color?)null), 1);
        return band;
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
        row.AddChild(labelCell, LabelWeight);

        int cursor = 1;
        for (int i = 0; i < segs.Length; i++)
        {
            (int s, int e, string l) = segs[i];
            if (s > cursor) row.AddChild(new Spacer(BarHeight), s - cursor);
            row.AddChild(Bar(l, shades[i % 2]), e - s + 1);
            cursor = e + 1;
        }
        if (cursor <= Months.Length) row.AddChild(new Spacer(BarHeight), Months.Length - cursor + 1);

        return row;
    }

    // One roadmap row: the striped column grid (background) with the bars floating
    // on top (this feature Layers). Both layers share the same weights, so the bars sit over
    // their month columns and the stripes run continuously behind them.
    private static Container RoadmapRow(IElement labelCell, (int S, int E, string L)[] segs, Color[] shades)
    {
        Container c = new();
        c.Layers(l => l.Background(StripeBand(RowHeight)).PrimaryLayer(BarLayer(labelCell, segs, shades)));
        return c;
    }

    // Left label for the first row of a team: name + sub with a coloured left rail.
    private static Container TeamLabel(string name, string sub, Color accent)
    {
        VerticalStack v = new();
        v.AddChild(Tb(name, 9, Ink, bold: true));
        v.AddChild(Tb(sub, 7, Mute, top: 2));
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
