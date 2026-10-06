using System.Text;
using BonniePDF.Drawing;
using Xunit;

namespace BonniePDF.Tests.Drawing;

/// <summary>
/// Verifies that <see cref="PdfCanvas.DrawImage"/> folds EXIF orientation
/// into the image placement matrix. Orientation 1 must stay byte-identical
/// to a plain translate+scale (so unrotated images are unchanged); 2-8
/// rotate/flip the placement without touching the pixel stream.
/// </summary>
public class PdfCanvasImageTests
{
    private static string DrawImageStream(ImageInfo image)
    {
        var sb = new StringBuilder();
        var canvas = new PdfCanvas(sb, pageHeight: 700, imageResolver: _ => "/Im1");
        canvas.DrawImage(image, x: 10, y: 20, width: 100, height: 50);
        return sb.ToString();
    }

    [Fact]
    public void DrawImage_Orientation1_EmitsPlainScaleMatrix()
    {
        var image = new ImageInfo { Width = 100, Height = 50, Orientation = 1 };

        string output = DrawImageStream(image);

        // pdfX = 10; pdfY = 700 - (20 + 50) = 630.
        Assert.Contains("1 0 0 1 10 630 cm", output);
        // Orientation 1 == plain scale: identical bytes to the pre-orientation path.
        Assert.Contains("100 0 0 50 0 0 cm", output);
        Assert.Contains("/Im1 Do", output);
    }

    [Fact]
    public void DrawImage_Orientation6_EmitsRotationMatrix()
    {
        var image = new ImageInfo { Width = 100, Height = 50, Orientation = 6 };

        string output = DrawImageStream(image);

        // orientationMatrix(6, 100, 50) = (0, -50, 100, 0, 0, 50).
        Assert.Contains("0 -50 100 0 0 50 cm", output);
    }

    [Fact]
    public void DrawImage_DefaultOrientation_MatchesOrientation1()
    {
        // A freshly constructed ImageInfo defaults Orientation to 1.
        var image = new ImageInfo { Width = 100, Height = 50 };

        string output = DrawImageStream(image);

        Assert.Contains("100 0 0 50 0 0 cm", output);
    }
}
