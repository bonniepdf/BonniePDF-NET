using System.Linq;
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
/// behind the wording, a compact monogram crest built from vectors, centred
/// serif typography in landscape, and a verification code that makes the
/// credential checkable. Output is PDF/A-2a + PDF/UA-1, with the decorative
/// artwork marked as such so a screen reader is read the award, not the ornament.
/// </summary>
[System.ComponentModel.DisplayName("Course Certificate")]
[System.ComponentModel.Category("Education")]
[System.ComponentModel.Description("A landscape course completion certificate: a drawn ornamental frame layered behind the wording, a compact monogram crest built from vectors, centred serif typography, and a verification code that makes the credential checkable. Decorative artwork is kept out of the reading order. PDF/A-2a + PDF/UA-1.")]
public static class CourseCertificateExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Accent = ColorPalette.Indigo[700];
    private static readonly Color AccentMid = ColorPalette.Indigo[400];
    private static readonly Color AccentSoft = ColorPalette.Indigo[100];
    private static readonly Color Gold = ColorPalette.Amber[600];

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
        // letting it start hard against the top rule. 30pt clears the crest's
        // own lozenge points (which reach to the very top of its 52pt box)
        // past the frame's inner rule at 16pt inset - without this the crest
        // visually crosses the border instead of sitting inside it.
        VerticalStack body = new();
        body.Add(new Spacer(30));
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
    /// The crest: a restrained circular monogram with four small gold lozenges.
    /// Drawn rather than embedded so it scales cleanly and carries no bitmap
    /// payload.
    /// </summary>
    private static CanvasElement SealMedallion(string institutionName) => new((canvas, width, height) =>
    {
        float cx = width / 2;
        float cy = height / 2;
        float outer = Math.Min(cx, cy) - 6;

        // Four restrained lozenges give the crest a ceremonial detail without
        // the visual noise of a full starburst.
        foreach (double a in new[] { 0, Math.PI / 2, Math.PI, Math.PI * 3 / 2 })
        {
            const float pointDistance = 3.5f;
            float px = cx + (float)Math.Cos(a) * (outer + pointDistance);
            float py = cy + (float)Math.Sin(a) * (outer + pointDistance);
            float tangentX = (float)Math.Cos(a + Math.PI / 2) * 2.5f;
            float tangentY = (float)Math.Sin(a + Math.PI / 2) * 2.5f;
            float radialX = (float)Math.Cos(a) * 3.5f;
            float radialY = (float)Math.Sin(a) * 3.5f;
            canvas.FillPath(
            [
                new Point(px + radialX, py + radialY),
                new Point(px + tangentX, py + tangentY),
                new Point(px - radialX, py - radialY),
                new Point(px - tangentX, py - tangentY),
            ], Gold);
        }

        canvas.FillCircle(cx, cy, outer, Accent);
        canvas.FillCircle(cx, cy, outer - 1.5f, Color.White);
        canvas.FillCircle(cx, cy, outer - 4.5f, AccentSoft);

        string monogram = string.Concat(institutionName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => word[0]))
            .ToUpperInvariant();
        const float fontSize = 13f;
        float monogramWidth = canvas.MeasureText(monogram, fontSize, StandardFonts.TimesBold).Width;
        canvas.DrawText(cx - monogramWidth / 2, cy - fontSize / 2, monogram, Accent, fontSize, StandardFonts.TimesBold, 0);
    })
    {
        CanvasWidth = 52,
        CanvasHeight = 52,
        IsDecorative = true,
    };

    /// <summary>A small crest, centred above the institution name.</summary>
    private static HorizontalStack InstitutionCrest(string name)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 0, 0, 7) };
        row.Add(new Spacer(1), 1f);
        row.Add(new Container { Child = SealMedallion(name) }, 0.15f);
        row.Add(new Spacer(1), 1f);
        return row;
    }

    // ── Section builders ──────────────────────────────────────────────────────

    /// <summary>The wording, centred and stacked — the part actually read aloud.</summary>
    private static VerticalStack Citation(CertificateData data)
    {
        VerticalStack stack = new();

        stack.Add(InstitutionCrest(data.Institution.Name));

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

    /// <summary>A balanced signature and verification row.</summary>
    private static VerticalStack Attestation(CertificateData data)
    {
        Signatory registrar = data.Signatories[0];
        Signatory programmeLead = data.Signatories[1];

        HorizontalStack signatures = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 56,
            Margin = new Thickness(0, 74, 0, 26),
        };
        signatures.Add(new Spacer(1), 1f);
        signatures.Add(SignatureColumn(registrar.Name, registrar.Role), 1.3f);
        signatures.Add(SignatureColumn(programmeLead.Name, programmeLead.Role), 1.3f);
        signatures.Add(new Spacer(1), 1f);

        QRCode qr = new()
        {
            Data = data.VerificationUrl,
            Size = 42,
            ForegroundColor = Ink,
            AltText = $"Verification code linking to {data.VerificationUrl}",
        };

        // QRCode always renders flush to the left of whatever width it is
        // given - unlike TextBlock, it has no alignment of its own, so a
        // Container with HorizontalAlignment.Center is what actually centres
        // it under the (wider) captions below. Without this the QR sits
        // offset from text that otherwise looks centred.
        Container qrBox = new() { HorizontalAlignment = HorizontalAlignment.Center, Child = qr };

        VerticalStack verification = new() { Spacing = 3 };
        verification.Add(qrBox);
        verification.Add(Caption("Verify online", 6.5f));
        verification.Add(Caption(data.Award.CertificateId, 6.5f, Faint));

        // The trailing spacer is deliberate, not decorative: the certificate
        // ID caption is wider than the QR code, and without room held clear
        // on the right the wider caption pokes past the frame's inner rule.
        HorizontalStack verificationRow = new() { WidthDistribution = WidthDistribution.Weighted };
        verificationRow.Add(new Spacer(1), 1f);
        verificationRow.Add(verification, 0.1f);
        verificationRow.Add(new Spacer(1), 0.04f);

        VerticalStack stack = new();
        stack.Add(signatures);
        stack.Add(verificationRow);
        return stack;
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
