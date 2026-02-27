namespace BonniePDF.Rendering.Images;

/// <summary>
/// Contains parsed image metadata and data for PDF embedding
/// </summary>
public class ImageInfo
{
    /// <summary>Raw image data (JPEG bytes or decompressed PNG pixels)</summary>
    public byte[] Data { get; set; } = [];

    /// <summary>Original file bytes (useful for SVG export)</summary>
    public byte[] OriginalData { get; set; } = [];

    /// <summary>Image width in pixels</summary>
    public int Width { get; set; }

    /// <summary>Image height in pixels</summary>
    public int Height { get; set; }

    /// <summary>Image format</summary>
    public ImageFormat Format { get; set; }

    /// <summary>Color space (RGB, CMYK, Gray)</summary>
    public ImageColorSpace ColorSpace { get; set; } = ImageColorSpace.DeviceRGB;

    /// <summary>Bits per component (usually 8)</summary>
    public int BitsPerComponent { get; set; } = 8;

    /// <summary>Number of color components (3=RGB, 4=CMYK, 1=Gray)</summary>
    public int Components { get; set; } = 3;

    /// <summary>Whether the image has transparency (alpha channel)</summary>
    public bool HasAlpha { get; set; }

    /// <summary>Alpha channel data (if HasAlpha is true)</summary>
    public byte[] AlphaData { get; set; } = [];

    /// <summary>Palette data for indexed color images</summary>
    public byte[] Palette { get; set; } = [];
}