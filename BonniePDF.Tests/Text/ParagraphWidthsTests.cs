using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace BonniePDF.Tests.Text;

/// <summary>
/// Word wrapping reads candidate-line widths from one shaping of the paragraph where both ends
/// of a line are safe-to-break boundaries. Those widths must be exactly what measuring the line
/// on its own returns, or line breaks would change.
/// </summary>
public class ParagraphWidthsTests(ITestOutputHelper output)
{
    // Kerning pairs (AV, To, LT), ligature triggers (fi, ffl), punctuation, digits and quotes.
    private static readonly string[] Vocabulary =
    [
        "office", "affluent", "fjord", "Wave", "AVATAR", "To.", "Yes,", "LT", "P.", "\"quoted\"", "(paren)", "don't",
        "1,234.56", "--", "e-mail", "ff", "fi", "fl", "ffi", "ffl", "The", "first", "staff", "lorem", "ipsum", "Tyre",
        "VAT", "y.", "r.", "f/", "W,", "Fly", "awkward", "kV", "T-", "rv", "Yo", "LY", "a", "I", "x", "#42", "100%",
    ];

    private static IEnumerable<string> FontPaths()
    {
        string root = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(root, "BonniePDF", "Typography", "Fonts"))) root = Path.GetDirectoryName(root)!;
        foreach (string name in new[] { "LiberationSans-Regular", "LiberationSans-Bold", "LiberationSerif-Regular", "LiberationSerif-Italic", "LiberationMono-Regular" })
            yield return Path.Combine(root, "BonniePDF", "Typography", "Fonts", name + ".ttf");
        yield return Path.Combine(root, "BonniePDF.Tests", "Typography", "Fixtures", "NotoSans-Regular.ttf");
        // A wider sweep, when a caller points at more fonts.
        foreach (string dir in (Environment.GetEnvironmentVariable("BONNIEPDF_EXTRA_FONT_DIRS") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                if (file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    yield return file;
    }

    private static string Paragraph(Random random, int words)
    {
        var text = new StringBuilder();
        for (int i = 0; i < words; i++)
        {
            if (i > 0) text.Append(random.Next(12) == 0 ? "  " : " ");
            text.Append(Vocabulary[random.Next(Vocabulary.Length)]);
        }
        return text.ToString();
    }

    [Fact]
    public void SpanWidthsEqualMeasuringTheSpan()
    {
        var featureSets = new IReadOnlyDictionary<string, int>?[] { null, new Dictionary<string, int> { ["liga"] = 0 }, new Dictionary<string, int> { ["tnum"] = 1 } };
        int fonts = 0;
        foreach (string font in FontPaths())
        {
            if (FontMetrics.ResolveShapingFont(font, out _) is not { } parsed || !TextRunShaper.PrimaryCovers("AVfi\"", parsed)) continue;
            fonts++;
            var random = new Random(11);
            long spans = 0, answered = 0;
            foreach (var features in featureSets)
            {
                for (int p = 0; p < 8; p++)
                {
                    string text = Paragraph(random, 30);
                    ParagraphWidths? widths = ParagraphWidths.TryCreate(text, font, 11, features);
                    Assert.NotNull(widths);
                    var starts = new List<int> { 0 };
                    var ends = new List<int>();
                    for (int i = 0; i < text.Length; i++)
                        if (text[i] == ' ') { ends.Add(i); starts.Add(i + 1); }
                    ends.Add(text.Length);
                    foreach (int s in starts)
                        foreach (int e in ends)
                        {
                            if (e <= s) continue;
                            spans++;
                            if (!widths!.TryGetWidth(s, e, out float width)) continue;
                            answered++;
                            float measured = FontMetrics.GetStringWidth(text.Substring(s, e - s), font, 11, features);
                            Assert.True(measured == width, $"{Path.GetFileName(font)} [{string.Join(",", features ?? new Dictionary<string, int>())}] \"{text.Substring(s, e - s)}\": {width} from the paragraph, {measured} measured");
                            // The line's glyphs, read the same way, are those of shaping the line on its own.
                            var sliced = widths.TryGetGlyphs(s, e);
                            Assert.NotNull(sliced);
                            var shaped = parsed.Shape(text.Substring(s, e - s), features);
                            Assert.Equal(shaped.Count, sliced!.Length);
                            for (int g = 0; g < shaped.Count; g++)
                            {
                                var (x, y) = (shaped[g], sliced[g]);
                                Assert.True(x.Id == y.Id && x.Cluster == y.Cluster && x.AdvanceX == y.AdvanceX && x.AdvanceY == y.AdvanceY &&
                                    x.OffsetX == y.OffsetX && x.OffsetY == y.OffsetY && x.ComponentCount == y.ComponentCount && x.IsMark == y.IsMark &&
                                    x.IsLigature == y.IsLigature && x.SourceLength == y.SourceLength && x.UnsafeToBreak == y.UnsafeToBreak &&
                                    x.ComponentIndex == y.ComponentIndex, $"{Path.GetFileName(font)} \"{text.Substring(s, e - s)}\" glyph {g} differs");
                            }
                        }
                }
            }
            output.WriteLine($"{Path.GetFileName(font)}: {answered} of {spans} spans read from the paragraph");
            Assert.True(answered > 0);
        }
        Assert.True(fonts >= 6);
    }

    [Fact]
    public void DeclinesWhatOneShapingCannotAnswer()
    {
        string font = FontPaths().First();
        Assert.Null(ParagraphWidths.TryCreate("café au lait", font, 11, null));   // outside printable ASCII
        Assert.Null(ParagraphWidths.TryCreate("tab\there", font, 11, null));          // control character
        Assert.Null(ParagraphWidths.TryCreate("plain text here", "Helvetica", 11, null)); // standard font: width table
        Assert.NotNull(ParagraphWidths.TryCreate("plain text here", font, 11, null));
    }
}
