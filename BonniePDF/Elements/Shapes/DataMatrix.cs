using System;

namespace BonniePDF;

/// <summary>
/// A native, high-performance, vector-based Data Matrix (ECC200) element.
///
/// Implements a full ISO/IEC 16022 compliant encoder with automatic encodation
/// (ASCII, C40, Text, X12, EDIFACT, Base-256) and interleaved Reed-Solomon error
/// correction. Like <see cref="QRCode"/>, it draws sharp vector rectangles
/// rather than a bitmap, so the symbol stays crisp at any zoom and adds almost
/// nothing to the PDF size, coalescing adjacent modules into the smallest
/// possible payload.
///
/// Data Matrix is a high-density 2D symbology favoured for small marks - parts,
/// labels, documents - and unlike QR it supports rectangular as well as square
/// symbols.
/// </summary>
public class DataMatrix : ElementBase, IRoleProvider, IAltTextProvider
{
    /// <summary>
    /// The data/text to encode. Any string is accepted; characters outside
    /// Latin-1 are encoded as their UTF-8 bytes.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Alternate text description for accessibility. When the document opts into
    /// <c>AccessibilityMode.PdfUa1</c>, this populates the <c>/Alt</c> entry on
    /// the structure element wrapping the symbol.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the symbol is purely decorative and is tagged as
    /// <c>/Artifact</c> with no alt-text warning.
    /// </summary>
    public bool IsDecorative { get; set; }

    /// <summary>
    /// The structure role in a tagged PDF: <c>Figure</c> when the element has alternative text, otherwise
    /// <c>Artifact</c>. It is always <c>Artifact</c> when <see cref="IsDecorative"/> is set.
    /// </summary>
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

    /// <summary>
    /// The display width of the symbol in points (the quiet zone is included).
    /// For square symbols the height equals the width; for rectangular symbols
    /// the height is derived from the symbol's aspect ratio.
    /// </summary>
    public float Size { get; set; } = 100f;

    /// <summary>
    /// Request a rectangular symbol for short messages where one is available.
    /// Defaults to <c>false</c> (square symbols).
    /// </summary>
    public bool Rectangular { get; set; }

    /// <summary>
    /// Number of quiet-zone modules around the symbol. The spec requires at
    /// least 1. Default 2.
    /// </summary>
    public int QuietZone { get; set; } = 2;

    /// <summary>The colour of the dark modules (usually black).</summary>
    public Color ForegroundColor { get; set; } = new Color(0, 0, 0);

    /// <summary>The background colour (usually white, recommended for reliable scanning).</summary>
    public Color BackgroundColor { get; set; } = new Color(255, 255, 255);

    /// <summary>Whether to draw the background. If <c>false</c>, the symbol is transparent.</summary>
    public bool DrawBackground { get; set; } = true;

    private string? _memoKey;
    private EncodedDataMatrix? _memo;

    private EncodedDataMatrix EncodeMatrix()
    {
        string key = (Rectangular ? "r:" : "s:") + Data;
        if (!string.Equals(_memoKey, key, StringComparison.Ordinal) || _memo is null)
        {
            _memo = DataMatrixEncoder.Encode(Data, Rectangular);
            _memoKey = key;
        }
        return _memo;
    }

    protected override float GetPreferredContentWidth()
    {
        return Size;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        if (string.IsNullOrEmpty(Data)) return Size;
        EncodedDataMatrix matrix = EncodeMatrix();
        int totalCols = matrix.Width + QuietZone * 2;
        int totalRows = matrix.Height + QuietZone * 2;
        return Size / totalCols * totalRows;
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

        EncodedDataMatrix matrix = EncodeMatrix();
        int totalCols = matrix.Width + QuietZone * 2;
        int totalRows = matrix.Height + QuietZone * 2;
        float moduleSize = Size / totalCols;
        float symbolHeight = moduleSize * totalRows;
        float qzOffset = QuietZone * moduleSize;

        // 1. Background (covers the quiet zone too, so it is genuinely light).
        if (DrawBackground)
        {
            ctx.Canvas.FillRect(x, y, Size, symbolHeight, BackgroundColor);
        }

        // 2. Dark modules, coalescing adjacent modules in a row into one rectangle.
        ctx.Canvas.BeginCrispEdges();
        for (int row = 0; row < matrix.Height; row++)
        {
            int startCol = -1;
            for (int col = 0; col <= matrix.Width; col++)
            {
                bool isDark = col < matrix.Width && matrix.Modules[row, col];
                if (isDark)
                {
                    if (startCol == -1) startCol = col;
                }
                else if (startCol != -1)
                {
                    float rectX = x + qzOffset + startCol * moduleSize;
                    float rectY = y + qzOffset + row * moduleSize;
                    float rectWidth = (col - startCol) * moduleSize;
                    ctx.Canvas.FillRect(rectX, rectY, rectWidth, moduleSize, ForegroundColor);
                    startCol = -1;
                }
            }
        }
        ctx.Canvas.EndCrispEdges();

        // 3. Advance the Y position.
        ctx.CurrentY += symbolHeight;
    }
}
