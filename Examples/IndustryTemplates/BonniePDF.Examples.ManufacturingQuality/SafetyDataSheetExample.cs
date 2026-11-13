using BonniePDF;
using BonniePDF.Drawing;

namespace BonniePDF.Examples.ManufacturingQuality;

/// <summary>
/// A safety data sheet for a flammable cleaning product: all 16 sections in the order the regulation
/// sets, drawn hazard pictograms with the signal word, composition and exposure-limit tables, transport
/// details, and the product and page number on every page.
/// </summary>
[System.ComponentModel.DisplayName("Safety Data Sheet")]
[System.ComponentModel.Category("Manufacturing & Quality")]
[System.ComponentModel.Description("A safety data sheet for a flammable cleaning product: all 16 sections in the order the regulation sets, drawn hazard pictograms with the signal word, composition and exposure-limit tables, transport details, and the product and page number on every page.")]
public static class SafetyDataSheetExample
{
    private const string Product = "Sample Surface Cleaner IPA 70";

    private static readonly Color Ink = ColorPalette.Stone[900];
    private static readonly Color Body = ColorPalette.Stone[700];
    private static readonly Color Mute = ColorPalette.Stone[600];
    private static readonly Color Accent = ColorPalette.Orange[800];

    /// <summary>A block of section content: a paragraph, label and value pairs, or a table with a header row.</summary>
    private abstract record Block;

    private sealed record Paragraph(string Text) : Block;

    private sealed record Pairs((string Label, string Value)[] Items) : Block;

    private sealed record DataTable(float[] Columns, string[] Header, string[][] Rows) : Block;

    /// <summary>The 16 sections, in the order Annex II of the REACH regulation sets. Section 2 adds the pictograms.</summary>
    private static readonly (string Title, Block[] Blocks)[] Sections =
    [
        ("Identification of the substance or mixture and of the company",
        [
            new Pairs(
            [
                ("Product name", Product),
                ("Product code", "SC-IPA70-5L"),
                ("Use", "Professional cleaning and disinfection of hard surfaces. Not for use on skin."),
                ("Supplier", "Sample Industrial Supplies Ltd, Unit 9, Sample Park, Sample Town ST7 3CD"),
                ("Emergency telephone", "0344 892 0111 (24 hours, English)"),
            ]),
        ]),
        ("Hazards identification",
        [
            new Pairs(
            [
                ("Classification", "Flam. Liq. 2, H225  ·  Eye Irrit. 2, H319  ·  STOT SE 3, H336"),
                ("Hazard statements", "H225 Highly flammable liquid and vapour. H319 Causes serious eye irritation. H336 May cause drowsiness or dizziness."),
                ("Precautionary statements", "P210 Keep away from heat, hot surfaces, sparks, open flames and other ignition sources. No smoking. P233 Keep container tightly closed. P305+P351+P338 IF IN EYES: Rinse cautiously with water for several minutes. Remove contact lenses, if present and easy to do. Continue rinsing. P403+P235 Store in a well-ventilated place. Keep cool."),
            ]),
        ]),
        ("Composition and information on ingredients",
        [
            new DataTable(
                [2, 1, 1, 0.7f, 2],
                ["Substance", "CAS number", "EC number", "Content", "Classification"],
                [
                    ["Propan-2-ol (isopropyl alcohol)", "67-63-0", "200-661-7", "70%", "Flam. Liq. 2, H225; Eye Irrit. 2, H319; STOT SE 3, H336"],
                    ["Water", "7732-18-5", "231-791-2", "30%", "Not classified"],
                ]),
        ]),
        ("First aid measures",
        [
            new Pairs(
            [
                ("Inhalation", "Move to fresh air and keep at rest. Get medical advice if dizziness or drowsiness persists."),
                ("Skin contact", "Wash with plenty of water. Remove contaminated clothing."),
                ("Eye contact", "Rinse cautiously with water for at least 15 minutes, holding the eyelids open. Get medical advice if irritation persists."),
                ("Ingestion", "Rinse the mouth. Do not induce vomiting. Get medical advice."),
            ]),
        ]),
        ("Firefighting measures",
        [
            new Pairs(
            [
                ("Extinguishing media", "Alcohol-resistant foam, carbon dioxide, dry powder or water spray. Do not use a water jet."),
                ("Special hazards", "Vapour is heavier than air, spreads along floors and can travel to a source of ignition and flash back."),
                ("Advice for firefighters", "Wear self-contained breathing apparatus. Cool closed containers with water spray."),
            ]),
        ]),
        ("Accidental release measures",
        [
            new Paragraph("Remove all sources of ignition and ventilate the area. Contain the spill with sand or another non-combustible absorbent and collect it in a closed container for disposal. Do not let the product enter drains."),
        ]),
        ("Handling and storage",
        [
            new Pairs(
            [
                ("Handling", "Use in a well-ventilated area. Keep away from ignition sources and take precautions against static discharge."),
                ("Storage", "Store in the original container, tightly closed, in a cool, well-ventilated flammables store below 30 °C."),
            ]),
        ]),
        ("Exposure controls and personal protection",
        [
            new DataTable(
                [2, 1.2f, 1.2f, 1.6f],
                ["Substance", "Long-term limit (8 h)", "Short-term limit (15 min)", "Source"],
                [
                    ["Propan-2-ol", "400 ppm, 999 mg/m³", "500 ppm, 1,250 mg/m³", "UK workplace exposure limit (EH40)"],
                ]),
            new Pairs(
            [
                ("Eye protection", "Safety glasses with side shields (EN 166)."),
                ("Hand protection", "Nitrile gloves, at least 0.4 mm thick, breakthrough time over 480 minutes (EN ISO 374)."),
                ("Respiratory protection", "Not normally needed with good ventilation. In confined spaces, a type A filter (EN 14387)."),
            ]),
        ]),
        ("Physical and chemical properties",
        [
            new DataTable(
                [1.4f, 2],
                ["Property", "Value"],
                [
                    ["Appearance", "Clear, colourless liquid"],
                    ["Odour", "Alcohol"],
                    ["Flash point", "21 °C (closed cup)"],
                    ["Boiling point", "About 82 °C"],
                    ["Relative density", "0.87 at 20 °C"],
                    ["Solubility in water", "Completely miscible"],
                    ["Explosive limits in air", "2.0 to 12.7% by volume"],
                ]),
        ]),
        ("Stability and reactivity",
        [
            new Paragraph("Stable under normal conditions. Avoid heat, sparks and open flames. Incompatible with strong oxidising agents, acids and alkali metals. Hazardous decomposition products in a fire: carbon monoxide and carbon dioxide."),
        ]),
        ("Toxicological information",
        [
            new Paragraph("Propan-2-ol: oral LD50 (rat) 5,045 mg/kg; dermal LD50 (rabbit) 12,800 mg/kg. Causes serious eye irritation. Vapour in high concentration may cause drowsiness and dizziness."),
        ]),
        ("Ecological information",
        [
            new Paragraph("Readily biodegradable. Low potential to bioaccumulate. Not classified as hazardous to the aquatic environment."),
        ]),
        ("Disposal considerations",
        [
            new Paragraph("Dispose of the product and contaminated packaging as hazardous waste through a licensed contractor (European Waste Catalogue code 07 06 04*). Empty containers may hold flammable vapour."),
        ]),
        ("Transport information",
        [
            new Pairs(
            [
                ("UN number", "UN 1219"),
                ("Proper shipping name", "ISOPROPANOL (ISOPROPYL ALCOHOL)"),
                ("Transport hazard class", "3 (flammable liquid)"),
                ("Packing group", "II"),
                ("Environmental hazards", "None"),
            ]),
        ]),
        ("Regulatory information",
        [
            new Paragraph("This safety data sheet is prepared under UK REACH and Regulation (EC) No 1907/2006, Annex II, as amended. Classification under Regulation (EC) No 1272/2008 (CLP) and its UK equivalent."),
        ]),
        ("Other information",
        [
            new Paragraph("Revision 3 replaces revision 2 of 12 January 2025. Changes: section 8 exposure limits and section 14 transport details updated. The information above is based on our present knowledge and describes the product for safety purposes only."),
        ]),
    ];

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Settings = new DocumentSettings
            {
                PageSize = PdfSize.A4,
                Margin = 40,
                Conformance = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(8.5f).WithColor(Body),
                FooterTemplate = PageTemplate.FromElements(@default: Footer()),
            },
            Metadata = new DocumentMetadata
            {
                Title = $"Safety data sheet: {Product}",
                Author = "Sample Industrial Supplies Ltd",
                Subject = "Safety data sheet under UK REACH and Regulation (EC) No 1907/2006, Annex II",
            },
        };

        // ── Title block ──
        HorizontalStack top = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 16, Margin = new Thickness(0, 0, 0, 14) };
        VerticalStack title = new() { Spacing = 3 };
        title.Add(Tb("SAFETY DATA SHEET", 8, Accent, bold: true));
        title.Add(new Heading(Product) { Level = 1, FontName = StandardFonts.HelveticaBold, FontSize = 20, Color = Ink, Margin = new Thickness(0) });
        title.Add(Tb("According to UK REACH and Regulation (EC) No 1907/2006, Annex II", 8.5f, Mute));
        top.Add(title, 1);
        VerticalStack revision = new() { Spacing = 2 };
        revision.Add(Tb("Revision 3", 9, Ink, bold: true, align: TextAlignment.Right));
        revision.Add(Tb("Issued 5 November 2026", 8, Mute, align: TextAlignment.Right));
        revision.Add(Tb("Replaces 12 January 2025", 8, Mute, align: TextAlignment.Right));
        top.Add(revision);
        doc.Add(top);

        // ── The 16 sections ──
        // Each heading is kept on the same page as the first block beneath it.
        for (int i = 0; i < Sections.Length; i++)
        {
            var (sectionTitle, blocks) = Sections[i];
            doc.Add(SectionHeading(i + 1, sectionTitle));
            if (i == 1) doc.Add(HazardBanner());
            foreach (Block block in blocks) doc.Add(Render(block));
        }
        return doc;
    }

    // Local helpers used by this document.

    /// <summary>Text in Helvetica or Helvetica Bold.</summary>
    private static TextBlock Tb(string text, float size, Color color, bool bold = false, TextAlignment? align = null)
    {
        TextBlock block = new(text)
        {
            FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
            FontSize = size,
            Color = color,
        };
        if (align.HasValue) block.Alignment = align.Value;
        return block;
    }

    /// <summary>The footer: the product and revision on the left, the page number on the right.</summary>
    private static HorizontalStack Footer()
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(Tb($"{Product}  ·  Safety data sheet, revision 3, 5 November 2026", 7, Mute), 3);
        row.Add(PageNumber.Text().FontSize(7).Color(Mute).AlignRight(), 1);
        return row;
    }

    /// <summary>A numbered section heading on a tinted band.</summary>
    private static Container SectionHeading(int number, string title) => new()
    {
        KeepWithNext = true,
        BackgroundColor = ColorPalette.Orange[50],
        Border = new Border(3, 0, 0, 0),
        BorderColors = new BorderColors(Accent),
        Padding = new Thickness(8, 3.5f, 8, 3.5f),
        Margin = new Thickness(0, 7, 0, 4),
        Child = new Heading($"Section {number}: {title}")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9.5f,
            Color = ColorPalette.Orange[900],
            Margin = new Thickness(0),
        },
    };

    /// <summary>One block of section content.</summary>
    private static IElement Render(Block block)
    {
        switch (block)
        {
            case Paragraph paragraph:
                return Tb(paragraph.Text, 8.5f, Body);
            case Pairs pairs:
            {
                Grid grid = new(1, 3) { CellPadding = 2, Margin = new Thickness(0, 0, 0, 2) };
                foreach (var (label, value) in pairs.Items) grid.AddRow(Tb(label, 8.5f, Mute, bold: true), Tb(value, 8.5f, Body));
                return grid;
            }
            case DataTable data:
            {
                Table table = new(data.Columns)
                {
                    CellPadding = 3,
                    Border = new Border(0, 0, 0, 0.5f),
                    BorderColors = new BorderColors(ColorPalette.Stone[200]),
                    HeaderBackgroundColor = ColorPalette.Stone[100],
                    HeaderTextColor = Ink,
                    Margin = new Thickness(0, 0, 0, 6),
                };
                table.AddHeader(data.Header.Select(text => (IElement)Tb(text, 7.5f, Ink, bold: true)).ToArray());
                foreach (string[] row in data.Rows) table.AddRow(row.Select(text => (IElement)Tb(text, 8.5f, Body)).ToArray());
                return table;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(block));
        }
    }

    /// <summary>The label elements of section 2: two pictograms and the signal word.</summary>
    private static HorizontalStack HazardBanner()
    {
        HorizontalStack banner = new() { KeepWithNext = true, WidthDistribution = WidthDistribution.Auto, Spacing = 10, Margin = new Thickness(0, 0, 0, 6) };
        banner.Add(Pictogram(flame: true, "Hazard pictogram GHS02, a flame: flammable"));
        banner.Add(Pictogram(flame: false, "Hazard pictogram GHS07, an exclamation mark: irritant and narcotic effects"));
        VerticalStack signal = new() { Spacing = 2, Margin = new Thickness(0, 8, 0, 0) };
        signal.Add(Tb("SIGNAL WORD", 6.5f, Mute, bold: true));
        signal.Add(Tb("Danger", 16, ColorPalette.Rose[700], bold: true));
        banner.Add(signal);
        return banner;
    }

    /// <summary>A hazard pictogram: a red-bordered diamond with a black symbol.</summary>
    private static CanvasElement Pictogram(bool flame, string altText) => new((canvas, width, _) => DrawPictogram(canvas, width, flame))
    {
        DrawWidth = 46,
        DrawHeight = 46,
        AltText = altText,
    };

    /// <summary>Draws a pictogram in a square of the given side.</summary>
    private static void DrawPictogram(ICanvas canvas, float side, bool flame)
    {
        float c = side / 2;
        void Diamond(float r, Color color) =>
            canvas.FillPath([new Point(c, c - r), new Point(c + r, c), new Point(c, c + r), new Point(c - r, c)], color);
        Diamond(c, ColorPalette.Red[600]);
        Diamond(c - 4, Color.White);
        Color black = Color.Black;
        if (!flame)
        {
            canvas.FillRect(c - 2, c - 11, 4, 14, black);
            canvas.FillCircle(c, c + 8, 2.4f, black);
            return;
        }
        // A stylised flame over a base bar.
        canvas.FillPath(
        [
            new Point(c - 7, c + 8),
            new Point(c - 9, c + 1),
            new Point(c - 5, c - 5),
            new Point(c - 3, c - 1),
            new Point(c - 1, c - 12),
            new Point(c + 4, c - 4),
            new Point(c + 6, c - 7),
            new Point(c + 9, c + 1),
            new Point(c + 7, c + 8),
        ],
            black);
        canvas.FillRect(c - 9, c + 10, 18, 2.5f, black);
    }
}
