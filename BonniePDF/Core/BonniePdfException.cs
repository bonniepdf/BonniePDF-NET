using System;

namespace BonniePDF;

/// <summary>
/// Base type for exceptions raised by BonniePDF itself (content that cannot be
/// encoded, tree operations that would produce invalid output, etc.), as
/// opposed to exceptions from the BCL or the caller's own code. Catch this to
/// handle "BonniePDF rejected this input/operation" as a single category
/// without enumerating every concrete exception type.
/// </summary>
public abstract class BonniePdfException : Exception
{
    /// <summary>Creates a new <see cref="BonniePdfException"/>.</summary>
    protected BonniePdfException(string message) : base(message)
    {
    }

    /// <summary>Creates a new <see cref="BonniePdfException"/> wrapping an inner exception.</summary>
    protected BonniePdfException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
