using System;

namespace BonniePDF;

/// <summary>
/// Thrown when image bytes cannot be decoded: the compressed pixel data is corrupt or truncated, or the image is
/// larger than <see cref="ImageLoader.MaxPixels"/>. Images often come from end users, so catch this where an upload
/// is turned into a document.
/// </summary>
public sealed class ImageDecodeException : BonniePdfException
{
    /// <summary>Creates the exception with a message describing what could not be decoded.</summary>
    public ImageDecodeException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the error that caused it.</summary>
    public ImageDecodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
