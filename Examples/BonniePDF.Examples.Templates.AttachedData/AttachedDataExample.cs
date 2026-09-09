using System.Text;
using BonniePDF.Attachments;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.AttachedData;

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
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A PDF/A-3 report that carries its source CSV inside the file, so a reader can check the numbers without re-keying them.")]
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

    public static Document Generate()
    {
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

        doc.Add(Tb("Quarterly revenue - 2026", 18, Ink, bold: true, bottom: 4));
        doc.Add(Tb("The source data for this report is embedded as quarterly-revenue.csv.", 9, Mute, bottom: 16));

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

        // ── Embed the source data as an attachment ────────────────
        doc.Attach(new FileAttachment
        {
            FileName = "quarterly-revenue.csv",
            Content = BuildCsv(),
            Relationship = AttachmentRelationship.Source,
            MediaType = "text/csv",
            Description = "Source data for the quarterly revenue report",
        });

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
            TextColor = color,
        };
        if (align.HasValue) t.Alignment = align.Value;
        if (bottom > 0) t.Margin = new Thickness(0, 0, 0, bottom);
        return t;
    }
}
