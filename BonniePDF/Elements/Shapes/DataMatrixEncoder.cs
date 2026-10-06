using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BonniePDF;

/// <summary>
/// Thrown when a message cannot fit in the largest Data Matrix symbol.
/// </summary>
public sealed class DataMatrixContentException : BonniePdfException
{
    public DataMatrixContentException(string message) : base(message) { }
}

/// <summary>
/// The result of encoding a message: a dense grid of dark/light modules plus the
/// symbol dimensions (in modules, including the finder pattern).
/// </summary>
internal sealed class EncodedDataMatrix
{
    public EncodedDataMatrix(int width, int height, bool[,] modules)
    {
        Width = width;
        Height = height;
        Modules = modules;
    }

    /// <summary>Symbol width in modules, including the finder pattern.</summary>
    public int Width { get; }

    /// <summary>Symbol height in modules, including the finder pattern.</summary>
    public int Height { get; }

    /// <summary>Row-major grid of modules indexed <c>[row, col]</c>; <c>true</c> is dark.</summary>
    public bool[,] Modules { get; }
}

/// <summary>
/// Data Matrix (ECC200) encoder.
///
/// Produces a square or rectangular module matrix for an arbitrary message,
/// choosing the most compact of the ASCII, C40, Text, X12, EDIFACT and Base-256
/// encodation schemes and protecting the data with interleaved Reed-Solomon
/// error correction, exactly per ISO/IEC 16022.
///
/// Ported from datalog/datamatrix-svg (MIT License, Copyright (c) 2020
/// Constantine - https://github.com/datalog/datamatrix-svg). The SVG/DOM
/// rendering layer of the original is intentionally dropped: BonniePDF draws the
/// matrix directly as vector rectangles in <see cref="DataMatrix"/>, mirroring
/// the way <see cref="QRCode"/> consumes <see cref="QrCodeEncoder"/>.
/// </summary>
internal static class DataMatrixEncoder
{
    // Code-set tables: [latch, then triples of (upperBound, shift, offset)].
    private static readonly int[] C40Set = { 230, 31, 0, 0, 32, 9, 29, 47, 1, 33, 57, 9, 44, 64, 1, 43, 90, 9, 51, 95, 1, 69, 127, 2, 96, 255, 1, 0 };
    private static readonly int[] TextSet = { 239, 31, 0, 0, 32, 9, 29, 47, 1, 33, 57, 9, 44, 64, 1, 43, 90, 2, 64, 95, 1, 69, 122, 9, 83, 127, 2, 96, 255, 1, 0 };
    private static readonly int[] X12Set = { 238, 12, 8, 0, 13, 9, 13, 31, 8, 0, 32, 9, 29, 41, 8, 0, 42, 9, 41, 47, 8, 0, 57, 9, 44, 64, 8, 0, 90, 9, 51, 255, 8, 0 };
    private static readonly int[] NominalByteLayout = { 0, 0, -1, 0, -2, 0, 0, -1, -1, -1, -2, -1, -1, -2, -2, -2 };

    /// <summary>
    /// Encode <paramref name="message"/> into a Data Matrix module grid.
    /// </summary>
    /// <param name="message">
    /// The text to encode. Any string is accepted; characters outside Latin-1 are
    /// encoded as their UTF-8 bytes.
    /// </param>
    /// <param name="rectangular">
    /// Request a rectangular symbol for short messages where one is available;
    /// otherwise a square symbol is produced.
    /// </param>
    /// <exception cref="DataMatrixContentException">
    /// Thrown if the message is too long for the largest (144x144) symbol.
    /// </exception>
    public static EncodedDataMatrix Encode(string message, bool rectangular = false)
    {
        string bytes = ToLatin1Bytes(message);
        List<int> data = ChooseBestEncoding(bytes);
        SymbolSize size = CalculateSymbolSize(data.Count, rectangular);

        // Lay the data into a fixed-size buffer; padding and RS fill the remainder.
        int[] encoded = new int[size.SymbolBytes];
        for (int i = 0; i < data.Count; i++) encoded[i] = data[i];

        ApplyReedSolomonErrorCorrection(encoded, data.Count, size.SymbolBytes, size.Checkwords, size.Blocks);
        return BuildMatrixWithLayout(encoded, size);
    }

    /// <summary>
    /// Convert a string to a Latin-1 byte string, the input form every encodation
    /// scheme operates on. Multi-byte characters become their UTF-8 bytes,
    /// matching the original's <c>unescape(encodeURI(text))</c>.
    /// </summary>
    private static string ToLatin1Bytes(string text)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        var sb = new StringBuilder(utf8.Length);
        foreach (byte b in utf8) sb.Append((char)b);
        return sb.ToString();
    }

    /// <summary>The "255 state" randomising algorithm used by Base-256 encoding.</summary>
    private static int Randomise255State(int value, int position)
    {
        return (value + ((149 * position) % 255) + 1) & 255;
    }

    private static List<int> EncodeAscii(string text)
    {
        var result = new List<int>();
        for (int i = 0; i < text.Length; i++)
        {
            int current = text[i];
            int next = i + 1 < text.Length ? text[i + 1] : 0;
            if (current >= 48 && current <= 57 && next >= 48 && next <= 57)
            {
                result.Add((current - 48) * 10 + next + 82); // two digits in one codeword
                i++;
            }
            else if (current > 127)
            {
                result.Add(235); // upper-shift (extended char)
                result.Add((current - 127) & 255);
            }
            else
            {
                result.Add(current + 1);
            }
        }
        return result;
    }

    private static List<int> EncodeBase256(string text)
    {
        var result = new List<int> { 231 }; // latch to Base 256
        int length = text.Length;
        if (length > 250) result.Add((37 + length / 250) & 255); // length high byte
        result.Add(Randomise255State(length % 250, result.Count + 1)); // length low byte
        for (int i = 0; i < length; i++)
        {
            result.Add(Randomise255State(text[i], result.Count + 1));
        }
        return result;
    }

    private static List<int> EncodeEdifact(string text)
    {
        int textLength = text.Length;
        int packedLength = (textLength + 1) & -4;
        var result = packedLength > 0 ? new List<int> { 240 } : new List<int>(); // latch to EDIFACT
        int codeword = 0;

        for (int i = 0; i < packedLength; i++)
        {
            int charCode;
            if (i < packedLength - 1)
            {
                charCode = text[i];
                if (charCode < 32 || charCode > 94) return new List<int>(); // not representable
            }
            else
            {
                charCode = 31; // unlatch back to ASCII
            }
            codeword = codeword * 64 + (charCode & 63);
            if ((i & 3) == 3)
            {
                result.Add(codeword >> 16); // 4 chars -> 3 codewords
                result.Add((codeword >> 8) & 255);
                result.Add(codeword & 255);
                codeword = 0;
            }
        }
        if (packedLength > textLength) return result;
        result.AddRange(EncodeAscii(text.Substring(packedLength == 0 ? 0 : packedLength - 1)));
        return result;
    }

    private static List<int> EncodeTextMode(string text, int[] set)
    {
        int charCount = 0;
        int codeword = 0;
        int i = 0;
        var result = new List<int> { set[0] };

        void Push(int value)
        {
            codeword = 40 * codeword + value; // pack 3 chars into 2 codewords
            if (charCount++ == 2)
            {
                codeword++;
                result.Add(codeword >> 8);
                result.Add(codeword & 255);
                charCount = 0;
                codeword = 0;
            }
        }

        for (i = 0; i < text.Length; i++)
        {
            if (charCount == 0 && i == text.Length - 1) break; // a trailing char is shorter in ASCII
            int charCode = text[i];
            if (charCode > 127 && result[0] != 238)
            {
                Push(1); // upper shift
                Push(30);
                charCode -= 128;
            }
            int j = 1;
            while (charCode > set[j]) j += 3; // select sub-set
            int shift = set[j + 1];
            int offset = set[j + 2];

            if (shift == 8 || (shift == 9 && charCount == 0 && i == text.Length - 1)) return new List<int>(); // not in set / padding fails
            if (shift < 5 && charCount == 2 && i == text.Length - 1) break; // last char in ASCII
            if (shift < 5) Push(shift);
            Push(charCode - offset);
        }

        if (charCount == 2 && result[0] != 238) Push(0); // pad
        result.Add(254); // unlatch to ASCII

        if (charCount > 0 || i < text.Length)
        {
            result.AddRange(EncodeAscii(text.Substring(i - charCount)));
        }
        return result;
    }

    /// <summary>Pick whichever encodation scheme yields the fewest data codewords.</summary>
    private static List<int> ChooseBestEncoding(string text)
    {
        List<int> encoded = EncodeAscii(text);
        var candidates = new[]
        {
            EncodeTextMode(text, C40Set),
            EncodeTextMode(text, TextSet),
            EncodeTextMode(text, X12Set),
            EncodeEdifact(text),
            EncodeBase256(text),
        };

        foreach (List<int> candidate in candidates)
        {
            if (candidate.Count > 0 && candidate.Count < encoded.Count)
            {
                encoded = candidate;
            }
        }
        return encoded;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct SymbolSize
    {
        public SymbolSize(int width, int height, int columns, int rows, int blocks, int symbolBytes, int checkwords)
        {
            Width = width;
            Height = height;
            Columns = columns;
            Rows = rows;
            Blocks = blocks;
            SymbolBytes = symbolBytes;
            Checkwords = checkwords;
        }

        public int Width { get; }
        public int Height { get; }
        public int Columns { get; }
        public int Rows { get; }
        public int Blocks { get; }
        public int SymbolBytes { get; }
        public int Checkwords { get; }
    }

    /// <summary>Choose the smallest symbol that can hold the encoded data.</summary>
    private static SymbolSize CalculateSymbolSize(int dataLength, bool rectangular)
    {
        int width, height, symbolBytes, checkwords;
        int columns = 1, rows = 1, blocks = 1;
        int tableIndex = -1;

        if (rectangular && dataLength < 50)
        {
            // [symbol width, checkwords] pairs for the six rectangular symbols.
            int[] symbolTable = { 16, 7, 28, 11, 24, 14, 32, 18, 32, 24, 44, 28 };
            do
            {
                width = symbolTable[++tableIndex];
                height = 6 + (tableIndex & 12);
                symbolBytes = (width * height) / 8;
                checkwords = symbolTable[++tableIndex];
            } while (symbolBytes - checkwords < dataLength);
            if (width > 25) columns = 2;
        }
        else
        {
            width = height = 6;
            int increment = 2;
            // Reed-Solomon checkwords for each successive square symbol.
            int[] symbolTable = { 5, 7, 10, 12, 14, 18, 20, 24, 28, 36, 42, 48, 56, 68, 84, 112, 144, 192, 224, 272, 336, 408, 496, 620 };
            do
            {
                if (++tableIndex == symbolTable.Length)
                    throw new DataMatrixContentException("Data Matrix message is too long to encode.");
                if (width > 11 * increment) increment = (4 + increment) & 12;
                width = height += increment;
                symbolBytes = (width * height) >> 3;
                checkwords = symbolTable[tableIndex];
            } while (symbolBytes - checkwords < dataLength);

            if (width > 27) rows = columns = 2 * (width / 54) + 2;
            if (symbolBytes > 255) blocks = 2 * (symbolBytes >> 9) + 2;
        }

        return new SymbolSize(width, height, columns, rows, blocks, symbolBytes, checkwords);
    }

    /// <summary>Pad the data region and append interleaved Reed-Solomon correction codewords.</summary>
    private static void ApplyReedSolomonErrorCorrection(int[] encoded, int dataLength, int symbolBytes, int checkwords, int blocks)
    {
        if (dataLength < symbolBytes - checkwords) encoded[dataLength++] = 129; // first pad
        while (dataLength < symbolBytes - checkwords)
        {
            encoded[dataLength++] = (((149 * dataLength) % 253) + 130) % 254; // randomised "253 state" pad
        }

        int blockCheckwords = checkwords / blocks;
        int[] reedSolomon = new int[70];
        int[] correction = new int[70];
        int[] log = new int[256];
        int[] exp = new int[255];

        // Galois field GF(256) log/exp tables (primitive polynomial x^8 + x^5 + x^3 + x^2 + 1 = 301).
        for (int value = 1, i = 0; i < 255; i++)
        {
            exp[i] = value;
            log[value] = i;
            value += value;
            if (value > 255) value ^= 301;
        }

        // Reed-Solomon generator polynomial.
        reedSolomon[blockCheckwords] = 0;
        for (int i = 1; i <= blockCheckwords; i++)
        {
            reedSolomon[blockCheckwords - i] = 1;
            for (int j = blockCheckwords - i; j < blockCheckwords; j++)
            {
                reedSolomon[j] = reedSolomon[j + 1] ^ exp[(log[reedSolomon[j]] + i) % 255];
            }
        }

        // Correction codewords for each interleaved block.
        for (int c = 0; c < blocks; c++)
        {
            Array.Clear(correction, 0, blockCheckwords + 1);
            for (int i = c; i < dataLength; i += blocks)
            {
                int x = correction[0] ^ encoded[i];
                for (int j = 0; j < blockCheckwords; j++)
                {
                    correction[j] = correction[j + 1] ^ (x != 0 ? exp[(log[reedSolomon[j]] + log[x]) % 255] : 0);
                }
            }
            for (int i = 0; i < blockCheckwords; i++)
            {
                encoded[dataLength + c + i * blocks] = correction[i];
            }
        }
    }

    /// <summary>Place codewords into the symbol following the ECC200 layout and finder pattern.</summary>
    private static EncodedDataMatrix BuildMatrixWithLayout(int[] encoded, SymbolSize size)
    {
        int width = size.Width;
        int height = size.Height;
        int columns = size.Columns;
        int rows = size.Rows;
        int symbolBytes = size.SymbolBytes;

        int finalWidth = width + 2 * columns;
        int finalHeight = height + 2 * rows;
        bool[,] matrix = new bool[finalHeight, finalWidth];

        void Set(int x, int y) => matrix[y, x] = true;

        int regionWidth = width / columns;
        int regionHeight = height / rows;

        // Finder pattern: horizontal borders (solid bottom, dashed top of each region).
        for (int y = 0; y < height + 2 * rows; y += regionHeight + 2)
        {
            for (int x = 0; x < width + 2 * columns; x++)
            {
                Set(x, y + regionHeight + 1);
                if ((x & 1) == 0) Set(x, y);
            }
        }

        // Finder pattern: vertical borders (solid left, dashed right of each region).
        for (int x = 0; x < width + 2 * columns; x += regionWidth + 2)
        {
            for (int y = 0; y < height; y++)
            {
                int regionOffset = (y / regionHeight) * 2;
                Set(x, y + regionOffset + 1);
                if ((y & 1) == 1) Set(x + regionWidth + 1, y + regionOffset);
            }
        }

        // Place each codeword's 8 bits along a diagonal utah-shaped path.
        int step = 2;
        int c = 0;
        int r = 4;
        for (int i = 0; i < symbolBytes; r -= step, c += step)
        {
            int[] layout;

            if (r == height - 3 && c == -1)
            {
                layout = new[] { width, 6 - height, width, 5 - height, width, 4 - height, width, 3 - height, width - 1, 3 - height, 3, 2, 2, 2, 1, 2 }; // corner A
            }
            else if (r == height + 1 && c == 1 && (width & 7) == 0 && (height & 7) == 6)
            {
                layout = new[] { width - 2, -height, width - 3, -height, width - 4, -height, width - 2, -1 - height, width - 3, -1 - height, width - 4, -1 - height, width - 2, -2, -1, -2 }; // corner D
            }
            else
            {
                if (r == 0 && c == width - 2 && (width & 3) != 0) continue; // corner B: skip
                if (r < 0 || c >= width || r >= height || c < 0)
                {
                    step = -step; // turn around
                    r += 2 + step / 2;
                    c += 2 - step / 2;
                    while (r < 0 || c >= width || r >= height || c < 0)
                    {
                        r -= step;
                        c += step;
                    }
                }
                if (r == height - 2 && c == 0 && (width & 3) != 0)
                {
                    layout = new[] { width - 1, 3 - height, width - 1, 2 - height, width - 2, 2 - height, width - 3, 2 - height, width - 4, 2 - height, 0, 1, 0, 0, 0, -1 }; // corner B
                }
                else if (r == height - 2 && c == 0 && (width & 7) == 4)
                {
                    layout = new[] { width - 1, 5 - height, width - 1, 4 - height, width - 1, 3 - height, width - 1, 2 - height, width - 2, 2 - height, 0, 1, 0, 0, 0, -1 }; // corner C
                }
                else if (r == 1 && c == width - 1 && (width & 7) == 0 && (height & 7) == 6)
                {
                    continue; // omit corner D
                }
                else
                {
                    layout = NominalByteLayout;
                }
            }

            for (int word = encoded[i++], j = 0; word > 0; j += 2, word >>= 1)
            {
                if ((word & 1) == 0) continue;
                int x = c + layout[j];
                int y = r + layout[j + 1];

                if (x < 0)
                {
                    x += width;
                    y += 4 - ((width + 4) & 7);
                }
                if (y < 0)
                {
                    y += height;
                    x += 4 - ((height + 4) & 7);
                }

                // Map the data coordinate into the symbol, skipping finder-pattern columns/rows.
                Set(x + 2 * (x / regionWidth) + 1, y + 2 * (y / regionHeight) + 1);
            }
        }

        // Fill the unused bottom-right corner of certain symbols with a fixed pattern.
        for (int i = width; (i & 3) != 0; i--)
        {
            Set(i, i);
        }

        return new EncodedDataMatrix(finalWidth, finalHeight, matrix);
    }
}
