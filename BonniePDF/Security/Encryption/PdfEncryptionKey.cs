using System;
using System.Security.Cryptography;
using System.Text;
using BonniePDF.Security.Models;
using BonniePDF.Security.Permissions;

namespace BonniePDF.Security.Encryption;

/// <summary>
/// Generates encryption keys for PDF documents following PDF Reference 1.4.
/// Implements Algorithms 3.2, 3.3, 3.4, 3.5 from the PDF specification.
/// </summary>
internal sealed class PdfEncryptionKey
{
    /// <summary>
    /// Standard 32-byte padding string used in PDF encryption (from PDF spec).
    /// </summary>
    private static readonly byte[] PaddingBytes =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
        0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
    ];

    private readonly int _keyLength;
    private readonly int _revision;

    /// <summary>
    /// Creates encryption keys for a PDF document.
    /// </summary>
    /// <param name="settings">The encryption settings</param>
    /// <param name="documentId">The document ID (first element of /ID array)</param>
    public PdfEncryptionKey(EncryptionSettings settings, byte[] documentId)
    {
        if (documentId == null || documentId.Length == 0)
            throw new ArgumentException("Document ID is required for encryption", nameof(documentId));

        _keyLength = settings.KeyLengthBytes;
        _revision = settings.RevisionValue;
        DocumentId = documentId;
        PermissionsValue = PermissionEncoder.Encode(settings.Permissions);

        // Step 1: Compute the O entry (Algorithm 3.3)
        OwnerEntry = ComputeOwnerEntry(settings.OwnerPassword, settings.UserPassword);

        // Step 2: Compute the encryption key (Algorithm 3.2)
        EncryptionKey = ComputeEncryptionKey(settings.UserPassword, OwnerEntry,
            PermissionsValue, documentId, settings.EncryptMetadata);

        // Step 3: Compute the U entry (Algorithm 3.4 or 3.5)
        UserEntry = ComputeUserEntry(EncryptionKey, documentId);
    }

    /// <summary>
    /// The master encryption key derived from passwords.
    /// </summary>
    public byte[] EncryptionKey { get; }

    /// <summary>
    /// The /O entry for the encryption dictionary (owner password hash).
    /// </summary>
    public byte[] OwnerEntry { get; }

    /// <summary>
    /// The /U entry for the encryption dictionary (user password hash).
    /// </summary>
    public byte[] UserEntry { get; }

    /// <summary>
    /// The encoded permissions value for the /P entry.
    /// </summary>
    public int PermissionsValue { get; }

    /// <summary>
    /// The document ID used in encryption.
    /// </summary>
    public byte[] DocumentId { get; }

    /// <summary>
    /// Pads or truncates a password to 32 bytes using the standard padding.
    /// </summary>
    private static byte[] PadPassword(string password)
    {
        byte[] padded = new byte[32];

        if (string.IsNullOrEmpty(password))
        {
            // Use only padding bytes
            Buffer.BlockCopy(PaddingBytes, 0, padded, 0, 32);
        }
        else
        {
            // Convert password to bytes (PDF uses Latin-1 encoding)
            byte[] passwordBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(password);
            int length = Math.Min(passwordBytes.Length, 32);

            Buffer.BlockCopy(passwordBytes, 0, padded, 0, length);

            // Fill remainder with padding
            if (length < 32) Buffer.BlockCopy(PaddingBytes, 0, padded, length, 32 - length);
        }

        return padded;
    }

    /// <summary>
    /// Algorithm 3.3: Computing the O (owner) entry.
    /// </summary>
    private byte[] ComputeOwnerEntry(string ownerPassword, string userPassword)
    {
        // Step a: Pad or truncate owner password
        byte[] ownerPadded = PadPassword(ownerPassword);

        // Step b: Hash the padded owner password
        byte[] hash = Md5Hash.Compute(ownerPadded);

        // Step c: For revision 3+, iterate the hash 50 times
        if (_revision >= 3)
            for (int i = 0; i < 50; i++)
                hash = Md5Hash.Compute(hash, 0, _keyLength);

        // Step d: Create RC4 key from first n bytes
        byte[] rc4Key = new byte[_keyLength];
        Buffer.BlockCopy(hash, 0, rc4Key, 0, _keyLength);

        // Step e: Pad the user password
        byte[] userPadded = PadPassword(userPassword);

        // Step f: Encrypt the padded user password
        byte[] result = new byte[32];
        Buffer.BlockCopy(userPadded, 0, result, 0, 32);

        RC4Cipher cipher = new(rc4Key);
        cipher.Transform(result);

        // Step g: For revision 3+, do 19 additional encryption rounds
        if (_revision >= 3)
            for (int i = 1; i <= 19; i++)
            {
                byte[] newKey = new byte[_keyLength];
                for (int j = 0; j < _keyLength; j++) newKey[j] = (byte)(rc4Key[j] ^ i);
                RC4Cipher iterCipher = new(newKey);
                iterCipher.Transform(result);
            }

        return result;
    }

    /// <summary>
    /// Algorithm 3.2: Computing the encryption key.
    /// </summary>
    private byte[] ComputeEncryptionKey(string userPassword, byte[] oEntry,
        int permissions, byte[] documentId, bool encryptMetadata)
    {
        // Step a: Pad the user password
        byte[] userPadded = PadPassword(userPassword);

        // Step b: Concatenate: password + O + P + ID + (metadata flag)
        byte[] permBytes = new byte[4];
        permBytes[0] = (byte)(permissions & 0xFF);
        permBytes[1] = (byte)((permissions >> 8) & 0xFF);
        permBytes[2] = (byte)((permissions >> 16) & 0xFF);
        permBytes[3] = (byte)((permissions >> 24) & 0xFF);

        byte[] toHash;
        if (_revision >= 3 && !encryptMetadata)
        {
            // Add 0xFFFFFFFF if not encrypting metadata
            byte[] noEncryptFlag = { 0xFF, 0xFF, 0xFF, 0xFF };
            toHash = Concatenate(userPadded, oEntry, permBytes, documentId, noEncryptFlag);
        }
        else
        {
            toHash = Concatenate(userPadded, oEntry, permBytes, documentId);
        }

        // Step c: Hash the concatenated data
        byte[] hash = Md5Hash.Compute(toHash);

        // Step d: For revision 3+, iterate the hash 50 times
        if (_revision >= 3)
            for (int i = 0; i < 50; i++)
            {
                byte[] keyPortion = new byte[_keyLength];
                Buffer.BlockCopy(hash, 0, keyPortion, 0, _keyLength);
                hash = Md5Hash.Compute(keyPortion);
            }

        // Step e: Return first n bytes as the key
        byte[] key = new byte[_keyLength];
        Buffer.BlockCopy(hash, 0, key, 0, _keyLength);
        return key;
    }

    /// <summary>
    /// Computes the U entry using Algorithm 3.4 (revision 2) or 3.5 (revision 3+).
    /// </summary>
    private byte[] ComputeUserEntry(byte[] encryptionKey, byte[] documentId)
    {
        if (_revision == 2)
        {
            // Algorithm 3.4: Simple RC4 encryption of padding
            byte[] result = new byte[32];
            Buffer.BlockCopy(PaddingBytes, 0, result, 0, 32);
            RC4Cipher cipher = new(encryptionKey);
            cipher.Transform(result);
            return result;
        }
        else
        {
            // Algorithm 3.5: More complex for revision 3+
            // Step a: Create hash of padding + document ID
            byte[] toHash = Concatenate(PaddingBytes, documentId);
            byte[] hash = Md5Hash.Compute(toHash);

            // Step b: Encrypt with RC4
            RC4Cipher cipher = new(encryptionKey);
            cipher.Transform(hash);

            // Step c: Do 19 more rounds with XOR'd keys
            for (int i = 1; i <= 19; i++)
            {
                byte[] newKey = new byte[_keyLength];
                for (int j = 0; j < _keyLength; j++) newKey[j] = (byte)(encryptionKey[j] ^ i);
                RC4Cipher iterCipher = new(newKey);
                iterCipher.Transform(hash);
            }

            // Step d: Append 16 arbitrary bytes to make 32-byte result
            byte[] result = new byte[32];
            Buffer.BlockCopy(hash, 0, result, 0, 16);
            // Fill remaining with zeros (or any value)
            return result;
        }
    }

    /// <summary>
    /// Computes the per-object encryption key for a specific object.
    /// </summary>
    /// <param name="objectNumber">The PDF object number</param>
    /// <param name="generationNumber">The PDF generation number (usually 0)</param>
    /// <returns>The object-specific encryption key</returns>
    public byte[] GetObjectKey(int objectNumber, int generationNumber = 0)
    {
        // Concatenate: encryption key + object number (3 bytes LE) + generation (2 bytes LE)
        byte[] data = new byte[_keyLength + 5];
        Buffer.BlockCopy(EncryptionKey, 0, data, 0, _keyLength);

        data[_keyLength] = (byte)(objectNumber & 0xFF);
        data[_keyLength + 1] = (byte)((objectNumber >> 8) & 0xFF);
        data[_keyLength + 2] = (byte)((objectNumber >> 16) & 0xFF);
        data[_keyLength + 3] = (byte)(generationNumber & 0xFF);
        data[_keyLength + 4] = (byte)((generationNumber >> 8) & 0xFF);

        // Hash and use first n+5 bytes (max 16)
        byte[] hash = Md5Hash.Compute(data);
        int keyLen = Math.Min(_keyLength + 5, 16);
        byte[] objectKey = new byte[keyLen];
        Buffer.BlockCopy(hash, 0, objectKey, 0, keyLen);
        return objectKey;
    }

    /// <summary>
    /// Concatenates multiple byte arrays.
    /// </summary>
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

    /// <summary>
    /// Generates a random document ID.
    /// </summary>
    public static byte[] GenerateDocumentId()
    {
        byte[] id = new byte[16];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(id);
        return id;
    }
}