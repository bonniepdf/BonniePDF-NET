using System.Text;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Svg;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>Mirrors core/tests/svg/canvas-sink.test.ts, including the expected bytes.</summary>
public class BonniePdfCanvasSinkTests
{
    private static string Draw(string svg, double width, double height)
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(stream, pageHeight: 1000, imageResolver: _ => "/Im0");
        SvgImporter.Import(svg, new BonniePdfCanvasSink(canvas), new ImportOptions(Width: width, Height: height));
        return stream.ToString().Replace("\r\n", "\n", System.StringComparison.Ordinal);
    }

    [Fact]
    public void StrokesWithSvgsDefaultMiterLimitOf4NotPdfs10()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\">" +
                           "<path d=\"M1 1 L9 9\" stroke=\"#0000ff\" stroke-width=\"2\" fill=\"none\"/></svg>";
        Assert.Equal("q\nq\n4 w\n0 J\n0 j\n4 M\n0 0 1 RG\n2 998 m\n18 982 l\nS\nQ\nQ\n", Draw(svg, 20, 20));
    }

    [Fact]
    public void KeepsAMiterLimitTheDocumentSets()
    {
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 10 10\">" +
                           "<path d=\"M1 1 L9 9\" stroke=\"#0000ff\" stroke-miterlimit=\"7\" fill=\"none\"/></svg>";
        Assert.Contains("7 M\n", Draw(svg, 10, 10), System.StringComparison.Ordinal);
    }
}
