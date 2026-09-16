using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BonniePDF.Core;

/// <summary>
/// Represents an RGB color with values ranging from 0 to 255.
/// Internally stores values as floats (0.0 to 1.0) for high-precision PDF operations.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Color : IEquatable<Color>
{
    // Internal storage as floats (0.0 - 1.0) for PDF compliance
    private readonly float _r;
    private readonly float _g;
    private readonly float _b;
    private readonly float _a;

    /// <summary>
    /// Creates a new Color from byte values (0-255).
    /// </summary>
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        _r = r / 255f;
        _g = g / 255f;
        _b = b / 255f;
        _a = a / 255f;
    }

    /// <summary>
    /// Creates a new Color from integer values (0-255).
    /// Values are clamped to the valid byte range.
    /// </summary>
    public Color(int r, int g, int b, int a = 255)
    {
        _r = Math.Clamp(r, 0, 255) / 255f;
        _g = Math.Clamp(g, 0, 255) / 255f;
        _b = Math.Clamp(b, 0, 255) / 255f;
        _a = Math.Clamp(a, 0, 255) / 255f;
    }

    // Private constructor for internal high-precision creation
    private Color(float r, float g, float b, float a)
    {
        _r = r;
        _g = g;
        _b = b;
        _a = a;
    }

    /// <summary>
    /// Creates a Color from scRGB float values (0.0 - 1.0).
    /// Use this for high-precision color definition or when porting from legacy code.
    /// </summary>
    public static Color FromScRgb(float r, float g, float b, float a = 1.0f)
    {
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Gets the Red component (0-255).
    /// </summary>
    public byte R => (byte)(_r * 255);

    /// <summary>
    /// Gets the Green component (0-255).
    /// </summary>
    public byte G => (byte)(_g * 255);

    /// <summary>
    /// Gets the Blue component (0-255).
    /// </summary>
    public byte B => (byte)(_b * 255);

    /// <summary>
    /// Gets the Alpha component (0-255).
    /// </summary>
    public byte A => (byte)(_a * 255);

    /// <summary>
    /// Internal: Gets the Red component as a float (0.0 - 1.0).
    /// </summary>
    internal float ScR => _r;

    /// <summary>
    /// Internal: Gets the Green component as a float (0.0 - 1.0).
    /// </summary>
    internal float ScG => _g;

    /// <summary>
    /// Internal: Gets the Blue component as a float (0.0 - 1.0).
    /// </summary>
    internal float ScB => _b;

    /// <summary>
    /// Internal: Gets the Alpha component as a float (0.0 - 1.0).
    /// </summary>
    internal float ScA => _a;

    // Basic Colors
    public static Color Black => new(0, 0, 0);
    public static Color White => new(255, 255, 255);
    public static Color Red => new(255, 0, 0);
    public static Color Blue => new(0, 0, 255);
    public static Color LightGray => new(229, 229, 229); // ~0.9f
    public static Color DarkGray => new(77, 77, 77);     // ~0.3f

    // Tailwind v4 palette tokens (Slate50, Red500, …) now live on `ColorPalette`, addressed
    // by shade: `ColorPalette.Red[500]`. `Color` is just the RGBA primitive.

    /// <summary>
    /// Creates a Color from a hex string (e.g., "FF0000" or "#FF0000")
    /// </summary>
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Black;

        // Remove hash if present
        hex = hex.TrimStart('#');

        // Handle shorthand (e.g., "F00" -> "FF0000")
        if (hex.Length == 3 || hex.Length == 4) hex = string.Join("", hex.Select(c => $"{c}{c}"));

        // Validate length (must be 6 or 8 characters now)
        if (hex.Length != 6 && hex.Length != 8) return Black;

        try
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = 255;

            if (hex.Length == 8) a = Convert.ToByte(hex.Substring(6, 2), 16);

            return new Color(r, g, b, a);
        }
        catch
        {
            // Fallback for non-hexadecimal characters
            return Black;
        }
    }

    /// <summary>
    /// Creates a new Color with the specified alpha value (0-255).
    /// </summary>
    /// <param name="alpha">The alpha value (0 to 255)</param>
    /// <returns>A new Color with the specified alpha value</returns>
    public Color WithAlpha(byte alpha)
    {
        return new Color(R, G, B, alpha);
    }
    
    /// <summary>
    /// Creates a new Color with the specified alpha value (0.0 to 1.0).
    /// </summary>
    /// <param name="alpha">The alpha value (0.0 to 1.0)</param>
    /// <returns>A new Color with the specified alpha value</returns>
    public Color WithAlpha(float alpha)
    {
        return FromScRgb(_r, _g, _b, alpha);
    }

    /// <summary>
    /// Converts the color to a hex string (e.g. "#ff0000").
    /// </summary>
    /// <remarks>
    /// Lower case, matching the TypeScript implementation. The two emit this
    /// string straight into SVG, which the shared specification names as the
    /// artefact the conformance tooling compares, so the casing is part of the
    /// contract rather than a formatting preference.
    /// </remarks>
    public string ToHexString()
    {
        return $"#{R:x2}{G:x2}{B:x2}";
    }

    public bool Equals(Color other)
        => _r == other._r && _g == other._g && _b == other._b && _a == other._a;

    public override bool Equals(object? obj)
        => obj is Color other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(_r, _g, _b, _a);

    public static bool operator ==(Color left, Color right) => left.Equals(right);

    public static bool operator !=(Color left, Color right) => !left.Equals(right);
}
