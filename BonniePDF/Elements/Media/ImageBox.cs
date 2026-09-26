using System;

namespace BonniePDF.Elements.Media;

/// <summary>
/// The display box of image-like content with a natural size, under a fit mode.
/// </summary>
/// <remarks>
/// Shared by <see cref="Image"/> and <see cref="Svg"/>, which size their box
/// identically: one concept, one set of rules.
/// </remarks>
internal static class ImageBox
{
    /// <summary>Resolves the display box.</summary>
    /// <param name="width">The element's requested width, if any.</param>
    /// <param name="height">The element's requested height, if any.</param>
    /// <param name="fitMode">How the content fills its box.</param>
    /// <param name="maintainAspectRatio">Whether an unset height follows the aspect ratio.</param>
    /// <param name="nativeWidth">The content's natural width.</param>
    /// <param name="nativeHeight">The content's natural height.</param>
    /// <param name="availableWidth">The width available to the element's content.</param>
    public static (float Width, float Height) Resolve(
        float? width,
        float? height,
        ImageFitMode fitMode,
        bool maintainAspectRatio,
        float nativeWidth,
        float nativeHeight,
        float availableWidth)
    {
        float aspectRatio = nativeWidth / nativeHeight;

        // Use specified width or available width
        float targetWidth = width ?? availableWidth;
        float targetHeight = height ?? (maintainAspectRatio ? targetWidth / aspectRatio : nativeHeight);

        float displayWidth = targetWidth;
        float displayHeight = targetHeight;

        switch (fitMode)
        {
            case ImageFitMode.Contain:
                // Scale to fit within bounds, maintain aspect ratio
                if (width.HasValue && height.HasValue)
                {
                    float scale = Math.Min(width.Value / nativeWidth, height.Value / nativeHeight);
                    displayWidth = nativeWidth * scale;
                    displayHeight = nativeHeight * scale;
                }
                else if (width.HasValue)
                {
                    displayWidth = width.Value;
                    displayHeight = displayWidth / aspectRatio;
                }
                else if (height.HasValue)
                {
                    displayHeight = height.Value;
                    displayWidth = displayHeight * aspectRatio;
                }

                break;

            case ImageFitMode.Cover:
                // Scale to cover the bounds, maintaining aspect ratio. Nothing is clipped:
                // the content is drawn at the covering size.
                if (width.HasValue && height.HasValue)
                {
                    float scale = Math.Max(width.Value / nativeWidth, height.Value / nativeHeight);
                    displayWidth = nativeWidth * scale;
                    displayHeight = nativeHeight * scale;
                }

                break;

            case ImageFitMode.Fill:
                // Stretch to fill, ignore aspect ratio
                displayWidth = width ?? availableWidth;
                displayHeight = height ?? nativeHeight;
                break;

            case ImageFitMode.FitHeight:
                // Scale to fit height, maintain aspect ratio
                if (height.HasValue)
                {
                    displayHeight = height.Value;
                    displayWidth = displayHeight * aspectRatio;
                }

                break;

            case ImageFitMode.ActualSize:
                displayWidth = nativeWidth;
                displayHeight = nativeHeight;
                break;
        }

        return (displayWidth, displayHeight);
    }
}
