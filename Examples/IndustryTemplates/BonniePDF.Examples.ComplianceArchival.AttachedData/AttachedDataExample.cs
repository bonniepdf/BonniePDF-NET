using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.ComplianceArchival.AttachedData;

/// <summary>
/// A PDF/A-3 report that carries its underlying data as an embedded associated
/// file. The rendered table is the human-readable view; the same numbers are
/// attached as <c>quarterly-revenue.csv</c>
/// so downstream tools (or an auditor) can consume the exact source data
/// without re-keying it from the page. Demonstrates the generic
/// attachment API (as opposed to the Factur-X-specific binding).
///
/// <para>Validated in CI by veraPDF as PDF/A-3b.</para>
/// </summary>
[System.ComponentModel.DisplayName("Report with Embedded Data")]
[System.ComponentModel.Category("Compliance & Archival")]
[System.ComponentModel.Description(
    "A PDF/A-3 report that carries its source CSV inside the file: the figures as a table and a chart, and a note naming the attached file, so a reader can check the numbers without re-keying them.")]
public static class AttachedDataExample
{
    private static readonly Color Ink = ColorPalette.Slate[900];
    private static readonly Color BodyCol = ColorPalette.Slate[600];
    private static readonly Color Mute = ColorPalette.Slate[500];
    private static readonly Color Brand = ColorPalette.Emerald[600];

    private static readonly (string Quarter, int Revenue, int Customers)[] Rows =
    {
        ("Q1 2026", 412_000, 128),
        ("Q2 2026", 468_500, 141),
        ("Q3 2026", 501_200, 152),
        ("Q4 2026", 559_900, 167),
    };

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        #region settings

        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 48,
            Conformance = PdfAConformance.PdfA3b,
            Language = "en-GB",
            DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(BodyCol),
        });

        doc.Metadata.Title = "Quarterly revenue 2026";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "PDF/A-3 Report with Embedded Data";

        #endregion settings

        doc.Add(Tb("Quarterly revenue - 2026", 18, Ink, bold: true, bottom: 4));
        doc.Add(Tb("The source data for this report is embedded as quarterly-revenue.csv.", 9, Mute, bottom: 16));

        #region table

        Table table = new(2f, 2f, 2f) { Border = Border.None, CellPadding = 6 };
        // Real tabular data → a proper header row (emits TH cells for assistive tech).
        table.AddHeader(
            Tb("Quarter", 8, Mute, bold: true),
            Tb("Revenue (EUR)", 8, Mute, bold: true, align: TextAlignment.Right),
            Tb("Customers", 8, Mute, bold: true, align: TextAlignment.Right));
        foreach (var (quarter, revenue, customers) in Rows)
        {
            table.AddRow(
                Tb(quarter, 10, Ink),
                Tb(revenue.ToString("N0"), 10, BodyCol, align: TextAlignment.Right),
                Tb(customers.ToString(), 10, BodyCol, align: TextAlignment.Right));
        }

        doc.Add(table);

        #endregion table

        // The same figures as a chart, read from the same rows as the table and the attachment.
        ChartSeries series = new() { Color = ColorPalette.Indigo[600] };
        foreach (var (quarter, revenue, _) in Rows)
            series.DataPoints.Add(new ChartDataPoint { Label = quarter, Value = revenue / 1000m, LabelFormat = "N0" });
        doc.Add(new BarChart(new ChartData
        {
            Title = "Revenue by quarter (EUR thousand)",
            Series = [series],
            YAxis = new AxisOptions { MaxValue = 600, StepCount = 6 },
        })
        {
            Theme = "modern",
            AltText = "Bar chart of revenue by quarter in 2026, rising from 412 to 560 thousand euros.",
            Height = 240,
            StretchToFit = true,
            FontFamily = StandardFonts.Helvetica,
            LabelFontSize = 9,
            BackgroundColor = Color.White,
            Margin = new Thickness(0, 18, 0, 18),
        });

        // A note that names the attachment, so a reader knows the data is there to open.
        HorizontalStack note = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 12 };
        note.Add(new Container
        {
            BackgroundColor = ColorPalette.Indigo[100],
            CornerRadius = 4,
            Padding = new Thickness(6, 3, 6, 3),
            Child = Tb("ATTACHED", 7.5f, ColorPalette.Indigo[800], bold: true, align: TextAlignment.Center),
        }, 1);
        note.Add(new TextBlock
        {
            Text = $"quarterly-revenue.csv: {Rows.Length} rows, 3 columns. Open it from your PDF viewer's attachments panel to reuse the exact figures.",
            FontSize = 9,
            Color = Ink,
        }, 6);
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            CornerRadius = 6,
            Padding = new Thickness(14, 10, 14, 10),
            Child = note,
        });

        // ── Embed the source data as an attachment ────────────────

        #region attach

        doc.Attach(new FileAttachment
        {
            FileName = "quarterly-revenue.csv",
            Content = BuildCsv(),
            Relationship = AttachmentRelationship.Source,
            MediaType = "text/csv",
            Description = "Source data for the quarterly revenue report",
        });

        #endregion attach

        return doc;
    }

    private static byte[] BuildCsv()
    {
        StringBuilder sb = new();
        sb.Append("quarter,revenue_eur,customers\n");
        foreach (var (quarter, revenue, customers) in Rows)
            sb.Append(quarter).Append(',').Append(revenue).Append(',').Append(customers).Append('\n');
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static TextBlock Tb(string content, float size, Color color, bool bold = false,
        TextAlignment? align = null, float bottom = 0)
    {
        TextBlock t = new()
        {
            Text = content,
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (bottom > 0) t.Margin = new Thickness(0, 0, 0, bottom);
        return t;
    }
}
