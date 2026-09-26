using System;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Media;

/// <summary>
/// An image element that displays JPEG or PNG images in the PDF
/// </summary>
public class Image : ElementBase, IRoleProvider, IAltTextProvider
{
    private ImageInfo? _cachedInfo;

    /// <summary>Path to image file (mutually exclusive with ImageData)</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Raw image data (mutually exclusive with FilePath)</summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>Desired display width in points (null = auto)</summary>
    public float? Width { get; set; }

    /// <summary>Desired display height in points (null = auto-scale)</summary>
    public float? Height { get; set; }

    /// <summary>How to fit the image within bounds</summary>
    public ImageFitMode FitMode { get; set; } = ImageFitMode.Contain;

    /// <summary>Whether to maintain aspect ratio (ignored for Fill mode)</summary>
    public bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the image is embedded at its source resolution regardless of
    /// <see cref="Configuration.DocumentSettings.MaxImageDpi"/>. If the same image is placed
    /// more than once, one placement with this set keeps the whole embedded copy at full resolution.
    /// </summary>
    public bool KeepResolution { get; set; }

    /// <summary>
    /// Alternate text description for accessibility. When the document
    /// opts into <c>AccessibilityMode.PdfUa1</c>, this populates the
    /// <c>/Alt</c> entry on the structure element wrapping the image.
    /// Required by PDF/UA for non-decorative figures.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the image is purely decorative and should be
    /// tagged as <c>/Artifact</c> with no alt-text warning. Set this
    /// explicitly for decorative borders, dividers, and background
    /// imagery so authors can distinguish "no alt by mistake" from
    /// "no alt is correct".
    /// </summary>
    public bool IsDecorative { get; set; }

    /// <inheritdoc />
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? Rendering.Tagging.SemanticRole.Artifact
            : Rendering.Tagging.SemanticRole.Figure;

    protected override float GetPreferredContentWidth()
    {
        ImageInfo info = GetImageInfo();
        // Use display dimensions so EXIF-rotated JPEGs (orientation 5-8) lay
        // out in their visual orientation, not their stored sensor orientation.
        float nativeWidth = info.DisplayWidth;
        float nativeHeight = info.DisplayHeight;
        float aspectRatio = nativeWidth / nativeHeight;

        if (Width.HasValue) return Width.Value;
        if (Height.HasValue) return Height.Value * aspectRatio;
        return nativeWidth;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        ImageInfo info = GetImageInfo();
        CalculateDimensions(contentWidth, out float displayWidth, out float displayHeight);
        return displayHeight;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ImageInfo info = GetImageInfo();

        // Calculate display dimensions
        CalculateDimensions(contentWidth, out float displayWidth, out float displayHeight);
        info.KeepResolution = KeepResolution;

        // Calculate position (Screen coordinates)
        float x = ctx.CurrentX;
        float y = ctx.CurrentY;

        // Draw via Canvas
        ctx.Canvas.DrawImage(info, x, y, displayWidth, displayHeight);

        // Advance Y by the image height
        ctx.CurrentY += displayHeight;
    }

    /// <summary>
    /// Get or load image information (auto-detects JPEG or PNG)
    /// </summary>
    private ImageInfo GetImageInfo()
    {
        if (_cachedInfo != null)
            return _cachedInfo;

        if (ImageData != null && ImageData.Length > 0)
            _cachedInfo = ImageLoader.Load(ImageData);
        else if (!string.IsNullOrEmpty(FilePath))
            _cachedInfo = ImageLoader.Load(FilePath);
        else
            throw new InvalidOperationException("Either FilePath or ImageData must be specified");

        return _cachedInfo;
    }

    /// <summary>
    /// Calculate display dimensions based on fit mode
    /// </summary>
    private void CalculateDimensions(float availableWidth, out float displayWidth, out float displayHeight)
    {
        ImageInfo info = GetImageInfo();

        // Display dimensions account for EXIF orientation (5-8 swap W/H).
        (displayWidth, displayHeight) = ImageBox.Resolve(
            Width, Height, FitMode, MaintainAspectRatio, info.DisplayWidth, info.DisplayHeight, availableWidth);
    }
}