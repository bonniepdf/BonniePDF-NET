using System.Text;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Tests for the native 1D barcode encoders. Mirrors the TypeScript
/// <c>barcode.test.ts</c> suite: authoritative check digits, encode -&gt;
/// decode round trips, structural invariants, validation, and tagging.
/// </summary>
public class BarcodeTests
{
    // ── Reverse tables (transcribed from the same ISO sources) used by the
    //    independent test decoders below. ──

    private static readonly string[] Code128Patterns =
    {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
        "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
        "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
        "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
        "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
        "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
        "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
        "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
        "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
        "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
        "114131","311141","411131","211412","211214","211232","2331112",
    };

    private static readonly string[] EanLCode =
    {
        "0001101","0011001","0010011","0111101","0100011",
        "0110001","0101111","0111011","0110111","0001011",
    };
    private static readonly string[] EanGCode =
    {
        "0100111","0110011","0011011","0100001","0011101",
        "0111001","0000101","0010001","0001001","0010111",
    };
    private static readonly string[] EanRCode =
    {
        "1110010","1100110","1101100","1000010","1011100",
        "1001110","1010000","1000100","1001000","1110100",
    };
    private static readonly string[] EanParity =
    {
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
        "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL",
    };
    private static readonly string[] ItfPatterns =
    {
        "NNWWN","WNNNW","NWNNW","WWNNN","NNWNW",
        "WNWNN","NWWNN","NNNWW","WNNWN","NWNWN",
    };

    // ── Check digits against authoritative GS1 / Wikipedia values ──

    [Fact]
    public void Ean13_ComputesCheckDigit()
    {
        Assert.Equal("4006381333931", BarcodeEncoder.Encode(BarcodeSymbology.Ean13, "400638133393").HumanReadable);
    }

    [Fact]
    public void UpcA_ComputesCheckDigit()
    {
        Assert.Equal("036000291452", BarcodeEncoder.Encode(BarcodeSymbology.UpcA, "03600029145").HumanReadable);
    }

    [Fact]
    public void Itf14_ComputesCheckDigit()
    {
        Assert.Equal("10614141000415", BarcodeEncoder.Encode(BarcodeSymbology.Itf14, "1061414100041").HumanReadable);
    }

    // ── Round trips ──

    [Theory]
    [InlineData("1Z9999W99999999999")]
    [InlineData("12345678")]
    [InlineData("ABC-123/xyz.99")]
    public void Code128_RoundTrips(string value)
    {
        var enc = BarcodeEncoder.Encode(BarcodeSymbology.Code128, value);
        Assert.True(enc.Modules[0]); // starts with a bar
        Assert.Equal(value, DecodeCode128(enc.Modules));
    }

    [Fact]
    public void Ean13_RoundTrips()
    {
        var enc = BarcodeEncoder.Encode(BarcodeSymbology.Ean13, "590123412345");
        Assert.Equal(enc.HumanReadable, DecodeEan13(enc.Modules));
    }

    [Fact]
    public void UpcA_RoundTripsAsLeadingZeroEan13()
    {
        var enc = BarcodeEncoder.Encode(BarcodeSymbology.UpcA, "03600029145");
        Assert.Equal("0" + enc.HumanReadable, DecodeEan13(enc.Modules));
    }

    [Fact]
    public void Itf14_RoundTrips()
    {
        var enc = BarcodeEncoder.Encode(BarcodeSymbology.Itf14, "1540014128876");
        Assert.Equal(enc.HumanReadable, DecodeItf14(enc.Modules));
    }

    // ── Structural invariants ──

    [Fact]
    public void Ean13_Has95Modules() =>
        Assert.Equal(95, BarcodeEncoder.Encode(BarcodeSymbology.Ean13, "400638133393").Modules.Length);

    [Fact]
    public void UpcA_Has95Modules() =>
        Assert.Equal(95, BarcodeEncoder.Encode(BarcodeSymbology.UpcA, "03600029145").Modules.Length);

    [Fact]
    public void Itf14_Has135Modules() =>
        Assert.Equal(135, BarcodeEncoder.Encode(BarcodeSymbology.Itf14, "1061414100041").Modules.Length);

    [Fact]
    public void Code128_ModuleCountMatchesLayout()
    {
        // Subset C: start + 4 pairs + checksum = 6 symbols at 11 modules + 13 stop.
        var enc = BarcodeEncoder.Encode(BarcodeSymbology.Code128, "12345678");
        Assert.Equal(11 * 6 + 13, enc.Modules.Length);
    }

    // ── Validation ──

    [Theory]
    [InlineData(BarcodeSymbology.Ean13, "ABC123")]
    [InlineData(BarcodeSymbology.Ean13, "12345")]
    [InlineData(BarcodeSymbology.UpcA, "123")]
    [InlineData(BarcodeSymbology.Itf14, "12")]
    [InlineData(BarcodeSymbology.Code128, "café")]
    [InlineData(BarcodeSymbology.Code128, "")]
    public void InvalidContent_Throws(BarcodeSymbology symbology, string value)
    {
        Assert.Throws<BarcodeContentException>(() => BarcodeEncoder.Encode(symbology, value));
    }

    // ── Tagging ──

    [Fact]
    public void Barcode_WithAltText_RoleIsFigure()
    {
        var bc = new Barcode { Value = "SHIP-12345", AltText = "Shipment tracking barcode" };
        Assert.Equal(SemanticRole.Figure, bc.SemanticRole);
    }

    [Fact]
    public void Barcode_WithoutAltText_RoleIsArtifact()
    {
        var bc = new Barcode { Value = "SHIP-12345" };
        Assert.Equal(SemanticRole.Artifact, bc.SemanticRole);
    }

    [Fact]
    public void TaggedDocument_BarcodeWithAltText_EmitsFigureWithAlt()
    {
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new Barcode
            {
                Value = "SHIP-12345",
                AltText = "Shipment tracking barcode SHIP-12345",
            });
        });

        Assert.Contains("/S /Figure", pdf);
        Assert.Contains("/Alt (Shipment tracking barcode SHIP-12345)", pdf);
    }

    [Fact]
    public void Document_BarcodeRendersValidPdf()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new Barcode { Value = "1Z9999W99999999999", Symbology = BarcodeSymbology.Code128 });
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.StartsWith("%PDF-", pdf);
        Assert.Contains("%%EOF", pdf);
    }

    private static string GenerateTagged(Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    // ── Independent test decoders (mirror the TypeScript suite) ──

    private static List<int> RunLengths(bool[] modules)
    {
        var runs = new List<int>();
        bool current = true; // streams always start dark
        int count = 0;
        foreach (bool m in modules)
        {
            if (m == current)
            {
                count++;
            }
            else
            {
                runs.Add(count);
                current = m;
                count = 1;
            }
        }
        runs.Add(count);
        return runs;
    }

    private static string DecodeCode128(bool[] modules)
    {
        var reverse = new Dictionary<string, int>();
        for (int v = 0; v < Code128Patterns.Length; v++) reverse[Code128Patterns[v]] = v;

        List<int> widths = RunLengths(modules);
        int symbolCount = (widths.Count - 7) / 6;
        Assert.Equal(0, (widths.Count - 7) % 6);

        var values = new List<int>();
        int i = 0;
        for (int s = 0; s < symbolCount; s++)
        {
            string pat = string.Concat(widths.Skip(i).Take(6));
            Assert.True(reverse.ContainsKey(pat));
            values.Add(reverse[pat]);
            i += 6;
        }
        Assert.Equal("2331112", string.Concat(widths.Skip(i)));

        int start = values[0];
        long sum = start;
        for (int k = 1; k < values.Count - 1; k++) sum += (long)k * values[k];
        Assert.Equal(values[^1], (int)(sum % 103));

        var payload = values.GetRange(1, values.Count - 2);
        var sb = new StringBuilder();
        if (start == 105)
        {
            foreach (int v in payload) sb.Append(v.ToString("D2"));
        }
        else
        {
            foreach (int v in payload) sb.Append((char)(v + 32));
        }
        return sb.ToString();
    }

    private static string DecodeEan13(bool[] modules)
    {
        Assert.Equal(95, modules.Length);
        string bits = string.Concat(modules.Select(m => m ? '1' : '0'));
        Assert.Equal("101", bits.Substring(0, 3));
        Assert.Equal("01010", bits.Substring(45, 5));
        Assert.Equal("101", bits.Substring(92, 3));

        var lRev = ReverseOf(EanLCode);
        var gRev = ReverseOf(EanGCode);
        var rRev = ReverseOf(EanRCode);
        var parityRev = ReverseOf(EanParity);

        var parity = new StringBuilder();
        var digits = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            string chunk = bits.Substring(3 + i * 7, 7);
            if (lRev.TryGetValue(chunk, out int l)) { parity.Append('L'); digits.Add(l); }
            else { Assert.True(gRev.TryGetValue(chunk, out int g)); parity.Append('G'); digits.Add(g); }
        }
        Assert.True(parityRev.ContainsKey(parity.ToString()));
        int firstDigit = parityRev[parity.ToString()];

        var right = new List<int>();
        for (int i = 0; i < 6; i++)
        {
            string chunk = bits.Substring(50 + i * 7, 7);
            Assert.True(rRev.ContainsKey(chunk));
            right.Add(rRev[chunk]);
        }

        var all = new List<int> { firstDigit };
        all.AddRange(digits);
        all.AddRange(right);

        int sum = 0;
        for (int i = 0; i < 12; i++) sum += all[i] * (i % 2 == 0 ? 1 : 3);
        Assert.Equal(all[12], (10 - (sum % 10)) % 10);

        return string.Concat(all);
    }

    private static string DecodeItf14(bool[] modules)
    {
        List<int> widths = RunLengths(modules);
        var sym = widths.Select(w => w == 3 ? 'W' : 'N').ToList();
        Assert.Equal("NNNN", string.Concat(sym.Take(4)));
        Assert.Equal("WNN", string.Concat(sym.Skip(sym.Count - 3)));

        var body = sym.GetRange(4, sym.Count - 7);
        var itfRev = ReverseOf(ItfPatterns);

        var digits = new List<int>();
        for (int p = 0; p < body.Count; p += 10)
        {
            var group = body.GetRange(p, 10);
            var bars = new StringBuilder();
            var spaces = new StringBuilder();
            for (int k = 0; k < 10; k += 2) { bars.Append(group[k]); spaces.Append(group[k + 1]); }
            Assert.True(itfRev.ContainsKey(bars.ToString()));
            Assert.True(itfRev.ContainsKey(spaces.ToString()));
            digits.Add(itfRev[bars.ToString()]);
            digits.Add(itfRev[spaces.ToString()]);
        }

        int sum = 0;
        for (int i = 0; i < 13; i++)
        {
            int fromRight = 13 - 1 - i;
            sum += digits[i] * (fromRight % 2 == 0 ? 3 : 1);
        }
        Assert.Equal(digits[13], (10 - (sum % 10)) % 10);

        return string.Concat(digits);
    }

    private static Dictionary<string, int> ReverseOf(string[] table)
    {
        var map = new Dictionary<string, int>();
        for (int i = 0; i < table.Length; i++) map[table[i]] = i;
        return map;
    }
}
