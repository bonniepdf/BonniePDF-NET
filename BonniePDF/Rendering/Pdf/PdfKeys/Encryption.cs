namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries configuring the document security handler.</summary>
    internal static class Encryption
    {
        /// <summary>Name of the security handler used to encrypt the document.</summary>
        internal const string Handler = "/Filter";

        /// <summary>Encryption algorithm version selecting the document's encryption scheme.</summary>
        internal const string AlgorithmVersion = "/V";

        /// <summary>Revision of the standard security handler.</summary>
        internal const string HandlerRevision = "/R";

        /// <summary>Owner-password authentication entry in the encryption dictionary.</summary>
        internal const string OwnerEntry = "/O";

        /// <summary>User-password authentication entry in the encryption dictionary.</summary>
        internal const string UserEntry = "/U";

        /// <summary>Integer bit field specifying the document's permitted operations.</summary>
        internal const string Permissions = "/P";

        /// <summary>Length of the encryption key in bits.</summary>
        internal const string KeyLength = "/Length";

        /// <summary>File encryption key encrypted with the owner-password-derived key for AES-256.</summary>
        internal const string OwnerEncryptionEntry = "/OE";

        /// <summary>File encryption key encrypted with the user-password-derived key for AES-256.</summary>
        internal const string UserEncryptionEntry = "/UE";

        /// <summary>Encrypted permissions block used by the AES-256 security handler.</summary>
        internal const string EncryptedPermissions = "/Perms";

        /// <summary>Dictionary defining the document's named crypt filters.</summary>
        internal const string CryptFilters = "/CF";

        /// <summary>Name of the default crypt filter for stream data.</summary>
        internal const string StreamFilter = "/StmF";

        /// <summary>Name of the default crypt filter for strings.</summary>
        internal const string StringFilter = "/StrF";

        /// <summary>Whether the security handler encrypts document metadata streams.</summary>
        internal const string EncryptMetadata = "/EncryptMetadata";
    }
}
