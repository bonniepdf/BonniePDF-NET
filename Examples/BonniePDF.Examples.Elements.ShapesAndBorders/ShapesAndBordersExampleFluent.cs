using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.ShapesAndBorders;

/// <summary>
/// Demonstrates rounded rectangles (corner radius) on containers and bordered elements
/// using the Fluent API.
/// All sections use chained extension methods - no property-initialiser syntax.
/// The card layout uses doc.Page → col.Item().Row(row => { row.RelativeItem(1)... })
/// with each card built inline.
/// </summary>
[System.ComponentModel.DisplayName("Shapes & Borders (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Drawing basic shapes and styling borders using the Fluent API.")]
public static class ShapesAndBordersExampleFluent
{
    /// <summary>Corner radii compared in the radius demonstration section.</summary>
    private static readonly int[] Radii = [0, 4, 8, 12, 20, 30];

    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title  = "Rounded Rectangles (Fluent API)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize    = PdfSize.A4;
                settings.Margin      = 50;
                settings.Version     = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12));

            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    // ── Title ──────────────────────────────────────────────────
                    col.Item()
                        .Text("Rounded Rectangles (Fluent API)")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20);

                    // ── Section 1: Basic Rounded Containers ────────────────────
                    SectionHeader(col, "Basic Rounded Containers");

                    col.Item()
                        .Padding(16)
                        .Background(Color.Blue50)
                        .CornerRadius(8)
                        .Margin(0, 0, 0, 12)
                        .Text("A container with 8pt corner radius and a light blue background.")
                        .TextColor(Color.Blue800);

                    col.Item()
                        .Padding(20)
                        .Background(Color.Green50)
                        .CornerRadius(16)
                        .Margin(0, 0, 0, 12)
                        .Text("A container with a larger 16pt corner radius. The arcs become more pronounced as the radius increases.")
                        .TextColor(Color.Green800);

                    // ── Section 2: Rounded Borders ────────────────────────────
                    SectionHeader(col, "Rounded Borders");

                    col.Item()
                        .Padding(16)
                        .Border(2, Color.Indigo500)
                        .CornerRadius(10)
                        .Margin(0, 0, 0, 12)
                        .Text("A rounded border with no background fill. The stroke follows the curved path.")
                        .TextColor(Color.Slate700);

                    col.Item()
                        .Padding(16)
                        .Background(Color.FromHex("#fef3c7"))
                        .Border(2, Color.FromHex("#f59e0b"))
                        .CornerRadius(10)
                        .Margin(0, 0, 0, 12)
                        .Text("Rounded border combined with a background fill - great for alert boxes and callouts.")
                        .TextColor(Color.FromHex("#92400e"));

                    // ── Section 3: Card Layout ────────────────────────────────
                    SectionHeader(col, "Card Layout");

                    col.Item()
                        .Margin(0, 0, 0, 12)
                        .Row(row =>
                        {
                            row.Spacing(12);

                            // Card A
                            row.RelativeItem(1)
                                .Padding(14)
                                .Background(Color.Blue50)
                                .Border(1, Color.Blue500)
                                .CornerRadius(10)
                                .Column(card =>
                                {
                                    card.Item()
                                        .Text("Feature A")
                                        .Font(StandardFonts.HelveticaBold)
                                        .FontSize(14)
                                        .TextColor(Color.Blue500)
                                        .Margin(0, 0, 0, 6);
                                    card.Item()
                                        .Text("Rounded corners make UI elements feel modern and approachable.")
                                        .FontSize(10)
                                        .TextColor(Color.Slate600);
                                });

                            // Card B
                            row.RelativeItem(1)
                                .Padding(14)
                                .Background(Color.FromHex("#faf5ff"))
                                .Border(1, Color.Purple500)
                                .CornerRadius(10)
                                .Column(card =>
                                {
                                    card.Item()
                                        .Text("Feature B")
                                        .Font(StandardFonts.HelveticaBold)
                                        .FontSize(14)
                                        .TextColor(Color.Purple500)
                                        .Margin(0, 0, 0, 6);
                                    card.Item()
                                        .Text("Bezier curves approximate perfect quarter-circle arcs.")
                                        .FontSize(10)
                                        .TextColor(Color.Slate600);
                                });

                            // Card C
                            row.RelativeItem(1)
                                .Padding(14)
                                .Background(Color.FromHex("#f0fdfa"))
                                .Border(1, Color.Teal500)
                                .CornerRadius(10)
                                .Column(card =>
                                {
                                    card.Item()
                                        .Text("Feature C")
                                        .Font(StandardFonts.HelveticaBold)
                                        .FontSize(14)
                                        .TextColor(Color.Teal500)
                                        .Margin(0, 0, 0, 6);
                                    card.Item()
                                        .Text("Works with backgrounds, borders, or both combined.")
                                        .FontSize(10)
                                        .TextColor(Color.Slate600);
                                });
                        });

                    // ── Section 4: Fluent API Chaining ────────────────────────
                    SectionHeader(col, "Fluent API Chaining");

                    col.Item()
                        .Padding(16)
                        .Background(Color.FromHex("#ede9fe"))
                        .CornerRadius(12)
                        .Border(1, Color.FromHex("#8b5cf6"))
                        .Text("This element uses chained fluent extensions: .Padding().Background().CornerRadius().Border().Text()")
                        .FontSize(12)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 8);

                    col.Item()
                        .Text("Chaining: col.Item().Padding(16).Background(...).CornerRadius(12).Border(...).Text(...)")
                        .FontSize(11)
                        .TextColor(Color.Slate500)
                        .Margin(0, 0, 0, 12);

                    // ── Section 5: Radius Comparison ──────────────────────────
                    SectionHeader(col, "Radius Comparison");

                    foreach (int r in Radii)
                    {
                        col.Item()
                            .Padding(12, 8, 12, 8)
                            .Background(Color.Slate100)
                            .Border(1, Color.Slate400)
                            .CornerRadius(r)
                            .Margin(0, 0, 0, 6)
                            .Text(r == 0 ? "CornerRadius = 0 (sharp corners, default)" : $"CornerRadius = {r}")
                            .FontSize(11)
                            .TextColor(Color.Slate700);
                    }

                    // ── Section 6: Practical Examples ─────────────────────────
                    col.Item().Spacer(16);
                    col.Item().Line(1, Color.Slate300);
                    SectionHeader(col, "Practical Examples");

                    // Success
                    col.Item()
                        .Padding(16, 12, 16, 12)
                        .Background(Color.FromHex("#dcfce7"))
                        .Border(1, Color.Green600)
                        .CornerRadius(8)
                        .Margin(0, 0, 0, 8)
                        .Text("✓  Success! Your document has been generated successfully.")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(12)
                        .TextColor(Color.Green800);

                    // Warning
                    col.Item()
                        .Padding(16, 12, 16, 12)
                        .Background(Color.FromHex("#fef9c3"))
                        .Border(1, Color.FromHex("#ca8a04"))
                        .CornerRadius(8)
                        .Margin(0, 0, 0, 8)
                        .Text("⚠  Warning: Please review the document before sending.")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(12)
                        .TextColor(Color.FromHex("#854d0e"));

                    // Error
                    col.Item()
                        .Padding(16, 12, 16, 12)
                        .Background(Color.FromHex("#fee2e2"))
                        .Border(1, Color.FromHex("#991b1b"))
                        .CornerRadius(8)
                        .Margin(0, 0, 0, 8)
                        .Text("✕  Error: Unable to process the request. Please try again.")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(12)
                        .TextColor(Color.FromHex("#991b1b"));

                    // Info
                    col.Item()
                        .Padding(16, 12, 16, 12)
                        .Background(Color.FromHex("#dbeafe"))
                        .Border(1, Color.Blue600)
                        .CornerRadius(8)
                        .Margin(0, 0, 0, 8)
                        .Text("ℹ  Info: Rounded corners are built from four Bezier arcs using the k=0.5523 approximation constant.")
                        .TextColor(Color.Blue800);
                });
            });

        }).Build();

    }

    /// <summary>Adds an Indigo600 bold section header at 14pt.</summary>
    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item()
            .Text(title)
            .Font(StandardFonts.HelveticaBold)
            .FontSize(14)
            .TextColor(Color.Indigo600)
            .Margin(0, 12, 0, 6);
    }
}

