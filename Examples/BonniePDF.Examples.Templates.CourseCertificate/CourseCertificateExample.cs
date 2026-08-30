using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.CourseCertificate;

/// <summary>
/// A course completion certificate. Every other template here is an
/// information-dense business document read for its data; a certificate is the
/// opposite — read once, framed, and judged almost entirely on how it looks.
/// That makes it the one place where the engine's compositional features earn
/// their keep rather than its tabular ones: a drawn ornamental frame layered
/// behind the wording, a rosette seal built from paths rather than a bitmap,
/// centred serif typography in landscape, and a verification code that makes
/// the credential checkable. Output is PDF/A-2a + PDF/UA-1, with the decorative
/// artwork marked as such so a screen reader is read the award, not the ornament.
/// </summary>
[System.ComponentModel.DisplayName("Course Certificate")]
[System.ComponentModel.Category("Education")]
[System.ComponentModel.Description("A landscape course completion certificate: a drawn ornamental frame layered behind the wording, a rosette seal built from paths rather than a bitmap, centred serif typography, and a verification code that makes the credential checkable. Decorative artwork is kept out of the reading order. PDF/A-2a + PDF/UA-1.")]
public static class CourseCertificateExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Accent = ColorPalette.Indigo[700];
    private static readonly Color AccentMid = ColorPalette.Indigo[400];
    private static readonly Color AccentSoft = ColorPalette.Indigo[100];
    private static readonly Color Gold = ColorPalette.Amber[500];

    // A4 landscape (595.276pt tall) less the page margin on both edges. A
    // certificate is a fixed-format document, so the frame is sized to the
    // content area explicitly rather than being asked to discover the height
    // of the wording it sits behind.
    private const float PageMargin = 34f;
    private const float FrameHeight = 595.276f - PageMargin * 2;

    // ── Deterministic sample fixture ──────────────────────────────────────────
    private static readonly CertificateData Sample = new(
        new Institution("Westbrook Institute", "School of Applied Computing"),
        Recipient: "Sample Recipient",
        new Course("Accessible Document Engineering", 45, "Distinction"),
        new Award("has successfully completed the programme of study and assessment in", "30 August 2026", "WI-ADE-2026-04188"),
        Signatories:
        [
            new Signatory("Sample Registrar", "Registrar"),
            new Signatory("Sample Programme Lead", "Programme Lead"),
        ],
        VerificationUrl: "https://example.com/verify/WI-ADE-2026-04188",
        Locale: "en-GB");

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Landscape, PageMargin)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Times").WithSize(11f).WithColor(Ink);
        return BuildDocument(Sample, settings);
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(CertificateData data, DocumentSettings settings)
    {
        Document doc = new(settings);
        doc.Metadata.Title = $"Certificate of Completion — {data.Recipient} — {data.Course.Title}";
        doc.Metadata.Author = data.Institution.Name;
        doc.Metadata.Subject = "Course completion certificate (fictional sample data).";

        // A leading spacer settles the wording into the frame rather than
        // letting it start hard against the top rule.
        VerticalStack body = new();
        body.Add(new Spacer(46));
        body.Add(Citation(data));
        body.Add(Attestation(data));

        // The frame sits behind everything; the wording rides on top as the
        // primary layer, which is also what sets the block's flow height.
        Layers framed = new();
        framed.AddBackground(OrnamentalFrame());
        framed.PrimaryLayer = body;

        doc.Add(framed);
        return doc;
    }

    // ── Ornament ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The frame: a double rule inset from the page edge with a filled corner
    /// flourish at each corner. Drawn once as a background layer, so the wording
    /// on top never has to know it exists.
    /// </summary>
    private static CanvasElement OrnamentalFrame() => new((canvas, width, height) =>
    {
        const float outer = 10f;
        const float inner = 16f;

        canvas.StrokeRect(outer, outer, width - outer * 2, height - outer * 2, Accent, 1.6f);
        canvas.StrokeRect(inner, inner, width - inner * 2, height - inner * 2, AccentMid, 0.5f);

        // A small filled lozenge tucked into each corner of the inner rule.
        const float r = 4.5f;
        (float X, float Y)[] corners =
        [
            (inner, inner),
            (width - inner, inner),
            (inner, height - inner),
            (width - inner, height - inner),
        ];
        foreach ((float cx, float cy) in corners)
        {
            canvas.FillPath(
            [
                new Point(cx, cy - r),
                new Point(cx + r, cy),
                new Point(cx, cy + r),
                new Point(cx - r, cy),
            ], Accent);
        }
    })
    {
        // CanvasWidth 0 (the default) means "fill the width available", which is
        // what makes the frame span the content area.
        CanvasHeight = FrameHeight,
        IsDecorative = true,
    };

    /// <summary>
    /// The seal: concentric rings with a ring of radiating points between them,
    /// then the grade struck across the middle. Drawn rather than embedded so it
    /// scales cleanly and carries no bitmap payload.
    /// </summary>
    private static CanvasElement SealMedallion(string grade) => new((canvas, width, height) =>
    {
        float cx = width / 2;
        float cy = height / 2;
        float outer = Math.Min(cx, cy) - 1;

        // Radiating points, drawn first so the discs above cover their roots.
        const int points = 16;
        for (int i = 0; i < points; i++)
        {
            double a = (double)i / points * Math.PI * 2;
            double half = Math.PI / points;
            canvas.FillPath(
            [
                new Point(cx + (float)Math.Cos(a) * outer, cy + (float)Math.Sin(a) * outer),
                new Point(cx + (float)Math.Cos(a + half) * (outer * 0.72f), cy + (float)Math.Sin(a + half) * (outer * 0.72f)),
                new Point(cx + (float)Math.Cos(a - half) * (outer * 0.72f), cy + (float)Math.Sin(a - half) * (outer * 0.72f)),
            ], Gold);
        }

        canvas.FillCircle(cx, cy, outer * 0.74f, AccentSoft);
        canvas.FillCircle(cx, cy, outer * 0.66f, Color.White);

        string label = grade.ToUpperInvariant();
        const float fontSize = 7.5f;
        float textWidth = canvas.MeasureText(label, fontSize, StandardFonts.TimesBold).Width;
        canvas.DrawText(cx - textWidth / 2, cy + fontSize / 3, label, Accent, fontSize, StandardFonts.TimesBold, 0);
    })
    {
        CanvasWidth = 74,
        CanvasHeight = 74,
        IsDecorative = true,
    };

    // ── Section builders ──────────────────────────────────────────────────────

    /// <summary>The wording, centred and stacked — the part actually read aloud.</summary>
    private static VerticalStack Citation(CertificateData data)
    {
        VerticalStack stack = new();

        stack.Add(Text(data.Institution.Name.ToUpperInvariant(), font: StandardFonts.Helvetica, size: 9, color: Accent, align: TextAlignment.Center));

        TextBlock department = Caption(data.Institution.Department);
        department.Margin = new Thickness(0, 2, 0, 14);
        stack.Add(department);

        stack.Add(new Heading("Certificate of Completion")
        {
            Level = 1,
            TocIdentifier = "certificate",
            FontName = StandardFonts.TimesBold,
            FontSize = 26f,
            TextColor = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
        });

        stack.Add(new Container
        {
            Height = 1,
            BackgroundColor = AccentMid,
            Margin = new Thickness(210, 0, 210, 16),
        });

        stack.Add(Text("This is to certify that", font: StandardFonts.TimesItalic, size: 11, color: Muted, align: TextAlignment.Center));

        TextBlock recipient = Text(data.Recipient, font: StandardFonts.TimesBold, size: 24, color: Accent, align: TextAlignment.Center);
        recipient.Margin = new Thickness(0, 6, 0, 6);
        stack.Add(recipient);

        stack.Add(Text(data.Award.Statement, font: StandardFonts.TimesItalic, size: 11, color: Muted, align: TextAlignment.Center));

        TextBlock course = Text(data.Course.Title, font: StandardFonts.TimesBold, size: 16, align: TextAlignment.Center);
        course.Margin = new Thickness(0, 6, 0, 6);
        stack.Add(course);

        stack.Add(Text($"{data.Course.CreditHours} credit hours · Awarded with {data.Course.Grade} · {data.Award.Awarded}",
            font: StandardFonts.Helvetica, size: 9, color: Muted, align: TextAlignment.Center));

        return stack;
    }

    /// <summary>Seal on the left, signatures in the middle, verification on the right.</summary>
    private static HorizontalStack Attestation(CertificateData data)
    {
        Container seal = new()
        {
            Child = SealMedallion(data.Course.Grade),
            Margin = new Thickness(0, 6, 0, 0),
        };

        HorizontalStack signatures = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 28 };
        foreach (Signatory signatory in data.Signatories)
        {
            signatures.Add(SignatureColumn(signatory.Name, signatory.Role), 1);
        }

        QRCode qr = new()
        {
            Data = data.VerificationUrl,
            Size = 52,
            ForegroundColor = Ink,
            AltText = $"Verification code linking to {data.VerificationUrl}",
        };

        VerticalStack verification = new() { Spacing = 3 };
        verification.Add(qr);
        verification.Add(Caption("Verify online", 7));
        verification.Add(Caption(data.Award.CertificateId, 7, Faint));

        HorizontalStack row = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 24,
            Margin = new Thickness(0, 74, 0, 0),
        };
        row.Add(seal, 0.9f);
        row.Add(signatures, 2.4f);
        row.Add(verification, 0.9f);
        return row;
    }

    /// <summary>A ruled line with the signatory beneath it — one signature column.</summary>
    private static VerticalStack SignatureColumn(string name, string role)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(new Spacer(16));
        stack.Add(new Line { Thickness = 0.6f, Color = Faint });
        stack.Add(Text(name, font: StandardFonts.TimesBold, size: 9.5f, align: TextAlignment.Center));
        stack.Add(Caption(role));
        return stack;
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static TextBlock Caption(string content, float size = 7.5f, Color? color = null) =>
        Text(content, font: StandardFonts.Helvetica, size: size, color: color ?? Muted, align: TextAlignment.Center);

    private static TextBlock Text(string content, string? font = null, float size = 11f, Color? color = null, TextAlignment align = TextAlignment.Left) => new()
    {
        Text = content,
        FontName = font ?? StandardFonts.TimesRoman,
        FontSize = size,
        TextColor = color ?? Ink,
        Alignment = align,
    };

    // ── Domain model (the data-to-document boundary) ──────────────────────────

    public sealed record Institution(string Name, string Department);

    public sealed record Course(string Title, int CreditHours, string Grade);

    public sealed record Award(string Statement, string Awarded, string CertificateId);

    public sealed record Signatory(string Name, string Role);

    public sealed record CertificateData(Institution Institution, string Recipient, Course Course, Award Award, Signatory[] Signatories, string VerificationUrl, string Locale);
}
