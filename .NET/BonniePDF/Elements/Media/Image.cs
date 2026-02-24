using System;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;

namespace BonniePDF.Elements.Media;

/// <summary>
/// An image element that displays JPEG or PNG images in the PDF
/// </summary>
public class Image : ElementBase
{
    private ImageInfo _cachedInfo;

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

    protected override float GetPreferredContentWidth()
    {
        ImageInfo info = GetImageInfo();
        float nativeWidth = info.Width;
        float nativeHeight = info.Height;
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

        float nativeWidth = info.Width;
        float nativeHeight = info.Height;
        float aspectRatio = nativeWidth / nativeHeight;

        // Use specified width or available width
        float targetWidth = Width ?? availableWidth;
        float targetHeight = Height ?? (MaintainAspectRatio ? targetWidth / aspectRatio : nativeHeight);

        displayWidth = targetWidth;
        displayHeight = targetHeight;

        // Apply fit mode
        switch (FitMode)
        {
            case ImageFitMode.Contain:
                // Scale to fit within bounds, maintain aspect ratio
                if (Width.HasValue && Height.HasValue)
                {
                    float widthRatio = Width.Value / nativeWidth;
                    float heightRatio = Height.Value / nativeHeight;
                    float scale = Math.Min(widthRatio, heightRatio);
                    displayWidth = nativeWidth * scale;
                    displayHeight = nativeHeight * scale;
                }
                else if (Width.HasValue)
                {
                    displayWidth = Width.Value;
                    displayHeight = displayWidth / aspectRatio;
                }
                else if (Height.HasValue)
                {
                    displayHeight = Height.Value;
                    displayWidth = displayHeight * aspectRatio;
                }

                break;

            case ImageFitMode.Cover:
                // Scale to fill bounds, maintain aspect ratio, crop if needed
                if (Width.HasValue && Height.HasValue)
                {
                    float widthRatio = Width.Value / nativeWidth;
                    float heightRatio = Height.Value / nativeHeight;
                    float scale = Math.Max(widthRatio, heightRatio);
                    displayWidth = nativeWidth * scale;
                    displayHeight = nativeHeight * scale;
                }

                break;

            case ImageFitMode.Fill:
                // Stretch to fill, ignore aspect ratio
                displayWidth = Width ?? availableWidth;
                displayHeight = Height ?? nativeHeight;
                break;

            case ImageFitMode.FitHeight:
                // Scale to fit height, maintain aspect ratio
                if (Height.HasValue)
                {
                    displayHeight = Height.Value;
                    displayWidth = displayHeight * aspectRatio;
                }

                break;

            case ImageFitMode.ActualSize:
                // Use native dimensions (1 pixel = 1 point)
                displayWidth = nativeWidth;
                displayHeight = nativeHeight;
                break;
        }
    }
}