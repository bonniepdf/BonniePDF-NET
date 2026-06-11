using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

[Collection("Serial")]

/// <summary>
/// stage-0 closeout. Builds a representative tagged document
/// (mirrors the public <c>SimpleDocumentTaggedExample</c> in the
/// Examples project - inline here so the test project doesn't take a
/// cross-project reference) and asserts the byte-level shape VeraPDF
/// profile <c>ua1</c> verifies:
///
/// - <c>/Catalog</c> references <c>/StructTreeRoot</c>, <c>/MarkInfo</c>
///   <c>/Marked true</c>, and <c>/Lang</c>.
/// - XMP declares <c>pdfuaid:part 1</c>.
/// - Structure tree contains <c>/Document</c>, headings, paragraphs,
///   and a <c>/Figure</c> with <c>/Alt</c>.
/// - Every page's content stream has balanced BDC/EMC counts.
///
/// The full VeraPDF rule-by-rule pass lives in a separate opt-in test
/// (<see cref="VeraPdfStageZeroCloseoutTests"/>) gated on the
/// <c>VERAPDF=1</c> environment variable and a local VeraPDF service.
/// </summary>
public class TaggedSimpleDocumentTests
{
    internal static Document BuildTaggedDocument()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Simple Document - Tagged (PDF/UA-1)",
                Author = "BonniePDF",
                Subject = "this feature stage-0 closeout fixture.",
                Keywords = "pdf, pdf/ua, accessibility",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
            },
        };

        doc.Add(new Heading("Accessible PDF Demo")
        {
            TocIdentifier = "intro",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "This document opts into PDF/UA-1 tagging by setting Settings.Accessibility = AccessibilityMode.PdfUa1.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "Existing elements tag themselves automatically - no semantic verbs required.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[700],
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new Heading("Scan to verify")
        {
            TocIdentifier = "qr",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new QRCode
        {
            Data = "https://github.com/bonniepdf/BonniePDF-NET",
            Size = 100,
            AltText = "QR code linking to the BonniePDF GitHub repository",
        });

        return doc;
    }

    internal static byte[] GenerateBytes()
    {
        using var doc = BuildTaggedDocument();
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return ms.ToArray();
    }

    private static string GenerateText() => Encoding.ASCII.GetString(GenerateBytes());

    [Fact]
    public void CatalogReferencesStructTreeRootAndMarkInfoAndLang()
    {
        string pdf = GenerateText();
        Assert.Contains("/StructTreeRoot", pdf);
        Assert.Contains("/MarkInfo << /Marked true >>", pdf);
        Assert.Contains("/Lang (en-GB)", pdf);
    }

    [Fact]
    public void XmpPacketDeclaresPdfUa1Conformance()
    {
        string pdf = GenerateText();
        Assert.Contains("xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"", pdf);
        // XmlWriter may place the namespace declaration inline on the element, so search for
        // the closing '>' of the opening tag immediately followed by the value.
        Assert.Contains(">1</pdfuaid:part>", pdf);
    }

    [Fact]
    public void StructureTreeHasDocumentRootAndHeadingHierarchy()
    {
        string pdf = GenerateText();
        Assert.Contains("/Type /StructTreeRoot", pdf);
        Assert.Contains("/S /Document", pdf);
        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /H2", pdf);
        Assert.Contains("/S /P", pdf);
        Assert.Contains("/S /Figure", pdf);
    }

    [Fact]
    public void FigureCarriesAltText()
    {
        string pdf = GenerateText();
        // The QRCode's AltText must surface as /Alt on the Figure
        // StructElem.
        Assert.Contains("/Alt (QR code linking to the BonniePDF GitHub repository)", pdf);
    }

    [Fact]
    public void EveryPageStreamHasBalancedMarkedContent()
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateText();
            string[] pageStreams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(pageStreams);

            foreach (string stream in pageStreams)
            {
                int bdc = SubstringCounter.Count(stream, "BDC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(bdc == emc, $"unbalanced marked-content on a page: BDC={bdc} EMC={emc}");
            }
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }

    [Fact]
    public void TaggedExampleGeneratesNonEmptyPdf()
    {
        byte[] bytes = GenerateBytes();
        Assert.True(bytes.Length > 1000, "tagged PDF should not be empty");
        // PDF header
        string header = Encoding.ASCII.GetString(bytes, 0, 8);
        Assert.StartsWith("%PDF-1.7", header);
    }
}

/// <summary>
/// Tiny helpers shared between tagged-document tests.
/// </summary>
internal static class SubstringCounter
{
    public static int Count(string source, string needle)
    {
        int count = 0;
        int idx = 0;
        while ((idx = source.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}

internal static class PdfPageStreamSplitter
{
    /// <summary>
    /// Pulls each page-content stream from the PDF body. Heuristic:
    /// any uncompressed stream containing BT and ET is treated as a
    /// page content stream. Good enough for tests; the real writer
    /// always emits page content uncompressed for now.
    /// </summary>
    public static string[] Split(string pdf)
    {
        // Normalize CRLF to LF so the search works on both Windows and Linux/macOS.
        // PdfWriter.WriteLine emits \r\n on Windows; the literals in Write() use \n.
        pdf = pdf.Replace("\r\n", "\n", System.StringComparison.Ordinal);

        var result = new System.Collections.Generic.List<string>();
        int idx = 0;
        while (true)
        {
            int streamStart = pdf.IndexOf("stream\n", idx, System.StringComparison.Ordinal);
            if (streamStart < 0) break;
            int contentStart = streamStart + "stream\n".Length;
            int streamEnd = pdf.IndexOf("endstream", contentStart, System.StringComparison.Ordinal);
            if (streamEnd < 0) break;
            string content = pdf.Substring(contentStart, streamEnd - contentStart);
            if (content.Contains("BT") && content.Contains("ET"))
                result.Add(content);
            idx = streamEnd + "endstream".Length;
        }
        return result.ToArray();
    }
}
