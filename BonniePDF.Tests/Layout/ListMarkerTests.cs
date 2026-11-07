using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Spec 03 §3, Markers: an item's marker is drawn beside its first line only, tagged or not
/// (fixtures/layout/list-markers.json).
/// </summary>
[Collection("Serial")]
public sealed class ListMarkerTests
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
        string content = PageContent(tagged);
        Assert.Contains("(and going well past the right margin of the page.) Tj", content, System.StringComparison.Ordinal);
        Assert.Single(Regex.Matches(content, @"\(1\.\) Tj"));
        Assert.Single(Regex.Matches(content, @"\(2\.\) Tj"));
    }
}
