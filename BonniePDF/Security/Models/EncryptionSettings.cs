using System;
using BonniePDF.Security.Permissions;

namespace BonniePDF.Security.Models;

/// <summary>
/// Configuration settings for PDF encryption.
/// </summary>
public class EncryptionSettings
{
    /// <summary>
    /// Gets or sets the encryption algorithm to use.
    /// Default is RC4_128 (128-bit RC4).
    /// </summary>
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.RC4_128;

    /// <summary>
    /// Gets or sets the user password (open password).
    /// If set, users must enter this password to open the document.
    /// Can be null or empty for no open password.
    /// </summary>
    public string UserPassword { get; set; } = "";

    /// <summary>
    /// Gets or sets the owner password (permissions password).
    /// Required for encryption. Controls access to change permissions and passwords.
    /// </summary>
    public string OwnerPassword { get; set; } = "";

    /// <summary>
    /// Gets or sets the document permissions.
    /// Default is All (no restrictions).
    /// </summary>
    public PdfPermissions Permissions { get; set; } = PdfPermissions.All;

    /// <summary>
    /// Gets or sets whether to encrypt document metadata.
    /// When false, metadata (title, author, etc.) remains readable without password.
    /// Default is true.
    /// </summary>
    public bool EncryptMetadata { get; set; } = true;

    /// <summary>
    /// Gets the key length in bits for the selected algorithm.
    /// </summary>
    public int KeyLengthBits => Algorithm switch
    {
        EncryptionAlgorithm.RC4_40 => 40,
        EncryptionAlgorithm.RC4_128 => 128,
        EncryptionAlgorithm.AES_256 => 256,
        _ => 128
    };

    /// <summary>
    /// Gets the key length in bytes for the selected algorithm.
    /// </summary>
    public int KeyLengthBytes => KeyLengthBits / 8;

    /// <summary>
    /// Gets the /V value for the encryption dictionary.
    /// </summary>
    public int VersionValue => Algorithm switch
    {
        EncryptionAlgorithm.RC4_40 => 1,
        EncryptionAlgorithm.RC4_128 => 2,
        EncryptionAlgorithm.AES_256 => 5,
        _ => 2
    };

    /// <summary>
    /// Gets the /R (revision) value for the encryption dictionary.
    /// </summary>
    public int RevisionValue => Algorithm switch
    {
        EncryptionAlgorithm.RC4_40 => 2,
        EncryptionAlgorithm.RC4_128 => 3,
        EncryptionAlgorithm.AES_256 => 6,
        _ => 3
    };

    /// <summary>
    /// Validates the encryption settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when settings are invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrEmpty(OwnerPassword) && string.IsNullOrEmpty(UserPassword))
            throw new InvalidOperationException(
                "At least one password (user or owner) must be set for encryption.");

        // If only user password is set, use it as owner password
        if (string.IsNullOrEmpty(OwnerPassword)) OwnerPassword = UserPassword;
    }

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public EncryptionSettings Clone()
    {
        return new EncryptionSettings
        {
            Algorithm = Algorithm,
            UserPassword = UserPassword,
            OwnerPassword = OwnerPassword,
            Permissions = Permissions,
            EncryptMetadata = EncryptMetadata,
        };
    }
}