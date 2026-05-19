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

    // --- Tailwind v4 Colors ---

    // Slate
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static Color Slate50 => FromHex("f8fafc");
    public static Color Slate100 => FromHex("f1f5f9");
    public static Color Slate200 => FromHex("e2e8f0");
    public static Color Slate300 => FromHex("cad5e2");
    public static Color Slate400 => FromHex("90a1b9");
    public static Color Slate500 => FromHex("62748e");
    public static Color Slate600 => FromHex("45556c");
    public static Color Slate700 => FromHex("314158");
    public static Color Slate800 => FromHex("1d293d");
    public static Color Slate900 => FromHex("0f172b");
    public static Color Slate950 => FromHex("020618");

    // Gray
    public static Color Gray50 => FromHex("f9fafb");
    public static Color Gray100 => FromHex("f3f4f6");
    public static Color Gray200 => FromHex("e5e7eb");
    public static Color Gray300 => FromHex("d1d5dc");
    public static Color Gray400 => FromHex("99a1af");
    public static Color Gray500 => FromHex("6a7282");
    public static Color Gray600 => FromHex("4a5565");
    public static Color Gray700 => FromHex("364153");
    public static Color Gray800 => FromHex("1e2939");
    public static Color Gray900 => FromHex("101828");
    public static Color Gray950 => FromHex("030712");

    // Zinc
    public static Color Zinc50 => FromHex("fafafa");
    public static Color Zinc100 => FromHex("f4f4f5");
    public static Color Zinc200 => FromHex("e4e4e7");
    public static Color Zinc300 => FromHex("d4d4d8");
    public static Color Zinc400 => FromHex("9f9fa9");
    public static Color Zinc500 => FromHex("71717b");
    public static Color Zinc600 => FromHex("52525c");
    public static Color Zinc700 => FromHex("3f3f46");
    public static Color Zinc800 => FromHex("27272a");
    public static Color Zinc900 => FromHex("18181b");
    public static Color Zinc950 => FromHex("09090b");

    // Neutral
    public static Color Neutral50 => FromHex("fafafa");
    public static Color Neutral100 => FromHex("f5f5f5");
    public static Color Neutral200 => FromHex("e5e5e5");
    public static Color Neutral300 => FromHex("d4d4d4");
    public static Color Neutral400 => FromHex("a1a1a1");
    public static Color Neutral500 => FromHex("737373");
    public static Color Neutral600 => FromHex("525252");
    public static Color Neutral700 => FromHex("404040");
    public static Color Neutral800 => FromHex("262626");
    public static Color Neutral900 => FromHex("171717");
    public static Color Neutral950 => FromHex("0a0a0a");

    // Stone
    public static Color Stone50 => FromHex("fafaf9");
    public static Color Stone100 => FromHex("f5f5f4");
    public static Color Stone200 => FromHex("e7e5e4");
    public static Color Stone300 => FromHex("d6d3d1");
    public static Color Stone400 => FromHex("a6a09b");
    public static Color Stone500 => FromHex("79716b");
    public static Color Stone600 => FromHex("57534d");
    public static Color Stone700 => FromHex("44403b");
    public static Color Stone800 => FromHex("292524");
    public static Color Stone900 => FromHex("1c1917");
    public static Color Stone950 => FromHex("0c0a09");

    // Red
    public static Color Red50 => FromHex("fef2f2");
    public static Color Red100 => FromHex("ffe2e2");
    public static Color Red200 => FromHex("ffc9c9");
    public static Color Red300 => FromHex("ffa2a2");
    public static Color Red400 => FromHex("ff6467");
    public static Color Red500 => FromHex("fb2c36");
    public static Color Red600 => FromHex("e7000b");
    public static Color Red700 => FromHex("c10007");
    public static Color Red800 => FromHex("9f0712");
    public static Color Red900 => FromHex("82181a");
    public static Color Red950 => FromHex("460809");

    // Orange
    public static Color Orange50 => FromHex("fff7ed");
    public static Color Orange100 => FromHex("ffedd4");
    public static Color Orange200 => FromHex("ffd6a7");
    public static Color Orange300 => FromHex("ffb86a");
    public static Color Orange400 => FromHex("ff8904");
    public static Color Orange500 => FromHex("ff6900");
    public static Color Orange600 => FromHex("f54900");
    public static Color Orange700 => FromHex("ca3500");
    public static Color Orange800 => FromHex("9f2d00");
    public static Color Orange900 => FromHex("7e2a0c");
    public static Color Orange950 => FromHex("441306");

    // Amber
    public static Color Amber50 => FromHex("fffbeb");
    public static Color Amber100 => FromHex("fef3c6");
    public static Color Amber200 => FromHex("fee685");
    public static Color Amber300 => FromHex("ffd230");
    public static Color Amber400 => FromHex("ffb900");
    public static Color Amber500 => FromHex("fe9a00");
    public static Color Amber600 => FromHex("e17100");
    public static Color Amber700 => FromHex("bb4d00");
    public static Color Amber800 => FromHex("973c00");
    public static Color Amber900 => FromHex("7b3306");
    public static Color Amber950 => FromHex("461901");

    // Yellow
    public static Color Yellow50 => FromHex("fefce8");
    public static Color Yellow100 => FromHex("fef9c2");
    public static Color Yellow200 => FromHex("fff085");
    public static Color Yellow300 => FromHex("ffdf20");
    public static Color Yellow400 => FromHex("fdc700");
    public static Color Yellow500 => FromHex("f0b100");
    public static Color Yellow600 => FromHex("d08700");
    public static Color Yellow700 => FromHex("a65f00");
    public static Color Yellow800 => FromHex("894b00");
    public static Color Yellow900 => FromHex("733e0a");
    public static Color Yellow950 => FromHex("432004");

    // Lime
    public static Color Lime50 => FromHex("f7fee7");
    public static Color Lime100 => FromHex("ecfcca");
    public static Color Lime200 => FromHex("d8f999");
    public static Color Lime300 => FromHex("bbf451");
    public static Color Lime400 => FromHex("9ae600");
    public static Color Lime500 => FromHex("7ccf00");
    public static Color Lime600 => FromHex("5ea500");
    public static Color Lime700 => FromHex("497d00");
    public static Color Lime800 => FromHex("3c6300");
    public static Color Lime900 => FromHex("35530e");
    public static Color Lime950 => FromHex("192e03");

    // Green
    public static Color Green50 => FromHex("f0fdf4");
    public static Color Green100 => FromHex("dcfce7");
    public static Color Green200 => FromHex("b9f8cf");
    public static Color Green300 => FromHex("7bf1a8");
    public static Color Green400 => FromHex("05df72");
    public static Color Green500 => FromHex("00c950");
    public static Color Green600 => FromHex("00a63e");
    public static Color Green700 => FromHex("008236");
    public static Color Green800 => FromHex("016630");
    public static Color Green900 => FromHex("0d542b");
    public static Color Green950 => FromHex("032e15");

    // Emerald
    public static Color Emerald50 => FromHex("ecfdf5");
    public static Color Emerald100 => FromHex("d0fae5");
    public static Color Emerald200 => FromHex("a4f4cf");
    public static Color Emerald300 => FromHex("5ee9b5");
    public static Color Emerald400 => FromHex("00d492");
    public static Color Emerald500 => FromHex("00bc7d");
    public static Color Emerald600 => FromHex("009966");
    public static Color Emerald700 => FromHex("007a55");
    public static Color Emerald800 => FromHex("006045");
    public static Color Emerald900 => FromHex("004f3b");
    public static Color Emerald950 => FromHex("002c22");

    // Teal
    public static Color Teal50 => FromHex("f0fdfa");
    public static Color Teal100 => FromHex("cbfbf1");
    public static Color Teal200 => FromHex("96f7e4");
    public static Color Teal300 => FromHex("46ecd5");
    public static Color Teal400 => FromHex("00d5be");
    public static Color Teal500 => FromHex("00bba7");
    public static Color Teal600 => FromHex("009689");
    public static Color Teal700 => FromHex("00786f");
    public static Color Teal800 => FromHex("005f5a");
    public static Color Teal900 => FromHex("0b4f4a");
    public static Color Teal950 => FromHex("022f2e");

    // Cyan
    public static Color Cyan50 => FromHex("ecfeff");
    public static Color Cyan100 => FromHex("cefafe");
    public static Color Cyan200 => FromHex("a2f4fd");
    public static Color Cyan300 => FromHex("53eafd");
    public static Color Cyan400 => FromHex("00d3f2");
    public static Color Cyan500 => FromHex("00b8db");
    public static Color Cyan600 => FromHex("0092b8");
    public static Color Cyan700 => FromHex("007595");
    public static Color Cyan800 => FromHex("005f78");
    public static Color Cyan900 => FromHex("104e64");
    public static Color Cyan950 => FromHex("053345");

    // Sky
    public static Color Sky50 => FromHex("f0f9ff");
    public static Color Sky100 => FromHex("dff2fe");
    public static Color Sky200 => FromHex("b8e6fe");
    public static Color Sky300 => FromHex("74d4ff");
    public static Color Sky400 => FromHex("00bcff");
    public static Color Sky500 => FromHex("00a6f4");
    public static Color Sky600 => FromHex("0084d1");
    public static Color Sky700 => FromHex("0069a8");
    public static Color Sky800 => FromHex("00598a");
    public static Color Sky900 => FromHex("024a70");
    public static Color Sky950 => FromHex("052f4a");

    // Blue
    public static Color Blue50 => FromHex("eff6ff");
    public static Color Blue100 => FromHex("dbeafe");
    public static Color Blue200 => FromHex("bedbff");
    public static Color Blue300 => FromHex("8ec5ff");
    public static Color Blue400 => FromHex("51a2ff");
    public static Color Blue500 => FromHex("2b7fff");
    public static Color Blue600 => FromHex("155dfc");
    public static Color Blue700 => FromHex("1447e6");
    public static Color Blue800 => FromHex("193cb8");
    public static Color Blue900 => FromHex("1c398e");
    public static Color Blue950 => FromHex("162456");

    // Indigo
    public static Color Indigo50 => FromHex("eef2ff");
    public static Color Indigo100 => FromHex("e0e7ff");
    public static Color Indigo200 => FromHex("c6d2ff");
    public static Color Indigo300 => FromHex("a3b3ff");
    public static Color Indigo400 => FromHex("7c86ff");
    public static Color Indigo500 => FromHex("615fff");
    public static Color Indigo600 => FromHex("4f39f6");
    public static Color Indigo700 => FromHex("432dd7");
    public static Color Indigo800 => FromHex("372aac");
    public static Color Indigo900 => FromHex("312c85");
    public static Color Indigo950 => FromHex("1e1a4d");

    // Violet
    public static Color Violet50 => FromHex("f5f3ff");
    public static Color Violet100 => FromHex("ede9fe");
    public static Color Violet200 => FromHex("ddd6ff");
    public static Color Violet300 => FromHex("c4b4ff");
    public static Color Violet400 => FromHex("a684ff");
    public static Color Violet500 => FromHex("8e51ff");
    public static Color Violet600 => FromHex("7f22fe");
    public static Color Violet700 => FromHex("7008e7");
    public static Color Violet800 => FromHex("5d0ec0");
    public static Color Violet900 => FromHex("4d179a");
    public static Color Violet950 => FromHex("2f0d68");

    // Purple
    public static Color Purple50 => FromHex("faf5ff");
    public static Color Purple100 => FromHex("f3e8ff");
    public static Color Purple200 => FromHex("e9d4ff");
    public static Color Purple300 => FromHex("dab2ff");
    public static Color Purple400 => FromHex("c27aff");
    public static Color Purple500 => FromHex("ad46ff");
    public static Color Purple600 => FromHex("9810fa");
    public static Color Purple700 => FromHex("8200db");
    public static Color Purple800 => FromHex("6e11b0");
    public static Color Purple900 => FromHex("59168b");
    public static Color Purple950 => FromHex("3c0366");

    // Fuchsia
    public static Color Fuchsia50 => FromHex("fdf4ff");
    public static Color Fuchsia100 => FromHex("fae8ff");
    public static Color Fuchsia200 => FromHex("f6cfff");
    public static Color Fuchsia300 => FromHex("f4a8ff");
    public static Color Fuchsia400 => FromHex("ed6aff");
    public static Color Fuchsia500 => FromHex("e12afb");
    public static Color Fuchsia600 => FromHex("c800de");
    public static Color Fuchsia700 => FromHex("a800b7");
    public static Color Fuchsia800 => FromHex("8a0194");
    public static Color Fuchsia900 => FromHex("721378");
    public static Color Fuchsia950 => FromHex("4b004f");

    // Pink
    public static Color Pink50 => FromHex("fdf2f8");
    public static Color Pink100 => FromHex("fce7f3");
    public static Color Pink200 => FromHex("fccee8");
    public static Color Pink300 => FromHex("fda5d5");
    public static Color Pink400 => FromHex("fb64b6");
    public static Color Pink500 => FromHex("f6339a");
    public static Color Pink600 => FromHex("e60076");
    public static Color Pink700 => FromHex("c6005c");
    public static Color Pink800 => FromHex("a3004c");
    public static Color Pink900 => FromHex("861043");
    public static Color Pink950 => FromHex("510424");

    // Rose
    public static Color Rose50 => FromHex("fff1f2");
    public static Color Rose100 => FromHex("ffe4e6");
    public static Color Rose200 => FromHex("ffccd3");
    public static Color Rose300 => FromHex("ffa1ad");
    public static Color Rose400 => FromHex("ff637e");
    public static Color Rose500 => FromHex("ff2056");
    public static Color Rose600 => FromHex("ec003f");
    public static Color Rose700 => FromHex("c70036");
    public static Color Rose800 => FromHex("a50036");
    public static Color Rose900 => FromHex("8b0836");
    public static Color Rose950 => FromHex("4d0218");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
    /// Converts the color to a hex string (e.g. "#FF0000").
    /// </summary>
    public string ToHexString()
    {
        return $"#{R:X2}{G:X2}{B:X2}";
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
