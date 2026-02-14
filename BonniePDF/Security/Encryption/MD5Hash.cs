using System;
using System.Security.Cryptography;

namespace BonniePDF.Security.Encryption;

/// <summary>
/// MD5 hashing utility for PDF encryption key derivation.
/// Uses the built-in .NET MD5 implementation.
/// </summary>
internal static class Md5Hash
{
    /// <summary>
    /// Computes the MD5 hash of the input data.
    /// </summary>
    /// <param name="data">The data to hash</param>
    /// <returns>16-byte MD5 hash</returns>
    public static byte[] Compute(byte[] data)
    {
        return MD5.HashData(data);
    }

    /// <summary>
    /// Computes the MD5 hash of a portion of the input data.
    /// </summary>
    /// <param name="data">The data buffer</param>
    /// <param name="offset">The starting offset</param>
    /// <param name="length">The number of bytes to hash</param>
    /// <returns>16-byte MD5 hash</returns>
    public static byte[] Compute(byte[] data, int offset, int length)
    {
        return MD5.HashData(data.AsSpan(offset, length));
    }
}