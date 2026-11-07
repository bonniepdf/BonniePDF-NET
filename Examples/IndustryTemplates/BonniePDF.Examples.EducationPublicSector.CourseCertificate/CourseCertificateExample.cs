using System.Linq;
using BonniePDF;

namespace BonniePDF.Examples.EducationPublicSector.CourseCertificate;

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
[System.ComponentModel.Category("Education & Public Sector")]
[System.ComponentModel.Description(
    "A landscape course completion certificate: a drawn ornamental frame layered behind the wording, a compact monogram crest built from vectors, a gold foil-style seal between the signatures, centred serif typography, and a verification code that makes the credential checkable. Decorative artwork is kept out of the reading order. PDF/A-2a + PDF/UA-1.")]
public static class CourseCertificateExample
{
    // ── Design tokens ─────────────────────────────────────────────────────────
    private static readonly Color Ink = ColorPalette.Slate[800];
    private static readonly Color Muted = ColorPalette.Slate[500];
    private static readonly Color Faint = ColorPalette.Slate[400];
    private static readonly Color Accent = ColorPalette.Indigo[700];
    private static readonly Color AccentMid = ColorPalette.Indigo[400];
    private static readonly Color Gold = ColorPalette.Amber[600];
    // The seal's foil: light, mid and deep golds, with the deepest for its lettering.
    private static readonly Color FoilLight = ColorPalette.Amber[300];
    private static readonly Color FoilMid = ColorPalette.Amber[500];
    private static readonly Color FoilDeep = ColorPalette.Amber[700];
    private static readonly Color FoilInk = ColorPalette.Amber[900];

    // A4 landscape (595.276pt tall) less the page margin on both edges. A
    // certificate is a fixed-format document, so the frame is sized to the
    // content area explicitly rather than being asked to discover the height
    // of the wording it sits behind.
    private const float PageMargin = 34f;
    private const float FrameHeight = 595.276f - PageMargin * 2;

    // ── Deterministic sample fixture ──────────────────────────────────────────
    private static readonly CertificateData Sample = new(
        new Institution("Sample Institute", "School of Applied Computing"),
        Recipient: "Sample Recipient",
        new Course("Accessible Document Engineering", 45, "Distinction"),
        new Award("has successfully completed the programme of study and assessment in", "30 August 2026",
            "SI-ADE-2026-04188"),
        Signatories:
        [
            new Signatory("Sample Registrar", "Registrar"),
            new Signatory("Sample Programme Lead", "Programme Lead"),
        ],
        VerificationUrl: "https://example.com/verify/SI-ADE-2026-04188",
        Locale: "en-GB");

    /// <summary>Asset paths required by this example, relative to the shared <c>Examples/assets</c> directory.</summary>
    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "images/svg/university-crest.svg",
        "images/svg/guilloche.svg",
    ];

    /// <summary>Builds the document using the default shared assets. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(LoadDefaultAssets());

    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        DocumentSettings settings = new(PdfSize.A4, PageOrientation.Landscape, PageMargin)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Times").WithSize(11f).WithColor(Ink);
        return BuildDocument(Sample, settings, assets);
    }

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(CertificateData data, DocumentSettings settings) =>
        BuildDocument(data, settings, LoadDefaultAssets());

    /// <summary>Pure builder: the same data always produces the same document.</summary>
    public static Document BuildDocument(CertificateData data, DocumentSettings settings,
        IReadOnlyDictionary<string, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);
        string crestSvg = System.Text.Encoding.UTF8.GetString(GetAsset(assets, "images/svg/university-crest.svg"));
        string guillocheSvg = System.Text.Encoding.UTF8.GetString(GetAsset(assets, "images/svg/guilloche.svg"));

        Document doc = new(settings);
        doc.Metadata.Title = $"Certificate of Completion — {data.Recipient} — {data.Course.Title}";
        doc.Metadata.Author = data.Institution.Name;
        doc.Metadata.Subject = "Course completion certificate (fictional sample data).";

        // A leading spacer settles the wording into the frame rather than
        // letting it start hard against the top rule. 30pt clears the crest
        // past the frame's inner rule at 16pt inset.
        VerticalStack body = new();
        body.Add(new Spacer(30));
        body.Add(Citation(data, crestSvg, guillocheSvg));
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
        // A fine gold rule just inside the inner one.
        const float gilt = inner + 4;
        canvas.StrokeRect(gilt, gilt, width - gilt * 2, height - gilt * 2, Gold, 0.4f);

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
        // DrawWidth 0 (the default) means "fill the width available", which is
        // what makes the frame span the content area.
        DrawHeight = FrameHeight,
        IsDecorative = true,
    };

    /// <summary>
    /// The seal: a gold starburst with foil-toned rings and the award's grade, on two ribbon tails.
    /// Shades of gold stand in for foil, so the seal prints the same on any printer.
    /// </summary>
    private static CanvasElement GoldSeal(string institutionName, string grade) => new((canvas, width, height) =>
    {
        float cx = width / 2;
        const float cy = 40f;

        // Ribbon tails first, so the seal sits on top of them.
        foreach (int side in new[] { -1, 1 })
        {
            canvas.FillPath(
            [
                new Point(cx + side * 16, cy + 18),
                new Point(cx + side * 26, cy + 50),
                new Point(cx + side * 19, cy + 45),
                new Point(cx + side * 13, cy + 52),
                new Point(cx + side * 5, cy + 22),
            ], Accent);
        }

        // A 32-point starburst: points alternate between the outer and inner radius.
        List<Point> points = [];
        for (int i = 0; i < 32; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 16;
            float radius = i % 2 == 0 ? 36f : 32.5f;
            points.Add(new Point(cx + (float)Math.Cos(angle) * radius, cy + (float)Math.Sin(angle) * radius));
        }
        canvas.FillPath(points, Gold);
        canvas.FillCircle(cx, cy, 30, FoilDeep);
        canvas.FillCircle(cx, cy, 28.5f, FoilLight);
        canvas.FillCircle(cx, cy, 26.5f, FoilMid);

        string monogram = string.Concat(institutionName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(word => word[0]))
            .ToUpperInvariant();
        float monogramWidth = canvas.MeasureText(monogram, 16, StandardFonts.TimesBold).Width;
        canvas.DrawText(cx - monogramWidth / 2, cy - 13, monogram, FoilInk, 16, StandardFonts.TimesBold, 0);
        string label = grade.ToUpperInvariant();
        float labelWidth = canvas.MeasureText(label, 5.5f, StandardFonts.HelveticaBold).Width;
        canvas.DrawText(cx - labelWidth / 2, cy + 6, label, FoilInk, 5.5f, StandardFonts.HelveticaBold, 0);
    })
    {
        DrawWidth = 84,
        DrawHeight = 94,
        IsDecorative = true,
    };

    /// <summary>A signature column lowered so its rule lines up with the seal's centre.</summary>
    private static VerticalStack LoweredSignature(string name, string role)
    {
        VerticalStack stack = new();
        stack.Add(new Spacer(40));
        stack.Add(SignatureColumn(name, role));
        return stack;
    }

    /// <summary>A small crest, centred above the institution name.</summary>
    private static HorizontalStack InstitutionCrest(string crestSvg)
    {
        HorizontalStack row = new()
            { WidthDistribution = WidthDistribution.Weighted, Margin = new Thickness(0, 0, 0, 7) };
        row.Add(new Spacer(1), 1f);
        row.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = new Svg
            {
                SvgData = crestSvg,
                Width = 32,
                Height = 37,
                AltText = "Institutional crest",
            },
        }, 0.15f);
        row.Add(new Spacer(1), 1f);
        return row;
    }

    // ── Section builders ──────────────────────────────────────────────────────

    /// <summary>The wording, centred and stacked — the part actually read aloud.</summary>
    private static VerticalStack Citation(CertificateData data, string crestSvg, string guillocheSvg)
    {
        VerticalStack stack = new();

        stack.Add(InstitutionCrest(crestSvg));

        stack.Add(Text(data.Institution.Name.ToUpperInvariant(), font: StandardFonts.Helvetica, size: 9, color: Accent,
            align: TextAlignment.Center));

        TextBlock department = Caption(data.Institution.Department);
        department.Margin = new Thickness(0, 2, 0, 14);
        stack.Add(department);

        stack.Add(new Heading("Certificate of Completion")
        {
            Level = 1,
            TocIdentifier = "certificate",
            FontName = StandardFonts.TimesBold,
            FontSize = 26f,
            Color = Ink,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
        });

        stack.Add(new Container { Height = 1, BackgroundColor = AccentMid, Margin = new Thickness(210, 0, 210, 6) });
        stack.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8),
            Child = new Svg { SvgData = guillocheSvg, Height = 14, IsDecorative = true },
        });

        stack.Add(Text("This is to certify that", font: StandardFonts.TimesItalic, size: 11, color: Muted,
            align: TextAlignment.Center));

        TextBlock recipient = Text(data.Recipient, font: StandardFonts.TimesBold, size: 24, color: Accent,
            align: TextAlignment.Center);
        recipient.Margin = new Thickness(0, 6, 0, 6);
        stack.Add(recipient);

        stack.Add(Text(data.Award.Statement, font: StandardFonts.TimesItalic, size: 11, color: Muted,
            align: TextAlignment.Center));

        TextBlock course = Text(data.Course.Title, font: StandardFonts.TimesBold, size: 16,
            align: TextAlignment.Center);
        course.Margin = new Thickness(0, 6, 0, 6);
        stack.Add(course);

        stack.Add(Text(
            $"{data.Course.CreditHours} credit hours · Awarded with {data.Course.Grade} · {data.Award.Awarded}",
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
            Spacing = 28,
            Margin = new Thickness(0, 34, 0, 10),
        };
        // The seal sits between the two signatures, where a certificate is sealed.
        signatures.Add(new Spacer(1), 0.8f);
        signatures.Add(LoweredSignature(registrar.Name, registrar.Role), 1.3f);
        signatures.Add(new Container
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = GoldSeal(data.Institution.Name, data.Course.Grade),
        }, 0.6f);
        signatures.Add(LoweredSignature(programmeLead.Name, programmeLead.Role), 1.3f);
        signatures.Add(new Spacer(1), 0.8f);

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
        // Muted, not Faint: this sits inside a Layers composition, so the
        // preflight can't verify it - but Faint (Slate[400]) is 2.63:1 against
        // the white it actually renders on, well under 4.5:1 at this tiny 6.5pt
        // size.
        verification.Add(Caption(data.Award.CertificateId, 6.5f, Muted));

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

    private static TextBlock Text(string content, string? font = null, float size = 11f, Color? color = null,
        TextAlignment align = TextAlignment.Left) => new()
    {
        Text = content,
        FontName = font ?? StandardFonts.TimesRoman,
        FontSize = size,
        Color = color ?? Ink,
        Alignment = align,
    };

    // ── Domain model (the data-to-document boundary) ──────────────────────────

    public sealed record Institution(string Name, string Department);

    public sealed record Course(string Title, int CreditHours, string Grade);

    public sealed record Award(string Statement, string Awarded, string CertificateId);

    public sealed record Signatory(string Name, string Role);

    public sealed record CertificateData(
        Institution Institution,
        string Recipient,
        Course Course,
        Award Award,
        Signatory[] Signatories,
        string VerificationUrl,
        string Locale);

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string assetsDir = FindAssetsDir();
        Dictionary<string, byte[]> dict = new(StringComparer.Ordinal);
        foreach (string key in RequiredAssets)
        {
            string fullPath = Path.Combine(assetsDir, key.Replace('/', Path.DirectorySeparatorChar));
            dict[key] = File.ReadAllBytes(fullPath);
        }

        return dict;
    }

    private static byte[] GetAsset(IReadOnlyDictionary<string, byte[]> assets, string key)
    {
        if (assets.TryGetValue(key, out byte[]? bytes))
            return bytes;
        string fullPath = Path.Combine(FindAssetsDir(), key.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllBytes(fullPath);
    }

    private static string FindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
                return candidate;
            string direct = Path.Combine(dir, "assets");
            if (Directory.Exists(direct))
                return direct;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Examples/assets directory.");
    }
}
