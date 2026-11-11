using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Spec 03 §3, Markers: an item's marker is drawn beside its first line only, tagged or not
/// (fixtures/layout/list-markers.json).
/// </summary>
[Collection("Serial")]
public sealed partial class ListMarkerTests
{
    private const string Long =
        "This is a long list item that will certainly wrap onto a second line because it keeps going and going well past the right margin of the page.";

    private static string PageContent(bool tagged)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using BonniePDF.Document doc = new();
            if (tagged)
            {
                doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
                doc.Settings.Language = "en-GB";
            }
            OrderedList list = new();
            list.AddItem(Long);
            list.AddItem("Short item");
            doc.Add(list);
            return Encoding.Latin1.GetString(doc.GenerateBytes());
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WrappedItem_DrawsItsNumberOnce(bool tagged)
    {
        string[] expected =
        [
            "1.",
            "This is a long list item that will certainly wrap onto a second line because it keeps going",
            "and going well past the right margin of the page.",
            "2.",
            "Short item",
        ];
        Assert.Equal(expected, DrawnRuns(PageContent(tagged), tagged));
    }

    /// <summary>The text of each run drawn on the page: literal strings untagged, their ActualText when tagged (embedded font).</summary>
    private static string[] DrawnRuns(string content, bool tagged) =>
        (tagged ? ActualTextRun() : LiteralRun()).Matches(content).Select(m => m.Groups[1].Value).ToArray();

    [GeneratedRegex(@"/ActualText \((.*?)\) >>")]
    private static partial Regex ActualTextRun();

    [GeneratedRegex(@"\((.*?)\) Tj")]
    private static partial Regex LiteralRun();
}
