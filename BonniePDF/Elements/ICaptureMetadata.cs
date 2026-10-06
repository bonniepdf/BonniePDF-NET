namespace BonniePDF;

/// <summary>
/// Optional metadata an element exposes to the layout snapshot recorder,
/// as defined by the shared specification. Implemented by
/// <see cref="ElementBase"/>; the recorder falls back to <c>GetType().Name</c>
/// for anything that does not implement it.
/// </summary>
internal interface ICaptureMetadata
{
    /// <summary>Canonical element type name used for the snapshot tree node.</summary>
    string CaptureType { get; }

    /// <summary>Optional author-supplied name/key surfaced in the snapshot; null when unset.</summary>
    string? CaptureName { get; }
}
