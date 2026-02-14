using System;
using System.Security.Cryptography;
using System.Text;
using BonniePDF.Security.Models;
using BonniePDF.Security.Permissions;

namespace BonniePDF.Security.Encryption;

/// <summary>
/// Generates encryption keys for PDF AES-256 encryption (V5, R6).
/// Implements the key derivation algorithm from PDF 2.0 / Extension Level 3.
/// Uses SHA-256 for key derivation instead of MD5.
/// </summary>
internal sealed class Aes256EncryptionKey
{
    /// <summary>
    /// Creates AES-256 encryption keys for a PDF document.
    /// </summary>
    public Aes256EncryptionKey(EncryptionSettings settings)
    {
        PermissionsValue = PermissionEncoder.Encode(settings.Permissions);

        // Generate random 32-byte file encryption key
        FileEncryptionKey = new byte[32];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(FileEncryptionKey);
        }

        // Compute U, UE entries
        ComputeUserEntries(settings.UserPassword ?? "");

        // Compute O, OE entries
        ComputeOwnerEntries(settings.OwnerPassword ?? "");

        // Compute Perms entry
        ComputePermsEntry();

        // Generate document ID
        DocumentId = PdfEncryptionKey.GenerateDocumentId();
    }

    /// <summary>
    /// The 32-byte file encryption key (randomly generated).
    /// </summary>
    public byte[] FileEncryptionKey { get; }

    /// <summary>
    /// The /U entry (48 bytes): SHA-256(password + validation_salt) + validation_salt + key_salt.
    /// </summary>
    public byte[] UserEntry { get; private set; }

    /// <summary>
    /// The /UE entry (32 bytes): AES-CBC encrypt of file key using SHA-256(password + key_salt).
    /// </summary>
    public byte[] UserEncryptionEntry { get; private set; }

    /// <summary>
    /// The /O entry (48 bytes): SHA-256(password + validation_salt + /U) + validation_salt + key_salt.
    /// </summary>
    public byte[] OwnerEntry { get; private set; }

    /// <summary>
    /// The /OE entry (32 bytes): AES-CBC encrypt of file key using SHA-256(password + key_salt + /U).
    /// </summary>
    public byte[] OwnerEncryptionEntry { get; private set; }

    /// <summary>
    /// The /Perms entry (16 bytes): AES-ECB encrypt of permissions block using file key.
    /// </summary>
    public byte[] PermsEntry { get; private set; }

    /// <summary>
    /// The document ID.
    /// </summary>
    public byte[] DocumentId { get; }

    /// <summary>
    /// The encoded permissions value for the /P entry.
    /// </summary>
    public int PermissionsValue { get; }

    /// <summary>
    /// Computes the /U and /UE entries.
    /// </summary>
    private void ComputeUserEntries(string password)
    {
        byte[] passwordBytes = TruncatePassword(password);

        // Generate random salts
        byte[] validationSalt = new byte[8];
        byte[] keySalt = new byte[8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(validationSalt);
            rng.GetBytes(keySalt);
        }

        // /U = SHA-256(password + validation_salt) + validation_salt + key_salt
        byte[] hashInput = Concatenate(passwordBytes, validationSalt);
        byte[] hash = SHA256Hash(hashInput);
        UserEntry = new byte[48];
        Buffer.BlockCopy(hash, 0, UserEntry, 0, 32);
        Buffer.BlockCopy(validationSalt, 0, UserEntry, 32, 8);
        Buffer.BlockCopy(keySalt, 0, UserEntry, 40, 8);

        // /UE = AES-CBC encrypt of file key using SHA-256(password + key_salt)
        byte[] ueKeyInput = Concatenate(passwordBytes, keySalt);
        byte[] ueKey = SHA256Hash(ueKeyInput);
        AesCipher cipher = new(ueKey);
        byte[] iv = new byte[16]; // zero IV for key wrapping
        UserEncryptionEntry = cipher.EncryptNoPadding(FileEncryptionKey, iv);
    }

    /// <summary>
    /// Computes the /O and /OE entries.
    /// </summary>
    private void ComputeOwnerEntries(string password)
    {
        byte[] passwordBytes = TruncatePassword(password);

        // Generate random salts
        byte[] validationSalt = new byte[8];
        byte[] keySalt = new byte[8];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(validationSalt);
            rng.GetBytes(keySalt);
        }

        // /O = SHA-256(password + validation_salt + /U) + validation_salt + key_salt
        byte[] hashInput = Concatenate(passwordBytes, validationSalt, UserEntry);
        byte[] hash = SHA256Hash(hashInput);
        OwnerEntry = new byte[48];
        Buffer.BlockCopy(hash, 0, OwnerEntry, 0, 32);
        Buffer.BlockCopy(validationSalt, 0, OwnerEntry, 32, 8);
        Buffer.BlockCopy(keySalt, 0, OwnerEntry, 40, 8);

        // /OE = AES-CBC encrypt of file key using SHA-256(password + key_salt + /U)
        byte[] oeKeyInput = Concatenate(passwordBytes, keySalt, UserEntry);
        byte[] oeKey = SHA256Hash(oeKeyInput);
        AesCipher cipher = new(oeKey);
        byte[] iv = new byte[16]; // zero IV for key wrapping
        OwnerEncryptionEntry = cipher.EncryptNoPadding(FileEncryptionKey, iv);
    }

    /// <summary>
    /// Computes the /Perms entry.
    /// </summary>
    private void ComputePermsEntry()
    {
        // Build 16-byte permissions block
        byte[] permsBlock = new byte[16];

        // Bytes 0-3: permissions (little-endian)
        permsBlock[0] = (byte)(PermissionsValue & 0xFF);
        permsBlock[1] = (byte)((PermissionsValue >> 8) & 0xFF);
        permsBlock[2] = (byte)((PermissionsValue >> 16) & 0xFF);
        permsBlock[3] = (byte)((PermissionsValue >> 24) & 0xFF);

        // Bytes 4-7: 0xFFFFFFFF
        permsBlock[4] = 0xFF;
        permsBlock[5] = 0xFF;
        permsBlock[6] = 0xFF;
        permsBlock[7] = 0xFF;

        // Byte 8: 'T' if encrypting metadata, 'F' otherwise
        permsBlock[8] = (byte)'T'; // We always encrypt metadata for AES-256

        // Bytes 9-11: 'adb'
        permsBlock[9] = (byte)'a';
        permsBlock[10] = (byte)'d';
        permsBlock[11] = (byte)'b';

        // Bytes 12-15: random
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            byte[] random = new byte[4];
            rng.GetBytes(random);
            Buffer.BlockCopy(random, 0, permsBlock, 12, 4);
        }

        // Encrypt with AES-ECB using the file encryption key
        AesCipher cipher = new(FileEncryptionKey);
        PermsEntry = cipher.EncryptEcb(permsBlock);
    }

    /// <summary>
    /// Truncates password to 127 bytes (UTF-8) as per the spec.
    /// </summary>
    private static byte[] TruncatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return Array.Empty<byte>();

        byte[] bytes = Encoding.UTF8.GetBytes(password);
        if (bytes.Length > 127)
        {
            byte[] truncated = new byte[127];
            Buffer.BlockCopy(bytes, 0, truncated, 0, 127);
            return truncated;
        }
        return bytes;
    }

    private static byte[] SHA256Hash(byte[] data)
    {
        using SHA256 sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    private static byte[] Concatenate(params byte[][] arrays)
    {
        int totalLength = 0;
        foreach (byte[] arr in arrays) totalLength += arr?.Length ?? 0;

        byte[] result = new byte[totalLength];
        int offset = 0;
        foreach (byte[] arr in arrays)
            if (arr is { Length: > 0 })
            {
                Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
                offset += arr.Length;
            }

        return result;
    }
}
