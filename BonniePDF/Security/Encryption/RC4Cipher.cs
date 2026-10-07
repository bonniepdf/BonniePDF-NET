using System;

namespace BonniePDF.Security.Encryption;

/// <summary>
/// RC4 stream cipher implementation for PDF encryption.
/// RC4 is a symmetric stream cipher used in PDF 1.4 encryption.
/// </summary>
internal sealed class RC4Cipher
{
    private readonly byte[] _state;
    private int _i;
    private int _j;

    /// <summary>
    /// Creates a new RC4 cipher with the specified key.
    /// </summary>
    /// <param name="key">The encryption key (5-16 bytes)</param>
    public RC4Cipher(byte[] key)
    {
        if (key == null || key.Length == 0)
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        _state = new byte[256];
        _i = 0;
        _j = 0;

        // Key Scheduling Algorithm (KSA)
        Initialize(key);
    }

    /// <summary>
    /// Key Scheduling Algorithm - initializes the permutation state.
    /// </summary>
    private void Initialize(byte[] key)
    {
        // Initialize state array with identity permutation
        for (int i = 0; i < 256; i++) _state[i] = (byte)i;

        // Key scheduling
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + _state[i] + key[i % key.Length]) & 0xFF;
            Swap(i, j);
        }
    }

    /// <summary>
    /// Swaps two elements in the state array.
    /// </summary>
    private void Swap(int i, int j)
    {
        byte temp = _state[i];
        _state[i] = _state[j];
        _state[j] = temp;
    }

    /// <summary>
    /// Generates the next byte in the keystream.
    /// Pseudo-Random Generation Algorithm (PRGA).
    /// </summary>
    private byte NextByte()
    {
        _i = (_i + 1) & 0xFF;
        _j = (_j + _state[_i]) & 0xFF;
        Swap(_i, _j);
        return _state[(_state[_i] + _state[_j]) & 0xFF];
    }

    /// <summary>
    /// Encrypts or decrypts data in place.
    /// RC4 encryption and decryption are the same operation (XOR with keystream).
    /// </summary>
    /// <param name="data">The data to transform</param>
    public void Transform(byte[]? data)
    {
        if (data == null) return;

        for (int k = 0; k < data.Length; k++) data[k] ^= NextByte();
    }

    /// <summary>
    /// Encrypts or decrypts data and returns a new array.
    /// </summary>
    /// <param name="data">The data to transform</param>
    /// <returns>Transformed data</returns>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(data))]
    public byte[]? TransformCopy(byte[]? data)
    {
        if (data == null) return data;

        byte[] result = new byte[data.Length];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        Transform(result);
        return result;
    }

    /// <summary>
    /// Encrypts or decrypts a portion of data.
    /// </summary>
    /// <param name="data">The data buffer</param>
    /// <param name="offset">Starting offset</param>
    /// <param name="length">Number of bytes to transform</param>
    public void Transform(byte[]? data, int offset, int length)
    {
        if (data == null) return;

        for (int k = 0; k < length; k++) data[offset + k] ^= NextByte();
    }

    /// <summary>
    /// Creates a fresh RC4 cipher for the given object key.
    /// Used for per-object encryption in PDF.
    /// </summary>
    /// <param name="key">The object-specific encryption key</param>
    /// <returns>A new RC4 cipher instance</returns>
    public static RC4Cipher ForObject(byte[] key)
    {
        return new RC4Cipher(key);
    }
}