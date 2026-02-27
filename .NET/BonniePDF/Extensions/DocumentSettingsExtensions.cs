using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Configuration;

namespace BonniePDF.Extensions;

/// <summary>
/// Extension methods for configuring PDF version, PDF/A, and bookmarks.
/// </summary>
public static class DocumentSettingsExtensions
{
    /// <summary>
    /// Sets the PDF version to 1.7 and enables all PDF 1.7 features.
    /// </summary>
    public static DocumentSettings AsPdf17(this DocumentSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Version = PdfVersion.Pdf17;
        return settings;
    }

    /// <summary>
    /// Enables PDF/A-1b archival mode. Automatically sets version to PDF 1.7.
    /// Cannot be used with encryption.
    /// </summary>
    public static DocumentSettings AsPdfA(this DocumentSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Version = PdfVersion.Pdf17;
        settings.PdfAEnabled = true;
        return settings;
    }

    /// <summary>
    /// Adds a bookmark (outline item) to the document.
    /// Automatically sets version to PDF 1.7.
    /// </summary>
    /// <param name="settings">The document settings</param>
    /// <param name="title">Display title of the bookmark</param>
    /// <param name="pageIndex">0-based page index this bookmark navigates to</param>
    /// <returns>The document settings for method chaining</returns>
    public static DocumentSettings AddBookmark(this DocumentSettings settings, string title, int pageIndex)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Version = PdfVersion.Pdf17;
        settings.Bookmarks.Add(new BookmarkItem { Title = title, PageIndex = pageIndex });
        return settings;
    }

    /// <summary>
    /// Adds multiple bookmarks to the document.
    /// Automatically sets version to PDF 1.7.
    /// </summary>
    public static DocumentSettings AddBookmarks(this DocumentSettings settings, IEnumerable<BookmarkItem> bookmarks)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (bookmarks == null) throw new ArgumentNullException(nameof(bookmarks));
        settings.Version = PdfVersion.Pdf17;
        settings.Bookmarks.ToList().AddRange(bookmarks);
        return settings;
    }

    /// <summary>
    /// Adds a text watermark with default settings (gray, 15% opacity, -45 degree rotation, 72pt).
    /// </summary>
    public static DocumentSettings WithWatermark(this DocumentSettings settings, string text)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Watermark = new WatermarkSettings { Text = text };
        return settings;
    }

    /// <summary>
    /// Adds a text watermark with custom opacity and rotation.
    /// </summary>
    public static DocumentSettings WithWatermark(this DocumentSettings settings, string text,
        float opacity, float rotationDegrees)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        settings.Watermark = new WatermarkSettings
        {
            Text = text,
            Opacity = opacity,
            RotationDegrees = rotationDegrees
        };
        return settings;
    }
}
