using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.Quotation;

/// <summary>
/// A B2B sales quotation. A quotation is not an invoice with a different
/// heading: an invoice states one settled figure, while a quotation presents a
/// choice and then asks for a decision. This document is built around that
/// difference — a three-tier pricing comparison instead of a flat line-item
/// table (with the recommended tier visually promoted), a numbered scope of
/// work, a bulleted assumptions list, a validity band, and an acceptance block
/// with real ruled signature lines. Output is PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Quotation")]
[System.ComponentModel.Category("Sales")]
[System.ComponentModel.Description("A B2B sales quotation built around a three-tier pricing comparison rather than a flat line-item table, with a numbered scope of work, priced optional add-ons, a validity band, and an acceptance block with ruled signature fields. PDF/A-2a + PDF/UA-1.")]
public static class QuotationExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Line = ColorPalette.Slate[200];
    private static readonly Color Panel = ColorPalette.Slate[50];
    private static readonly Color Accent = ColorPalette.Violet[600];
    private static readonly Color AccentSoft = ColorPalette.Violet[50];
    private static readonly Color AccentDeep = ColorPalette.Violet[700];

    private const float TierCardHeight = 178f;
    private const float TierPad = 12f;
    private const float TierBorderOn = 1.5f;
    private const float TierBorderOff = 0.75f;

    // ── Deterministic sample fixture ──────────────────────────────────────────
    private static readonly QuotationData Sample = new(
        new Supplier("Northwind Document Systems Ltd", "12 Harbour Square, Bristol BS1 4QA, United Kingdom", "proposals@example.com", "Company No. 09876543 · VAT GB 123 4567 89"),
        new Customer("Meridian Logistics plc", "Sample Recipient, Head of Platform", "Ashford Distribution Park, Kent TN23 7HL"),
        new Reference("QTE-2026-0417", "30 August 2026", "29 September 2026", "Sample Account Manager"),
        Scope:
        [
            "Discovery workshop covering current document templates, volumes, and retention obligations.",
            "Implementation of the document generation service in your existing deployment pipeline.",
            "Migration of eleven existing templates, with archival and accessible output enabled.",
            "Two rounds of review against your brand and accessibility guidelines.",
            "Handover, runbook, and a recorded walkthrough for your platform team.",
        ],
        Tiers:
        [
            new QuoteTier("Essential", "Single team, standard templates", 1450m, "per month",
            [
                "Up to 3 templates",
                "50,000 documents / month",
                "Business-hours support",
                "Archival output",
            ]),
            new QuoteTier("Professional", "Multi-team, accessibility included", 2900m, "per month",
            [
                "Up to 15 templates",
                "250,000 documents / month",
                "Priority support, 4-hour response",
                "Archival and accessible output",
                "Quarterly template review",
            ], Recommended: true),
            new QuoteTier("Enterprise", "Unlimited scale, dedicated support", 5600m, "per month",
            [
                "Unlimited templates",
                "Unmetered document volume",
                "Named support engineer",
                "Archival and accessible output",
                "Annual on-site review",
            ]),
        ],
        AddOns:
        [
            new QuoteAddOn("Digital signing", "Signed and timestamped output", 480m, "per month"),
            new QuoteAddOn("Template design", "Bespoke layout, per template", 1200m, "one-off"),
            new QuoteAddOn("Extended retention", "Seven-year archive tier", 260m, "per month"),
        ],
        Assumptions:
        [
            "Prices exclude VAT, which is charged at the prevailing rate on invoice.",
            "Volumes are measured monthly and averaged across the billing quarter.",
            "Existing templates are supplied as source files; recreating from scanned output is quoted separately.",
            "Implementation assumes access to a staging environment within ten working days of signature.",
        ],
        Locale: "en-GB",
        Currency: "GBP");

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Portrait, 40)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"{Sample.Reference.QuoteNumber} · Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7f,
                TextColor = Faint,
                Alignment = TextAlignment.Center,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9f).WithColor(Ink);

        return BuildDocument(Sample, settings);
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(QuotationData data, DocumentSettings settings)
    {
        Document doc = new(settings);
        doc.Metadata.Title = $"Quotation {data.Reference.QuoteNumber} — {data.Customer.Company}";
        doc.Metadata.Author = data.Supplier.Name;
        doc.Metadata.Subject = "Commercial quotation with tiered pricing options (fictional sample data).";

        doc.Add(Masthead(data));
        doc.Add(ReferenceStrip(data));
        doc.Add(ScopeSection(data));
        doc.Add(TierColumns(data));
        doc.Add(AddOnTable(data));
        doc.Add(ValidityBand(data));
        doc.Add(AssumptionsSection(data));
        doc.Add(AcceptanceBlock(data));
        return doc;
    }

    // ── Section builders (top to bottom) ──────────────────────────────────────

    private static VerticalStack Masthead(QuotationData data)
    {
        VerticalStack identity = new() { Spacing = 2 };
        identity.Add(new Heading(data.Supplier.Name)
        {
            Level = 1,
            TocIdentifier = "quotation",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 15f,
            TextColor = Accent,
        });
        identity.Add(Text(data.Supplier.Address, size: 7.5f, color: Muted));
        identity.Add(Text(data.Supplier.Registration, size: 7.5f, color: Faint));

        VerticalStack marker = new() { Spacing = 2 };
        marker.Add(Text("QUOTATION", bold: true, size: 15, align: TextAlignment.Right));
        marker.Add(Text(data.Reference.QuoteNumber, bold: true, size: 9, color: Muted, align: TextAlignment.Right));

        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(identity, 1);
        row.Add(marker, 1);

        VerticalStack stack = new();
        stack.Add(row);
        stack.Add(new Container { Height = 2, BackgroundColor = Accent, Margin = new Thickness(0, 8, 0, 12) });
        return stack;
    }

    private static Container ReferenceStrip(QuotationData data)
    {
        IElement[] fields =
        [
            Field("Prepared for", data.Customer.Company),
            Field("Attention", data.Customer.Contact),
            Field("Issued", data.Reference.Issued),
            Field("Valid until", data.Reference.ValidUntil),
            Field("Account manager", data.Reference.Owner),
        ];

        float[] columns = new float[fields.Length];
        Array.Fill(columns, 1f);
        Grid grid = new(columns) { CellPadding = 5, Border = Border.None };
        grid.AddRow(fields);

        return new Container
        {
            BackgroundColor = Panel,
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(0, 0, 0, 14),
            Child = grid,
        };
    }

    /// <summary>The numbered deliverables. An ordered list, not a table — these are steps.</summary>
    private static VerticalStack ScopeSection(QuotationData data)
    {
        OrderedList list = new()
        {
            NumberFormat = NumberFormat.Decimal,
            FontName = StandardFonts.Helvetica,
            FontSize = 9f,
            TextColor = Ink,
            ItemSpacing = 4,
        };
        foreach (string item in data.Scope)
        {
            list.AddItem(item);
        }

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionTitle("Scope of work", "scope"));
        stack.Add(list);
        return stack;
    }

    /// <summary>
    /// The centrepiece: three tiers side by side. The recommended tier gets the
    /// accent border, a filled background, and a badge, so the eye lands on it
    /// first — the visual grammar every SaaS pricing page uses, applied to a
    /// printed quote.
    /// </summary>
    private static VerticalStack TierColumns(QuotationData data)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 10 };

        foreach (QuoteTier tier in data.Tiers)
        {
            bool promoted = tier.Recommended;

            // A fixed-height slot, filled only on the promoted tier. Reserving
            // the space unconditionally keeps all three tier names on the same
            // baseline; rendering an invisible label would tie alignment to a colour.
            Container badge = new() { Height = 9 };
            if (promoted)
            {
                badge.Child = Text("RECOMMENDED", bold: true, size: 6.5f, color: AccentDeep);
            }

            VerticalStack header = new() { Spacing = 2 };
            header.Add(badge);
            header.Add(Text(tier.Name, bold: true, size: 13, color: promoted ? AccentDeep : Ink));
            header.Add(Text(tier.Tagline, size: 7.5f, color: Muted));

            VerticalStack price = new() { Spacing = 0, Margin = new Thickness(0, 8, 0, 8) };
            price.Add(Text(Money(tier.Price, data), bold: true, size: 20, color: promoted ? AccentDeep : Ink));
            price.Add(Text(tier.Cadence, size: 7.5f, color: Muted));

            UnorderedList features = new()
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 8f,
                TextColor = Ink,
                BulletColor = promoted ? Accent : Muted,
                ItemSpacing = 3.5f,
            };
            foreach (string item in tier.Includes)
            {
                features.AddItem(item);
            }

            VerticalStack body = new();
            body.Add(header);
            body.Add(price);
            body.Add(features);

            // A child's origin is border + padding, so the promoted card's
            // heavier border would otherwise shift its whole column down and
            // right against its neighbours. Give back exactly what it takes.
            float pad = promoted ? TierPad - (TierBorderOn - TierBorderOff) : TierPad;

            Container card = new()
            {
                // A pricing comparison only reads as a comparison when the
                // columns line up, so the cards share one height rather than
                // each shrinking to its own feature count.
                Height = TierCardHeight,
                Border = new Border(promoted ? TierBorderOn : TierBorderOff),
                BorderColors = new BorderColors(promoted ? Accent : Line),
                Padding = new Thickness(pad, pad, pad, pad),
                Child = body,
            };
            if (promoted)
            {
                card.BackgroundColor = AccentSoft;
            }

            row.Add(card, 1);
        }

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionTitle("Pricing options", "pricing"));
        stack.Add(row);
        return stack;
    }

    private static VerticalStack AddOnTable(QuotationData data)
    {
        Table table = new(1.6f, 2.2f, 1f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Line),
            HeaderBackgroundColor = Panel,
        };
        table.AddHeader(ColumnHead("Optional add-on"), ColumnHead("Detail"), ColumnHead("Price", TextAlignment.Right));
        foreach (QuoteAddOn addOn in data.AddOns)
        {
            table.AddRow(
                Text(addOn.Label, bold: true),
                Text(addOn.Detail, color: Muted),
                Text($"{Money(addOn.Price, data)} {addOn.Cadence}", align: TextAlignment.Right));
        }

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 14) };
        stack.Add(SectionTitle("Optional add-ons", "add-ons"));
        stack.Add(table);
        return stack;
    }

    /// <summary>A quote that never expires is a standing offer. Say when it lapses.</summary>
    private static Container ValidityBand(QuotationData data)
    {
        VerticalStack copy = new() { Spacing = 2 };
        copy.Add(Text($"This quotation is valid until {data.Reference.ValidUntil}", bold: true, size: 10, color: AccentDeep));
        copy.Add(Text("Prices are held for the period shown. After that date we are happy to re-quote at prevailing rates.", size: 8, color: Muted));

        return new Container
        {
            BackgroundColor = AccentSoft,
            Border = Border.LeftOnly(3),
            BorderColors = BorderColors.LeftOnly(Accent),
            Padding = new Thickness(10, 12, 10, 12),
            Margin = new Thickness(0, 0, 0, 14),
            Child = copy,
        };
    }

    private static VerticalStack AssumptionsSection(QuotationData data)
    {
        UnorderedList list = new()
        {
            FontName = StandardFonts.Helvetica,
            FontSize = 8.5f,
            TextColor = Muted,
            BulletColor = Faint,
            ItemSpacing = 4,
        };
        foreach (string item in data.Assumptions)
        {
            list.AddItem(item);
        }

        VerticalStack stack = new() { Margin = new Thickness(0, 0, 0, 16) };
        stack.Add(SectionTitle("Assumptions and exclusions", "assumptions"));
        stack.Add(list);
        return stack;
    }

    /// <summary>A ruled line with its caption underneath — one signature field.</summary>
    private static VerticalStack SignatureField(string caption)
    {
        VerticalStack stack = new() { Spacing = 4 };
        stack.Add(new Spacer(22));
        stack.Add(new Line { Thickness = 0.75f, Color = Faint });
        stack.Add(Text(caption, size: 7.5f, color: Muted));
        return stack;
    }

    /// <summary>
    /// The acceptance block. This is what turns the page from information into
    /// an instrument: the customer signs this document rather than replying to it.
    /// </summary>
    private static Container AcceptanceBlock(QuotationData data)
    {
        HorizontalStack fields = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 18 };
        fields.Add(SignatureField("Signed for and on behalf of " + data.Customer.Company), 2f);
        fields.Add(SignatureField("Name and position"), 1.5f);
        fields.Add(SignatureField("Date"), 1f);

        TextBlock intro = Text($"To accept, please sign below and return to {data.Supplier.Email}, quoting {data.Reference.QuoteNumber}.", color: Muted);
        intro.Margin = new Thickness(0, 0, 0, 4);

        VerticalStack body = new();
        body.Add(SectionTitle("Acceptance", "acceptance"));
        body.Add(intro);
        body.Add(fields);

        return new Container
        {
            Padding = new Thickness(12, 12, 12, 12),
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Line),
            Child = body,
        };
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static TextBlock Text(string text, bool bold = false, float size = 9f, Color? color = null, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = size,
        TextColor = color ?? Ink,
        Alignment = align,
    };

    /// <summary>
    /// Section titles are real level-2 headings rather than styled body text, so
    /// assistive technology gets a navigable outline of the quotation instead of
    /// a flat wall of paragraphs. They nest under the level-1 masthead.
    /// </summary>
    private static Heading SectionTitle(string text, string id) => new(text)
    {
        Level = 2,
        TocIdentifier = id,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 11f,
        TextColor = Ink,
        Margin = new Thickness(0, 0, 0, 6),
    };

    private static TextBlock ColumnHead(string text, TextAlignment align = TextAlignment.Left) =>
        Text(text, bold: true, size: 7.5f, color: Muted, align: align);

    /// <summary>A label-over-value cell, the unit the reference strip is built from.</summary>
    private static VerticalStack Field(string label, string value)
    {
        VerticalStack stack = new() { Spacing = 1 };
        stack.Add(Text(label.ToUpperInvariant(), bold: true, size: 6.5f, color: Faint));
        stack.Add(Text(value));
        return stack;
    }

    private static string Money(decimal value, QuotationData data) =>
        value.ToString("C0", CultureInfo.GetCultureInfo(data.Locale));

    // ── Domain model (the data-to-document boundary) ──────────────────────────

    public sealed record Supplier(string Name, string Address, string Email, string Registration);

    public sealed record Customer(string Company, string Contact, string Address);

    public sealed record Reference(string QuoteNumber, string Issued, string ValidUntil, string Owner);

    /// <summary>One purchasable tier. Exactly one tier should set <paramref name="Recommended"/>.</summary>
    public sealed record QuoteTier(string Name, string Tagline, decimal Price, string Cadence, string[] Includes, bool Recommended = false);

    public sealed record QuoteAddOn(string Label, string Detail, decimal Price, string Cadence);

    public sealed record QuotationData(Supplier Supplier, Customer Customer, Reference Reference, string[] Scope, QuoteTier[] Tiers, QuoteAddOn[] AddOns, string[] Assumptions, string Locale, string Currency);
}
