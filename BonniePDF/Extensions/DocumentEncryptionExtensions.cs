using System;

namespace BonniePDF;

/// <summary>
/// Extension methods for configuring PDF document encryption.
/// </summary>
public static class DocumentEncryptionExtensions
{
    /// <summary>
    /// Configures 40-bit RC4 encryption (legacy, not recommended for new documents).
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="configure">Action to configure encryption options</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithEncryption40(this DocumentSettings settings, Action<EncryptionOptions> configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        EncryptionOptions options = new(EncryptionAlgorithm.RC4_40);
        configure(options);
        settings.Encryption = options.Build();
        return settings;
    }

    /// <summary>
    /// Configures 128-bit RC4 encryption (standard for PDF 1.4).
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="configure">Action to configure encryption options</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithEncryption128(this DocumentSettings settings,
        Action<EncryptionOptions> configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        EncryptionOptions options = new(EncryptionAlgorithm.RC4_128);
        configure(options);
        settings.Encryption = options.Build();
        return settings;
    }

    /// <summary>
    /// Configures encryption with the specified settings.
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="encryptionSettings">The encryption settings to use</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithEncryption(this DocumentSettings settings, EncryptionSettings encryptionSettings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (encryptionSettings == null)
            throw new ArgumentNullException(nameof(encryptionSettings));

        encryptionSettings.Validate();
        settings.Encryption = encryptionSettings.Clone();
        return settings;
    }

    /// <summary>
    /// Configures encryption with a simple user password (128-bit RC4, all permissions).
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="userPassword">The password required to open the document</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithPassword(this DocumentSettings settings, string? userPassword)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.RC4_128,
            UserPassword = userPassword ?? "",
            OwnerPassword = userPassword ?? "",
            Permissions = PdfPermissions.All
        };
        return settings;
    }

    /// <summary>
    /// Configures encryption with user and owner passwords (128-bit RC4).
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="userPassword">The password required to open the document</param>
    /// <param name="ownerPassword">The password required to change permissions</param>
    /// <param name="permissions">The permissions to grant</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithPasswords(
        this DocumentSettings settings,
        string? userPassword,
        string? ownerPassword,
        PdfPermissions permissions = PdfPermissions.All)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        settings.Encryption = new EncryptionSettings
        {
            Algorithm = EncryptionAlgorithm.RC4_128,
            UserPassword = userPassword ?? "",
            OwnerPassword = ownerPassword ?? "",
            Permissions = permissions
        };
        return settings;
    }

    /// <summary>
    /// Configures AES-256 encryption (PDF 1.7 required).
    /// Automatically sets Version to Pdf17.
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="configure">Action to configure encryption options</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithEncryptionAes256(this DocumentSettings settings,
        Action<EncryptionOptions> configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        settings.Version = PdfVersion.Pdf17;
        EncryptionOptions options = new(EncryptionAlgorithm.AES_256);
        configure(options);
        settings.Encryption = options.Build();
        return settings;
    }

    /// <summary>
    /// Removes encryption from the document settings.
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings WithoutEncryption(this DocumentSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        settings.Encryption = null;
        return settings;
    }
}