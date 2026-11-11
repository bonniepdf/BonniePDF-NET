using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Content taller than a page - shared conformance fixture <c>layout/oversized-content</c> (spec 02 §4.1-4.3).
/// A4 portrait, uniform 50pt margin: the body band is 741.89pt, from y = 50 to y = 791.89 in top-down coordinates.
/// Mirrors the TypeScript oversized-content tests.
/// </summary>
public partial class OversizedContentTests
{
    private static string Words(int n) => string.Join(' ', Enumerable.Range(0, n).Select(i => $"word{i}"));

    private sealed record Result(byte[] Bytes, string Pdf, List<string> Streams, List<Diagnostic> Errors, List<Diagnostic> Warnings)
    {
        public string Text => string.Join("\n", Streams);
    }

    private static Result Generate(Action<BonniePDF.Document> build, bool tagged = false)
    {
        DocumentSettings settings = tagged ? new() { Language = "en-GB", Accessibility = AccessibilityMode.PdfUa1 } : new();
        using BonniePDF.Document doc = new(settings);
        build(doc);
        byte[] bytes = doc.GenerateBytes();
        List<string> streams = PageStreams(bytes);
        return new Result(bytes, System.Text.Encoding.Latin1.GetString(bytes), streams,
            doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList(),
            doc.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList());
    }

    [GeneratedRegex(@"(?<!end)stream\r?\n")]
    private static partial Regex StreamKeyword();

    [GeneratedRegex(@"/Length (\d+)")]
    private static partial Regex LengthEntry();

    [GeneratedRegex(@"1 0 0 1 (-?[\d.]+) (-?[\d.]+) Tm")]
    private static partial Regex TextMatrix();

    [GeneratedRegex(@"/S /TR\b")]
    private static partial Regex RowTag();

    [GeneratedRegex(@"/S /TD\b")]
    private static partial Regex CellTag();

    [GeneratedRegex(@" c\n")]
    private static partial Regex CurveOperator();

    // The inflated streams that draw text or paths: page content, not fonts or images.
    private static List<string> PageStreams(byte[] pdf)
    {
        string text = System.Text.Encoding.Latin1.GetString(pdf);
        var streams = new List<string>();
        foreach (Match m in StreamKeyword().Matches(text))
        {
            string before = text.Substring(Math.Max(0, m.Index - 600), Math.Min(600, m.Index));
            MatchCollection lengths = LengthEntry().Matches(before);
            if (lengths.Count == 0) continue;
            try
            {
                using var inflated = new MemoryStream();
                using (var z = new ZLibStream(new MemoryStream(pdf, m.Index + m.Length, int.Parse(lengths[^1].Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)), CompressionMode.Decompress))
                    z.CopyTo(inflated);
                string content = System.Text.Encoding.Latin1.GetString(inflated.ToArray());
                if (content.StartsWith('q') || content.Contains(" Tj", StringComparison.Ordinal)) streams.Add(content);
            }
            catch (InvalidDataException)
            {
            }
        }
        return streams;
    }

    private static IEnumerable<float> LowestText(IEnumerable<string> streams) =>
        streams.Select(s => TextMatrix().Matches(s).Select(m => float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(float.MaxValue).Min());

    private static int Count(string text, string word) => Regex.Count(text, Regex.Escape(word) + @"\b", RegexOptions.None, TimeSpan.FromSeconds(1));

    // ---- group table-rows ----

    [Fact]
    public void SplitsASingleCellRowAcrossPagesAndKeepsEveryWord()
    {
        Result r = Generate(doc =>
        {
            var t = new Table(1);
            t.AddRow(new TextBlock(Words(3000)));
            doc.Add(t);
        });
        Assert.True(r.Streams.Count > 1);
        Assert.Empty(r.Errors);
        Assert.Empty(r.Warnings);
        Assert.All(LowestText(r.Streams), y => Assert.True(y >= 50, $"text at y={y}"));
        Assert.Contains("word0 ", r.Text, StringComparison.Ordinal);
        Assert.Contains("word2999", r.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ContinuesEveryUnfinishedCellAndDrawsAShortCellOnce()
    {
        Result r = Generate(doc =>
        {
            var t = new Table(1, 1);
            t.AddHeader(new TextBlock("Left"), new TextBlock("Right"));
            t.AddRow(new TextBlock(Words(2000)), new TextBlock("short"));
            doc.Add(t);
        });
        Assert.Empty(r.Errors);
        Assert.Empty(r.Warnings);
        Assert.Equal(1, Count(r.Text, "short"));
        Assert.Equal(r.Streams.Count, Count(r.Text, "Left"));
        Assert.Contains("word1999", r.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void MovesARowFirstWhenUnsplittableContentWouldBeCutHere()
    {
        Result r = Generate(doc =>
        {
            doc.Add(new Spacer(600));
            var t = new Table(1, 1);
            t.AddRow(new TextBlock(Words(2000)), new Container { Height = 300, Child = new TextBlock("box") });
            doc.Add(t);
        });
        Assert.Empty(r.Errors);
        Assert.DoesNotContain("word0 ", r.Streams[0], StringComparison.Ordinal);
    }

    [Fact]
    public void DrawsAFixedHeightBoxInASplitRowOnce()
    {
        Result r = Generate(doc =>
        {
            var t = new Table(1, 1);
            t.AddRow(new TextBlock(Words(2000)), new Container { Height = 100, Child = new TextBlock("boxed") });
            doc.Add(t);
        });
        Assert.Empty(r.Errors);
        Assert.Empty(r.Warnings);
        Assert.Equal(1, Count(r.Text, "boxed"));
        Assert.Contains("word1999", r.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void IsOneTrWithOneTdPerCellHoweverManyPagesItSpans()
    {
        Result r = Generate(doc =>
        {
            var t = new Table(1, 1);
            t.AddRow(new TextBlock(Words(2000)), new TextBlock(Words(10)));
            doc.Add(t);
        }, tagged: true);
        Assert.True(r.Streams.Count > 1);
        Assert.Equal(1, RowTag().Count(r.Pdf));
        Assert.Equal(2, CellTag().Count(r.Pdf));
    }

    [Fact]
    public void AddsASplitRowToARunningTotalOnce()
    {
        Result r = Generate(doc =>
        {
            var t = new Table(3, 1) { RunningTotalColumn = 1, RunningTotalPrefix = "TOTAL:" };
            foreach ((string text, decimal value) in new[] { ("one", 5m), (Words(2000), 7m), ("three", 11m) })
                t.AddRow(new TableRow
                {
                    Cells = [new TableCell { Content = new TextBlock(text) }, new TableCell { Content = new TextBlock(value.ToString(System.Globalization.CultureInfo.InvariantCulture)) }],
                    RunningValue = value,
                });
            doc.Add(t);
        });
        Assert.Empty(r.Errors);
        Assert.Empty(r.Warnings);
        Assert.Contains("three", r.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("TOTAL:", r.Text, StringComparison.Ordinal);
    }

    // ---- rounded containers ----

    [Fact]
    public void RoundedContainerSplitsWithCornersOnlyAtTheEnds()
    {
        Result r = Generate(doc => doc.Add(new Container
        {
            CornerRadius = 8,
            BackgroundColor = Color.FromHex("#f1f5f9"),
            Border = new Border(1),
            Padding = new Thickness(10),
            Child = new TextBlock(Words(2000)),
        }));
        Assert.True(r.Streams.Count > 1);
        Assert.Empty(r.Errors);
        Assert.All(LowestText(r.Streams), y => Assert.True(y >= 50, $"text at y={y}"));
        List<int> curves = r.Streams.Select(s => CurveOperator().Count(s)).ToList();
        Assert.Equal(4, curves[0]);
        Assert.Equal(4, curves[^1]);
        Assert.All(curves.Skip(1).Take(curves.Count - 2), middle => Assert.Equal(0, middle));
    }

    // ---- groups clip and throw ----

    [Fact]
    public void DrawsAFixedHeightContainerAtTheTopOfAPageClippedAndReportsIt()
    {
        Result r = Generate(doc =>
        {
            doc.Add(new TextBlock("before"));
            doc.Add(new Container { Height = 5000, BackgroundColor = Color.FromHex("#e2e8f0"), Child = new TextBlock("inside") });
            doc.Add(new TextBlock("after"));
        });
        Diagnostic error = Assert.Single(r.Errors);
        Assert.Equal("A Container is taller than the page body and cannot be split: 4258pt is cut off at the bottom of page 2.", error.Message);
        Assert.Equal("pagination", error.Source);
        Assert.Equal(3, r.Streams.Count);
        Assert.Contains("inside", r.Streams[1], StringComparison.Ordinal);
        Assert.Contains("0 841.89 m\n595.276 841.89 l\n595.276 50 l\n0 50 l\nh\nW n", r.Streams[1], StringComparison.Ordinal);
        Assert.Contains("after", r.Streams[2], StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsOnlyTheOutermostOversizedContent()
    {
        Result r = Generate(doc => doc.Add(new Container { Height = 5000, Child = new Container { Height = 4000, Child = new TextBlock("x") } }));
        Assert.Single(r.Errors);
    }

    [Fact]
    public void ClipsARowOfItemsThatCannotSplit()
    {
        Result r = Generate(doc =>
        {
            var row = new HorizontalStack();
            row.Add(new Spacer(900));
            row.Add(new Spacer(10));
            doc.Add(row);
        });
        Assert.Equal(["A HorizontalStack is taller than the page body and cannot be split: 158pt is cut off at the bottom of page 1."], r.Errors.Select(d => d.Message));
    }

    [Fact]
    public void ClipsOversizedContentInsideAStack()
    {
        Result r = Generate(doc =>
        {
            var stack = new VerticalStack();
            stack.Add(new TextBlock("a"));
            stack.Add(new Container { Height = 800 });
            doc.Add(stack);
        });
        Assert.Equal(["A Container is taller than the page body and cannot be split: 58pt is cut off at the bottom of page 2."], r.Errors.Select(d => d.Message));
    }

    [Fact]
    public void ThrowsOversizedContentExceptionWhenSetToThrow()
    {
        using BonniePDF.Document doc = new(new DocumentSettings { OversizedContent = OversizedContent.Throw });
        doc.Add(new Container { Height = 5000 });
        var ex = Assert.Throws<OversizedContentException>(() => doc.GenerateBytes());
        Assert.Equal("A Container is taller than the page body and cannot be split: 4258pt is cut off at the bottom of page 1.", ex.Message);
    }

    [Fact]
    public void RejectsAnUndefinedOversizedContentValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DocumentSettings { OversizedContent = (OversizedContent)7 });
    }

    // ---- content that cannot split moves when it does not fit ----

    [Fact]
    public void MovesATopLevelQrCodeInsteadOfDrawingItPastTheBottomMargin()
    {
        Result r = Generate(doc =>
        {
            doc.Add(new Spacer(700));
            doc.Add(new QRCode { Data = "hello", Size = 120, AltText = "QR" });
        });
        Assert.Equal(2, r.Streams.Count);
        Assert.Empty(r.Errors);
        Assert.DoesNotMatch(@" re\n", r.Streams[0]);
    }
}
