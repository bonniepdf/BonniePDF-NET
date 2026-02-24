using System.Collections.Generic;

namespace BonniePDF.Configuration;

/// <summary>
/// Represents a bookmark (outline) item in the PDF document.
/// Bookmarks allow users to click and navigate to specific pages.
/// </summary>
public class BookmarkItem
{
    /// <summary>
    /// The display title of the bookmark.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The 0-based page index this bookmark navigates to.
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Child bookmarks (nested outline items).
    /// </summary>
    public List<BookmarkItem> Children { get; } = new();
}
