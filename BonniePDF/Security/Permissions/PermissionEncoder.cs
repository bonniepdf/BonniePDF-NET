namespace BonniePDF.Security.Permissions;

/// <summary>
/// Encodes PDF permissions into the integer format required by the /P entry
/// in the encryption dictionary, following PDF Reference 1.4 specifications.
/// </summary>
internal static class PermissionEncoder
{
    /// <summary>
    /// Reserved bits that must always be set to 1 in the permissions value.
    /// Bits 1-2, 7-8, and 13-32 are reserved and must be 1 for PDF 1.4+.
    /// </summary>
    private const int ReservedBits = unchecked((int)0xFFFFF0C0);

    /// <summary>
    /// Encodes the permission flags into the signed 32-bit integer format
    /// required by the /P entry in the encryption dictionary.
    /// </summary>
    /// <param name="permissions">The permission flags to encode</param>
    /// <returns>The encoded permissions as a signed 32-bit integer</returns>
    public static int Encode(PdfPermissions permissions)
    {
        // Start with all reserved bits set to 1
        int result = ReservedBits;

        // Set the permission bits based on the flags
        if (permissions.HasFlag(PdfPermissions.Print))
            result |= 1 << 2; // Bit 3

        if (permissions.HasFlag(PdfPermissions.Modify))
            result |= 1 << 3; // Bit 4

        if (permissions.HasFlag(PdfPermissions.Copy))
            result |= 1 << 4; // Bit 5

        if (permissions.HasFlag(PdfPermissions.AnnotateAndFillForms))
            result |= 1 << 5; // Bit 6

        if (permissions.HasFlag(PdfPermissions.FillForms))
            result |= 1 << 8; // Bit 9

        if (permissions.HasFlag(PdfPermissions.ExtractForAccessibility))
            result |= 1 << 9; // Bit 10

        if (permissions.HasFlag(PdfPermissions.Assemble))
            result |= 1 << 10; // Bit 11

        if (permissions.HasFlag(PdfPermissions.PrintHighQuality))
            result |= 1 << 11; // Bit 12

        return result;
    }
}