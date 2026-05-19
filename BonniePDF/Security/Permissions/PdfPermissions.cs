using System;

namespace BonniePDF.Security.Permissions;

/// <summary>
/// Flags enumeration for PDF document permissions.
/// These control what actions users can perform on the encrypted document.
/// </summary>
[Flags]
public enum PdfPermissions
{
    /// <summary>
    /// No permissions granted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allow printing the document.
    /// </summary>
    Print = 1 << 0,

    /// <summary>
    /// Allow modifying the document contents.
    /// </summary>
    Modify = 1 << 1,

    /// <summary>
    /// Allow copying text and graphics from the document.
    /// </summary>
    Copy = 1 << 2,

    /// <summary>
    /// Allow adding or modifying annotations and filling form fields.
    /// </summary>
    AnnotateAndFillForms = 1 << 3,

    /// <summary>
    /// Allow filling in interactive form fields (including signature fields).
    /// </summary>
    FillForms = 1 << 4,

    /// <summary>
    /// Allow extracting text and graphics for accessibility purposes.
    /// </summary>
    ExtractForAccessibility = 1 << 5,

    /// <summary>
    /// Allow assembling the document (insert, rotate, delete pages, bookmarks).
    /// </summary>
    Assemble = 1 << 6,

    /// <summary>
    /// Allow printing in high quality (when Print is also set).
    /// </summary>
    PrintHighQuality = 1 << 7,

    /// <summary>
    /// All permissions granted.
    /// </summary>
    All = Print | Modify | Copy | AnnotateAndFillForms | FillForms |
          ExtractForAccessibility | Assemble | PrintHighQuality
}