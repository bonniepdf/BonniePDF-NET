using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BonniePDF.Elements.Shapes;

/// <summary>
/// A segment of character/binary/control data in a QR Code symbol.
/// Each segment has an encoding mode and a sequence of data bits.
///
/// Copyright (c) Project Nayuki. (MIT License)
/// https://www.nayuki.io/page/qr-code-generator-library
/// Ported to C# for BonniePDF.
/// </summary>
internal sealed partial class QrSegment
{
    /// <summary>The encoding mode of this segment.</summary>
    internal Mode SegmentMode { get; }

    /// <summary>The number of characters in the original data (not bits).</summary>
    internal int NumChars { get; }

    private readonly List<bool> _data;

    internal QrSegment(Mode mode, int numChars, List<bool> data)
    {
        SegmentMode = mode;
        NumChars = numChars;
        _data = new List<bool>(data);
    }

    internal IReadOnlyList<bool> GetData() => _data;

    // ---- Static factory methods ----

    /// <summary>
    /// Creates an optimal list of segments to represent the given Unicode text string.
    /// Uses byte mode for simplicity (handles all characters).
    /// </summary>
    internal static List<QrSegment> MakeSegments(string text)
    {
        var result = new List<QrSegment>();
        if (string.IsNullOrEmpty(text))
            return result;

        if (NumericRegex().IsMatch(text))
            result.Add(MakeNumeric(text));
        else if (AlphanumericRegex().IsMatch(text))
            result.Add(MakeAlphanumeric(text));
        else
            result.Add(MakeBytes(Encoding.UTF8.GetBytes(text)));

        return result;
    }

    /// <summary>
    /// Creates a segment encoding the given binary data in byte mode.
    /// </summary>
    internal static QrSegment MakeBytes(byte[] data)
    {
        var bits = new List<bool>();
        foreach (byte b in data)
            AppendBits(b, 8, bits);
        return new QrSegment(Mode.Byte, data.Length, bits);
    }

    /// <summary>
    /// Creates a segment encoding the given string of decimal digits in numeric mode.
    /// </summary>
    internal static QrSegment MakeNumeric(string digits)
    {
        var bits = new List<bool>();
        int i = 0;
        while (i < digits.Length)
        {
            int n = Math.Min(digits.Length - i, 3);
            int value = int.Parse(digits.Substring(i, n));
            AppendBits(value, n * 3 + 1, bits);
            i += n;
        }
        return new QrSegment(Mode.Numeric, digits.Length, bits);
    }

    /// <summary>
    /// Creates a segment encoding the given string in alphanumeric mode.
    /// Valid characters: 0-9, A-Z, space, $, %, *, +, -, ., /, :
    /// </summary>
    internal static QrSegment MakeAlphanumeric(string text)
    {
        var bits = new List<bool>();
        int i = 0;
        while (i + 1 < text.Length)
        {
            int val = AlphanumericCharset.IndexOf(text[i], StringComparison.OrdinalIgnoreCase) * 45 + AlphanumericCharset.IndexOf(text[i + 1]);
            AppendBits(val, 11, bits);
            i += 2;
        }
        if (i < text.Length)
        {
            AppendBits(AlphanumericCharset.IndexOf(text[i]), 6, bits);
        }
        return new QrSegment(Mode.Alphanumeric, text.Length, bits);
    }

    /// <summary>
    /// Calculates the total number of bits needed to encode all segments for a given version.
    /// Returns -1 if a segment's character count doesn't fit its field.
    /// </summary>
    internal static int GetTotalBits(List<QrSegment> segs, int version)
    {
        long result = 0;
        foreach (var seg in segs)
        {
            int ccbits = seg.SegmentMode.NumCharCountBits(version);
            if (seg.NumChars >= (1L << ccbits))
                return -1; // Segment's character count doesn't fit
            result += 4L + ccbits + seg._data.Count;
        }
        return result <= int.MaxValue ? (int)result : -1;
    }

    // ---- Helpers ----

    internal static void AppendBits(int val, int len, List<bool> buffer)
    {
        if (len < 0 || len > 31 || val >> len != 0)
            throw new ArgumentOutOfRangeException(nameof(val), "Value out of range");
        for (int i = len - 1; i >= 0; i--)
            buffer.Add(((val >> i) & 1) != 0);
    }

    // ---- Constants ----

    [GeneratedRegex(@"^[0-9]*$", RegexOptions.Compiled)]
    private static partial Regex NumericRegex();

    [GeneratedRegex(@"^[0-9A-Z $%*+./:-]*$", RegexOptions.Compiled)]
    private static partial Regex AlphanumericRegex();
    private const string AlphanumericCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";

    // ---- Mode enum ----

    /// <summary>
    /// Describes how a segment's data bits are interpreted.
    /// </summary>
    internal sealed class Mode
    {
        internal static readonly Mode Numeric = new(0x1, 10, 12, 14);
        internal static readonly Mode Alphanumeric = new(0x2, 9, 11, 13);
        internal static readonly Mode Byte = new(0x4, 8, 16, 16);

        internal int ModeBits { get; }
        private readonly int _numBitsCharCountV1;
        private readonly int _numBitsCharCountV10;
        private readonly int _numBitsCharCountV27;

        private Mode(int modeBits, int cc1, int cc10, int cc27)
        {
            ModeBits = modeBits;
            _numBitsCharCountV1 = cc1;
            _numBitsCharCountV10 = cc10;
            _numBitsCharCountV27 = cc27;
        }

        /// <summary>
        /// Returns the number of bits used for the character count field for a segment in this mode
        /// in a QR Code at the given version number.
        /// </summary>
        internal int NumCharCountBits(int version)
        {
            if (version <= 9) return _numBitsCharCountV1;
            if (version <= 26) return _numBitsCharCountV10;
            return _numBitsCharCountV27;
        }
    }
}
