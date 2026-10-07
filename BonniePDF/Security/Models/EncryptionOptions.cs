
namespace BonniePDF;

/// <summary>
/// Fluent builder for configuring PDF encryption options.
/// </summary>
public class EncryptionOptions
{
    private readonly EncryptionSettings _settings;

    /// <summary>
    /// Creates a new EncryptionOptions builder with default settings.
    /// </summary>
    public EncryptionOptions()
    {
        _settings = new EncryptionSettings();
    }

    /// <summary>
    /// Creates a new EncryptionOptions builder with the specified algorithm.
    /// </summary>
    internal EncryptionOptions(EncryptionAlgorithm algorithm)
    {
        _settings = new EncryptionSettings { Algorithm = algorithm };
    }

    /// <summary>
    /// Sets the user password (required to open the document).
    /// </summary>
    /// <param name="password">The user password</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions WithUserPassword(string? password)
    {
        _settings.UserPassword = password ?? "";
        return this;
    }

    /// <summary>
    /// Sets the owner password (required to change permissions).
    /// </summary>
    /// <param name="password">The owner password</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions WithOwnerPassword(string? password)
    {
        _settings.OwnerPassword = password ?? "";
        return this;
    }

    /// <summary>
    /// Sets both user and owner passwords to the same value.
    /// </summary>
    /// <param name="password">The password to use for both</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions WithPassword(string? password)
    {
        _settings.UserPassword = password ?? "";
        _settings.OwnerPassword = password ?? "";
        return this;
    }

    /// <summary>
    /// Sets the document permissions.
    /// </summary>
    /// <param name="permissions">The permissions to grant</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions WithPermissions(PdfPermissions permissions)
    {
        _settings.Permissions = permissions;
        return this;
    }

    /// <summary>
    /// Allows printing of the document.
    /// </summary>
    /// <param name="highQuality">If true, allows high-quality printing</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowPrinting(bool highQuality = true)
    {
        _settings.Permissions |= PdfPermissions.Print;
        if (highQuality)
            _settings.Permissions |= PdfPermissions.PrintHighQuality;
        return this;
    }

    /// <summary>
    /// Allows copying of text and graphics from the document.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowCopying()
    {
        _settings.Permissions |= PdfPermissions.Copy;
        return this;
    }

    /// <summary>
    /// Allows modification of the document.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowModification()
    {
        _settings.Permissions |= PdfPermissions.Modify;
        return this;
    }

    /// <summary>
    /// Allows adding and modifying annotations and filling forms.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowAnnotations()
    {
        _settings.Permissions |= PdfPermissions.AnnotateAndFillForms;
        return this;
    }

    /// <summary>
    /// Allows filling of interactive form fields.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowFormFilling()
    {
        _settings.Permissions |= PdfPermissions.FillForms;
        return this;
    }

    /// <summary>
    /// Allows document assembly (inserting, rotating, deleting pages).
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowAssembly()
    {
        _settings.Permissions |= PdfPermissions.Assemble;
        return this;
    }

    /// <summary>
    /// Allows extraction for accessibility purposes.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowAccessibility()
    {
        _settings.Permissions |= PdfPermissions.ExtractForAccessibility;
        return this;
    }

    /// <summary>
    /// Denies all permissions (most restrictive).
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions DenyAll()
    {
        _settings.Permissions = PdfPermissions.None;
        return this;
    }

    /// <summary>
    /// Grants all permissions (least restrictive).
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions AllowAll()
    {
        _settings.Permissions = PdfPermissions.All;
        return this;
    }

    /// <summary>
    /// Sets whether to encrypt the document metadata.
    /// </summary>
    /// <param name="encrypt">If true, metadata is encrypted</param>
    /// <returns>This builder for method chaining</returns>
    public EncryptionOptions EncryptMetadata(bool encrypt = true)
    {
        _settings.EncryptMetadata = encrypt;
        return this;
    }

    /// <summary>
    /// Builds the final EncryptionSettings from this builder.
    /// </summary>
    /// <returns>The configured encryption settings</returns>
    internal EncryptionSettings Build()
    {
        _settings.Validate();
        return _settings.Clone();
    }
}