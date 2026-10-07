using System;
using System.Security.Cryptography;
using System.Text;

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
        ComputePermsEntry(settings.EncryptMetadata);

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
    public byte[] UserEntry { get; private set; } = null!;

    /// <summary>
    /// The /UE entry (32 bytes): AES-CBC encrypt of file key using SHA-256(password + key_salt).
    /// </summary>
    public byte[] UserEncryptionEntry { get; private set; } = null!;

    /// <summary>
    /// The /O entry (48 bytes): SHA-256(password + validation_salt + /U) + validation_salt + key_salt.
    /// </summary>
    public byte[] OwnerEntry { get; private set; } = null!;

    /// <summary>
    /// The /OE entry (32 bytes): AES-CBC encrypt of file key using SHA-256(password + key_salt + /U).
    /// </summary>
    public byte[] OwnerEncryptionEntry { get; private set; } = null!;

    /// <summary>
    /// The /Perms entry (16 bytes): AES-ECB encrypt of permissions block using file key.
    /// </summary>
    public byte[] PermsEntry { get; private set; } = null!;

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

        // /U = Algorithm2B(password, validation_salt, "") + validation_salt + key_salt
        byte[] hash = Algorithm2B(passwordBytes, validationSalt, Array.Empty<byte>());
        UserEntry = new byte[48];
        Buffer.BlockCopy(hash, 0, UserEntry, 0, 32);
        Buffer.BlockCopy(validationSalt, 0, UserEntry, 32, 8);
        Buffer.BlockCopy(keySalt, 0, UserEntry, 40, 8);

        // /UE = AES-CBC encrypt of file key using Algorithm2B(password, key_salt, ""), IV=0
        byte[] ueKey = Algorithm2B(passwordBytes, keySalt, Array.Empty<byte>());
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

        // /O = Algorithm2B(password, validation_salt, /U) + validation_salt + key_salt
        byte[] hash = Algorithm2B(passwordBytes, validationSalt, UserEntry);
        OwnerEntry = new byte[48];
        Buffer.BlockCopy(hash, 0, OwnerEntry, 0, 32);
        Buffer.BlockCopy(validationSalt, 0, OwnerEntry, 32, 8);
        Buffer.BlockCopy(keySalt, 0, OwnerEntry, 40, 8);

        // /OE = AES-CBC encrypt of file key using Algorithm2B(password, key_salt, /U), IV=0
        byte[] oeKey = Algorithm2B(passwordBytes, keySalt, UserEntry);
        AesCipher cipher = new(oeKey);
        byte[] iv = new byte[16]; // zero IV for key wrapping
        OwnerEncryptionEntry = cipher.EncryptNoPadding(FileEncryptionKey, iv);
    }

    /// <summary>
    /// Computes the /Perms entry.
    /// </summary>
    private void ComputePermsEntry(bool encryptMetadata)
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
        permsBlock[8] = (byte)(encryptMetadata ? 'T' : 'F');

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

    /// <summary>
    /// Algorithm 2.B from ISO 32000-2:2020 §7.6.4.3.4.
    /// Iterative hash used by R=6 for password validation and key derivation.
    /// </summary>
    private static byte[] Algorithm2B(byte[] password, byte[] salt, byte[] userKey)
    {
        byte[] K = SHA256.HashData(Concatenate(password, salt, userKey));

        int round = 0;
        while (true)
        {
            // K1 = (password + K + userKey) repeated 64 times
            byte[] sequence = Concatenate(password, K, userKey);
            byte[] K1 = new byte[sequence.Length * 64];
            for (int i = 0; i < 64; i++)
                Buffer.BlockCopy(sequence, 0, K1, i * sequence.Length, sequence.Length);

            // E = AES-128-CBC encrypt K1 (no padding) using first 16 bytes of K as key, next 16 as IV
            using Aes aes = Aes.Create();
            aes.Key = K[..16];
            aes.IV = K[16..32];
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] E = encryptor.TransformFinalBlock(K1, 0, K1.Length);

            // Sum first 16 bytes mod 3 to pick hash algorithm
            int sum = 0;
            for (int i = 0; i < 16; i++) sum += E[i];
            int mod3 = sum % 3;

            K = mod3 switch
            {
                0 => SHA256.HashData(E),
                1 => SHA384.HashData(E),
                _ => SHA512.HashData(E)
            };

            if (round >= 63 && E[^1] <= round - 32) break;
            round++;
        }

        return K[..32];
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
