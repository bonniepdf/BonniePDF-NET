using System;
using System.Collections.Generic;

namespace BonniePDF.Elements.Shapes;

/// <summary>
/// A QR Code symbol compliant with ISO/IEC 18004 (QR Code Model 2).
/// Supports versions 1-40, all 4 error correction levels, and automatic mask selection.
/// Based on the algorithms from Project Nayuki (MIT License).
///
/// Copyright (c) Project Nayuki. (MIT License)
/// https://www.nayuki.io/page/qr-code-generator-library
/// Ported to C# for BonniePDF.
/// </summary>
internal sealed class QrCodeEncoder
{
    // ---- Constants ----
    private const int MinVersion = 1;
    private const int MaxVersion = 40;
    private const int PenaltyN1 = 3;
    private const int PenaltyN2 = 3;
    private const int PenaltyN3 = 40;
    private const int PenaltyN4 = 10;

    // ---- Public properties ----

    /// <summary>The version number (1-40).</summary>
    internal int Version { get; }

    /// <summary>The size of the QR code (version * 4 + 17).</summary>
    internal int Size { get; }

    /// <summary>The error correction level.</summary>
    internal QrEcc ErrorCorrectionLevel { get; }

    /// <summary>The mask pattern index (0-7).</summary>
    internal int Mask { get; private set; }

    // ---- Internal fields ----
    private readonly bool[,] _modules;
    private readonly bool[,] _isFunction;

    // ---- High-level static factory ----

    /// <summary>
    /// Encodes the given text string into a QR Code at the given error correction level.
    /// Automatically selects the smallest version that fits the data.
    /// </summary>
    internal static QrCodeEncoder EncodeText(string text, QrEcc ecl)
    {
        var segs = QrSegment.MakeSegments(text);
        return EncodeSegments(segs, ecl);
    }

    /// <summary>
    /// Encodes the given binary data into a QR Code at the given error correction level.
    /// </summary>
    internal static QrCodeEncoder EncodeBinary(byte[] data, QrEcc ecl)
    {
        var seg = QrSegment.MakeBytes(data);
        return EncodeSegments(new List<QrSegment> { seg }, ecl);
    }

    /// <summary>
    /// Encodes the given segments into a QR Code, automatically choosing the smallest version
    /// that fits, and optionally boosting the error correction level.
    /// </summary>
    internal static QrCodeEncoder EncodeSegments(List<QrSegment> segs, QrEcc ecl,
        int minVersion = MinVersion, int maxVersion = MaxVersion,
        int mask = -1, bool boostEcl = true)
    {
        if (minVersion < MinVersion || minVersion > maxVersion || maxVersion > MaxVersion || mask < -1 || mask > 7)
            throw new ArgumentOutOfRangeException();

        // Find the minimal version number to use
        int version, dataUsedBits;
        for (version = minVersion; ; version++)
        {
            int dataCapacityBits = GetNumDataCodewords(version, ecl) * 8;
            int usedBits = QrSegment.GetTotalBits(segs, version);
            if (usedBits != -1 && usedBits <= dataCapacityBits)
            {
                dataUsedBits = usedBits;
                break;
            }
            if (version >= maxVersion)
                throw new InvalidOperationException("Data too long to fit in any QR Code version");
        }

        // Boost ECC level while data still fits
        foreach (var newEcl in new[] { QrEcc.Medium, QrEcc.Quartile, QrEcc.High })
        {
            if (boostEcl && dataUsedBits <= GetNumDataCodewords(version, newEcl) * 8)
                ecl = newEcl;
        }

        // Concatenate all segments to create the data bit string
        var bb = new List<bool>();
        foreach (var seg in segs)
        {
            QrSegment.AppendBits(seg.SegmentMode.ModeBits, 4, bb);
            QrSegment.AppendBits(seg.NumChars, seg.SegmentMode.NumCharCountBits(version), bb);
            foreach (bool b in seg.GetData())
                bb.Add(b);
        }

        // Add terminator and pad up to a byte
        int dataCapacity = GetNumDataCodewords(version, ecl) * 8;
        QrSegment.AppendBits(0, Math.Min(4, dataCapacity - bb.Count), bb);
        QrSegment.AppendBits(0, (8 - bb.Count % 8) % 8, bb);

        // Pad with alternating bytes until data capacity is reached
        for (int padByte = 0xEC; bb.Count < dataCapacity; padByte ^= 0xEC ^ 0x11)
            QrSegment.AppendBits(padByte, 8, bb);

        // Pack bits into bytes
        var dataCodewords = new byte[(bb.Count + 7) / 8];
        for (int i = 0; i < bb.Count; i++)
        {
            if (bb[i])
                dataCodewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));
        }

        return new QrCodeEncoder(version, ecl, dataCodewords, mask);
    }

    // ---- Constructor (low level) ----

    private QrCodeEncoder(int version, QrEcc ecl, byte[] dataCodewords, int msk)
    {
        Version = version;
        Size = version * 4 + 17;
        ErrorCorrectionLevel = ecl;
        _modules = new bool[Size, Size];
        _isFunction = new bool[Size, Size];

        // Draw function patterns
        DrawFunctionPatterns();

        // Compute ECC and interleave, then draw codewords
        byte[] allCodewords = AddEccAndInterleave(dataCodewords);
        DrawCodewords(allCodewords);

        // Choose and apply the best mask
        if (msk == -1)
        {
            int minPenalty = int.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                ApplyMask(i);
                DrawFormatBits(i);
                int penalty = GetPenaltyScore();
                if (penalty < minPenalty)
                {
                    msk = i;
                    minPenalty = penalty;
                }
                ApplyMask(i); // Undo mask (XOR is self-inverse)
            }
        }

        Mask = msk;
        ApplyMask(msk);
        DrawFormatBits(msk);
        // _isFunction is no longer needed after construction
    }

    // ---- Public accessors ----

    /// <summary>
    /// Returns whether the module at (x, y) is dark.
    /// Returns false for out-of-bounds coordinates.
    /// </summary>
    internal bool GetModule(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size && _modules[y, x];
    }

    /// <summary>
    /// Converts this QR Code to a bool[,] matrix (row, col).
    /// true = dark module, false = light module.
    /// </summary>
    internal bool[,] ToMatrix()
    {
        var result = new bool[Size, Size];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                result[y, x] = _modules[y, x];
        return result;
    }

    // ---- Drawing function modules ----

    private void DrawFunctionPatterns()
    {
        // Timing patterns
        for (int i = 0; i < Size; i++)
        {
            SetFunctionModule(6, i, i % 2 == 0);
            SetFunctionModule(i, 6, i % 2 == 0);
        }

        // Finder patterns (3 corners)
        DrawFinderPattern(3, 3);
        DrawFinderPattern(Size - 4, 3);
        DrawFinderPattern(3, Size - 4);

        // Alignment patterns
        int[] alignPatPos = GetAlignmentPatternPositions();
        int numAlign = alignPatPos.Length;
        for (int i = 0; i < numAlign; i++)
        {
            for (int j = 0; j < numAlign; j++)
            {
                // Skip positions that overlap with finder patterns
                if ((i == 0 && j == 0) || (i == 0 && j == numAlign - 1) || (i == numAlign - 1 && j == 0))
                    continue;
                DrawAlignmentPattern(alignPatPos[i], alignPatPos[j]);
            }
        }

        // Format and version info (dummy values, overwritten later)
        DrawFormatBits(0);
        DrawVersion();
    }

    private void DrawFormatBits(int mask)
    {
        int data = ErrorCorrectionLevel.FormatBits << 3 | mask;
        int rem = data;
        for (int i = 0; i < 10; i++)
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        int bits = (data << 10 | rem) ^ 0x5412;

        // Draw first copy
        for (int i = 0; i <= 5; i++)
            SetFunctionModule(8, i, GetBit(bits, i));
        SetFunctionModule(8, 7, GetBit(bits, 6));
        SetFunctionModule(8, 8, GetBit(bits, 7));
        SetFunctionModule(7, 8, GetBit(bits, 8));
        for (int i = 9; i < 15; i++)
            SetFunctionModule(14 - i, 8, GetBit(bits, i));

        // Draw second copy
        for (int i = 0; i < 8; i++)
            SetFunctionModule(Size - 1 - i, 8, GetBit(bits, i));
        for (int i = 8; i < 15; i++)
            SetFunctionModule(8, Size - 15 + i, GetBit(bits, i));
        SetFunctionModule(8, Size - 8, true); // Always dark
    }

    private void DrawVersion()
    {
        if (Version < 7) return;

        int rem = Version;
        for (int i = 0; i < 12; i++)
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        int bits = Version << 12 | rem;

        for (int i = 0; i < 18; i++)
        {
            bool color = GetBit(bits, i);
            int a = Size - 11 + i % 3;
            int b = i / 3;
            SetFunctionModule(a, b, color);
            SetFunctionModule(b, a, color);
        }
    }

    private void DrawFinderPattern(int x, int y)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < Size && yy >= 0 && yy < Size)
                    SetFunctionModule(xx, yy, dist != 2 && dist != 4);
            }
        }
    }

    private void DrawAlignmentPattern(int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                SetFunctionModule(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private void SetFunctionModule(int x, int y, bool isDark)
    {
        _modules[y, x] = isDark;
        _isFunction[y, x] = true;
    }

    // ---- Codewords and masking ----

    private byte[] AddEccAndInterleave(byte[] data)
    {
        int ver = Version;
        QrEcc ecl = ErrorCorrectionLevel;
        if (data.Length != GetNumDataCodewords(ver, ecl))
            throw new ArgumentException("Invalid data length");

        int numBlocks = NumErrorCorrectionBlocks[ecl.Ordinal][ver];
        int blockEccLen = EccCodewordsPerBlock[ecl.Ordinal][ver];
        int rawCodewords = GetNumRawDataModules(ver) / 8;
        int numShortBlocks = numBlocks - rawCodewords % numBlocks;
        int shortBlockLen = rawCodewords / numBlocks;

        // Split data into blocks and append ECC
        var blocks = new List<byte[]>();
        byte[] rsDiv = ReedSolomonComputeDivisor(blockEccLen);
        for (int i = 0, k = 0; i < numBlocks; i++)
        {
            int datLen = shortBlockLen - blockEccLen + (i < numShortBlocks ? 0 : 1);
            var dat = new byte[datLen];
            Array.Copy(data, k, dat, 0, datLen);
            k += datLen;

            byte[] ecc = ReedSolomonComputeRemainder(dat, rsDiv);

            // For short blocks, insert a padding byte before ECC
            var block = new byte[datLen + (i < numShortBlocks ? 1 : 0) + blockEccLen];
            Array.Copy(dat, 0, block, 0, datLen);
            if (i < numShortBlocks)
            {
                // padding byte at position datLen (value 0)
                Array.Copy(ecc, 0, block, datLen + 1, blockEccLen);
            }
            else
            {
                Array.Copy(ecc, 0, block, datLen, blockEccLen);
            }
            blocks.Add(block);
        }

        // Interleave bytes from every block
        var result = new List<byte>();
        for (int i = 0; i < blocks[0].Length; i++)
        {
            for (int j = 0; j < blocks.Count; j++)
            {
                // Skip the padding byte in short blocks
                if (i != shortBlockLen - blockEccLen || j >= numShortBlocks)
                    result.Add(blocks[j][i]);
            }
        }

        if (result.Count != rawCodewords)
            throw new InvalidOperationException("Interleave mismatch");
        return result.ToArray();
    }

    private void DrawCodewords(byte[] data)
    {
        if (data.Length != GetNumRawDataModules(Version) / 8)
            throw new ArgumentException("Invalid data length");

        int i = 0;
        // Zigzag scan
        for (int right = Size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (int vert = 0; vert < Size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? Size - 1 - vert : vert;
                    if (!_isFunction[y, x] && i < data.Length * 8)
                    {
                        _modules[y, x] = GetBit(data[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
            }
        }
    }

    private void ApplyMask(int mask)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bool invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    7 => ((x + y) % 2 + x * y % 3) % 2 == 0,
                    _ => throw new ArgumentOutOfRangeException(nameof(mask))
                };
                if (!_isFunction[y, x] && invert)
                    _modules[y, x] = !_modules[y, x];
            }
        }
    }

    private int GetPenaltyScore()
    {
        int result = 0;

        // Adjacent modules in row having same color, and finder-like patterns
        for (int y = 0; y < Size; y++)
        {
            bool runColor = false;
            int runX = 0;
            int[] runHistory = new int[7];
            for (int x = 0; x < Size; x++)
            {
                if (_modules[y, x] == runColor)
                {
                    runX++;
                    if (runX == 5) result += PenaltyN1;
                    else if (runX > 5) result++;
                }
                else
                {
                    FinderPenaltyAddHistory(runX, runHistory);
                    if (!runColor)
                        result += FinderPenaltyCountPatterns(runHistory) * PenaltyN3;
                    runColor = _modules[y, x];
                    runX = 1;
                }
            }
            result += FinderPenaltyTerminateAndCount(runColor, runX, runHistory) * PenaltyN3;
        }

        // Adjacent modules in column having same color, and finder-like patterns
        for (int x = 0; x < Size; x++)
        {
            bool runColor = false;
            int runY = 0;
            int[] runHistory = new int[7];
            for (int y = 0; y < Size; y++)
            {
                if (_modules[y, x] == runColor)
                {
                    runY++;
                    if (runY == 5) result += PenaltyN1;
                    else if (runY > 5) result++;
                }
                else
                {
                    FinderPenaltyAddHistory(runY, runHistory);
                    if (!runColor)
                        result += FinderPenaltyCountPatterns(runHistory) * PenaltyN3;
                    runColor = _modules[y, x];
                    runY = 1;
                }
            }
            result += FinderPenaltyTerminateAndCount(runColor, runY, runHistory) * PenaltyN3;
        }

        // 2x2 blocks of same color
        for (int y = 0; y < Size - 1; y++)
        {
            for (int x = 0; x < Size - 1; x++)
            {
                bool color = _modules[y, x];
                if (color == _modules[y, x + 1] &&
                    color == _modules[y + 1, x] &&
                    color == _modules[y + 1, x + 1])
                    result += PenaltyN2;
            }
        }

        // Balance of dark and light modules
        int dark = 0;
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (_modules[y, x]) dark++;
        int total = Size * Size;
        int k = (int)Math.Ceiling(Math.Abs(dark * 20 - total * 10) / (double)total) - 1;
        result += k * PenaltyN4;

        return result;
    }

    // ---- Penalty helper methods ----

    private int FinderPenaltyCountPatterns(int[] runHistory)
    {
        int n = runHistory[1];
        if (n > Size * 3) return 0;
        bool core = n > 0 && runHistory[2] == n && runHistory[3] == n * 3 &&
                    runHistory[4] == n && runHistory[5] == n;
        return (core && runHistory[0] >= n * 4 && runHistory[6] >= n ? 1 : 0)
             + (core && runHistory[6] >= n * 4 && runHistory[0] >= n ? 1 : 0);
    }

    private int FinderPenaltyTerminateAndCount(bool currentRunColor, int currentRunLength, int[] runHistory)
    {
        if (currentRunColor)
        {
            FinderPenaltyAddHistory(currentRunLength, runHistory);
            currentRunLength = 0;
        }
        currentRunLength += Size;
        FinderPenaltyAddHistory(currentRunLength, runHistory);
        return FinderPenaltyCountPatterns(runHistory);
    }

    private void FinderPenaltyAddHistory(int currentRunLength, int[] runHistory)
    {
        if (runHistory[0] == 0)
            currentRunLength += Size;
        Array.Copy(runHistory, 0, runHistory, 1, runHistory.Length - 1);
        runHistory[0] = currentRunLength;
    }

    // ---- Alignment pattern positions ----

    private int[] GetAlignmentPatternPositions()
    {
        if (Version == 1) return Array.Empty<int>();

        int numAlign = Version / 7 + 2;
        int step = ((Version * 8 + numAlign * 3 + 5) / (numAlign * 4 - 4)) * 2;

        var result = new List<int> { 6 };
        for (int pos = Size - 7; result.Count < numAlign; pos -= step)
            result.Insert(1, pos);
        return result.ToArray();
    }

    // ---- Static helper methods ----

    private static int GetNumRawDataModules(int ver)
    {
        int result = (16 * ver + 128) * ver + 64;
        if (ver >= 2)
        {
            int numAlign = ver / 7 + 2;
            result -= (25 * numAlign - 10) * numAlign - 55;
            if (ver >= 7) result -= 36;
        }
        return result;
    }

    private static int GetNumDataCodewords(int ver, QrEcc ecl)
    {
        return GetNumRawDataModules(ver) / 8
             - EccCodewordsPerBlock[ecl.Ordinal][ver]
             * NumErrorCorrectionBlocks[ecl.Ordinal][ver];
    }

    private static bool GetBit(int x, int i)
    {
        return ((x >> i) & 1) != 0;
    }

    // ---- Reed-Solomon error correction ----

    private static byte[] ReedSolomonComputeDivisor(int degree)
    {
        var result = new byte[degree];
        result[degree - 1] = 1;

        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < result.Length; j++)
            {
                result[j] = (byte)ReedSolomonMultiply(result[j], root);
                if (j + 1 < result.Length)
                    result[j] ^= result[j + 1];
            }
            root = ReedSolomonMultiply(root, 0x02);
        }
        return result;
    }

    private static byte[] ReedSolomonComputeRemainder(byte[] data, byte[] divisor)
    {
        var result = new byte[divisor.Length];
        foreach (byte b in data)
        {
            byte factor = (byte)(b ^ result[0]);
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[result.Length - 1] = 0;
            for (int i = 0; i < divisor.Length; i++)
                result[i] ^= (byte)ReedSolomonMultiply(divisor[i], factor);
        }
        return result;
    }

    private static int ReedSolomonMultiply(int x, int y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return z;
    }

    // ---- ECC tables (indexed by [eccLevel][version]) ----

    private static readonly int[][] EccCodewordsPerBlock =
    {
        new[] { -1, 7,10,15,20,26,18,20,24,30,18,20,24,26,30,22,24,28,30,28,28,28,28,30,30,26,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30 },
        new[] { -1,10,16,26,18,24,16,18,22,22,26,30,22,22,24,24,28,28,26,26,26,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28 },
        new[] { -1,13,22,18,26,18,24,18,22,20,24,28,26,24,20,30,24,28,28,26,30,28,30,30,30,30,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30 },
        new[] { -1,17,28,22,16,22,28,26,26,24,28,24,28,22,24,24,30,28,28,26,28,30,24,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30 },
    };

    private static readonly int[][] NumErrorCorrectionBlocks =
    {
        new[] { -1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 4,  4,  4,  4,  4,  6,  6,  6,  6,  7,  8,  8,  9,  9, 10, 12, 12, 12, 13, 14, 15, 16, 17, 18, 19, 19, 20, 21, 22, 24, 25 },
        new[] { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5,  5,  8,  9,  9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49 },
        new[] { -1, 1, 1, 2, 2, 4, 4, 6, 6, 8, 8,  8, 10, 12, 16, 12, 17, 16, 18, 21, 20, 23, 23, 25, 27, 29, 34, 34, 35, 38, 40, 43, 45, 48, 51, 53, 56, 59, 62, 65, 68 },
        new[] { -1, 1, 1, 2, 4, 4, 4, 5, 6, 8, 8, 11, 11, 16, 16, 18, 16, 19, 21, 25, 25, 25, 34, 30, 32, 35, 37, 40, 42, 45, 48, 51, 54, 57, 60, 63, 66, 70, 74, 77, 81 },
    };
}
