using BonniePDF.Core;

using BonniePDF.Rendering;

namespace BonniePDF.Elements.Shapes;

/// <summary>
/// A native, high-performance, vector-based QR Code element.
/// Implements a full ISO/IEC 18004 compliant encoder with Reed-Solomon error correction,
/// data masking, and automatic version selection.
/// Optimized to generate the smallest possible PDF payload by coalescing adjacent modules.
///
/// Copyright (c) Project Nayuki. (MIT License)
/// https://www.nayuki.io/page/qr-code-generator-library
/// Ported to C# for BonniePDF.
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

    /// <summary>
    /// The error correction level to use. Higher levels provide more redundancy
    /// at the cost of reduced data capacity. Default is Medium (~15%).
    /// </summary>
    public QrErrorCorrection ErrorCorrection { get; set; } = QrErrorCorrection.Medium;

    /// <summary>
    /// Number of quiet-zone modules around the QR code. The spec recommends 4.
    /// Set to 0 to disable the quiet zone.
    /// </summary>
    public int QuietZone { get; set; }

    protected override float GetPreferredContentWidth()
    {
        return Size;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
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

        // 1. Generate the matrix using the real ISO/IEC 18004 encoder
        QrEcc ecl = ErrorCorrection switch
        {
            QrErrorCorrection.Low => QrEcc.Low,
            QrErrorCorrection.Medium => QrEcc.Medium,
            QrErrorCorrection.Quartile => QrEcc.Quartile,
            QrErrorCorrection.High => QrEcc.High,
            _ => QrEcc.Medium
        };

        QrCodeEncoder encoder = QrCodeEncoder.EncodeText(Data, ecl);
        bool[,] matrix = encoder.ToMatrix();
        int moduleCount = encoder.Size;

        // Account for quiet zone
        int totalModules = moduleCount + QuietZone * 2;
        float moduleSize = Size / totalModules;
        float qzOffset = QuietZone * moduleSize;

        // 2. Draw the background (if requested)
        if (DrawBackground)
        {
            ctx.Canvas.FillRect(x, y, Size, Size, BackgroundColor);
        }

        // 3. Draw the foreground modules with Horizontal Coalescing Optimization
        for (int row = 0; row < moduleCount; row++)
        {
            int startCol = -1;

            for (int col = 0; col <= moduleCount; col++)
            {
                bool isDark = col < moduleCount && matrix[row, col];

                if (isDark)
                {
                    if (startCol == -1)
                        startCol = col;
                }
                else
                {
                    if (startCol != -1)
                    {
                        float rectX = x + qzOffset + (startCol * moduleSize);
                        float rectY = y + qzOffset + (row * moduleSize);
                        float rectWidth = (col - startCol) * moduleSize;

                        ctx.Canvas.FillRect(rectX, rectY, rectWidth, moduleSize, ForegroundColor);
                        startCol = -1;
                    }
                }
            }
        }

        // 4. Advance the Y position
        ctx.CurrentY += Size;
    }
}

/// <summary>
/// Error correction levels for QR codes.
/// </summary>
public enum QrErrorCorrection
{
    /// <summary>~7% of codewords can be restored.</summary>
    Low,

    /// <summary>~15% of codewords can be restored.</summary>
    Medium,

    /// <summary>~25% of codewords can be restored.</summary>
    Quartile,

    /// <summary>~30% of codewords can be restored.</summary>
    High
}
