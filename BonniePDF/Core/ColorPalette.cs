using System;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// A Tailwind v4 colour ramp: eleven fixed shade stops (50 → 950) addressed by index,
/// e.g. <c>ColorPalette.Red[500]</c>.
/// </summary>
/// <remarks>
/// This is the single home for the palette tokens — <see cref="Color"/> is just the RGBA
/// primitive and no longer carries <c>Red500</c> etc. The values are the exact published Tailwind
/// swatches and are <b>not</b> computed: Tailwind's ramps are hand-tuned in OKLCH and cannot be
/// reproduced by lightening or darkening a single base colour, so storing the real values (rather
/// than a formula) is what keeps the swatches accurate.
/// </remarks>
public sealed class ColorPalette
{
    /// <summary>The canonical shade stops, lightest (50) to darkest (950).</summary>
    public static IReadOnlyList<int> Shades { get; } =
        new[] { 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950 };

    private readonly Color[] _ramp;

    private ColorPalette(
        string h50, string h100, string h200, string h300, string h400, string h500,
        string h600, string h700, string h800, string h900, string h950)
    {
        _ramp = new[]
        {
            Color.FromHex(h50), Color.FromHex(h100), Color.FromHex(h200), Color.FromHex(h300),
            Color.FromHex(h400), Color.FromHex(h500), Color.FromHex(h600), Color.FromHex(h700),
            Color.FromHex(h800), Color.FromHex(h900), Color.FromHex(h950),
        };
    }

    /// <summary>Gets the colour at the given shade stop (50, 100, … 950).</summary>
    /// <param name="shade">One of the stops in <see cref="Shades"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The shade is not one of the eleven stops.</exception>
    public Color this[int shade]
    {
        get
        {
            for (int i = 0; i < Shades.Count; i++)
                if (Shades[i] == shade) return _ramp[i];

            throw new ArgumentOutOfRangeException(
                nameof(shade), shade, $"Shade must be one of: {string.Join(", ", Shades)}.");
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static ColorPalette Slate { get; } = new("f8fafc", "f1f5f9", "e2e8f0", "cad5e2", "90a1b9", "62748e", "45556c", "314158", "1d293d", "0f172b", "020618");
    public static ColorPalette Gray { get; } = new("f9fafb", "f3f4f6", "e5e7eb", "d1d5dc", "99a1af", "6a7282", "4a5565", "364153", "1e2939", "101828", "030712");
    public static ColorPalette Zinc { get; } = new("fafafa", "f4f4f5", "e4e4e7", "d4d4d8", "9f9fa9", "71717b", "52525c", "3f3f46", "27272a", "18181b", "09090b");
    public static ColorPalette Neutral { get; } = new("fafafa", "f5f5f5", "e5e5e5", "d4d4d4", "a1a1a1", "737373", "525252", "404040", "262626", "171717", "0a0a0a");
    public static ColorPalette Stone { get; } = new("fafaf9", "f5f5f4", "e7e5e4", "d6d3d1", "a6a09b", "79716b", "57534d", "44403b", "292524", "1c1917", "0c0a09");
    public static ColorPalette Red { get; } = new("fef2f2", "ffe2e2", "ffc9c9", "ffa2a2", "ff6467", "fb2c36", "e7000b", "c10007", "9f0712", "82181a", "460809");
    public static ColorPalette Orange { get; } = new("fff7ed", "ffedd4", "ffd6a7", "ffb86a", "ff8904", "ff6900", "f54900", "ca3500", "9f2d00", "7e2a0c", "441306");
    public static ColorPalette Amber { get; } = new("fffbeb", "fef3c6", "fee685", "ffd230", "ffb900", "fe9a00", "e17100", "bb4d00", "973c00", "7b3306", "461901");
    public static ColorPalette Yellow { get; } = new("fefce8", "fef9c2", "fff085", "ffdf20", "fdc700", "f0b100", "d08700", "a65f00", "894b00", "733e0a", "432004");
    public static ColorPalette Lime { get; } = new("f7fee7", "ecfcca", "d8f999", "bbf451", "9ae600", "7ccf00", "5ea500", "497d00", "3c6300", "35530e", "192e03");
    public static ColorPalette Green { get; } = new("f0fdf4", "dcfce7", "b9f8cf", "7bf1a8", "05df72", "00c950", "00a63e", "008236", "016630", "0d542b", "032e15");
    public static ColorPalette Emerald { get; } = new("ecfdf5", "d0fae5", "a4f4cf", "5ee9b5", "00d492", "00bc7d", "009966", "007a55", "006045", "004f3b", "002c22");
    public static ColorPalette Teal { get; } = new("f0fdfa", "cbfbf1", "96f7e4", "46ecd5", "00d5be", "00bba7", "009689", "00786f", "005f5a", "0b4f4a", "022f2e");
    public static ColorPalette Cyan { get; } = new("ecfeff", "cefafe", "a2f4fd", "53eafd", "00d3f2", "00b8db", "0092b8", "007595", "005f78", "104e64", "053345");
    public static ColorPalette Sky { get; } = new("f0f9ff", "dff2fe", "b8e6fe", "74d4ff", "00bcff", "00a6f4", "0084d1", "0069a8", "00598a", "024a70", "052f4a");
    public static ColorPalette Blue { get; } = new("eff6ff", "dbeafe", "bedbff", "8ec5ff", "51a2ff", "2b7fff", "155dfc", "1447e6", "193cb8", "1c398e", "162456");
    public static ColorPalette Indigo { get; } = new("eef2ff", "e0e7ff", "c6d2ff", "a3b3ff", "7c86ff", "615fff", "4f39f6", "432dd7", "372aac", "312c85", "1e1a4d");
    public static ColorPalette Violet { get; } = new("f5f3ff", "ede9fe", "ddd6ff", "c4b4ff", "a684ff", "8e51ff", "7f22fe", "7008e7", "5d0ec0", "4d179a", "2f0d68");
    public static ColorPalette Purple { get; } = new("faf5ff", "f3e8ff", "e9d4ff", "dab2ff", "c27aff", "ad46ff", "9810fa", "8200db", "6e11b0", "59168b", "3c0366");
    public static ColorPalette Fuchsia { get; } = new("fdf4ff", "fae8ff", "f6cfff", "f4a8ff", "ed6aff", "e12afb", "c800de", "a800b7", "8a0194", "721378", "4b004f");
    public static ColorPalette Pink { get; } = new("fdf2f8", "fce7f3", "fccee8", "fda5d5", "fb64b6", "f6339a", "e60076", "c6005c", "a3004c", "861043", "510424");
    public static ColorPalette Rose { get; } = new("fff1f2", "ffe4e6", "ffccd3", "ffa1ad", "ff637e", "ff2056", "ec003f", "c70036", "a50036", "8b0836", "4d0218");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
