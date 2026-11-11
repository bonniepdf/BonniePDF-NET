using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Typography;

/// <summary>Spec 22, fallback-font boundaries: a right-to-left line in two fonts is painted in visual order.</summary>
[Collection("Serial")]
public sealed partial class RtlFallbackRunTests
{
    private static string FixturePath(string name) => Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures", name);

    private static readonly JsonElement Runs =
        JsonDocument.Parse(File.ReadAllText(FixturePath("rtl.json"))).RootElement.GetProperty("fallbackRuns");

    public static TheoryData<int> Cases()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Runs.GetProperty("cases").GetArrayLength(); i++) data.Add(i);
        return data;
    }

    private static (string Text, string[][] Visual) Case(int index)
    {
        JsonElement sample = Runs.GetProperty("cases")[index];
        return (sample.GetProperty("text").GetString()!,
            sample.GetProperty("visual").EnumerateArray().Select(piece => piece.EnumerateArray().Select(v => v.GetString()!).ToArray()).ToArray());
    }

    private static PdfDocument Build(string text)
    {
        PdfDocument doc = new();
        string face = doc.Fonts.Add(File.ReadAllBytes(FixturePath(Runs.GetProperty("primary").GetString()!))).FontName;
        doc.Fonts.Add(File.ReadAllBytes(FixturePath(Runs.GetProperty("fallback").GetString()!)));
        doc.Add(new TextBlock(text)
        {
            FontName = face,
            FontSize = Runs.GetProperty("fontSize").GetSingle(),
            ContentDirection = ContentDirection.RightToLeft
        });
        return doc;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Line_IsPaintedInVisualOrder(int index)
    {
        var (text, visual) = Case(index);
        using PdfDocument doc = Build(text);
        var pieces = new List<string[]>();
        foreach (Match m in SvgTextRun().Matches(doc.GenerateSvgPages()[0]))
        {
            string role = m.Groups[2].Value.Contains("hebrew", StringComparison.Ordinal) ? "primary" : "fallback";
            string painted = System.Net.WebUtility.HtmlDecode(m.Groups[3].Value);
            if (pieces.Count > 0 && pieces[^1][0] == role) pieces[^1][1] += painted;
            else pieces.Add([role, painted]);
        }
        Assert.Equal(visual.Select(p => string.Join('|', p)), pieces.Select(p => string.Join('|', p)));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Line_HasOneActualText_InLogicalOrder(int index)
    {
        var (text, _) = Case(index);
        InternalConfiguration.EnableCompression = false;
        try
        {
            using PdfDocument doc = Build(text);
            string pdf = Encoding.Latin1.GetString(doc.GenerateBytes());
            string[] actual = ActualText().Matches(pdf)
                .Select(m => Encoding.BigEndianUnicode.GetString(Convert.FromHexString(m.Groups[1].Value)))
                .ToArray();
            Assert.Equal(new[] { text }, actual);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [GeneratedRegex("<text x=\"([^\"]+)\"[^>]*font-family:'([^']+)'[^>]*>([^<]*)</text>")]
    private static partial Regex SvgTextRun();

    [GeneratedRegex("/ActualText <FEFF([0-9A-F]+)>")]
    private static partial Regex ActualText();
}
