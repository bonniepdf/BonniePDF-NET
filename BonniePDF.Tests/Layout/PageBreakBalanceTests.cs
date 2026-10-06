using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Neither a graphics-state scope (PDF <c>q</c>/<c>Q</c>) nor its SVG equivalent
/// (<c>&lt;g&gt;</c>) may cross a page boundary, so a page break has to close what
/// is open on the outgoing page and reopen it on the new one.
///
/// The render pipeline wraps every element in SaveState/RestoreState. When an
/// element breaks the page from inside its own Render, that RestoreState runs
/// against the *new* page - so the close was silently dropped and the page the
/// element started on was left unbalanced: an unclosed <c>q</c> in the PDF content
/// stream (invalid per ISO 32000-1) and an unclosed <c>&lt;g&gt;</c> that made the
/// exported SVG fail to parse as XML.
/// </summary>
public sealed partial class PageBreakBalanceTests
{
    [GeneratedRegex(@"(?:^|\s)q(?:\s|$)")]
    private static partial Regex SaveRegex();

    [GeneratedRegex(@"(?:^|\s)Q(?:\s|$)")]
    private static partial Regex RestoreRegex();

    /// <summary>
    /// A document whose content breaks the page from inside a container's own
    /// Render - the shape that leaves a scope open across the boundary. A
    /// container is measured whole and moves itself to a new page when it does
    /// not fit, which is exactly that path.
    /// </summary>
    private static BonniePDF.Document PaginatingDoc()
    {
        var doc = new BonniePDF.Document();
        for (int block = 0; block < 4; block++)
        {
            var stack = new VerticalStack();
            for (int line = 0; line < 20; line++)
                stack.Add(new TextBlock { Text = $"Block {block}, line {line} of flowing body content.", FontSize = 11 });
            doc.Add(new Container { Padding = new Thickness(8), Child = stack });
        }
        return doc;
    }

    private static List<string> ContentStreams(BonniePDF.Document doc)
    {
        using MemoryStream ms = new();
        doc.Save(ms, leaveOpen: true);
        string text = Encoding.GetEncoding("ISO-8859-1").GetString(ms.ToArray());

        List<string> streams = [];
        int idx = 0;
        while (true)
        {
            int start = text.IndexOf("stream", idx, StringComparison.Ordinal);
            if (start == -1) break;
            int contentStart = start + "stream".Length;
            if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
            if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;
            int end = text.IndexOf("endstream", contentStart, StringComparison.Ordinal);
            if (end == -1) break;

            byte[] raw = Encoding.GetEncoding("ISO-8859-1").GetBytes(text, contentStart, end - contentStart);
            string s;
            try
            {
                using MemoryStream infl = new(raw);
                using ZLibStream zlib = new(infl, CompressionMode.Decompress);
                using StreamReader reader = new(zlib, Encoding.GetEncoding("ISO-8859-1"));
                s = reader.ReadToEnd();
            }
            catch
            {
                s = Encoding.GetEncoding("ISO-8859-1").GetString(raw);
            }

            // Text-bearing streams are the page content streams.
            if (s.Contains("BT", StringComparison.Ordinal)) streams.Add(s);
            idx = end + "endstream".Length;
        }
        return streams;
    }

    [Fact]
    public void ContentBreaksAcrossPages_SoTheScenarioIsRealistic()
    {
        using BonniePDF.Document doc = PaginatingDoc();
        Assert.True(doc.GenerateSvgPages().Count > 1, "the fixture must paginate for this test to mean anything");
    }

    [Fact]
    public void EveryPdfContentStream_BalancesSaveAndRestore()
    {
        using BonniePDF.Document doc = PaginatingDoc();
        List<string> streams = ContentStreams(doc);
        Assert.NotEmpty(streams);

        for (int i = 0; i < streams.Count; i++)
        {
            int q = SaveRegex().Matches(streams[i]).Count;
            int qq = RestoreRegex().Matches(streams[i]).Count;
            Assert.True(q == qq, $"content stream {i + 1} has {q} q operators and {qq} Q operators");
        }
    }

    [Fact]
    public void EverySvgPage_IsWellFormedXml()
    {
        using BonniePDF.Document doc = PaginatingDoc();
        IReadOnlyList<string> pages = doc.GenerateSvgPages();

        for (int i = 0; i < pages.Count; i++)
        {
            // An unclosed <g> still renders in a browser but is not parseable XML,
            // which is how a malformed page went unnoticed.
            Exception? failure = Record.Exception(() => XDocument.Parse(pages[i]));
            Assert.True(failure is null, $"SVG page {i + 1} is not well-formed: {failure?.Message}");
        }
    }
}
