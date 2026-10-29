namespace BonniePDF;

/// <summary>
/// The 1D barcode symbologies supported natively by BonniePDF. These four
/// cover retail (EAN-13, UPC-A) and logistics (Code 128, ITF-14) across the
/// UK, EU, and US. Additional symbologies (Code 39/93, DataMatrix, PDF417,
/// Aztec, …) are provided by the optional extension package.
/// </summary>
public enum BarcodeSymbology
{
    /// <summary>Code 128 (ISO/IEC 15417) - the universal logistics barcode. Full ASCII.</summary>
    Code128,

    /// <summary>EAN-13 (ISO/IEC 15420) - the EU/UK retail product barcode. 12 digits + check.</summary>
    Ean13,

    /// <summary>UPC-A (ISO/IEC 15420) - the US/Canada retail product barcode. 11 digits + check.</summary>
    UpcA,

    /// <summary>ITF-14 (ISO/IEC 16390 + GS1) - the outer-case/shipping barcode. 13 digits + check.</summary>
    Itf14,
}

/// <summary>
/// Raised when a value cannot be encoded in the requested
/// <see cref="BarcodeSymbology"/> (wrong length, invalid characters, etc.).
/// </summary>
public sealed class BarcodeContentException : BonniePdfException
{
    /// <summary>The symbology that rejected the value.</summary>
    public BarcodeSymbology Symbology { get; }

    /// <summary>Creates a new <see cref="BarcodeContentException"/>.</summary>
    public BarcodeContentException(BarcodeSymbology symbology, string message) : base(message)
    {
        Symbology = symbology;
    }
}
