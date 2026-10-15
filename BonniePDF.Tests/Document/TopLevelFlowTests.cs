using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// Spec fixture layout/top-level-flow (02 §6.1, 04 §3.3): an element may span any number of
/// pages while it makes progress, is skipped with a warning after two fresh pages on which it
/// still cannot fit, and keeps its marked content balanced on every page it spans.
/// Mirrored by the TypeScript suite's top-level-flow tests.
/// </summary>
public partial class TopLevelFlowTests
{
    private const RenderStatus O = RenderStatus.Overflow;
    private const RenderStatus W = RenderStatus.WrapRequired;
    private const RenderStatus C = RenderStatus.Complete;

    [Fact]
    public void SpansMoreThan500Pages()
    {
        var element = new ScriptedElement(i => i < 799 ? O : C);
        var after = new ScriptedElement(_ => C);
        var doc = new BonniePDF.Document();
        doc.Add(element);
        doc.Add(after);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(800, element.Calls);
        Assert.Equal(800, PageCount(bytes));
        Assert.Equal(799, after.LastPageIndex);
        Assert.Empty(doc.Diagnostics);
    }

    [Fact]
    public void SkipsAnElementAfterTwoFreshPages()
    {
        var element = new ScriptedElement(_ => W);
        var after = new ScriptedElement(_ => C);
        var doc = new BonniePDF.Document();
        doc.Add(element);
        doc.Add(after);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(3, element.Calls);
        Assert.Equal(3, PageCount(bytes));
        Assert.Equal(2, after.LastPageIndex);
    }

    [Fact]
    public void ProgressResetsTheCount()
    {
        RenderStatus[] pattern = [W, W, O];
        var element = new ScriptedElement(i => i < 9 ? pattern[i % 3] : C);
        var doc = new BonniePDF.Document();
        doc.Add(element);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(10, element.Calls);
        Assert.Equal(10, PageCount(bytes));
        Assert.Empty(doc.Diagnostics);
    }

    [Fact]
    public void WarnsWhenAnElementIsSkipped()
    {
        var doc = new BonniePDF.Document();
        doc.Add(new ScriptedElement(_ => W));

        doc.GenerateBytes();

        Diagnostic warning = Assert.Single(doc.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(DiagnosticCategory.Render, warning.Category);
        Assert.Equal("pagination", warning.Source);
        Assert.Equal("An element that does not fit on an empty page was skipped (page 3).", warning.Message);
    }

    [Fact]
    public void SplitsAParagraphWithItsGraphicsStateNestedInsideOnEveryPage()
    {
        var doc = TaggedDocument();
        doc.Add(new TextBlock(string.Join(" ", Enumerable.Range(0, 600).Select(i => $"Line {i} of a long paragraph that spans pages."))));

        List<List<string>> pages = ScopeOperators(doc.GenerateBytes());

        Assert.Equal(7, pages.Count);
        foreach (List<string> ops in pages)
        {
            Assert.True(Balanced(ops));
            Assert.Equal(["/P << /MCID 0 >> BDC", "q"], ops.Take(2));
            Assert.Equal(["Q", "EMC"], ops.TakeLast(2));
        }
    }

    [Fact]
    public void KeepsADecorativeRegionAnArtifactOnEveryPage()
    {
        var doc = TaggedDocument();
        var group = new SemanticGroup(SemanticRole.Artifact);
        for (int i = 0; i < 120; i++) group.Add(new TextBlock($"Decorative line {i}"));
        doc.Add(group);

        byte[] bytes = doc.GenerateBytes();
        List<List<string>> pages = ScopeOperators(bytes);

        Assert.Equal(3, pages.Count);
        foreach (List<string> ops in pages)
        {
            Assert.True(Balanced(ops));
            Assert.Equal(["/Artifact BMC", "q"], ops.Take(2));
        }
        Assert.DoesNotContain(ContentStreams(bytes), s => s.Contains("/MCID", StringComparison.Ordinal));
    }

    private static BonniePDF.Document TaggedDocument()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Metadata.Title = "Flow";
        return doc;
    }

    private static int PageCount(byte[] pdf) => PageObject().Count(Encoding.Latin1.GetString(pdf));

    private static bool Balanced(List<string> ops)
    {
        int opens = ops.Count(op => op.EndsWith(" BDC", StringComparison.Ordinal) || op.EndsWith(" BMC", StringComparison.Ordinal));
        int closes = ops.Count(op => op == "EMC");
        return opens == closes;
    }

    /// <summary>The marked-content and graphics-state operator lines of each page content stream.</summary>
    private static List<List<string>> ScopeOperators(byte[] pdf) =>
        ContentStreams(pdf)
            .Where(s => s.Contains("BT", StringComparison.Ordinal))
            .Select(s => s.Split('\n').Where(line => ScopeOperator().IsMatch(line)).ToList())
            .ToList();

    /// <summary>Every stream in the file, inflated where it is compressed.</summary>
    private static List<string> ContentStreams(byte[] pdf)
    {
        string text = Encoding.Latin1.GetString(pdf);
        List<string> streams = [];
        for (int start = text.IndexOf("stream\n", StringComparison.Ordinal); start >= 0;
             start = text.IndexOf("stream\n", start, StringComparison.Ordinal))
        {
            int content = start + "stream\n".Length;
            int end = text.IndexOf("endstream", content, StringComparison.Ordinal);
            byte[] raw = pdf[content..end];
            try
            {
                using var zlib = new ZLibStream(new MemoryStream(raw), CompressionMode.Decompress);
                using var reader = new StreamReader(zlib, Encoding.Latin1);
                streams.Add(reader.ReadToEnd());
            }
            catch (InvalidDataException)
            {
                streams.Add(Encoding.Latin1.GetString(raw));
            }
            start = end + "endstream".Length;
        }
        return streams;
    }

    [GeneratedRegex(@"/Type\s*/Page(?![a-zA-Z])")]
    private static partial Regex PageObject();

    [GeneratedRegex(@"(?:^|\s)(?:q|Q|BDC|BMC|EMC)$")]
    private static partial Regex ScopeOperator();

    /// <summary>Returns the scripted status for each RenderStateful call.</summary>
    private sealed class ScriptedElement(Func<int, RenderStatus> results) : IStatefulElement
    {
        public int Calls { get; private set; }
        public int LastPageIndex { get; private set; } = -1;

        public float MeasureHeight(float availableWidth) => 10;
        public float GetPreferredWidth() => 0;
        public void Render(RenderContext ctx, float width) { }

        public RenderStatus RenderStateful(RenderContext ctx, float width)
        {
            ctx.CurrentY += 10;
            LastPageIndex = ctx.CurrentPageIndex;
            return results(Calls++);
        }

        public void ResetState() { }
    }
}
