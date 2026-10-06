using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// Document fonts - shared conformance fixture fonts/document-font-resolution (spec 16).
/// A document owns its faces: several families, several weights and styles each, resolved by
/// style then CSS weight matching, ahead of the global registries and invisible to any other
/// document.
///
/// Mirrors core/tests/typography/document-fonts.test.ts.
/// </summary>
public partial class DocumentFontsTests
{
    [GeneratedRegex(@"/BaseFont /(?:[A-Z]{6}\+)?([A-Za-z0-9-]+)")]
    private static partial Regex BaseFontName();

    [GeneratedRegex(@"/ID \[[^\]]*\]|/(?:CreationDate|ModDate) \([^)]*\)")]
    private static partial Regex TrailerId();

    [GeneratedRegex("[A-Z]{6}\\+")]
    private static partial Regex SubsetPrefix();

    private static byte[] Bundled(string family, string subfamily) =>
        FontManager.GetBytesForFamily(family, subfamily) ?? throw new InvalidOperationException($"{family} {subfamily} is not bundled.");

    private static readonly byte[] SansRegular = Bundled("liberation sans", "regular");
    private static readonly byte[] SansBold = Bundled("liberation sans", "bold");
    private static readonly byte[] SansItalic = Bundled("liberation sans", "italic");
    private static readonly byte[] SansBoldItalic = Bundled("liberation sans", "bolditalic");
    private static readonly byte[] SerifRegular = Bundled("liberation serif", "regular");

    /// <summary>The base font names of every font in a PDF, subset prefix removed.</summary>
    private static List<string> EmbeddedFonts(byte[] pdf) =>
        BaseFontName().Matches(Encoding.Latin1.GetString(pdf)).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal).ToList();

    /// <summary>Adds one face per spec ("400", "700", "400I"), each from the same font file.</summary>
    private static void FacesOf(DocumentFonts fonts, string family, params string[] specs)
    {
        foreach (string spec in specs)
            fonts.Add(SansRegular, family, (FontWeight)int.Parse(spec.Replace("I", "", StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture),
                spec.EndsWith('I'));
    }

    private static string? Name(DocumentFontFace? face) => face is null ? null : $"{face.Weight}{(face.Italic ? "I" : "")}";

    public static TheoryData<string, string[], string, int, bool, string?> Cases()
    {
        TheoryData<string, string[], string, int, bool, string?> data = new();
        void Add(string id, string[] faces, params (string Family, int Weight, bool Italic, string? Face)[] expect)
        {
            foreach ((string family, int weight, bool italic, string? face) in expect) data.Add(id, faces, family, weight, italic, face);
        }

        Add("exact-match", ["400", "700", "400I", "700I"],
            ("Inter", 400, false, "400"), ("Inter", 700, false, "700"), ("Inter", 400, true, "400I"), ("Inter", 700, true, "700I"));
        Add("case and space", ["400"], ("inter", 400, false, "400"), ("  INTER ", 400, false, "400"));
        Add("unknown family", ["400"], ("Roboto", 400, false, null), ("Arial", 400, false, null));
        Add("above 500 prefers heavier", ["400", "700"], ("Inter", 600, false, "700"), ("Inter", 800, false, "700"), ("Inter", 900, false, "700"));
        Add("below 400 prefers lighter", ["300", "400", "700"], ("Inter", 200, false, "300"), ("Inter", 100, false, "300"), ("Inter", 350, false, "300"));
        Add("400 to 500", ["400", "700"], ("Inter", 500, false, "400"), ("Inter", 450, false, "400"));
        Add("400 to 500 takes medium", ["400", "500", "700"], ("Inter", 450, false, "500"), ("Inter", 400, false, "400"), ("Inter", 500, false, "500"));
        Add("only heavier available", ["700"], ("Inter", 400, false, "700"));
        Add("italic preferred", ["400", "700", "400I"], ("Inter", 400, true, "400I"), ("Inter", 700, true, "400I"));
        Add("no italic falls back to upright", ["400", "700"], ("Inter", 400, true, "400"), ("Inter", 700, true, "700"));
        Add("italic-only serves upright", ["400I"], ("Inter", 400, false, "400I"));
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ResolvesTheFaceTheFixtureNames(string id, string[] faces, string family, int weight, bool italic, string? expected)
    {
        DocumentFonts fonts = new();
        FacesOf(fonts, "Inter", faces);

        Assert.True(expected == Name(fonts.Resolve(family, (FontWeight)weight, italic)), $"{id}: {family} {weight}{(italic ? "I" : "")}");
    }

    [Fact]
    public void ReplacesAFaceWithTheSameIdentityAndKeepsItsPlace()
    {
        DocumentFonts fonts = new();
        FacesOf(fonts, "Inter", "400", "700");
        fonts.Add(SansBold, "inter", FontWeight.Normal);

        Assert.Equal([400, 700], fonts.Faces.Select(f => f.Weight));
        Assert.Same(SansBold, fonts.Faces[0].Bytes);
    }

    [Fact]
    public void HoldsSeveralFamilies_EachResolvingWithinItself()
    {
        DocumentFonts fonts = new();
        FacesOf(fonts, "Inter", "400", "700");
        FacesOf(fonts, "Lora", "400", "400I");

        Assert.Equal("700", Name(fonts.Resolve("Inter", FontWeight.Bold, false)));
        Assert.Equal("400I", Name(fonts.Resolve("Lora", FontWeight.Normal, true)));
        Assert.Equal("400", Name(fonts.Resolve("Lora", FontWeight.Bold, false)));
    }

    [Fact]
    public void Add_ReadsFamilyWeightAndItalicFromTheFont()
    {
        DocumentFonts fonts = new();
        DocumentFontFace regular = fonts.Add(SansRegular);
        DocumentFontFace bold = fonts.Add(SansBold);
        DocumentFontFace italic = fonts.Add(SansItalic);
        DocumentFontFace boldItalic = fonts.Add(SansBoldItalic);

        Assert.Equal("Liberation Sans", regular.Family);
        Assert.Equal((400, false), (regular.Weight, regular.Italic));
        Assert.Equal((700, false), (bold.Weight, bold.Italic));
        Assert.Equal((400, true), (italic.Weight, italic.Italic));
        Assert.Equal((700, true), (boldItalic.Weight, boldItalic.Italic));
    }

    [Fact]
    public void Add_LetsExplicitValuesWinOverTheFont()
    {
        DocumentFontFace face = new DocumentFonts().Add(SansRegular, "Inter", FontWeight.SemiBold, true);

        Assert.Equal(("Inter", 600, true), (face.Family, face.Weight, face.Italic));
    }

    [Fact]
    public void Add_RejectsBytesThatAreNotAFontAndWeightsOutsideTheRange()
    {
        DocumentFonts fonts = new();

        Assert.Throws<ArgumentException>(() => fonts.Add([1, 2, 3, 4]));
        Assert.Throws<ArgumentException>(() => fonts.Add(SansRegular, "Inter", (FontWeight)0));
        Assert.Throws<ArgumentException>(() => fonts.Add(SansRegular, "Inter", (FontWeight)1001));
        Assert.True(fonts.IsEmpty);
    }

    [Fact]
    public void EmbedsTheFaceAFamilyWeightAndStyleResolveTo()
    {
        using BonniePDF.Document doc = new();
        doc.Fonts.Add(SansRegular, "Inter");
        doc.Fonts.Add(SansBold, "Inter", FontWeight.Bold);
        doc.Fonts.Add(SansItalic, "Inter", italic: true);
        doc.Add(new TextBlock { Text = "Plain", FontFamily = "Inter" });
        doc.Add(new TextBlock { Text = "Strong", FontFamily = "Inter", FontWeight = FontWeight.Bold });
        doc.Add(new TextBlock { Text = "Slanted", FontFamily = "Inter", Italic = true });

        Assert.Equal(["LiberationSans", "LiberationSans-Bold", "LiberationSans-Italic"], EmbeddedFonts(doc.GenerateBytes()));
    }

    [Fact]
    public void AppliesWhateverOrderTheFamilyAndTheFontsWereSetIn()
    {
        using BonniePDF.Document doc = new();
        doc.Add(new TextBlock { Text = "Set first", FontFamily = "Inter" });
        doc.Fonts.Add(SerifRegular, "Inter"); // added after the element named it

        Assert.Equal(["LiberationSerif"], EmbeddedFonts(doc.GenerateBytes()));
    }

    [Fact]
    public void LetsTwoDocumentsUseDifferentFilesUnderTheSameFamilyName()
    {
        using BonniePDF.Document a = new();
        a.Fonts.Add(SansRegular, "Inter");
        a.Add(new TextBlock { Text = "A", FontFamily = "Inter" });
        using BonniePDF.Document b = new();
        b.Fonts.Add(SerifRegular, "Inter");
        b.Add(new TextBlock { Text = "B", FontFamily = "Inter" });
        using BonniePDF.Document neither = new();
        neither.Add(new TextBlock { Text = "C", FontFamily = "Inter" });

        Assert.Equal(["LiberationSans"], EmbeddedFonts(a.GenerateBytes()));
        Assert.Equal(["LiberationSerif"], EmbeddedFonts(b.GenerateBytes()));
        Assert.DoesNotContain("LiberationSerif", EmbeddedFonts(neither.GenerateBytes()));
        Assert.Null(FontManager.GetBytesForFamily("Inter", "Regular"));
    }

    [Fact]
    public void LetsTwoDocumentsGenerateAtTheSameTimeWithoutSeeingEachOthersFonts()
    {
        byte[] Generate(byte[] font, string text)
        {
            using BonniePDF.Document doc = new();
            doc.Fonts.Add(font, "Inter");
            doc.Add(new TextBlock { Text = text, FontFamily = "Inter" });
            return doc.GenerateBytes();
        }

        Task<byte[]>[] tasks = Enumerable.Range(0, 16)
            .Select(i => Task.Run(() => i % 2 == 0 ? Generate(SansRegular, "sans") : Generate(SerifRegular, "serif")))
            .ToArray();
        Task.WaitAll(tasks);

        for (int i = 0; i < tasks.Length; i++)
            Assert.Equal([i % 2 == 0 ? "LiberationSans" : "LiberationSerif"], EmbeddedFonts(tasks[i].Result));
    }

    [Fact]
    public void ShadowsAStandardFamilyForThatDocumentOnly()
    {
        using BonniePDF.Document own = new();
        own.Fonts.Add(SerifRegular, "Helvetica");
        own.Add(new TextBlock { Text = "Mine", FontFamily = "Helvetica" });
        using BonniePDF.Document plain = new();
        plain.Add(new TextBlock { Text = "Standard", FontFamily = "Helvetica" });

        Assert.Equal(["LiberationSerif"], EmbeddedFonts(own.GenerateBytes()));
        Assert.Equal(["Helvetica"], EmbeddedFonts(plain.GenerateBytes())); // the neighbour still gets the standard font
    }

    [Fact]
    public void MeasuresWithTheFaceItDrawsWith()
    {
        DocumentFonts fonts = new();
        DocumentFontFace bold = fonts.Add(SansBold, "Inter", FontWeight.Bold);
        using IDisposable scope = FontMetrics.BeginDocumentScope(false, fonts);

        float measured = FontMetrics.GetStringWidth("Measure me", bold.FontName, 12);

        Assert.Equal(FontMetrics.GetKernedStringWidth("Measure me", bold.Font, 12), measured, 4);
        Assert.NotEqual(FontMetrics.GetStringWidth("Measure me", "Helvetica-Bold", 12), measured, 3);
    }

    [Fact]
    public void UsesDocumentFontsInTheSvgExportAsWell()
    {
        using BonniePDF.Document doc = new();
        doc.Fonts.Add(SerifRegular, "Inter");
        doc.Add(new TextBlock { Text = "Serif please", FontFamily = "Inter" });

        Assert.Contains("@font-face", doc.GenerateSvgDocument().FontFaceCss, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratesIdenticalBytesTwice()
    {
        static BonniePDF.Document Build()
        {
            BonniePDF.Document doc = new();
            doc.Fonts.Add(SansRegular, "Inter");
            doc.Fonts.Add(SansBold, "Inter", FontWeight.Bold);
            doc.Add(new TextBlock { Text = "Same", FontFamily = "Inter", FontWeight = FontWeight.Bold });
            return doc;
        }

        // The trailer's /ID and the subset prefixes are random per generation.
        static string Mask(byte[] pdf) =>
            SubsetPrefix().Replace(TrailerId().Replace(Encoding.Latin1.GetString(pdf), "/ID []"), "SUBSET+");

        using BonniePDF.Document a = Build();
        using BonniePDF.Document b = Build();
        Assert.Equal(Mask(a.GenerateBytes()), Mask(b.GenerateBytes()));
    }

    [Fact]
    public void LeavesADocumentWithoutFontsOfItsOwnExactlyAsBefore()
    {
        using BonniePDF.Document doc = new();
        doc.Add(new TextBlock { Text = "No document fonts" });

        Assert.True(doc.Fonts.IsEmpty);
        Assert.Equal(["Helvetica"], EmbeddedFonts(doc.GenerateBytes())); // the standard font, unembedded
    }
}
