using System;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Shapes;

/// <summary>
/// The 1D barcode symbologies supported natively by BonniePDF. These four
/// cover retail (EAN-13, UPC-A) and logistics (Code 128, ITF-14) across the
/// UK, EU, and US. Additional symbologies (Code 39/93, DataMatrix, PDF417,
/// Aztec, …) are provided by the optional extension package.
/// </summary>
public enum BarcodeSymbology
{
    /// <summary>Code 128 (ISO/IEC 15417) - the universal logistics barcode. Full ASCII.</summary>
    Code128,

    /// <summary>EAN-13 (ISO/IEC 15420) - the EU/UK retail product barcode. 12 digits + check.</summary>
    Ean13,

    /// <summary>UPC-A (ISO/IEC 15420) - the US/Canada retail product barcode. 11 digits + check.</summary>
    UpcA,

    /// <summary>ITF-14 (ISO/IEC 16390 + GS1) - the outer-case/shipping barcode. 13 digits + check.</summary>
    Itf14,
}

/// <summary>
/// Raised when a value cannot be encoded in the requested
/// <see cref="BarcodeSymbology"/> (wrong length, invalid characters, etc.).
/// </summary>
public sealed class BarcodeContentException : BonniePdfException
{
    /// <summary>The symbology that rejected the value.</summary>
    public BarcodeSymbology Symbology { get; }

    /// <summary>Creates a new <see cref="BarcodeContentException"/>.</summary>
    public BarcodeContentException(BarcodeSymbology symbology, string message) : base(message)
    {
        Symbology = symbology;
    }
}

/// <summary>
/// A native, vector-based 1D barcode element. Renders sharp, scannable bars
/// as filled rectangles sized in PDF points, with an optional human-readable
/// text label beneath. Mirrors the shape of <see cref="QRCode"/> so the two
/// are learned together.
///
/// Encoding is pure managed code with zero dependencies. The bar patterns are
/// identical to the BonniePDF TypeScript port for the same input.
/// </summary>
public class Barcode : ElementBase, IRoleProvider, IAltTextProvider
{
    /// <summary>The value to encode. Content rules depend on <see cref="Symbology"/>.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The barcode symbology. Defaults to <see cref="BarcodeSymbology.Code128"/>.</summary>
    public BarcodeSymbology Symbology { get; set; } = BarcodeSymbology.Code128;

    /// <summary>
    /// Alternate text description for accessibility. When the document opts into
    /// <c>AccessibilityMode.PdfUa1</c>, this populates the <c>/Alt</c> entry on the
    /// structure element wrapping the barcode.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>When <c>true</c>, the barcode is tagged as a decorative <c>/Artifact</c>.</summary>
    public bool IsDecorative { get; set; }

    /// <inheritdoc />
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? Rendering.Tagging.SemanticRole.Artifact
            : Rendering.Tagging.SemanticRole.Figure;

    /// <summary>Height of the bars in points (excludes the text label). Default 60.</summary>
    public float Height { get; set; } = 60f;

    /// <summary>Width of the narrowest bar (one module) in points. Default 1.5.</summary>
    public float ModuleWidth { get; set; } = 1.5f;

    /// <summary>Number of quiet-zone modules on each side. Defaults to the per-symbology spec value.</summary>
    public int QuietZone { get; set; } = -1;

    /// <summary>Whether to render the human-readable value beneath the bars. Default true.</summary>
    public bool IncludeTextLabel { get; set; } = true;

    /// <summary>Font size of the human-readable text label, in points. Default 9.</summary>
    public float TextLabelFontSize { get; set; } = 9f;

    /// <summary>The colour of the bars. Default black.</summary>
    public Color ForegroundColor { get; set; } = new Color(0, 0, 0);

    /// <summary>The background colour. Default white (recommended for reliable scanning).</summary>
    public Color BackgroundColor { get; set; } = new Color(255, 255, 255);

    /// <summary>Whether to draw the background. Default true.</summary>
    public bool DrawBackground { get; set; } = true;

    private int EffectiveQuietZone =>
        QuietZone >= 0 ? QuietZone : BarcodeEncoder.DefaultQuietZone(Symbology);

    private float LabelArea => IncludeTextLabel ? TextLabelFontSize + 3f : 0f;

    protected override float GetPreferredContentWidth()
    {
        if (string.IsNullOrEmpty(Value)) return 0f;
        EncodedBarcode encoded = BarcodeEncoder.Encode(Symbology, Value);
        int totalModules = encoded.Modules.Length + EffectiveQuietZone * 2;
        return totalModules * ModuleWidth;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        return Height + LabelArea;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (string.IsNullOrEmpty(Value))
        {
            ctx.CurrentY += Height + LabelArea;
            return;
        }

        EncodedBarcode encoded = BarcodeEncoder.Encode(Symbology, Value);
        bool[] modules = encoded.Modules;

        float x = ctx.CurrentX;
        float y = ctx.CurrentY;

        int quietZone = EffectiveQuietZone;
        int totalModules = modules.Length + quietZone * 2;
        float naturalWidth = totalModules * ModuleWidth;
        float effectiveModuleWidth =
            contentWidth > 0 && naturalWidth > contentWidth ? contentWidth / totalModules : ModuleWidth;
        float width = totalModules * effectiveModuleWidth;
        float qzOffset = quietZone * effectiveModuleWidth;

        // 1. Background (covers bars + quiet zones so the quiet zone is genuinely light).
        if (DrawBackground)
            ctx.Canvas.FillRect(x, y, width, Height + LabelArea, BackgroundColor);

        // 2. Bars, coalescing adjacent dark modules into a single rectangle.
        ctx.Canvas.BeginCrispEdges();
        int startCol = -1;
        for (int col = 0; col <= modules.Length; col++)
        {
            bool isDark = col < modules.Length && modules[col];
            if (isDark)
            {
                if (startCol == -1) startCol = col;
            }
            else if (startCol != -1)
            {
                float rectX = x + qzOffset + startCol * effectiveModuleWidth;
                float rectWidth = (col - startCol) * effectiveModuleWidth;
                ctx.Canvas.FillRect(rectX, y, rectWidth, Height, ForegroundColor);
                startCol = -1;
            }
        }

        // 3. ITF-14 bearer bars (a heavy box that frames the symbol per GS1 spec).
        if (Symbology == BarcodeSymbology.Itf14)
        {
            float bearer = Math.Max(effectiveModuleWidth * 2f, 2f);
            ctx.Canvas.StrokeRect(x + bearer / 2f, y + bearer / 2f, width - bearer, Height - bearer,
                ForegroundColor, bearer);
        }

        ctx.Canvas.EndCrispEdges();

        // 4. Human-readable text label, centred beneath the bars.
        if (IncludeTextLabel)
        {
            ctx.Canvas.DrawText(x + width / 2f, y + Height + 2f, encoded.HumanReadable,
                ForegroundColor, TextLabelFontSize, StandardFontFamily, TextAlignment.Center);
        }

        ctx.CurrentY += Height + LabelArea;
    }

    private const string StandardFontFamily = "Helvetica";
}
