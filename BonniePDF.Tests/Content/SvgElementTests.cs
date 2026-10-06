using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Svg;
using BonniePDF.Rendering.Svg.Fonts;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// The Svg element, against spec/fixtures/svg-import/svg-element.json.
/// Mirrors core/tests/svg/svg-element.test.ts, including the expected bytes.
/// </summary>
public class SvgElementTests
{
    private const string Ns = "xmlns=\"http://www.w3.org/2000/svg\"";

    private const string Filtered =
        "<svg " + Ns + " viewBox=\"0 0 20 10\"><defs><filter id=\"blur\"><feGaussianBlur stdDeviation=\"1\"/></filter></defs>" +
        "<rect width=\"10\" height=\"10\" fill=\"#ff0000\" filter=\"url(#blur)\"/><rect x=\"10\" width=\"10\" height=\"10\" fill=\"#0000ff\"/></svg>";

    /// <summary>A 1x1 PNG, standing in for what a real rasterizer returns.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==");

    private static readonly Encoding Latin1 = Encoding.Latin1;

    /// <summary>The preferred width, and the height at that width: the element's natural box.</summary>
    private static (float Width, float Height) NaturalBox(Svg svg)
    {
        float width = svg.GetPreferredWidth();
        return (width, svg.MeasureHeight(width));
    }

    private static BonniePDF.Document Page(params Svg[] elements)
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Margin = 0;
        foreach (Svg element in elements) doc.Add(element);
        return doc;
    }

    private static string FirstContentStream(byte[] bytes)
    {
        string text = Latin1.GetString(bytes);
        int contentStart = text.IndexOf("stream", StringComparison.Ordinal) + "stream".Length;
        if (text[contentStart] == '\r') contentStart++;
        if (text[contentStart] == '\n') contentStart++;
        int end = text.IndexOf("endstream", contentStart, StringComparison.Ordinal);
        byte[] raw = Latin1.GetBytes(text.Substring(contentStart, end - contentStart));
        try
        {
            using var input = new MemoryStream(raw);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(zlib, Latin1);
            return reader.ReadToEnd();
        }
        catch (InvalidDataException)
        {
            return Latin1.GetString(raw);
        }
    }

    private static string Ops(BonniePDF.Document doc) => FirstContentStream(doc.GenerateBytes());

    // ---- Intrinsic size ----

    [Theory]
    [InlineData("unitless-is-px", "<svg " + Ns + " width=\"100\" height=\"50\"/>", 75.0, 37.5)]
    [InlineData("absolute-units", "<svg " + Ns + " width=\"1in\" height=\"2cm\"/>", 72.0, 56.69291338582677)]
    [InlineData("viewbox-stands-in", "<svg " + Ns + " viewBox=\"0 0 40 20\"/>", 30.0, 15.0)]
    [InlineData("percentage-defers-to-viewbox", "<svg " + Ns + " width=\"50%\" viewBox=\"0 0 40 20\"/>", 30.0, 15.0)]
    public void IntrinsicSize(string id, string svgData, double width, double height)
    {
        Assert.NotNull(id);
        (float w, float h) = NaturalBox(new Svg { SvgData = svgData });
        Assert.Equal(width, w, 4);
        Assert.Equal(height, h, 4);
    }

    [Fact]
    public void NoIntrinsicSizeIsReportedUnlessTheElementSetsBothWidthAndHeight()
    {
        using BonniePDF.Document unsized = Page(new Svg { SvgData = "<svg " + Ns + "/>" });
        unsized.GenerateBytes();
        Assert.Contains(unsized.Diagnostics,
            d => d.Severity == DiagnosticSeverity.Error && d.Message.Contains("no intrinsic size", StringComparison.Ordinal));

        Assert.Equal((40f, 20f), NaturalBox(new Svg { SvgData = "<svg " + Ns + "/>", Width = 40, Height = 20 }));
    }

    // ---- The box ----

    [Fact]
    public void WidthOnlyKeepsAspect()
    {
        var svg = new Svg { SvgData = "<svg " + Ns + " width=\"100\" height=\"50\"/>", Width = 150, FitMode = ImageFitMode.Contain };
        Assert.Equal((150f, 75f), NaturalBox(svg));
    }

    [Fact]
    public void FillStretchesBoxNotContent()
    {
        var svg = new Svg
        {
            SvgData = "<svg " + Ns + " viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg>",
            Width = 200,
            Height = 100,
            FitMode = ImageFitMode.Fill,
        };
        Assert.Equal((200f, 100f), NaturalBox(svg));
        // The 10x10 viewBox is centred in the 200x100 box: 100 pt square, 50 pt in.
        using BonniePDF.Document doc = Page(svg);
        string ops = Ops(doc);
        Assert.Contains("50 841.89 m\n150 841.89 l\n150 741.89 l\n50 741.89 l\n", ops[ops.IndexOf("W n", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void AspectNoneDistorts()
    {
        var svg = new Svg
        {
            SvgData = "<svg " + Ns + " viewBox=\"0 0 10 10\" preserveAspectRatio=\"none\"><rect width=\"10\" height=\"10\"/></svg>",
            Width = 200,
            Height = 100,
            FitMode = ImageFitMode.Fill,
        };
        Assert.Equal((200f, 100f), NaturalBox(svg));
        using BonniePDF.Document doc = Page(svg);
        string ops = Ops(doc);
        Assert.Contains("0 841.89 m\n200 841.89 l\n200 741.89 l\n0 741.89 l\n", ops[ops.IndexOf("W n", StringComparison.Ordinal)..], StringComparison.Ordinal);
    }

    [Fact]
    public void CoverScalesPastTheRequestedBoxAndDrawsAtThatSizeAsImageDoes()
    {
        var svg = new Svg { SvgData = "<svg " + Ns + " viewBox=\"0 0 10 20\"/>", Width = 100, Height = 100, FitMode = ImageFitMode.Cover };
        Assert.Equal((100f, 200f), NaturalBox(svg));
    }

    [Fact]
    public void AGeneratorIsGivenTheBoxAndNeedsBothWidthAndHeight()
    {
        PdfSize? seen = null;
        var svg = new Svg
        {
            SvgGenerator = size =>
            {
                seen ??= size;
                return $"<svg {Ns} viewBox=\"0 0 {size.Width} {size.Height}\"><rect width=\"10\" height=\"10\"/></svg>";
            },
            Width = 120,
            Height = 60,
        };
        using BonniePDF.Document doc = Page(svg);
        doc.GenerateBytes();
        Assert.Equal(120f, seen!.Value.Width);
        Assert.Equal(60f, seen.Value.Height);

        using BonniePDF.Document half = Page(new Svg { SvgGenerator = _ => "<svg/>", Width = 10 });
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => half.GenerateBytes());
        Assert.Contains("needs both Width and Height", error.Message, StringComparison.Ordinal);
    }

    // ---- Fonts ----

    private static string OutlineOf(IFontFace face) => JsonSerializer.Serialize(face.Outline(face.GlyphIndex(0x41)).Select(s => s.ToString()));

    private static string Liberation(string family, string subfamily) =>
        OutlineOf(new TrueTypeFace(FontManager.GetBytesForFamily(family, subfamily)!, "reference"));

    [Fact]
    public void AliasResolvesArialIsHelveticaDrawnFromLiberationSans()
    {
        IFontFace? face = new BonniePdfFontProvider().Resolve("Arial", 400, false);
        Assert.NotNull(face);
        Assert.Equal(Liberation("liberation sans", "regular"), OutlineOf(face));
    }

    [Fact]
    public void GenericResolvesMonospaceIsCourierDrawnFromLiberationMono()
    {
        IFontFace? face = new BonniePdfFontProvider().Resolve("monospace", 700, false);
        Assert.Equal(Liberation("liberation mono", "bold"), OutlineOf(face!));
    }

    [Fact]
    public void ARegisteredFontResolvesByItsOwnFamilyName()
    {
        IFontFace? face = new BonniePdfFontProvider().Resolve("Liberation Serif", 400, true);
        Assert.Equal(Liberation("liberation serif", "italic"), OutlineOf(face!));
    }

    [Fact]
    public void AnUnknownFamilyResolvesToNothingRatherThanToHelvetica()
    {
        Assert.Null(new BonniePdfFontProvider().Resolve("No Such Font", 400, false));
        Assert.Null(new BonniePdfFontProvider().Resolve("cursive", 400, false));
    }

    private static Svg Text(string family) => new()
    {
        SvgData = $"<svg {Ns} viewBox=\"0 0 100 20\"><text x=\"0\" y=\"15\" font-family=\"{family}\" font-size=\"12\">Ho</text></svg>",
        StrictUnsupportedFeatures = false,
    };

    [Fact]
    public void FallsDownTheListNothingReported()
    {
        using BonniePDF.Document doc = Page(Text("No Such Font, sans-serif"));
        string ops = Ops(doc);
        Assert.DoesNotContain(doc.Diagnostics, d => d.Source == "svg");
        Assert.Contains(" c\n", ops, StringComparison.Ordinal); // glyph outlines
    }

    [Fact]
    public void NothingResolvesTextNotDrawnReportedNamingTheFamily()
    {
        using BonniePDF.Document doc = Page(Text("No Such Font"));
        doc.GenerateBytes();
        Diagnostic reported = Assert.Single(doc.Diagnostics, d => d.Source == "svg");
        Assert.Contains("No Such Font", reported.Message, StringComparison.Ordinal);
    }

    // ---- Unsupported features ----

    [Fact]
    public void StrictFailsNamingFilter()
    {
        using BonniePDF.Document doc = Page(new Svg { SvgData = Filtered });
        SvgUnsupportedFeatureException error = Assert.Throws<SvgUnsupportedFeatureException>(() => doc.GenerateBytes());
        Assert.Equal(["filter"], error.Features.Select(f => f.Feature));
    }

    [Fact]
    public void LenientDrawsSubsetTheRectDrawnWithoutItsFilterOneDiagnosticNamingFilter()
    {
        using BonniePDF.Document doc = Page(new Svg { SvgData = Filtered, StrictUnsupportedFeatures = false });
        string ops = Ops(doc);
        Diagnostic reported = Assert.Single(doc.Diagnostics, d => d.Source == "svg");
        Assert.Contains("filter", reported.Message, StringComparison.Ordinal);
        Assert.Contains("1 0 0 rg", ops, StringComparison.Ordinal);
        Assert.Contains("0 0 1 rg", ops, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LenientRasterizesDrawnAsAnImageInItsPlaceOnTheAsyncPath()
    {
        using BonniePDF.Document doc = Page(new Svg { SvgData = Filtered, StrictUnsupportedFeatures = false });
        var snippets = new System.Collections.Generic.List<string>();
        doc.Settings.SvgRasterizer = svg =>
        {
            snippets.Add(svg);
            return Task.FromResult(Png);
        };
        byte[] bytes = await doc.GenerateBytesAsync();
        Assert.Single(snippets);
        Assert.Contains("filter", snippets[0], StringComparison.Ordinal);
        string ops = FirstContentStream(bytes);
        Assert.Matches(@"/I\d+ Do", ops);
        Assert.DoesNotContain("1 0 0 rg", ops, StringComparison.Ordinal);
        Assert.True(ops.IndexOf(" Do", StringComparison.Ordinal) < ops.IndexOf("0 0 1 rg", StringComparison.Ordinal));
        Assert.DoesNotContain(doc.Diagnostics, d => d.Source == "svg");
    }

    [Fact]
    public void ASynchronousGenerateThatWouldNeedTheRasterizerFailsNamingTheAsyncPath()
    {
        using BonniePDF.Document doc = Page(new Svg { SvgData = Filtered, StrictUnsupportedFeatures = false });
        doc.Settings.SvgRasterizer = _ => Task.FromResult(Png);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => doc.GenerateBytes());
        Assert.Contains("async", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARasterizerThatIsNotNeededDoesNotAffectTheSynchronousPath()
    {
        const string plain = "<svg " + Ns + " viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\" fill=\"#0000ff\"/></svg>";
        using BonniePDF.Document without = Page(new Svg { SvgData = plain, StrictUnsupportedFeatures = false });
        using BonniePDF.Document with = Page(new Svg { SvgData = plain, StrictUnsupportedFeatures = false });
        with.Settings.SvgRasterizer = _ => Task.FromResult(Png);
        Assert.Equal(Ops(without), Ops(with));
    }

    // ---- Tagging ----

    private static BonniePDF.Document Tagged(Svg svg)
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en";
        doc.Metadata.Title = "Svg";
        doc.Add(svg);
        return doc;
    }

    private static string Logo(string extra = "") =>
        $"<svg {Ns} viewBox=\"0 0 10 10\">{extra}<rect width=\"10\" height=\"10\"/></svg>";

    [Fact]
    public void AltTextFigureWithAlt()
    {
        using BonniePDF.Document doc = Tagged(new Svg { SvgData = Logo(), AltText = "Company logo" });
        string text = Latin1.GetString(doc.GenerateBytes());
        Assert.Contains("/S /Figure", text, StringComparison.Ordinal);
        Assert.Contains("/Alt (Company logo)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TitleAsAltFigureWithAltFromTheDocumentsTitle()
    {
        var svg = new Svg { SvgData = Logo("<title>Logo</title>") };
        Assert.Equal(SemanticRole.Figure, svg.SemanticRole);
        using BonniePDF.Document doc = Tagged(svg);
        Assert.Contains("/Alt (Logo)", Latin1.GetString(doc.GenerateBytes()), StringComparison.Ordinal);
    }

    [Fact]
    public void NoAltArtifactPreflightReportsFigureMissingAlt()
    {
        using BonniePDF.Document doc = Tagged(new Svg { SvgData = Logo() });
        doc.Add(new Heading("Title") { Level = 1 });
        Assert.DoesNotContain("/S /Figure", Latin1.GetString(doc.GenerateBytes()), StringComparison.Ordinal);
        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;
        Assert.Contains(CompliancePreflight.Evaluate(snapshot).Findings, f => f.Code == "figure-missing-alt");
    }

    [Fact]
    public void DecorativeArtifactNoPreflightFinding()
    {
        using BonniePDF.Document doc = Tagged(new Svg { SvgData = Logo(), IsDecorative = true });
        LayoutSnapshot snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot!;
        Assert.DoesNotContain(CompliancePreflight.Evaluate(snapshot).Findings, f => f.Code == "figure-missing-alt");
    }

    // ---- Compliance ----

    [Fact]
    public void PdfA1ForcesOpaque()
    {
        using BonniePDF.Document doc = Page(new Svg
        {
            SvgData = "<svg " + Ns + " viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\" fill-opacity=\"0.5\"/></svg>",
        });
        doc.Settings.Conformance = PdfAConformance.PdfA1b;
        string text = Latin1.GetString(doc.GenerateBytes());
        Assert.Contains("/ca 1.0000", text, StringComparison.Ordinal);
        Assert.DoesNotContain("/ca 0.5000", text, StringComparison.Ordinal);
    }

    // ---- Output ----

    [Fact]
    public void DrawsAtTheFlowPositionInCssPxScaledOntoPointsClippedToItsBoxSameBytesAsTypeScript()
    {
        using BonniePDF.Document doc = Page(new Svg
        {
            SvgData = "<svg " + Ns + " width=\"40\" height=\"20\" viewBox=\"0 0 4 2\"><rect x=\"1\" width=\"2\" height=\"2\" fill=\"#ff0000\"/></svg>",
            Width = 30,
            Margin = new Thickness(10, 20, 0, 0),
        });
        Assert.Equal(
            "q\nq\n10 821.89 m\n40 821.89 l\n40 806.89 l\n10 806.89 l\nh\nW n\n" +
            "q\nq\n1 0 0 rg\n17.5 821.89 m\n32.5 821.89 l\n32.5 806.89 l\n17.5 806.89 l\nh\nf\nQ\nQ\nQ\nQ\n",
            Ops(doc));
    }

    private const string Rich =
        "<svg " + Ns + " width=\"120\" height=\"80\" viewBox=\"0 0 120 80\">" +
        "<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\"><stop offset=\"0\" stop-color=\"#1d4ed8\"/>" +
        "<stop offset=\"1\" stop-color=\"#f59e0b\" stop-opacity=\"0.6\"/></linearGradient>" +
        "<clipPath id=\"c\"><circle cx=\"60\" cy=\"40\" r=\"36\"/></clipPath></defs>" +
        "<g transform=\"rotate(-8 60 40)\" clip-path=\"url(#c)\"><rect x=\"4\" y=\"4\" width=\"112\" height=\"72\" rx=\"9\" fill=\"url(#g)\"/></g>" +
        "<path d=\"M10 70 A 25 18 0 0 1 60 60 Q 85 45 110 70\" stroke=\"#111827\" stroke-width=\"2.5\" stroke-dasharray=\"4 2\" fill=\"none\" stroke-linecap=\"round\"/>" +
        "<text x=\"60\" y=\"46\" text-anchor=\"middle\" font-family=\"Arial\" font-size=\"15\" font-weight=\"bold\" fill=\"#ffffff\" opacity=\"0.9\">AVAWATYo</text></svg>";

    /// <summary>
    /// Gradient, rotated clip, dashed arc and kerned bold text, fitted at the natural
    /// size and at an arbitrary width. The hashes are asserted in both suites: the
    /// operators must be byte-identical across the implementations. A page given in
    /// points, since the two implementations' A4 differ in the last float digit.
    /// </summary>
    [Theory]
    [InlineData(null, "9296848b18978881ef6f4e9ed3179ec40e2c36a7767f63a2c5b82a07269fa51e")]
    [InlineData(173.3f, "9c314747a63c584c830785d826952564fcd01be9d16240380c71e09886abb1d7")]
    public void DrawsARichDocumentByteIdenticallyToTypeScript(float? width, string hash)
    {
        using var doc = new BonniePDF.Document(new DocumentSettings { Margin = 36, PageSize = new PdfSize(600, 800) });
        doc.Add(new Svg { SvgData = Rich, Width = width });
        string ops = Ops(doc);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(Latin1.GetBytes(ops))).ToLowerInvariant());
    }
}
