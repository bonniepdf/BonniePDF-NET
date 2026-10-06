using System;
using System.IO;
using System.Text;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// A pagination wrapper declares no role of its own, so it has to open its child's role the
/// way a stack or container does - otherwise a wrapped heading is written as untagged content
/// and drops out of the structure tree. Mirrors the TypeScript suite.
/// </summary>
[Collection("Serial")]
public class PaginationWrapperTaggingTests
{
    public static TheoryData<string> Wrappers => new() { "EnsureSpace", "PreventPageBreak", "ShowEntire", "ShowIf", "ShowOnce", "StopPaging" };

    private static IElement Wrap(string wrapper, Heading heading) => wrapper switch
    {
        "EnsureSpace" => new EnsureSpace(50, heading),
        "PreventPageBreak" => new PreventPageBreak(heading),
        "ShowEntire" => new ShowEntire(heading),
        "ShowIf" => new ShowIf(_ => true) { Child = heading },
        "ShowOnce" => new ShowOnce(heading),
        "StopPaging" => new StopPaging(heading),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapper)),
    };

    private static int HeadingCount(IElement? wrapped)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using var doc = new BonniePDF.Document();
            doc.Settings.Version = PdfVersion.Pdf17;
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            doc.Add(wrapped ?? new Heading("Section") { Level = 2 });
            doc.Add(new TextBlock { Text = "Body text." });
            using var ms = new MemoryStream();
            doc.Save(ms, true);
            string pdf = Encoding.ASCII.GetString(ms.ToArray());
            int count = 0;
            for (int i = pdf.IndexOf("/H2", StringComparison.Ordinal); i >= 0; i = pdf.IndexOf("/H2", i + 1, StringComparison.Ordinal))
                count++;
            return count;
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }

    [Theory]
    [MemberData(nameof(Wrappers))]
    public void WrappedHeading_StaysInTheStructureTree(string wrapper)
    {
        int unwrapped = HeadingCount(null);
        Assert.True(unwrapped > 0, "the baseline document must carry an H2");
        Assert.Equal(unwrapped, HeadingCount(Wrap(wrapper, new Heading("Section") { Level = 2 })));
    }
}
