using System.IO.Compression;
using System.Text;
using Xunit;

namespace BonniePDF.Tests.Document;

public class PageColorTests
{
    /// <summary>Generates the document and extracts its (decompressed) first content stream.</summary>
    private static string FirstPageStream(BonniePDF.Document doc)
    {
        using MemoryStream ms = new();
        doc.Save(ms, leaveOpen: true);
        return FirstPageStream(ms.ToArray());
    }

    private static string FirstPageStream(byte[] bytes)
    {
        string text = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);

        int start = text.IndexOf("stream");
        int contentStart = start + "stream".Length;
        if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
        if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;
        int end = text.IndexOf("endstream", contentStart);

        return Decompress(Encoding.GetEncoding("ISO-8859-1").GetBytes(text, contentStart, end - contentStart));
    }

    /// <summary>Returns every decompressed content stream that contains a fill-rectangle, in file order.</summary>
    private static List<string> FillStreams(BonniePDF.Document doc)
    {
        using MemoryStream ms = new();
        doc.Save(ms, leaveOpen: true);
        byte[] bytes = ms.ToArray();
        string text = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);

        List<string> streams = new();
        int idx = 0;
        while (true)
        {
            int start = text.IndexOf("stream", idx);
            if (start == -1) break;
            int contentStart = start + "stream".Length;
            if (contentStart < text.Length && text[contentStart] == '\r') contentStart++;
            if (contentStart < text.Length && text[contentStart] == '\n') contentStart++;
            int end = text.IndexOf("endstream", contentStart);
            if (end == -1) break;
            string s = Decompress(Encoding.GetEncoding("ISO-8859-1").GetBytes(text, contentStart, end - contentStart));
            if (s.Contains(" re")) streams.Add(s);
            idx = end + "endstream".Length;
        }
        return streams;
    }

    private static string Decompress(byte[] raw)
    {
        // The writer compresses content streams with FlateDecode (zlib). Inflate
        // so the operators are readable; fall back to the raw bytes if not
        // compressed.
        try
        {
            using MemoryStream infl = new(raw);
            using ZLibStream zlib = new(infl, CompressionMode.Decompress);
            using StreamReader reader = new(zlib, Encoding.GetEncoding("ISO-8859-1"));
            return reader.ReadToEnd();
        }
        catch
        {
            return Encoding.GetEncoding("ISO-8859-1").GetString(raw);
        }
    }

    [Fact]
    public void PageColor_FillsPageWhenSet()
    {
        DocumentSettings settings = new() { PageColor = ColorPalette.Indigo[50] };
        using BonniePDF.Document doc = new(settings);
        string stream = FirstPageStream(doc);

        // The page colour fill is a filled rectangle covering the full page:
        // setFillColor ('rg') + rectangle ('re') + fill ('f').
        Assert.Contains("rg", stream);
        Assert.Contains(" re", stream);
        Assert.Contains(" f", stream);
    }

    [Fact]
    public void PageColor_OmittedWhenUnset()
    {
        using BonniePDF.Document doc = new();
        string stream = FirstPageStream(doc);

        // An empty document with no page colour produces no fill-rectangle ops.
        Assert.DoesNotContain(" re", stream);
        Assert.DoesNotContain(" f", stream);
    }

    [Fact]
    public void PageColor_PaintsBehindBodyContent()
    {
        DocumentSettings settings = new() { PageColor = Color.FromHex("#e2e8f0") };
        using BonniePDF.Document doc = new(settings);
        doc.Add(new TextBlock("on top of the background"));
        string stream = FirstPageStream(doc);

        // The fill rect must precede the text-showing operator (Tj) so it sits
        // behind the text in the painter's-model stream.
        int fillIndex = stream.IndexOf(" re");
        int textIndex = stream.IndexOf("Tj");
        Assert.True(fillIndex > -1);
        Assert.True(textIndex > -1);
        Assert.True(fillIndex < textIndex);
    }

    [Fact]
    public void PageColor_AppliesPerPageWithNoDocumentLevelColor()
    {
        using BonniePDF.Document doc = new();
        doc.Pages[0].PageColor = Color.FromHex("#00ff00"); // green, per-page only
        doc.Add(new TextBlock("green page"));

        List<string> streams = FillStreams(doc);
        Assert.Single(streams);
        Assert.Contains("0 1 0 rg", streams[0]); // green fill
    }

    [Fact]
    public void PageColor_PerPageOverridesDocumentLevelColor()
    {
        DocumentSettings settings = new() { PageColor = Color.FromHex("#ff0000") }; // red default
        using BonniePDF.Document doc = new(settings);
        doc.Add(new TextBlock("page 0: default red"));

        Page page1 = doc.AddPage();
        page1.PageColor = Color.FromHex("#00ff00"); // green override on page 1
        doc.Add(new TextBlock("page 1: green override"));

        List<string> streams = FillStreams(doc);
        Assert.Equal(2, streams.Count);
        // Page 0 keeps the document-level red; page 1 uses its own green override.
        Assert.Contains("1 0 0 rg", streams[0]);
        Assert.Contains("0 1 0 rg", streams[1]);
        Assert.DoesNotContain("1 0 0 rg", streams[1]);
    }

    [Fact]
    public void PageBackground_PerPageElementOverridesDocumentTemplate()
    {
        // Document-level template draws a RED fill on every page; page 1 overrides
        // it with its own GREEN background element.
        using BonniePDF.Document doc = new();
        doc.Settings.PageBackground = _ =>
        {
            Container c = new() { BackgroundColor = Color.FromHex("#ff0000"), Height = 9999f };
            return c;
        };
        doc.Add(new TextBlock("page 0: document-level red background"));

        Page cover = doc.AddPage();
        cover.PageBackground = new Container { BackgroundColor = Color.FromHex("#00ff00"), Height = 9999f };
        doc.Add(new TextBlock("page 1: per-page green background"));

        List<string> streams = FillStreams(doc);
        Assert.Equal(2, streams.Count);
        Assert.Contains("1 0 0 rg", streams[0]); // page 0: red (document template)
        Assert.Contains("0 1 0 rg", streams[1]); // page 1: green (per-page override)
        Assert.DoesNotContain("1 0 0 rg", streams[1]);
    }
}
