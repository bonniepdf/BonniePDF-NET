using System;
using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Text;
using BonniePDF.Security.Models;

namespace BonniePDF.Configuration;

/// <summary>
/// Configuration settings for PDF document creation and display
/// </summary>
public class DocumentSettings
{
    /// <summary>
    /// Creates a new instance of DocumentSettings with default values
    /// </summary>
    public DocumentSettings()
    {
    }

    /// <summary>
    /// Creates a new instance of DocumentSettings with the specified page size and margin
    /// </summary>
    public DocumentSettings(PdfSize pageSize, float margin)
    {
        PageSize = pageSize;
        Margin = margin;
    }

    /// <summary>
    /// Creates a new instance of DocumentSettings with the specified page size, orientation, and margin
    /// </summary>
    public DocumentSettings(PdfSize pageSize, PageOrientation orientation, float margin)
    {
        PageSize = pageSize;
        Orientation = orientation;
        Margin = margin;
    }

    /// <summary>
    /// Creates a new instance of DocumentSettings with the specified page size and orientation
    /// </summary>
    public DocumentSettings(PdfSize pageSize, PageOrientation orientation)
    {
        PageSize = pageSize;
        Orientation = orientation;
    }

    /// <summary>
    /// Template function for generating page headers.
    /// Called with the page number (1-based) and should return an IElement to render at the top of each page.
    /// The returned element is rendered at the top margin position.
    /// </summary>
    public Func<int, IElement?>? HeaderTemplate { get; set; }

    /// <summary>
    /// Template function for generating page footers.
    /// Called with the page number (1-based) and should return an IElement to render at the bottom of each page.
    /// The returned element is rendered at the bottom margin position.
    /// </summary>
    public Func<int, IElement?>? FooterTemplate { get; set; }

    /// <summary>
    /// Template function for the full-page <b>background</b> slot.
    /// Called with the page number (1-based); the returned element is
    /// painted <i>behind</i> all page content. Unlike
    /// <see cref="HeaderTemplate"/> / <see cref="FooterTemplate"/>, the
    /// background slot spans the <b>entire page and ignores the margins</b>,
    /// so bleed images, scanned letterhead, and certificate backgrounds
    /// reach the page edge. Rendered at the page origin (0, 0) with the
    /// full page width available.
    /// </summary>
    public Func<int, IElement?>? PageBackground { get; set; }

    /// <summary>
    /// Template function for the full-page <b>foreground</b> slot.
    /// Called with the page number (1-based); the returned element is
    /// painted <i>on top of</i> all page content (including headers and
    /// footers), making it ideal for "DRAFT"/"CONFIDENTIAL" overlays and
    /// stamps. Like <see cref="PageBackground"/> it spans the entire page
    /// and ignores the margins, rendered at the page origin (0, 0) with
    /// the full page width available.
    /// </summary>
    public Func<int, IElement?>? PageForeground { get; set; }

    /// <summary>
    /// The page size for the document (default: A4)
    /// </summary>
    public PdfSize PageSize { get; set; } = PdfSize.A4;

    /// <summary>
    /// Background colour painted behind every page. When set, the render
    /// pipeline fills the entire page (origin 0,0, full width and height,
    /// ignoring margins) with this colour before anything else is drawn, so it
    /// sits behind the <see cref="PageBackground"/> slot, headers, body
    /// content, and footers. Leave <c>null</c> for a transparent
    /// (viewer-default, usually white) background.
    ///
    /// This is the simple "page colour" shortcut; for textured or image
    /// backgrounds use <see cref="PageBackground"/> instead (or both —
    /// <c>PageColor</c> fills, <c>PageBackground</c> layers on top).
    /// </summary>
    public Color? PageColor { get; set; }

    /// <summary>
    /// The page orientation for the document (default: Portrait)
    /// </summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>
    /// The margin size in points for all pages (default: 50)
    /// </summary>
    public float Margin { get; set; } = 50;

    /// <summary>
    /// The default zoom level when the PDF is opened (default: FitPage)
    /// </summary>
    public ZoomLevel DefaultZoom { get; set; } = ZoomLevel.FitPage;

    /// <summary>
    /// Custom zoom level for ZoomLevel.Custom (1.0 = 100%, default: 1.0)
    /// </summary>
    public double CustomZoomLevel { get; set; } = 1.0;

    /// <summary>
    /// How the PDF document should be displayed when opened (default: UseNone)
    /// </summary>
    public PageMode PageMode { get; set; } = PageMode.UseNone;

    /// <summary>
    /// Whether to hide the toolbar in the PDF viewer (default: false)
    /// </summary>
    public bool HideToolbar { get; set; }

    /// <summary>
    /// Whether to hide the menubar in the PDF viewer (default: false)
    /// </summary>
    public bool HideMenubar { get; set; }

    /// <summary>
    /// Whether to hide the window UI in the PDF viewer (default: false)
    /// </summary>
    public bool HideWindowUI { get; set; }

    /// <summary>
    /// Whether to fit the document window to the first page (default: false)
    /// </summary>
    public bool FitWindow { get; set; }

    /// <summary>
    /// Whether to center the document window on screen (default: false)
    /// </summary>
    public bool CenterWindow { get; set; }

    /// <summary>
    /// Whether to display the document title in the title bar instead of filename (default: false)
    /// </summary>
    public bool DisplayDocTitle { get; set; }

    /// <summary>
    /// Gets or sets the PDF version for the output document (default: PDF 1.4).
    /// </summary>
    public PdfVersion Version { get; set; } = PdfVersion.Pdf14;

    /// <summary>
    /// Gets or sets the PDF/A archival conformance level. Default
    /// <see cref="PdfAConformance.None"/> - the document is a normal PDF.
    ///
    /// When set to any non-<see cref="PdfAConformance.None"/> value, the
    /// serializer auto-upgrades <see cref="Version"/> to PDF 1.7 (PDF/A
    /// emission requires PDF 1.7), embeds the sRGB ICC profile,
    /// emits the matching <c>/OutputIntents</c> entry, and writes a
    /// <c>pdfaid:part</c> / <c>pdfaid:conformance</c> XMP block.
    ///
    /// "a" levels (<see cref="PdfAConformance.PdfA1a"/>,
    /// <see cref="PdfAConformance.PdfA2a"/>,
    /// <see cref="PdfAConformance.PdfA3a"/>) require
    /// <see cref="Accessibility"/> = <see cref="AccessibilityMode.PdfUa1"/>;
    /// the serializer throws <c>PdfAConformanceException</c> otherwise.
    ///
    /// <b>Honest-claim behaviour:</b> setting this to a non-None
    /// value is a request, not a guarantee. The serializer suppresses
    /// the <c>pdfaid</c> XMP block and <c>/OutputIntents</c> entry
    /// whenever the document references any of the standard 14 fonts
    /// (Helvetica, Times-*, Courier-*, Symbol, ZapfDingbats), because
    /// PDF/A §6.3.4 requires every font to be embedded and BonniePDF
    /// does not bundle subset versions of those faces. To actually
    /// produce a PDF/A-conformant file, register a real TTF via
    /// <c>TtfFont.FromFile()</c> / <c>FontMetrics.RegisterFontAlias()</c>
    /// (or rely on the bundled Liberation faces) and use that for every
    /// text element.
    /// </summary>
    public PdfAConformance Conformance { get; set; } = PdfAConformance.None;

    /// <summary>
    /// Legacy boolean PDF/A switch. Maps onto <see cref="Conformance"/>:
    /// reading returns <c>true</c> whenever any PDF/A level is selected;
    /// writing <c>true</c> selects <see cref="PdfAConformance.PdfA2b"/>
    /// (the modern visual-reproducibility baseline) and writing
    /// <c>false</c> selects <see cref="PdfAConformance.None"/>.
    /// </summary>
    public bool PdfAEnabled
    {
        get => Conformance != PdfAConformance.None;
        set => Conformance = value ? PdfAConformance.PdfA2b : PdfAConformance.None;
    }

    /// <summary>
    /// Gets or sets the accessibility / tagged-PDF conformance mode.
    /// Default <see cref="AccessibilityMode.None"/> - the writer
    /// produces an untagged PDF and ignores all tagging machinery.
    /// Set to <see cref="AccessibilityMode.PdfUa1"/> to opt the
    /// document into PDF/UA-1 emission (structure tree, MCIDs, XMP
    /// conformance claim, <c>/Lang</c>, <c>/MarkInfo</c>).
    ///
    /// Orthogonal to <see cref="PdfAEnabled"/>: a document can be
    /// PDF/A, PDF/UA, both, or neither. See
    /// <c>the specification</c>.
    /// </summary>
    public AccessibilityMode Accessibility { get; set; } = AccessibilityMode.None;

    /// <summary>
    /// Document natural language as a BCP-47 tag (e.g. <c>"en-GB"</c>,
    /// <c>"de-DE"</c>, <c>"fr"</c>). Written to the catalog as
    /// <c>/Lang</c> when set; required for PDF/UA-1 conformance, where
    /// it tells assistive tech the language of unmarked content.
    /// Default <c>null</c> (no language declared).
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets the bookmark items for the document outline tree.
    /// </summary>
    public IList<BookmarkItem> Bookmarks { get; set; } = new List<BookmarkItem>();

    /// <summary>
    /// Gets whether the document has any bookmarks defined.
    /// </summary>
    public bool HasBookmarks => Bookmarks != null && Bookmarks.Count > 0;

    /// <summary>
    /// Internal destinations mapping a name to a 0-based page index.
    /// </summary>
    public IDictionary<string, int> NamedDestinations { get; set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Default text style applied to all text elements that have not had their style
    /// properties explicitly set. Use this to define a document-wide font/size/color
    /// baseline and avoid repeating style assignments on every element.
    ///
    /// <example>
    /// <code>
    /// settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[600]);
    /// </code>
    /// </example>
    /// </summary>
    public TextStyle DefaultTextStyle { get; set; }

    /// <summary>
    /// Gets or sets the watermark settings for the document.
    /// When null, no watermark is rendered.
    /// </summary>
    public WatermarkSettings Watermark { get; set; }

    /// <summary>
    /// Gets or sets the encryption settings for the document.
    /// When null, the document is not encrypted.
    /// </summary>
    public EncryptionSettings Encryption { get; set; }

    /// <summary>
    /// Gets whether encryption is enabled for this document.
    /// </summary>
    public bool IsEncrypted => Encryption != null;

    /// <summary>
    /// Page labelling configuration written as <c>/PageLabels</c> in the
    /// document catalog (PDF §12.4.2). Controls the number shown in the
    /// viewer's page counter and "go to page" dialog — not the printed text
    /// on the page itself.
    /// <para>
    /// Use when the document represents a subset of a larger work and the
    /// viewer counter should reflect the logical page numbering rather than
    /// the physical 1-based sheet count.
    /// </para>
    /// <example>
    /// <code>
    /// Settings = new DocumentSettings
    /// {
    ///     PageLabel = new PageLabel { StartAt = 42 }
    /// }
    /// // → viewer shows "42" on page 1, "43" on page 2, etc.
    /// </code>
    /// </example>
    /// <para>
    /// Note: the footer template still receives the physical page number.
    /// Offset it manually if you want the printed number to match:
    /// <c>FooterTemplate = n => new TextBlock { Text = (n + 41).ToString() }</c>
    /// </para>
    /// </summary>
    public PageLabel? PageLabel { get; set; }

    /// <summary>
    /// Gets the actual page size with orientation applied
    /// </summary>
    public PdfSize GetOrientedPageSize()
    {
        return PageSize.GetOrientedSize(Orientation);
    }
}