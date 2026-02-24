using System;

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
    /// Keywords associated with the document
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// The application that created the document
    /// </summary>
    public string Creator { get; set; } = "BonniePDF";

    /// <summary>
    /// The application that converted the document to PDF
    /// </summary>
    public string Producer { get; set; } = "BonniePDF";

    /// <summary>
    /// The date the document was created
    /// </summary>
    public DateTime? CreationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date the document was last modified
    /// </summary>
    public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;
}