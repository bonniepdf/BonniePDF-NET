namespace BonniePDF;

/// <summary>
/// The error correction level in a QR Code symbol.
/// Higher levels provide more redundancy at the cost of reduced data capacity.
///
/// Copyright (c) Project Nayuki. (MIT License)
/// https://www.nayuki.io/page/qr-code-generator-library
/// Ported to C# for BonniePDF.
/// </summary>
public sealed class QrEcc
{
    /// <summary>
    /// Low error correction: ~7% of codewords can be restored.
    /// </summary>
    public static readonly QrEcc Low = new(0, 1);

    /// <summary>
    /// Medium error correction: ~15% of codewords can be restored.
    /// </summary>
    public static readonly QrEcc Medium = new(1, 0);

    /// <summary>
    /// Quartile error correction: ~25% of codewords can be restored.
    /// </summary>
    public static readonly QrEcc Quartile = new(2, 3);

    /// <summary>
    /// High error correction: ~30% of codewords can be restored.
    /// </summary>
    public static readonly QrEcc High = new(3, 2);

    /// <summary>
    /// Ordinal index used for table lookups (0=Low, 1=Medium, 2=Quartile, 3=High).
    /// </summary>
    internal int Ordinal { get; }

    /// <summary>
    /// The 2-bit format info value for this ECC level, used in the format information area.
    /// </summary>
    internal int FormatBits { get; }

    private QrEcc(int ordinal, int formatBits)
    {
        Ordinal = ordinal;
        FormatBits = formatBits;
    }
}
