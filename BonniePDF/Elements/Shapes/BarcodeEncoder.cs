using System;
using System.Collections.Generic;
using System.Text;

namespace BonniePDF;

/// <summary>
/// The result of encoding a value into a 1D barcode: a flat array of
/// equal-width modules (dark/light, left to right, with no quiet zone)
/// plus the human-readable text (including any computed check digit).
/// </summary>
internal sealed class EncodedBarcode
{
    public EncodedBarcode(bool[] modules, string humanReadable)
    {
        Modules = modules;
        HumanReadable = humanReadable;
    }

    /// <summary>Dark (true) / light (false) modules, left to right, no quiet zone.</summary>
    public bool[] Modules { get; }

    /// <summary>The human-readable value, including the computed check digit.</summary>
    public string HumanReadable { get; }
}

/// <summary>
/// Pure, dependency-free encoders for the tier-1 1D barcode symbologies
/// (Code 128, EAN-13, UPC-A, ITF-14). Every symbology reduces to a flat
/// array of equal-width modules so the <see cref="Barcode"/> element can
/// render any of them with a single coalescing draw loop.
///
/// This mirrors the TypeScript implementation module-for-module so the
/// two libraries emit identical bar patterns for identical input.
/// </summary>
internal static class BarcodeEncoder
{
    public static EncodedBarcode Encode(BarcodeSymbology symbology, string value)
    {
        if (value == null)
            throw new BarcodeContentException(symbology, "Barcode value must not be null.");

        return symbology switch
        {
            BarcodeSymbology.Code128 => EncodeCode128(value),
            BarcodeSymbology.Ean13 => EncodeEan13(value),
            BarcodeSymbology.UpcA => EncodeUpcA(value),
            BarcodeSymbology.Itf14 => EncodeItf14(value),
            _ => throw new BarcodeContentException(symbology, $"Unsupported symbology '{symbology}'."),
        };
    }

    /// <summary>Recommended quiet zone (light margin) in narrow modules per side.</summary>
    public static int DefaultQuietZone(BarcodeSymbology symbology) => symbology switch
    {
        BarcodeSymbology.Ean13 => 9,
        BarcodeSymbology.UpcA => 9,
        _ => 10,
    };

    // ── Code 128 (ISO/IEC 15417) ──────────────────────────────────────

    // The 107 symbol patterns (values 0-106). Each is a 6-element (values
    // 0-105) or 7-element (value 106, the stop) sequence of bar/space
    // widths starting with a bar. Widths sum to 11 modules (13 for stop).
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

    private const int Code128StartB = 104;
    private const int Code128StartC = 105;
    private const int Code128Stop = 106;

    private static EncodedBarcode EncodeCode128(string value)
    {
        if (value.Length == 0)
            throw new BarcodeContentException(BarcodeSymbology.Code128, "Code 128 requires at least one character.");

        foreach (char ch in value)
        {
            if (ch < 32 || ch > 126)
                throw new BarcodeContentException(BarcodeSymbology.Code128,
                    $"Code 128 (subset B) supports ASCII 32-126 only. Got character U+{(int)ch:X4}.");
        }

        List<int> codes = BuildCode128Values(value);

        // Modulo-103 weighted checksum: start value + sum(i * value_i).
        long sum = codes[0];
        for (int i = 1; i < codes.Count; i++)
            sum += (long)i * codes[i];
        codes.Add((int)(sum % 103));
        codes.Add(Code128Stop);

        var modules = new List<bool>(codes.Count * 11 + 2);
        foreach (int code in codes)
            AppendCode128Symbol(modules, code);

        return new EncodedBarcode(modules.ToArray(), value);
    }

    private static List<int> BuildCode128Values(string value)
    {
        // Subset C (digit pairs) is used when the whole payload is digits
        // and even-length; otherwise subset B encodes every character.
        // Both are valid and scannable; C is purely a density optimisation.
        bool allDigits = true;
        foreach (char ch in value)
        {
            if (ch < '0' || ch > '9') { allDigits = false; break; }
        }

        var codes = new List<int>();
        if (allDigits && value.Length % 2 == 0)
        {
            codes.Add(Code128StartC);
            for (int i = 0; i < value.Length; i += 2)
                codes.Add((value[i] - '0') * 10 + (value[i + 1] - '0'));
        }
        else
        {
            codes.Add(Code128StartB);
            foreach (char ch in value)
                codes.Add(ch - 32);
        }

        return codes;
    }

    private static void AppendCode128Symbol(List<bool> modules, int code)
    {
        string pattern = Code128Patterns[code];
        bool dark = true; // every symbol starts with a bar
        foreach (char widthChar in pattern)
        {
            int width = widthChar - '0';
            for (int i = 0; i < width; i++)
                modules.Add(dark);
            dark = !dark;
        }
    }

    // ── EAN-13 / UPC-A (ISO/IEC 15420) ────────────────────────────────

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

    // For each leading digit, the L/G parity pattern of the six left-half
    // digits. 'L' selects EanLCode, 'G' selects EanGCode.
    private static readonly string[] EanParity =
    {
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
        "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL",
    };

    private static EncodedBarcode EncodeUpcA(string value)
    {
        RequireDigits(BarcodeSymbology.UpcA, value, 11, "UPC-A requires exactly 11 digits (excluding the check digit).");

        // UPC-A is structurally EAN-13 with an implicit leading zero.
        int[] twelve = ToDigits("0" + value);
        int check = Ean13CheckDigit(twelve);
        bool[] modules = BuildEan13Modules(twelve, check);

        // Human-readable form shows the 12-digit UPC-A (11 data + check).
        string human = value + check.ToString();
        return new EncodedBarcode(modules, human);
    }

    private static EncodedBarcode EncodeEan13(string value)
    {
        RequireDigits(BarcodeSymbology.Ean13, value, 12, "EAN-13 requires exactly 12 digits (excluding the check digit).");

        int[] twelve = ToDigits(value);
        int check = Ean13CheckDigit(twelve);
        bool[] modules = BuildEan13Modules(twelve, check);

        string human = value + check.ToString();
        return new EncodedBarcode(modules, human);
    }

    private static bool[] BuildEan13Modules(int[] twelve, int check)
    {
        var sb = new StringBuilder(95);

        // Left guard.
        sb.Append("101");

        // First digit is encoded implicitly by the L/G parity of the next six.
        string parity = EanParity[twelve[0]];
        for (int i = 0; i < 6; i++)
        {
            int digit = twelve[1 + i];
            sb.Append(parity[i] == 'L' ? EanLCode[digit] : EanGCode[digit]);
        }

        // Centre guard.
        sb.Append("01010");

        // Right half: digits 8-12 plus the check digit, all R-code.
        for (int i = 0; i < 5; i++)
            sb.Append(EanRCode[twelve[7 + i]]);
        sb.Append(EanRCode[check]);

        // Right guard.
        sb.Append("101");

        return BitsToModules(sb.ToString());
    }

    private static int Ean13CheckDigit(int[] twelve)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += twelve[i] * ((i % 2 == 0) ? 1 : 3);
        return (10 - (sum % 10)) % 10;
    }

    // ── ITF-14 (ISO/IEC 16390 + GS1) ──────────────────────────────────

    // Interleaved 2 of 5: each digit is five elements, exactly two wide.
    // true = wide, false = narrow.
    private static readonly bool[][] ItfPatterns =
    {
        new[] { false, false, true,  true,  false }, // 0
        new[] { true,  false, false, false, true  }, // 1
        new[] { false, true,  false, false, true  }, // 2
        new[] { true,  true,  false, false, false }, // 3
        new[] { false, false, true,  false, true  }, // 4
        new[] { true,  false, true,  false, false }, // 5
        new[] { false, true,  true,  false, false }, // 6
        new[] { false, false, false, true,  true  }, // 7
        new[] { true,  false, false, true,  false }, // 8
        new[] { false, true,  false, true,  false }, // 9
    };

    private const int ItfNarrow = 1;
    private const int ItfWide = 3;

    private static EncodedBarcode EncodeItf14(string value)
    {
        RequireDigits(BarcodeSymbology.Itf14, value, 13, "ITF-14 requires exactly 13 digits (excluding the check digit).");

        int[] data = ToDigits(value);
        int check = ItfCheckDigit(data);
        int[] all = new int[14];
        Array.Copy(data, all, 13);
        all[13] = check;

        var modules = new List<bool>(135);

        // Start pattern: narrow bar, narrow space, narrow bar, narrow space.
        AppendRun(modules, true, ItfNarrow);
        AppendRun(modules, false, ItfNarrow);
        AppendRun(modules, true, ItfNarrow);
        AppendRun(modules, false, ItfNarrow);

        // Interleave digit pairs: first digit drives the five bars, the
        // second drives the five spaces, alternating bar/space.
        for (int i = 0; i < all.Length; i += 2)
        {
            bool[] bars = ItfPatterns[all[i]];
            bool[] spaces = ItfPatterns[all[i + 1]];
            for (int k = 0; k < 5; k++)
            {
                AppendRun(modules, true, bars[k] ? ItfWide : ItfNarrow);
                AppendRun(modules, false, spaces[k] ? ItfWide : ItfNarrow);
            }
        }

        // Stop pattern: wide bar, narrow space, narrow bar.
        AppendRun(modules, true, ItfWide);
        AppendRun(modules, false, ItfNarrow);
        AppendRun(modules, true, ItfNarrow);

        var sb = new StringBuilder(14);
        foreach (int d in all) sb.Append((char)('0' + d));
        return new EncodedBarcode(modules.ToArray(), sb.ToString());
    }

    private static int ItfCheckDigit(int[] data)
    {
        // Weights alternate 3,1,... starting from the rightmost data digit.
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            int fromRight = data.Length - 1 - i;
            sum += data[i] * ((fromRight % 2 == 0) ? 3 : 1);
        }
        return (10 - (sum % 10)) % 10;
    }

    // ── Shared helpers ────────────────────────────────────────────────

    private static void AppendRun(List<bool> modules, bool dark, int count)
    {
        for (int i = 0; i < count; i++)
            modules.Add(dark);
    }

    private static bool[] BitsToModules(string bits)
    {
        var modules = new bool[bits.Length];
        for (int i = 0; i < bits.Length; i++)
            modules[i] = bits[i] == '1';
        return modules;
    }

    private static int[] ToDigits(string value)
    {
        var digits = new int[value.Length];
        for (int i = 0; i < value.Length; i++)
            digits[i] = value[i] - '0';
        return digits;
    }

    private static void RequireDigits(BarcodeSymbology symbology, string value, int expectedLength, string message)
    {
        if (value.Length != expectedLength || !IsAllDigits(value))
        {
            throw new BarcodeContentException(symbology,
                $"{message} Got: '{value}' (length {value.Length}).");
        }
    }

    private static bool IsAllDigits(string value)
    {
        foreach (char ch in value)
        {
            if (ch < '0' || ch > '9') return false;
        }
        return value.Length > 0;
    }
}
