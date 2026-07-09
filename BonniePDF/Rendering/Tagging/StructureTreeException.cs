using BonniePDF.Core;

namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// Thrown when the structure-tree builder is asked to perform an
/// operation that would produce an invalid tree (e.g. closing a node
/// that wasn't opened, ending past the document root).
/// </summary>
public sealed class StructureTreeException : BonniePdfException
{
    /// <summary>Creates a new <see cref="StructureTreeException"/>.</summary>
    public StructureTreeException(string message) : base(message) { }
}
