using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.PatientSummary;

/// <summary>
/// An A4 portrait healthcare "patient summary" sheet built from rounded-corner
/// cards. Each card sets <see cref="Container.ClipContent"/> with a
/// positive <c>CornerRadius</c> so its full-bleed coloured header band is clipped
/// to the rounded outline - the band follows the rounded top corners instead of
/// poking square corners out past the card edge, and the white body is masked at
/// the bottom the same way.
/// A teal patient card, an indigo vitals grid, and a rose allergies/alerts panel.
/// </summary>
[System.ComponentModel.DisplayName("Patient Summary")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A4 portrait healthcare patient-summary sheet built from rounded-corner cards: each card clips a full-bleed coloured header band to its rounded outline - a teal patient card, an indigo vitals grid, and a rose allergies/alerts panel.")]
public static class PatientSummaryExample
{
    // ── Healthcare palette (Slate neutrals + Teal/Indigo/Rose accents) ────────
    private static readonly Color Ink = Color.FromHex("#0f172a");      // slate-900  headings / values
    private static readonly Color Body = Color.FromHex("#334155");     // slate-700  body text
    private static readonly Color Mute = Color.FromHex("#64748b");     // slate-500  labels / captions
    private static readonly Color Hairline = Color.FromHex("#e2e8f0"); // slate-200  card border / rules
    private static readonly Color Card = Color.FromHex("#ffffff");     // card surface
    private static readonly Color Tint = Color.FromHex("#f8fafc");     // slate-50   page tint
    private static readonly Color Teal = Color.FromHex("#0d9488");     // patient band
    private static readonly Color Indigo = Color.FromHex("#4f46e5");   // vitals band
    private static readonly Color Rose = Color.FromHex("#e11d48");     // allergy band / alerts
    private static readonly Color Cream = Color.FromHex("#f1f5f9");    // band caption text

    public static Document Generate()
    {
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 0,
            Language = "en-GB",
        };

        Document doc = new(settings);
        doc.Metadata.Title = "Patient Summary";
        doc.Metadata.Author = "Acme Health";
        doc.Metadata.Subject = "Patient summary sheet - demographics, vitals, and alerts";

        // ── Page header band ──────────────────────────────────────────────────
        VerticalStack brand = new();
        brand.AddChild(Tb("ACME HEALTH", 13, Color.White, bold: true));
        brand.AddChild(Tb("Patient Summary", 8, Cream, top: 4));

        VerticalStack issued = new();
        issued.AddChild(Tb("Confidential", 8, Cream, align: TextAlignment.Right));
        issued.AddChild(Tb("Generated 18 June 2026", 8, Cream, align: TextAlignment.Right, top: 4));

        doc.Add(new Container
        {
            BackgroundColor = Ink,
            Padding = new Thickness(28, 18, 28, 18),
            Child = TwoUp(brand, issued),
        });

        doc.Add(Pad(new Spacer(20)));

        // ── 1. Patient card (teal bleed band) ─────────────────────────────────
        VerticalStack idLine = new();
        idLine.AddChild(Tb("Eleanor M. Whitfield", 15, Color.White, bold: true));
        idLine.AddChild(Tb("MRN 00482931  ·  DOB 14 Mar 1979  ·  Female", 9, Cream, top: 5));

        Container patientBand = new()
        {
            BackgroundColor = Teal,
            Padding = new Thickness(16, 12, 16, 12),
            Child = idLine,
        };

        VerticalStack detailsLeft = new();
        detailsLeft.AddChild(DetailRow("Blood type", "O positive"));
        detailsLeft.AddChild(DetailRow("Primary GP", "Dr. A. Okafor"));
        detailsLeft.AddChild(DetailRow("Phone", "+44 7700 900482"));

        VerticalStack detailsRight = new();
        detailsRight.AddChild(DetailRow("Patient ID", "ACME-9434765919"));
        detailsRight.AddChild(DetailRow("Ward", "Cardiology - 4B"));
        detailsRight.AddChild(DetailRow("Admitted", "16 Jun 2026"));

        Container detailsBody = new()
        {
            BackgroundColor = Card,
            Padding = new Thickness(16, 14, 16, 16),
            Child = TwoUp(detailsLeft, detailsRight),
        };

        VerticalStack patientStack = new();
        patientStack.AddChild(patientBand);
        patientStack.AddChild(detailsBody);

        doc.Add(Pad(new Container
        {
            BackgroundColor = Card,
            CornerRadius = 14,
            ClipContent = true, // ← mask the bleed band + body to the rounded outline
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Hairline),
            Child = patientStack,
        }));

        doc.Add(Pad(new Spacer(16)));

        // ── 2. Vitals card (indigo bleed band, 4-up grid) ─────────────────────
        Grid vitalsGrid = new(1, 1, 1, 1) { Border = Border.None, CellPadding = 6 };
        vitalsGrid.AddRow(
            Vital("128/82", "mmHg", "Blood pressure", "Normal", Teal),
            Vital("72", "bpm", "Heart rate", "Normal", Teal),
            Vital("37.1", "°C", "Temperature", "Normal", Teal),
            Vital("97", "% SpO2", "Oxygen", "Watch", Rose));

        doc.Add(Pad(MakeCard(Indigo, "VITALS  ·  18 Jun 2026, 09:14", vitalsGrid)));

        doc.Add(Pad(new Spacer(16)));

        // ── 3. Allergies & alerts card (rose bleed band) ──────────────────────
        VerticalStack alerts = new();
        alerts.AddChild(Alert("Penicillin", "Anaphylaxis - documented 2014. Avoid all beta-lactams.", 0));
        alerts.AddChild(Alert("Latex", "Contact urticaria. Use nitrile gloves.", 6));
        alerts.AddChild(Alert("Warfarin", "On anticoagulation - INR target 2.0-3.0.", 6));

        doc.Add(Pad(MakeCard(Rose, "ALLERGIES & ALERTS", alerts)));

        doc.Add(Pad(new Spacer(28)));

        return doc;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // A rounded, clipped card whose first child is a full-bleed coloured band.
    private static Container MakeCard(Color bandColor, string bandTitle, IElement body)
    {
        Container band = new()
        {
            BackgroundColor = bandColor,
            Padding = new Thickness(16, 10, 16, 10),
            Child = Tb(bandTitle, 10, Color.White, bold: true),
        };

        Container bodyWrap = new()
        {
            BackgroundColor = Card,
            Padding = new Thickness(16, 14, 16, 16),
            Child = body,
        };

        VerticalStack stack = new();
        stack.AddChild(band);
        stack.AddChild(bodyWrap);

        return new Container
        {
            BackgroundColor = Card,
            CornerRadius = 14,
            ClipContent = true, // ← this feature
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Hairline),
            Child = stack,
        };
    }

    // A label/value pair as one borderless 2-column row.
    private static Grid DetailRow(string label, string value)
    {
        Grid row = new(1, 1.6f) { Border = Border.None, CellPadding = 3 };
        row.AddRow(Tb(label.ToUpperInvariant(), 7.5f, Mute), Tb(value, 10, Body, bold: true));
        return row;
    }

    private static VerticalStack Vital(string value, string unit, string label, string status, Color tone)
    {
        VerticalStack v = new();
        v.AddChild(Tb(value, 18, Ink, bold: true));
        v.AddChild(Tb(unit, 8, Mute, top: 2));
        v.AddChild(Tb(label.ToUpperInvariant(), 7.5f, Body, bold: true, top: 8));
        v.AddChild(Tb(status, 7.5f, tone, bold: true, top: 3));
        return v;
    }

    private static Container Alert(string name, string detail, float top)
    {
        Grid row = new(1, 2.4f) { Border = Border.None, CellPadding = 3 };
        row.AddRow(Tb(name, 10, Rose, bold: true), Tb(detail, 9.5f, Body));
        Container c = new() { Child = row };
        if (top > 0) c.Margin = new Thickness(0, top, 0, 0);
        return c;
    }

    // A borderless two-column grid (equal weights) wrapping the two children.
    private static Grid TwoUp(IElement left, IElement right)
    {
        Grid grid = new(1, 1) { Border = Border.None, CellPadding = 0 };
        grid.AddRow(left, right);
        return grid;
    }

    // Tint band with 28pt side padding, so each card reads against the page tint.
    private static Container Pad(IElement child) => new()
    {
        BackgroundColor = Tint,
        Padding = new Thickness(28, 0, 28, 0),
        Child = child,
    };
}
