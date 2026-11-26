// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
namespace BonniePDF;

/// <summary>
/// The colour a pie slice gradient starts from: each sRGB channel moved 45% of the way to white,
/// in integers from the colour's hex form so every implementation produces the same bytes.
/// </summary>
internal static class ChartTint
{
    /// <summary>A darker shade for mark outlines: each sRGB channel reduced by 30%, in integers from the colour's hex form.</summary>
    internal static Color Darken(Color color)
    {
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(hex.Substring(offset, 2), 16);
            return c - (c * 30 + 50) / 100;
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }

    internal static Color Tint(Color color)
    {
        if (color.ScA != 1) throw new ArgumentException("Pie slice gradients require opaque colours.", nameof(color));
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(hex.Substring(offset, 2), 16);
            return c + ((255 - c) * 45 + 50) / 100;
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }

    /// <summary>
    /// Mixes <paramref name="color"/> towards <paramref name="background"/>, keeping the fraction <paramref name="keep"/> of the colour:
    /// <c>1</c> returns the colour, <c>0</c> the background. A colour mix rather than transparency, so it is valid under PDF/A-1.
    /// </summary>
    internal static Color Dim(Color color, Color background, double keep)
    {
        string fg = color.ToHexString();
        string bg = background.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(fg.Substring(offset, 2), 16);
            int b = Convert.ToInt32(bg.Substring(offset, 2), 16);
            return (int)Math.Round(c * keep + b * (1 - keep), MidpointRounding.AwayFromZero);
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }

    /// <summary>The top face of a 3D bar: each channel 20 % of the way to white, in integers.</summary>
    internal static Color TintFace(Color color)
    {
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(hex.Substring(offset, 2), 16);
            return c + ((255 - c) * 20 + 50) / 100;
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }

    /// <summary>The side face of a 3D bar: each channel reduced by 20 %, in integers.</summary>
    internal static Color ShadeFace(Color color)
    {
        string hex = color.ToHexString();
        int Channel(int offset)
        {
            int c = Convert.ToInt32(hex.Substring(offset, 2), 16);
            return c - (c * 20 + 50) / 100;
        }
        return new Color(Channel(1), Channel(3), Channel(5));
    }
}
