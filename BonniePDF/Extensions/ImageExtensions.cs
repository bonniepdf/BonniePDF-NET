using BonniePDF.Elements.Media;

namespace BonniePDF.Extensions;

/// <summary>
/// Fluent extension methods for images
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Sets the fit mode to contain
    /// </summary>
    public static Image FitContain(this Image i)
    {
        i.FitMode = ImageFitMode.Contain;
        return i;
    }

    /// <summary>
    /// Sets the fit mode to cover
    /// </summary>
    public static Image FitCover(this Image i)
    {
        i.FitMode = ImageFitMode.Cover;
        return i;
    }

    /// <summary>
    /// Sets the fit mode to fill
    /// </summary>
    public static Image FitFill(this Image i)
    {
        i.FitMode = ImageFitMode.Fill;
        return i;
    }

    /// <summary>
    /// Sets the fit mode to fit height
    /// </summary>
    public static Image FitHeight(this Image i)
    {
        i.FitMode = ImageFitMode.FitHeight;
        return i;
    }

    /// <summary>
    /// Sets the fit mode to actual size
    /// </summary>
    public static Image ActualSize(this Image i)
    {
        i.FitMode = ImageFitMode.ActualSize;
        return i;
    }

    /// <summary>
    /// Sets the desired width of the image
    /// </summary>
    public static Image Width(this Image i, float value)
    {
        i.Width = value;
        return i;
    }

    /// <summary>
    /// Sets the desired height of the image
    /// </summary>
    public static Image Height(this Image i, float value)
    {
        i.Height = value;
        return i;
    }

    /// <summary>
    /// Sets the desired size of the image
    /// </summary>
    public static Image Size(this Image i, float width, float height)
    {
        i.Width = width;
        i.Height = height;
        return i;
    }
}
