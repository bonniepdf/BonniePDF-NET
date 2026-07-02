using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using static BonniePDF.Examples.Templates.RegionalInvoices.InvoiceKit;

namespace BonniePDF.Examples.Templates.RegionalInvoices;

/// <summary>
/// EU cross-border e-invoice (PDF/A-3, EN 16931 layout). EU e-invoicing
/// (EN 16931, and its Factur-X / ZUGFeRD / XRechnung profiles) pairs a
/// human-readable PDF with a machine-readable XML payload inside a single
/// PDF/A-3 file. This renders the human-readable half in that idiom: a PDF/A-3b
/// container, both parties' VAT identifiers, an intra-Community reverse-charge
/// notice (Art. 196 of the EU VAT Directive - the recipient self-accounts for
/// VAT, so the supplier charges 0%), IBAN/BIC remittance, and EUR amounts.
///
/// NOTE ON THE XML HALF: embedding the structured EN 16931 XML as a PDF/A-3
/// associated file needs an embedded-file / associated-file API the engine does
/// not yet expose (tracked internally as a library gap). Until it lands, the XML
/// is represented here by a visible placeholder panel rather than a real
/// attachment, so the example never over-claims a conformance it cannot emit.
/// </summary>
[System.ComponentModel.DisplayName("EU E-Invoice (PDF/A-3, EN 16931)")]
[System.ComponentModel.Category("Invoices")]
[System.ComponentModel.Description("EU cross-border e-invoice in the EN 16931 / Factur-X idiom: PDF/A-3b container, both VAT IDs, intra-Community reverse charge, IBAN/BIC, EUR. Structured XML shown as a placeholder.")]
public static class EuEInvoiceExample
{
    private static readonly Color Accent = ColorPalette.Blue[800];
    private const string Eur = "€";
    private const string Nf = "de-DE";

    private static readonly InvoiceItem[] Items =
    [
        new("Software licences (annual)", 50, 120m, "50 seats"),
        new("Implementation services", 15, 720m, "Consultant day rate"),
        new("Data migration", 1, 3400m, "Fixed scope"),
    ];

    public static Document Generate()
    {
        DateTime now = DateTime.Now;
        int year = now.Year;
        string date = now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo(Nf));

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        // PDF/A-3b is the archival container profile used by hybrid e-invoices.
        doc.Settings.Conformance = PdfAConformance.PdfA3b;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(Ink);

        doc.Metadata.Title = $"E-Invoice INV-EU-{year}-2049";
        doc.Metadata.Author = "Nordlicht Software GmbH";
        doc.Metadata.Subject = "EU cross-border e-invoice (PDF/A-3, EN 16931 layout) with intra-Community reverse charge.";

        VerticalStack root = new();
        root.AddChild(AccentBar(Accent));

        // ── Header ────────────────────────────────────────────────────────────
        VerticalStack seller = new();
        seller.AddChild(Cell("Nordlicht Software GmbH", size: 16, bold: true, color: Ink, margin: new Thickness(0, 0, 0, 2)));
        AddLines(seller, Muted, "Speicherstadt 12", "20457 Hamburg", "Germany");

        seller.AddChild(Cell("USt-IdNr. DE 811 204 471", bold: true, color: Accent, margin: new Thickness(0, 6, 0, 0)));

        VerticalStack titleStack = new();
        titleStack.AddChild(new Heading("E-INVOICE")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Accent,
            Alignment = TextAlignment.Right,
        });
        titleStack.AddChild(Cell("EN 16931 · Factur-X profile", size: 8, color: Muted, align: TextAlignment.Right, margin: new Thickness(0, 2, 0, 0)));
        titleStack.AddChild(Cell($"INV-EU-{year}-2049", color: Muted, align: TextAlignment.Right));

        Table header = new(1.3f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 18) };
        header.AddRow(new Container { Child = seller }, new Container { Child = titleStack });
        root.AddChild(header);

        // ── Buyer + details ─────────────────────────────────────────────────
        VerticalStack details = new();
        details.AddChild(TotalLine("Invoice date", date));
        details.AddChild(TotalLine("Delivery date", date));
        details.AddChild(TotalLine("Buyer reference", "LUM-PO-5521"));
        details.AddChild(TotalLine("Terms", "14 days net"));

        Table meta = new(1f, 1f)
        {
            CellPadding = 12,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            Margin = new Thickness(0, 0, 0, 18),
        };
        meta.AddRow(
            Labelled("BUYER", Lines(Muted, "Lumière Analytique SAS", "24 Rue de la Paix", "75002 Paris", "France", "TVA FR 32 552118430"), Accent),
            Labelled("DETAILS", details, Accent));
        root.AddChild(meta);

        // ── Line items ──────────────────────────────────────────────────────
        Table table = new(3f, 0.7f, 1f, 1f)
        {
            CellPadding = 8,
            Border = Border.None,
            HeaderBackgroundColor = HeaderBg,
            HeaderTextColor = Ink,
        };
        table.AddHeader(
            Cell("Description", bold: true),
            Cell("Qty", bold: true, align: TextAlignment.Center),
            Cell("Unit price", bold: true, align: TextAlignment.Right),
            Cell("Net amount", bold: true, align: TextAlignment.Right));

        decimal net = 0m;
        foreach (InvoiceItem it in Items)
        {
            decimal amount = it.Qty * it.UnitPrice;
            net += amount;
            VerticalStack desc = new();
            desc.AddChild(Cell(it.Description));
            if (it.Detail is not null)
            {
                desc.AddChild(Cell(it.Detail, size: 8, color: Muted));
            }

            table.AddRow(
                new Container { Child = desc },
                Cell(it.Qty.ToString(CultureInfo.InvariantCulture), align: TextAlignment.Center),
                Cell(Money(it.UnitPrice, Eur, Nf), align: TextAlignment.Right),
                Cell(Money(amount, Eur, Nf), align: TextAlignment.Right));
        }

        root.AddChild(table);
        root.AddChild(Divider(Rule, 0.5f));

        // ── Totals: reverse charge → VAT is 0, buyer self-accounts ──────────
        VerticalStack totals = new();
        totals.AddChild(TotalLine("Total net", Money(net, Eur, Nf)));
        totals.AddChild(TotalLine("VAT (reverse charge)", Money(0m, Eur, Nf)));
        totals.AddChild(Divider(Ink, 0.6f));
        totals.AddChild(TotalLine("Total due (EUR)", Money(net, Eur, Nf), bold: true, size: 13, color: Accent));

        Container totalsBox = new()
        {
            Child = totals,
            Padding = new Thickness(14, 12, 14, 12),
            BackgroundColor = ColorPalette.Blue[50],
            CornerRadius = 8,
        };
        HorizontalStack totalsRow = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 6, 0, 0) };
        totalsRow.AddChild(new Container(), 1);
        totalsRow.AddChild(totalsBox, 1);
        root.AddChild(totalsRow);

        // ── Reverse-charge legal notice ─────────────────────────────────────
        root.AddChild(new Container
        {
            BackgroundColor = ColorPalette.Amber[50],
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(ColorPalette.Amber[500]),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 20, 0, 0),
            Child = Cell(
                "Reverse charge: VAT to be accounted for by the recipient under Article 196 of "
                    + "Council Directive 2006/112/EC. Intra-Community supply of services - supplier charges 0% VAT.",
                size: 8.5f,
                color: ColorPalette.Amber[800]),
        });

        // ── Remittance + XML placeholder ────────────────────────────────────
        VerticalStack xmlStack = new();
        xmlStack.AddChild(Cell("STRUCTURED DATA", bold: true, size: 8, color: Muted, margin: new Thickness(0, 0, 0, 4)));
        xmlStack.AddChild(Cell("factur-x.xml (EN 16931) travels inside this PDF/A-3 as an", size: 8, color: Muted));
        xmlStack.AddChild(Cell("associated file. Attachment API pending — see strategy doc.", size: 8, color: Muted));

        Container payCell = Labelled(
            "PAYMENT",
            Lines(Muted, "IBAN: DE89 2007 0000 0081 1204 71", "BIC: DEUTDEHH", "Deutsche Bank, Hamburg"),
            Accent);
        payCell.Padding = new Thickness(0, 0, 20, 0);

        Table bottom = new(1.1f, 1f) { CellPadding = 0, Border = Border.None, Margin = new Thickness(0, 16, 0, 0) };
        bottom.AddRow(
            payCell,
            new Container { BackgroundColor = HeaderBg, CornerRadius = 6, Padding = new Thickness(12, 10, 12, 10), Child = xmlStack });
        root.AddChild(bottom);

        doc.Add(root);
        return doc;
    }
}
