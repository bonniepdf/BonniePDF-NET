using System;

namespace BonniePDF;

/// <summary>
/// A native, vector-based 1D barcode element. Renders sharp, scannable bars
/// as filled rectangles sized in PDF points, with an optional human-readable
/// text label beneath. Mirrors the shape of <see cref="QRCode"/> so the two
/// are learned together.
///
/// Encoding is pure managed code with zero dependencies. The bar patterns are
/// identical to the BonniePDF TypeScript implementation for the same input.
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

    /// <summary>
    /// The structure role in a tagged PDF: <c>Figure</c> when the element has alternative text, otherwise
    /// <c>Artifact</c>. It is always <c>Artifact</c> when <see cref="IsDecorative"/> is set.
    /// </summary>
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

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
