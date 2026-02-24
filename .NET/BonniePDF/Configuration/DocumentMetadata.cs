using System;
using System.Collections.Generic;

namespace BonniePDF.Configuration;

/// <summary>
/// Contains metadata information about a PDF document
/// </summary>
public class DocumentMetadata
{
    /// <summary>
    /// Creates a new instance of DocumentMetadata with default values
    /// </summary>
    public DocumentMetadata()
    {
    }

    /// <summary>
    /// Creates a new instance of DocumentMetadata with the specified title
    /// </summary>
    public DocumentMetadata(string title)
    {
        Title = title;
    }

    /// <summary>
    /// Creates a new instance of DocumentMetadata with the specified title and author
    /// </summary>
    public DocumentMetadata(string title, string author)
    {
        Title = title;
        Author = author;
    }

    /// <summary>
    /// The document's title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The name of the person who created the document
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// The subject of the document
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Keywords associated with the document. For PDF generation, these are stored as a
    /// comma-separated string. Use <see cref="SetKeywords"/> to set from a collection.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// The application that created the original content (e.g., "Microsoft Word")
    /// </summary>
    public string Creator { get; set; } = "BonniePDF";

    /// <summary>
    /// The application that converted the document to PDF
    /// </summary>
    public string Producer { get; set; } = "BonniePDF";

    /// <summary>
    /// The date and time the document was created, including timezone offset
    /// </summary>
    public DateTimeOffset? CreationDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the document was last modified, including timezone offset
    /// </summary>
    public DateTimeOffset? ModifiedDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates whether document-level trapping has been applied for professional printing.
    /// True means trapped, false means not trapped, null means not specified.
    /// </summary>
    public bool? Trapped { get; set; }

    /// <summary>
    /// A collection of custom metadata key-value pairs to embed in the document's
    /// Information Dictionary alongside the standard entries.
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Sets the Keywords property from a collection of individual tags,
    /// joining them as a comma-separated string.
    /// </summary>
    public void SetKeywords(IEnumerable<string> keywords)
    {
        if (keywords != null)
            Keywords = string.Join(", ", keywords);
    }
}
