using System.Collections.Generic;

namespace BonniePDF.Typography;

/// <summary>
/// WinAnsiEncoding (Windows-1252) for the standard text fonts: the single byte that shows a character.
/// Latin-1 characters keep their code, the characters Windows-1252 places at 0x80-0x9F use that byte,
/// and any other character has no code and shows as <c>?</c>.
/// </summary>
internal static class WinAnsi
{
    private static readonly Dictionary<int, byte> High = new()
    {
        [0x20AC] = 0x80, [0x201A] = 0x82, [0x0192] = 0x83, [0x201E] = 0x84,
        [0x2026] = 0x85, [0x2020] = 0x86, [0x2021] = 0x87, [0x02C6] = 0x88,
        [0x2030] = 0x89, [0x0160] = 0x8A, [0x2039] = 0x8B, [0x0152] = 0x8C,
        [0x017D] = 0x8E, [0x2018] = 0x91, [0x2019] = 0x92, [0x201C] = 0x93,
        [0x201D] = 0x94, [0x2022] = 0x95, [0x2013] = 0x96, [0x2014] = 0x97,
        [0x02DC] = 0x98, [0x2122] = 0x99, [0x0161] = 0x9A, [0x203A] = 0x9B,
        [0x0153] = 0x9C, [0x017E] = 0x9E, [0x0178] = 0x9F,
    };

    /// <summary>The WinAnsi byte that shows <paramref name="codePoint"/> in a standard font, or 0x3F (<c>?</c>) when it has none.</summary>
    internal static byte Byte(int codePoint) =>
        codePoint <= 0xFF ? (byte)codePoint : High.TryGetValue(codePoint, out byte b) ? b : (byte)0x3F;
}
