using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Text shorthand (spec 03 "Text shorthand", fixture <c>layout/text-shorthand</c>): text content (a string,
/// or several lines as one paragraph) wherever an element can be added, and strings as table cells.
/// </summary>
/// <remarks>
/// Every case builds the shorthand and the explicit form and requires the same result, compared by page
/// SVG (layout) and by PDF bytes with the random file identifier masked (tagged structure, fonts,
/// everything else). The TypeScript suite asserts the same cases against the same fixture. When a case
/// changes here it changes there, and in the fixture first.
/// <para>
/// Compression is switched off so the tagged structure can be counted in the bytes. That is process-wide
/// state, so the class is in the "Serial" collection and restores it afterwards.
/// </para>
/// </remarks>
[Collection("Serial")]
public sealed partial class TextShorthandTests : IDisposable
{
    private static readonly DateTimeOffset Fixed = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly bool _compression = InternalConfiguration.EnableCompression;

    public TextShorthandTests() => InternalConfiguration.EnableCompression = false;

    public void Dispose() => InternalConfiguration.EnableCompression = _compression;

    private static BonniePDF.Document NewDoc(Action<DocumentSettings>? configure = null)
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.Title = "Text shorthand";
        doc.Metadata.CreationDate = Fixed;
        doc.Metadata.ModifiedDate = Fixed;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11);
        configure?.Invoke(doc.Settings);
        return doc;
    }

    private static string Bytes(BonniePDF.Document doc)
    {
        using var stream = new MemoryStream();
        doc.Save(stream, leaveOpen: true);
        return FileIdentifier().Replace(Encoding.Latin1.GetString(stream.ToArray()), "/ID [<MASKED> <MASKED>]");
    }

    private static string Svg(BonniePDF.Document doc) => string.Join("\n", doc.GenerateSvgPages());

    private static int Count(string haystack, string pattern) => Regex.Count(haystack, pattern);

    [GeneratedRegex(@"/ID \[<[0-9A-F]*> <[0-9A-F]*>\]")]
    private static partial Regex FileIdentifier();

    /// <summary>Build the same document two ways and require identical output.</summary>
    private static void Same(
        Action<BonniePDF.Document> shorthand,
        Action<BonniePDF.Document> explicitForm,
        Action<DocumentSettings>? configure = null)
    {
        using BonniePDF.Document a = NewDoc(configure);
        shorthand(a);
        using BonniePDF.Document b = NewDoc(configure);
        explicitForm(b);
        Assert.Equal(Svg(b), Svg(a));
        Assert.Equal(Bytes(b), Bytes(a));
    }

    [Fact]
    public void Comparison_IsDeterministic()
    {
        // Control: the explicit form twice. A failure elsewhere is then the shorthand's, not the harness's.
        Same(d => d.Add(new TextBlock("Hello")), d => d.Add(new TextBlock("Hello")));

        // And it is not vacuous: different documents differ, in layout and in bytes.
        using BonniePDF.Document hello = NewDoc();
        hello.Add(new TextBlock("Hello"));
        using BonniePDF.Document goodbye = NewDoc();
        goodbye.Add(new TextBlock("Goodbye"));
        Assert.NotEqual(Svg(hello), Svg(goodbye));
        Assert.NotEqual(Bytes(hello), Bytes(goodbye));
    }

    [Fact]
    public void AddString_EqualsAddTextBlock_OnDocumentPageAndStacks()
    {
        Same(d => d.Add("Hello"), d => d.Add(new TextBlock("Hello")));
        Same(d => d.AddPage().Add("Hello"), d => d.AddPage().Add(new TextBlock("Hello")));
        Same(
            d =>
            {
                var stack = new VerticalStack();
                stack.Add("One");
                stack.Add("Two");
                d.Add(stack);
            },
            d =>
            {
                var stack = new VerticalStack();
                stack.Add(new TextBlock("One"));
                stack.Add(new TextBlock("Two"));
                d.Add(stack);
            });
        Same(
            d =>
            {
                var row = new HorizontalStack();
                row.Add("Left");
                row.Add("Right");
                d.Add(row);
            },
            d =>
            {
                var row = new HorizontalStack();
                row.Add(new TextBlock("Left"));
                row.Add(new TextBlock("Right"));
                d.Add(row);
            });
    }

    [Fact]
    public void Lines_AreOneTextElementJoinedByLineBreaks()
    {
        // Several strings, and a list of strings: both are the lines of one text element.
        Same(d => d.Add("Line one", "Line two", "Line three"), d => d.Add(new TextBlock("Line one\nLine two\nLine three")));
        Same(
            d => d.Add(new List<string> { "Line one", "Line two", "Line three" }),
            d => d.Add(new TextBlock("Line one\nLine two\nLine three")));

        using BonniePDF.Document doc = NewDoc();
        doc.Add("Line one", "Line two", "Line three");
        IElement[] elements = doc.Pages[0].Elements.ToArray();
        TextBlock block = Assert.IsType<TextBlock>(Assert.Single(elements));
        Assert.Equal("Line one\nLine two\nLine three", block.Text);
    }

    [Fact]
    public void Lines_AreOneParagraphInTaggedOutput()
    {
        static void Tagged(DocumentSettings s)
        {
            s.Accessibility = AccessibilityMode.PdfUa1;
            s.Language = "en-GB";
        }

        static int Paragraphs(Action<BonniePDF.Document> build)
        {
            using BonniePDF.Document d = NewDoc(Tagged);
            build(d);
            return Count(Bytes(d), @"/S\s*/P\b");
        }

        int one = Paragraphs(d => d.Add(new TextBlock("x")));
        int lines = Paragraphs(d => d.Add("Line one", "Line two", "Line three"));
        int separate = Paragraphs(d =>
        {
            d.Add(new TextBlock("Line one"));
            d.Add(new TextBlock("Line two"));
            d.Add(new TextBlock("Line three"));
        });

        Assert.True(one > 0);
        Assert.Equal(one, lines); // three lines, one paragraph
        Assert.Equal(3 * one, separate);
        Same(
            d => d.Add("Line one", "Line two", "Line three"),
            d => d.Add(new TextBlock("Line one\nLine two\nLine three")),
            Tagged);
    }

    [Fact]
    public void LineWithBreak_KeepsIt() =>
        Same(d => d.Add("a\nb", "c"), d => d.Add(new TextBlock("a\nb\nc")));

    [Fact]
    public void EmptySequence_IsEmptyString() =>
        Same(d => d.Add(Array.Empty<string>()), d => d.Add(new TextBlock("")));

    [Fact]
    public void NullStringOrLine_IsAnError()
    {
        using BonniePDF.Document doc = NewDoc();
        Assert.Throws<ArgumentNullException>(() => doc.Add((string)null!));
        Assert.Throws<ArgumentNullException>(() => doc.Add("a", null!));
        Assert.Throws<ArgumentNullException>(() => doc.Add(new[] { "a", null! }));
        Assert.Throws<ArgumentNullException>(() => new TextBlock("a", null!));
        Assert.Empty(doc.Pages.SelectMany(p => p.Elements));
    }

    [Fact]
    public void TextBlockConstructor_TakesLines()
    {
        var block = new TextBlock("Hello!", "Thank you.") { FontSize = 8 };
        Assert.Equal("Hello!\nThank you.", block.Text);
        Assert.Equal(8f, block.FontSize);
        Same(
            d => d.Add(new TextBlock("Hello!", "Thank you.") { FontSize = 8 }),
            d => d.Add(new TextBlock("Hello!\nThank you.") { FontSize = 8 }));
    }

    [Fact]
    public void HorizontalStack_TakesWeightWithText() =>
        Same(
            d =>
            {
                var row = new HorizontalStack();
                row.Add("Left", 2f);
                row.Add(new[] { "Right", "side" }, 1f);
                d.Add(row);
            },
            d =>
            {
                var row = new HorizontalStack();
                row.Add(new TextBlock("Left"), 2f);
                row.Add(new TextBlock("Right\nside"), 1f);
                d.Add(row);
            });

    [Fact]
    public void TableRow_StringsAreCells() =>
        Same(
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow("Widget", "4", "12.00");
                d.Add(table);
            },
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow(new TextBlock("Widget"), new TextBlock("4"), new TextBlock("12.00"));
                d.Add(table);
            });

    [Fact]
    public void TableRow_MixesStringsAndElements() =>
        Same(
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow("Widget", new TextBlock("4") { FontSize = 8 }, "12.00");
                d.Add(table);
            },
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow(new TextBlock("Widget"), new TextBlock("4") { FontSize = 8 }, new TextBlock("12.00"));
                d.Add(table);
            });

    [Fact]
    public void TableRow_SequenceIsARowOfCells() =>
        Same(
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow(new List<string> { "Widget", "4", "12.00" });
                d.Add(table);
            },
            d =>
            {
                var table = new Table(2f, 1f, 1f);
                table.AddRow(new TextBlock("Widget"), new TextBlock("4"), new TextBlock("12.00"));
                d.Add(table);
            });

    [Fact]
    public void HeaderAndFooter_TakeStrings() =>
        Same(
            d =>
            {
                var table = new Table(2f, 1f);
                table.AddHeader("Item", "Qty");
                table.AddRow("Widget", "4");
                table.AddFooter("Total", "4");
                d.Add(table);
            },
            d =>
            {
                var table = new Table(2f, 1f);
                table.AddHeader(new TextBlock("Item"), new TextBlock("Qty"));
                table.AddRow(new TextBlock("Widget"), new TextBlock("4"));
                table.AddFooter(new TextBlock("Total"), new TextBlock("4"));
                d.Add(table);
            });

    [Fact]
    public void ShorthandText_TakesDefaultTextStyle()
    {
        static void Times(DocumentSettings s) =>
            s.DefaultTextStyle = new TextStyle().WithFamily("Times").WithSize(14);

        Same(d => d.Add("Hello"), d => d.Add(new TextBlock("Hello")), Times);

        using BonniePDF.Document doc = NewDoc(Times);
        doc.Add("Hello");
        Assert.Contains("/BaseFont /Times-Roman", Bytes(doc));
    }

    [Fact]
    public void ExistingElementCalls_AreUnchanged()
    {
        using BonniePDF.Document doc = NewDoc();
        var table = new Table(1f, 1f);
        table.AddHeader(TableRow.From(new TextBlock("A"), new TextBlock("B")));
        table.AddRow(new TextBlock("1"), new TextBlock("2"));
        table.AddRow(TableRow.From(new TextBlock("3"), new TextBlock("4")));
        doc.Add(new TextBlock("Intro"));
        doc.Add(table);
        Assert.Equal(2, doc.Pages[0].Elements.Count);
    }
}
