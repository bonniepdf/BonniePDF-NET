namespace BonniePDF;

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

    /// <summary>
    /// Set by the placing element: embed this image at its source resolution, whatever the
    /// document's resolution cap. Not part of the decoded image.
    /// </summary>
    public bool KeepResolution { get; set; }

    /// <summary>Whether the image has transparency (alpha channel)</summary>
    public bool HasAlpha { get; set; }

    /// <summary>Alpha channel data (if HasAlpha is true)</summary>
    public byte[] AlphaData { get; set; } = [];

    /// <summary>Palette data for indexed color images</summary>
    public byte[] Palette { get; set; } = [];

    /// <summary>
    /// Embedded ICC colour profile, uncompressed. Populated from JPEG APP2
    /// <c>ICC_PROFILE\0</c> segments and from PNG <c>iCCP</c> chunks. When
    /// non-empty, the writer emits a PDF <c>/ICCBased</c> colour space
    /// referencing a dedicated stream object so the image renders with its
    /// captured colour transform instead of falling back to the device
    /// colour space.
    /// </summary>
    public byte[] IccProfile { get; set; } = [];

    /// <summary>
    /// True for Photoshop-exported CMYK JPEGs (Adobe APP14 marker with
    /// transform=0), where ink values are stored inverted. The writer maps
    /// this to PDF <c>/Decode [1 0 1 0 1 0 1 0]</c> so the colours render
    /// correctly.
    /// </summary>
    public bool InvertCmykColors { get; set; }

    /// <summary>
    /// EXIF orientation (1-8) read from JPEG APP1 metadata. 1 is "natural"
    /// (no transform); 5-8 are rotations/flips that swap width and height for
    /// display. PNGs and JPEGs without EXIF default to 1. The canvas applies
    /// the matching placement matrix so the image renders upright without
    /// re-encoding the pixel stream.
    /// </summary>
    public int Orientation { get; set; } = 1;

    /// <summary>
    /// Width of the image as it should appear on the page, accounting for
    /// EXIF orientation. For orientations 5-8 (90 degree/270 degree
    /// rotations) this is the stored height, since the image was captured
    /// rotated.
    /// </summary>
    public int DisplayWidth => Orientation is >= 5 and <= 8 ? Height : Width;

    /// <summary>
    /// Height of the image as it should appear on the page, accounting for
    /// EXIF orientation. Mirror of <see cref="DisplayWidth"/>.
    /// </summary>
    public int DisplayHeight => Orientation is >= 5 and <= 8 ? Width : Height;
}