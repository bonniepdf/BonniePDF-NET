using System;
using System.Security.Cryptography;

namespace BonniePDF.Security.Encryption;

/// <summary>
/// AES-CBC encryption for PDF AES-256 encryption (V5, R6).
/// Uses System.Security.Cryptography.Aes (zero external dependencies).
/// </summary>
internal sealed class AesCipher
{
    private readonly byte[] _key;

    /// <summary>
    /// Creates a new AES cipher with the specified key.
    /// </summary>
    /// <param name="key">The AES encryption key (16, 24, or 32 bytes)</param>
    public AesCipher(byte[] key)
    {
        _key = key ?? throw new ArgumentNullException(nameof(key));
    }

    /// <summary>
    /// Encrypts data using AES-CBC with PKCS7 padding.
    /// Returns IV (16 bytes) + ciphertext.
    /// </summary>
    public byte[] Encrypt(byte[] data)
    {
        if (data == null || data.Length == 0)
            return data;

        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        byte[] ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Return IV + ciphertext
        byte[] result = new byte[16 + ciphertext.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, 16);
        Buffer.BlockCopy(ciphertext, 0, result, 16, ciphertext.Length);
        return result;
    }

    /// <summary>
    /// Encrypts data using AES-CBC with a specified IV and no padding.
    /// Used for key wrapping in the encryption dictionary.
    /// </summary>
    public byte[] EncryptNoPadding(byte[] data, byte[] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    /// <summary>
    /// Encrypts data using AES-ECB with no padding.
    /// Used for the /Perms entry in AES-256 encryption.
    /// </summary>
    public byte[] EncryptEcb(byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    /// <summary>
    /// Creates a fresh AES cipher for the given object key.
    /// Used for per-object encryption in PDF AES mode.
    /// </summary>
    public static AesCipher ForObject(byte[] key)
    {
        return new AesCipher(key);
    }
}
