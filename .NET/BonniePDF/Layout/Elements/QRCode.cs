
using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// A native, high-performance, vector-based QR Code element.
/// Optimized to generate the smallest possible PDF payload by coalescing adjacent modules.
/// </summary>
public class QRCode : ElementBase
{
    /// <summary>
    /// The data/text to encode in the QR Code.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The display size of the QR Code (QR codes are always square).
    /// </summary>
    public float Size { get; set; } = 100f;

    /// <summary>
    /// The color of the QR code modules (usually black).
    /// </summary>
    public Color ForegroundColor { get; set; } = new Color(0, 0, 0);

    /// <summary>
    /// The background color of the QR code (usually white).
    /// </summary>
    public Color BackgroundColor { get; set; } = new Color(255, 255, 255);

    /// <summary>
    /// Whether to draw the background color. If false, the QR code will be transparent.
    /// </summary>
    public bool DrawBackground { get; set; } = true;

    protected override float GetPreferredContentWidth()
    {
        return Size;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        // QR codes are square, so height is strictly tied to the defined Size
        return Size;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (string.IsNullOrEmpty(Data))
        {
            ctx.CurrentY += Size;
            return;
        }

        float x = ctx.CurrentX;
        float y = ctx.CurrentY;

        // 1. Generate the matrix (Zero-dependency internal encoder)
        bool[,] matrix = GenerateQrMatrix(Data);
        int moduleCount = matrix.GetLength(0);
        float moduleSize = Size / moduleCount;

        // 2. Draw the background (if requested)
        if (DrawBackground)
        {
            ctx.Canvas.FillRect(x, y, Size, Size, BackgroundColor);
        }

        // 3. Draw the foreground modules with Horizontal Coalescing Optimization
        // This is the performance secret: instead of drawing 10 adjacent individual squares, 
        // we draw a single rectangle spanning the width of the 10 squares.
        for (int row = 0; row < moduleCount; row++)
        {
            int startCol = -1;

            for (int col = 0; col <= moduleCount; col++)
            {
                // Check if current module is dark (treat out-of-bounds as light to force line completion)
                bool isDark = col < moduleCount && matrix[row, col];

                if (isDark)
                {
                    if (startCol == -1)
                    {
                        // Start of a new dark line
                        startCol = col;
                    }
                    // If already in a dark line, do nothing (continue extending)
                }
                else
                {
                    if (startCol != -1)
                    {
                        // End of a dark line: emit a single rectangle for the entire run
                        float rectX = x + (startCol * moduleSize);
                        float rectY = y + (row * moduleSize);
                        float rectWidth = (col - startCol) * moduleSize;

                        ctx.Canvas.FillRect(rectX, rectY, rectWidth, moduleSize, ForegroundColor);

                        // Reset for the next potential sequence in this row
                        startCol = -1;
                    }
                }
            }
        }

        // 4. Advance the Y position for the layout engine
        ctx.CurrentY += Size;
    }

    /// <summary>
    /// Internal ISO/IEC 18004 compliant encoder. 
    /// (Note for MVP: This requires the mathematical implementation of QR standard including 
    /// Reed-Solomon error correction, data masking, and placement rules).
    /// </summary>
    private static bool[,] GenerateQrMatrix(string data)
    {
        // TODO: Replace with the actual byte-encoding and matrix placement algorithm.
        // For demonstration purposes, returning a mock 21x21 (Version 1) matrix.
        int versionSize = 21;
        bool[,] matrix = new bool[versionSize, versionSize];

        // Mock data to demonstrate the rendering output
        for (int i = 0; i < versionSize; i++)
        {
            for (int j = 0; j < versionSize; j++)
            {
                // Random pattern for visual placeholder
                matrix[i, j] = (i + j) % 2 == 0 || (i * j) % 3 == 0;
            }
        }

        return matrix;
    }
}
