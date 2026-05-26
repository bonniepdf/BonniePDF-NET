using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Security.Encryption;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Interface for PDF writers, allowing both sync and async implementations
/// </summary>
public interface IPdfWriter
{
    /// <summary>
    /// Gets or sets the encryptor for this writer. When set, streams are encrypted.
    /// </summary>
    PdfEncryptor Encryptor { get; set; }

    /// <summary>
    /// Gets a new unique object ID for the PDF
    /// </summary>
    int GetNewId();

    /// <summary>
    /// Writes a PDF object with the specified ID and dictionary
    /// </summary>
    void WriteObject(int id, IDictionary<string, string> dict, string streamContent = null);

    /// <summary>
    /// Compresses and writes a PDF object
    /// </summary>
    void WriteCompressedObject(int id, IDictionary<string, string> dict, string content);

    /// <summary>
    /// Writes a PDF object with binary stream content
    /// </summary>
    void WriteBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent);

    /// <summary>
    /// Compresses binary data using FlateDecode and writes it as a PDF stream object
    /// </summary>
    void WriteCompressedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent);

    /// <summary>
    /// Writes a link annotation object for a hyperlink
    /// </summary>
    int WriteLinkAnnotation(int id, Hyperlink hyperlink, IList<int> pageIds = null, int? structParentKey = null);

    /// <summary>
    /// Writes viewer preferences to the document catalog.
    /// </summary>
    int WriteViewerPreferences(int id, DocumentSettings settings);

    /// <summary>
    /// Writes a PDF catalog object with catalog options
    /// </summary>
    void WriteCatalog(int id, int pagesId, int firstPageId, DocumentSettings settings, CatalogOptions options);

    /// <summary>
    /// Writes an uncompressed binary stream object, bypassing encryption.
    /// Used for XMP metadata which must not be encrypted.
    /// </summary>
    void WriteUnencryptedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent);

    /// <summary>
    /// Finalizes the PDF by writing the xref table and trailer
    /// </summary>
    void Close(int rootId, int infoId = 0);

    /// <summary>
    /// Writes the encryption dictionary object
    /// </summary>
    int WriteEncryptDictionary(int id);
}