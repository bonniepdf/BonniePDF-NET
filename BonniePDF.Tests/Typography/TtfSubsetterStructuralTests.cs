using BonniePDF.Tests.Support;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// Structural validity of the emitted subset. These guard browser-side rendering
/// (the SVG preview embeds the subset as an @font-face): the OpenType sanitizer in
/// Chrome/Firefox rejects a font whose composite glyphs reference component GIDs
/// outside the subset, which silently falls the text back to a default face and
/// wrecks the baked-in glyph metrics. Mirrors the TypeScript port's
/// <c>core/tests/typography/ttf-subsetter.test.ts</c>.
/// </summary>
public class TtfSubsetterStructuralTests
{
    private static ushort U16(byte[] b, int o) => (ushort)((b[o] << 8) | b[o + 1]);
    private static short S16(byte[] b, int o) => (short)((b[o] << 8) | b[o + 1]);
    private static uint U32(byte[] b, int o) => (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]);

    private static uint Checksum(byte[] b, int start, int len)
    {
        uint sum = 0;
        for (int i = 0; i < len; i += 4)
        {
            uint v = 0;
            for (int j = 0; j < 4; j++) v = (v << 8) | (uint)(i + j < len ? b[start + i + j] : 0);
            sum += v;
        }
        return sum;
    }

    // Subset a set of characters that map to composite glyphs (accented Latin
    // letters in Liberation Sans are 'base + diacritic' composites).
    private static byte[] SubsetAccented(out Dictionary<string, (int off, int len, uint csum)> dir, out byte[] data)
    {
        var font = new TtfFont(TestFonts.SansRegularPath);
        var subsetter = new TtfSubsetter(font);
        var chars = "Aeéàñüçôî".ToCharArray();
        var usedGlyphs = new HashSet<ushort>(chars.Select(c => font.GetGlyphId(c)).Where(g => g != 0));
        var charMap = chars.Where(c => font.GetGlyphId(c) != 0).ToDictionary(c => (int)c, c => font.GetGlyphId(c));
        data = subsetter.CreateSubset(usedGlyphs, charMap);

        int num = U16(data, 4);
        dir = new Dictionary<string, (int, int, uint)>();
        for (int i = 0; i < num; i++)
        {
            int o = 12 + i * 16;
            string tag = System.Text.Encoding.ASCII.GetString(data, o, 4);
            dir[tag] = ((int)U32(data, o + 8), (int)U32(data, o + 12), U32(data, o + 4));
        }
        return data;
    }

    [Fact]
    public void CompositeGlyphs_ReferenceOnlyInRangeComponentGids()
    {
        SubsetAccented(out var dir, out byte[] b);
        int numGlyphs = U16(b, dir["maxp"].off + 4);
        int locaO = dir["loca"].off; // subset always writes long loca
        int glyfO = dir["glyf"].off;

        int composites = 0;
        for (int g = 0; g < numGlyphs; g++)
        {
            int st = (int)U32(b, locaO + g * 4), en = (int)U32(b, locaO + g * 4 + 4);
            if (en == st) continue;
            short numberOfContours = S16(b, glyfO + st);
            if (numberOfContours >= 0) continue; // simple glyph
            composites++;
            int off = glyfO + st + 10;
            while (true)
            {
                ushort flags = U16(b, off);
                ushort comp = U16(b, off + 2);
                Assert.True(comp < numGlyphs, $"composite glyph {g} references out-of-range component {comp} (numGlyphs {numGlyphs})");
                off += 4;
                off += (flags & 0x0001) != 0 ? 4 : 2; // ARG_1_AND_2_ARE_WORDS
                if ((flags & 0x0008) != 0) off += 2;        // WE_HAVE_A_SCALE
                else if ((flags & 0x0040) != 0) off += 4;   // X_AND_Y_SCALE
                else if ((flags & 0x0080) != 0) off += 8;   // TWO_BY_TWO
                if ((flags & 0x0020) == 0) break;            // MORE_COMPONENTS
            }
        }
        // The chosen characters must actually exercise composite glyphs, otherwise
        // the assertion above is vacuous.
        Assert.True(composites > 0, "test must exercise composite glyphs");
    }

    [Fact]
    public void EmitsCorrectTableChecksumsAndHeadCheckSumAdjustment()
    {
        SubsetAccented(out var dir, out byte[] b);
        int headOffset = dir["head"].off;

        foreach (var (tag, info) in dir)
        {
            if (tag == "head")
            {
                // checkSumAdjustment is excluded from the head table's own checksum.
                var copy = (byte[])b.Clone();
                copy[info.off + 8] = copy[info.off + 9] = copy[info.off + 10] = copy[info.off + 11] = 0;
                Assert.Equal(info.csum, Checksum(copy, info.off, info.len));
            }
            else
            {
                Assert.Equal(info.csum, Checksum(b, info.off, info.len));
            }
        }

        // checkSumAdjustment = 0xB1B0AFBA - checksum(whole file with the field zeroed)
        var file = (byte[])b.Clone();
        file[headOffset + 8] = file[headOffset + 9] = file[headOffset + 10] = file[headOffset + 11] = 0;
        uint expected = unchecked(0xB1B0AFBAu - Checksum(file, 0, file.Length));
        Assert.Equal(expected, U32(b, headOffset + 8));
    }
}
