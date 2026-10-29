namespace BonniePDF;

/// <summary>
/// Error correction levels for QR codes.
/// </summary>
public enum QrErrorCorrection
{
    /// <summary>~7% of codewords can be restored.</summary>
    Low,

    /// <summary>~15% of codewords can be restored.</summary>
    Medium,

    /// <summary>~25% of codewords can be restored.</summary>
    Quartile,

    /// <summary>~30% of codewords can be restored.</summary>
    High
}
