using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Element-based page templates - shared conformance fixture
/// layout/page-template-resolution. For page n the template
/// returns the first present of firstPage (n = 1), odd/even, default.
///
/// Mirrors core/tests/layout/page-template.test.ts.
/// </summary>
public class PageTemplateTests
{
    private static readonly string[] Members = ["default", "firstPage", "odd", "even"];

    /// <summary>One distinct marker element per member, so the answer can be read off by identity.</summary>
    private static Dictionary<string, IElement> Markers() => new()
    {
        ["default"] = new Container { Height = 1 },
        ["firstPage"] = new Container { Height = 2 },
        ["odd"] = new Container { Height = 3 },
        ["even"] = new Container { Height = 4 },
    };

    /// <summary>Which member's marker the template returned for a page, or null for nothing.</summary>
    private static string? Resolve(string[] members, int page)
    {
        Dictionary<string, IElement> all = Markers();
        IElement? Pick(string name) => members.Contains(name) ? all[name] : null;
        Func<int, IElement?> template = PageTemplate.FromElements(
            @default: Pick("default"), firstPage: Pick("firstPage"), odd: Pick("odd"), even: Pick("even"));

        IElement? result = template(page);
        return result is null ? null : Members.First(m => ReferenceEquals(all[m], result));
    }

    public static TheoryData<string, string[], int, string?> Cases()
    {
        TheoryData<string, string[], int, string?> data = new();
        void Add(string id, string[] members, params (int Page, string? Member)[] expect)
        {
            foreach ((int page, string? member) in expect) data.Add(id, members, page, member);
        }

        Add("default-only", ["default"], (1, "default"), (2, "default"), (3, "default"), (10, "default"));
        Add("first-page-then-default", ["default", "firstPage"], (1, "firstPage"), (2, "default"), (3, "default"));
        Add("first-page-alone", ["firstPage"], (1, "firstPage"), (2, null), (3, null));
        Add("odd-even", ["odd", "even"], (1, "odd"), (2, "even"), (3, "odd"), (4, "even"), (11, "odd"), (12, "even"));
        Add("even-alone", ["even"], (1, null), (2, "even"), (3, null), (4, "even"));
        Add("odd-even-with-default", ["default", "even"], (1, "default"), (2, "even"), (3, "default"), (4, "even"));
        Add("first-page-beats-parity", ["default", "firstPage", "odd", "even"],
            (1, "firstPage"), (2, "even"), (3, "odd"), (4, "even"));
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ReturnsTheFirstPresentMemberForThePage(string id, string[] members, int page, string? expected)
    {
        Assert.True(expected == Resolve(members, page), $"{id}, page {page}");
    }

    [Fact]
    public void FailsAtConstructionWhenBuiltFromNoElement()
    {
        Assert.Throws<ArgumentException>(() => PageTemplate.FromElements());
        Assert.Throws<ArgumentException>(() => PageTemplate.FromElements(@default: null, odd: null));
    }

    [Fact]
    public void TryGetElementsReadsBackTheElementsATemplateWasBuiltFrom()
    {
        Dictionary<string, IElement> all = Markers();
        Func<int, IElement?> template = PageTemplate.FromElements(@default: all["default"], even: all["even"]);

        Assert.True(PageTemplate.TryGetElements(template, out PageTemplateElements back));
        Assert.Same(all["default"], back.Default);
        Assert.Same(all["even"], back.Even);
        Assert.Null(back.FirstPage);
        Assert.Null(back.Odd);
    }

    [Fact]
    public void TryGetElementsIsFalseForAHandWrittenFunctionAndForNoTemplate()
    {
        Assert.False(PageTemplate.TryGetElements(_ => null, out _));
        Assert.False(PageTemplate.TryGetElements(null, out _));
    }

    private static BonniePDF.Document ThreePageDocument()
    {
        BonniePDF.Document doc = new();
        doc.Settings.Margin = 50;
        doc.Add(new TextBlock { Text = "first" });
        doc.Add(new PageBreak());
        doc.Add(new TextBlock { Text = "second" });
        doc.Add(new PageBreak());
        doc.Add(new TextBlock { Text = "third" });
        return doc;
    }

    private static List<PageRenderOutput> RenderPages(BonniePDF.Document doc)
    {
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).ToList();
    }

    [Fact]
    public void DrawsTheChosenFooterOnEachPage()
    {
        using BonniePDF.Document doc = ThreePageDocument();
        doc.Settings.FooterTemplate = PageTemplate.FromElements(
            @default: new TextBlock { Text = "Page {{CurrentPage}}" },
            even: new TextBlock { Text = "Even {{CurrentPage}}" });

        List<PageRenderOutput> pages = RenderPages(doc);

        Assert.Equal(3, pages.Count);
        Assert.Contains("(Page 1) Tj", pages[0].ContentStream);
        Assert.Contains("(Even 2) Tj", pages[1].ContentStream);
        Assert.Contains("(Page 3) Tj", pages[2].ContentStream);
        Assert.DoesNotContain("(Even", pages[0].ContentStream);
        Assert.DoesNotContain("(Page", pages[1].ContentStream);
    }

    [Fact]
    public void DrawsAFirstPageOnlyHeaderOnPageOneAlone()
    {
        using BonniePDF.Document doc = ThreePageDocument();
        doc.Settings.HeaderTemplate = PageTemplate.FromElements(firstPage: new TextBlock { Text = "Confidential" });

        List<PageRenderOutput> pages = RenderPages(doc);

        Assert.Contains("(Confidential) Tj", pages[0].ContentStream);
        Assert.DoesNotContain("Confidential", pages[1].ContentStream);
        Assert.DoesNotContain("Confidential", pages[2].ContentStream);
    }

    [Fact]
    public void MatchesAnEquivalentHandWrittenFunctionPageForPage()
    {
        TextBlock a = new() { Text = "A {{CurrentPage}}" };
        TextBlock b = new() { Text = "B {{CurrentPage}}" };

        using BonniePDF.Document fromFactory = ThreePageDocument();
        fromFactory.Settings.FooterTemplate = PageTemplate.FromElements(@default: a, even: b);
        using BonniePDF.Document fromFunction = ThreePageDocument();
        fromFunction.Settings.FooterTemplate = n => n % 2 == 0 ? b : a;

        List<string> x = RenderPages(fromFactory).Select(p => p.ContentStream).ToList();
        List<string> y = RenderPages(fromFunction).Select(p => p.ContentStream).ToList();
        Assert.Equal(y, x);
    }
}
