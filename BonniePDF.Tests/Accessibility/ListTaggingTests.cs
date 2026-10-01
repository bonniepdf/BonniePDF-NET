using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Text;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

[Collection("Serial")]

/// <summary>
/// List structure tagging. A tagged <see cref="OrderedList"/> / <see cref="UnorderedList"/>
/// must produce <c>/L &gt; /LI &gt; /Lbl + /LBody</c> in the structure tree
/// (mirrors the TypeScript ListTaggingTests).
/// </summary>
public class ListTaggingTests
{
    private static string GenerateTagged(System.Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    private static OrderedList MakeOrdered(params string[] items)
    {
        var list = new OrderedList();
        foreach (string item in items) list.AddItem(item);
        return list;
    }

    private static UnorderedList MakeUnordered(params string[] items)
    {
        var list = new UnorderedList { BulletStyle = BulletStyle.Disc };
        foreach (string item in items) list.AddItem(item);
        return list;
    }

    // ── Struct-elem presence ────────────────────────────────────────────────

    [Fact]
    public void TaggedOrderedList_EmitsLLILblLBody()
    {
        string pdf = GenerateTagged(d => d.Add(MakeOrdered("Alpha", "Beta", "Gamma")));

        Assert.Contains("/S /L", pdf);
        Assert.Contains("/S /LI", pdf);
        Assert.Contains("/S /Lbl", pdf);
        Assert.Contains("/S /LBody", pdf);
    }

    [Fact]
    public void TaggedUnorderedList_EmitsLLILblLBody()
    {
        string pdf = GenerateTagged(d => d.Add(MakeUnordered("One", "Two", "Three")));

        Assert.Contains("/S /L", pdf);
        Assert.Contains("/S /LI", pdf);
        Assert.Contains("/S /Lbl", pdf);
        Assert.Contains("/S /LBody", pdf);
    }

    // ── Struct-elem counts ──────────────────────────────────────────────────

    [Fact]
    public void TaggedOrderedList_ThreeItems_ThreeLI()
    {
        string pdf = GenerateTagged(d => d.Add(MakeOrdered("A", "B", "C")));

        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /LI"));
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /Lbl"));
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /LBody"));
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /L\n"));
    }

    [Fact]
    public void TaggedUnorderedList_TwoItems_TwoLI()
    {
        string pdf = GenerateTagged(d => d.Add(MakeUnordered("X", "Y")));

        Assert.Equal(2, SubstringCounter.Count(pdf, "/S /LI"));
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /L\n"));
    }

    // ── Untagged path unchanged ─────────────────────────────────────────────

    [Fact]
    public void UntaggedOrderedList_EmitsNoStructElems()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        doc.Add(MakeOrdered("A", "B"));

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain("/S /L", pdf);
        Assert.DoesNotContain("/S /LI", pdf);
        Assert.DoesNotContain("/S /Lbl", pdf);
        Assert.DoesNotContain("/S /LBody", pdf);
    }

    [Fact]
    public void UntaggedUnorderedList_EmitsNoStructElems()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        doc.Add(MakeUnordered("X", "Y"));

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain("/S /L", pdf);
        Assert.DoesNotContain("/S /LI", pdf);
    }

    // ── Nested lists ────────────────────────────────────────────────────────

    [Fact]
    public void TaggedNestedOrderedList_TwoLStructElems()
    {
        var outer = MakeOrdered("Parent A", "Parent B");
        var inner = MakeOrdered("Child 1", "Child 2");
        outer.AddNestedList(inner);

        string pdf = GenerateTagged(d => d.Add(outer));

        Assert.Equal(2, SubstringCounter.Count(pdf, "/S /L\n"));
        Assert.Equal(4, SubstringCounter.Count(pdf, "/S /LI"));
    }

    [Fact]
    public void TaggedNestedUnorderedList_TwoLStructElems()
    {
        var outer = MakeUnordered("Main 1", "Main 2");
        var inner = MakeUnordered("Sub a", "Sub b", "Sub c");
        outer.AddNestedList(inner);

        string pdf = GenerateTagged(d => d.Add(outer));

        Assert.Equal(2, SubstringCounter.Count(pdf, "/S /L\n"));
        Assert.Equal(5, SubstringCounter.Count(pdf, "/S /LI"));
    }

    // ── Marked-content balance ──────────────────────────────────────────────

    [Fact]
    public void DefaultTextStyle_CascadesIntoNestedOrderedAndUnorderedLists()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle().WithColor(ColorPalette.Slate[700]);

        var orderedOuter = MakeOrdered("Parent");
        var orderedInner = MakeOrdered("Child");
        orderedOuter.AddNestedList(orderedInner);
        doc.Add(orderedOuter);

        var unorderedOuter = MakeUnordered("Parent");
        var unorderedInner = MakeUnordered("Child");
        unorderedOuter.AddNestedList(unorderedInner);
        doc.Add(unorderedOuter);

        using var ms = new MemoryStream();
        doc.Save(ms, true);

        Assert.Equal(ColorPalette.Slate[700], orderedInner.Color);
        Assert.Equal(ColorPalette.Slate[700], unorderedInner.Color);
        Assert.Equal(ColorPalette.Slate[700], unorderedInner.BulletColor);
    }

    [Fact]
    public void ExplicitUnorderedBulletColor_IsPreservedWhenTextStyleCascades()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.DefaultTextStyle = new TextStyle().WithColor(ColorPalette.Slate[700]);

        var outer = MakeUnordered("Parent");
        var inner = MakeUnordered("Child");
        inner.BulletColor = Color.Red;
        outer.AddNestedList(inner);
        doc.Add(outer);

        using var ms = new MemoryStream();
        doc.Save(ms, true);

        Assert.Equal(ColorPalette.Slate[700], inner.Color);
        Assert.Equal(Color.Red, inner.BulletColor);
    }

    [Fact]
    public void TaggedOrderedList_EveryPageStreamIsMarkedContentBalanced()
    {
        // Disable compression so PdfPageStreamSplitter can read the content streams
        // as plain text; production compression is verified by other build artefacts.
        InternalConfiguration.EnableCompression = false;
        try
        {
            // 20 items fit on one page; avoids cross-page close complexity that is a
            // pre-existing infrastructure issue unrelated to list tagging.
            string pdf = GenerateTagged(d =>
            {
                var list = new OrderedList();
                for (int i = 1; i <= 20; i++) list.AddItem($"Item {i}");
                d.Add(list);
            });

            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(streams);
            foreach (string stream in streams)
            {
                // Container elements emit BMC (not BDC), so count both open types.
                int opens = SubstringCounter.Count(stream, "BDC") + SubstringCounter.Count(stream, "BMC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(opens == emc, $"unbalanced marked-content on a page: opens={opens} EMC={emc}");
            }
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }

    [Fact]
    public void TaggedUnorderedList_EveryPageStreamIsMarkedContentBalanced()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(d =>
            {
                var list = new UnorderedList { BulletStyle = BulletStyle.Disc };
                for (int i = 1; i <= 20; i++) list.AddItem($"Item {i}");
                d.Add(list);
            });

            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(streams);
            foreach (string stream in streams)
            {
                int opens = SubstringCounter.Count(stream, "BDC") + SubstringCounter.Count(stream, "BMC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(opens == emc, $"unbalanced marked-content on a page: opens={opens} EMC={emc}");
            }
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }
}
