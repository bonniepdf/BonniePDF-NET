namespace BonniePDF.Models;

/// <summary>
/// PDF color space for images
/// </summary>
public enum ImageColorSpace
{
    /// <summary>RGB color space (3 components: red, green, blue)</summary>
    DeviceRGB,

    /// <summary>CMYK color space (4 components: cyan, magenta, yellow, black)</summary>
    DeviceCMYK,

    /// <summary>Grayscale color space (1 component)</summary>
    DeviceGray,

    /// <summary>Indexed color space (palette-based)</summary>
    Indexed
}