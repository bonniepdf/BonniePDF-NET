// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
namespace BonniePDF;

/// <summary>
/// The colour a pie slice gradient starts from: each sRGB channel moved 45% of the way to white,
/// in integers from the colour's hex form so every implementation produces the same bytes.
/// </summary>
internal static class ChartTint
{
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
}
