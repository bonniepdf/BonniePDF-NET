namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// PDF structure-element role. Maps directly to one of the standard
/// structure types in ISO 32000-1 §14.8.4 (grouping, headings,
/// paragraph, list, table, inline, illustration, navigation, and
/// artifact).
///
/// String form via <see cref="SemanticRoleExtensions.ToPdfTag(SemanticRole)"/>
/// is the value that appears in PDF dictionaries (the leading <c>/</c>
/// is added by the writer, not stored here).
/// </summary>
public enum SemanticRole
{
    // ── Grouping elements (ISO 32000-1 §14.8.4.2) ───────────────────
    /// <summary>Document — top-level grouping element. The builder
    /// auto-creates a Document root.</summary>
    Document,
    /// <summary>Sect — a section.</summary>
    Sect,
    /// <summary>Art — an article.</summary>
    Art,
    /// <summary>Div — a generic division (no semantic meaning beyond
    /// "container").</summary>
    Div,
    /// <summary>BlockQuote — a block quotation.</summary>
    BlockQuote,
    /// <summary>Caption — a caption associated with a Figure / Formula /
    /// Form / Table.</summary>
    Caption,
    /// <summary>TOC — table of contents container.</summary>
    TOC,
    /// <summary>TOCI — a single TOC item.</summary>
    TOCI,
    /// <summary>Index — a structured index.</summary>
    Index,

    // ── Heading and paragraph-like (ISO 32000-1 §14.8.4.3) ──────────
    /// <summary>H — generic heading (level inferred from context).</summary>
    H,
    /// <summary>H1 — heading level 1.</summary>
    H1,
    /// <summary>H2 — heading level 2.</summary>
    H2,
    /// <summary>H3 — heading level 3.</summary>
    H3,
    /// <summary>H4 — heading level 4.</summary>
    H4,
    /// <summary>H5 — heading level 5.</summary>
    H5,
    /// <summary>H6 — heading level 6.</summary>
    H6,
    /// <summary>P — paragraph.</summary>
    P,

    // ── List (ISO 32000-1 §14.8.4.3) ────────────────────────────────
    /// <summary>L — a list.</summary>
    L,
    /// <summary>LI — a list item (the row).</summary>
    LI,
    /// <summary>Lbl — a list item label (bullet, number, …).</summary>
    Lbl,
    /// <summary>LBody — a list item body (the content of the item).</summary>
    LBody,

    // ── Tables (ISO 32000-1 §14.8.4.3) ──────────────────────────────
    /// <summary>Table — table grouping.</summary>
    Table,
    /// <summary>TR — table row.</summary>
    TR,
    /// <summary>TH — table header cell.</summary>
    TH,
    /// <summary>TD — table data cell.</summary>
    TD,
    /// <summary>THead — table header section.</summary>
    THead,
    /// <summary>TBody — table body section.</summary>
    TBody,
    /// <summary>TFoot — table footer section.</summary>
    TFoot,

    // ── Inline (ISO 32000-1 §14.8.4.4) ──────────────────────────────
    /// <summary>Span — generic inline grouping.</summary>
    Span,
    /// <summary>Quote — inline quotation.</summary>
    Quote,
    /// <summary>Note — footnote or endnote.</summary>
    Note,
    /// <summary>Reference — cross-reference.</summary>
    Reference,
    /// <summary>BibEntry — bibliography entry.</summary>
    BibEntry,
    /// <summary>Code — inline code or computer output.</summary>
    Code,
    /// <summary>Link — hyperlink wrapper. Used around the visible link
    /// text; the actual <c>/Link</c> annotation references this structure
    /// element via <c>/OBJR</c>.</summary>
    Link,
    /// <summary>Annot — generic annotation reference.</summary>
    Annot,

    // ── Illustration (ISO 32000-1 §14.8.4.5) ────────────────────────
    /// <summary>Figure — an illustration / image.</summary>
    Figure,
    /// <summary>Formula — a mathematical formula.</summary>
    Formula,
    /// <summary>Form — a form widget.</summary>
    Form,

    // ── Non-content ────────────────────────────────────────────────
    /// <summary>Artifact — content that is not part of the logical
    /// document order (page headers, footers, watermarks, decorative
    /// borders). Required by PDF/UA for any drawn content that is not
    /// otherwise tagged.</summary>
    Artifact,
}

/// <summary>
/// Helpers for projecting <see cref="SemanticRole"/> values to the
/// strings used in PDF structure dictionaries.
/// </summary>
public static class SemanticRoleExtensions
{
    /// <summary>
    /// Returns the PDF tag name for the role (e.g. <c>"H1"</c>,
    /// <c>"Figure"</c>). The leading <c>/</c> is added by the writer
    /// when the tag is emitted as a PDF name object.
    /// </summary>
    public static string ToPdfTag(this SemanticRole role)
    {
        // The enum names match the PDF spec exactly for every entry,
        // so ToString() suffices. Kept as an extension method so the
        // call sites read intent clearly and future spec aliases
        // (e.g. PDF/UA-2 renames) have one place to land.
        return role.ToString();
    }

    /// <summary>
    /// Returns <c>true</c> for grouping/container roles whose content is
    /// referenced by child structure elements rather than directly by an
    /// MCID. Container roles emit <c>BMC</c> (no MCID) in the content
    /// stream; leaf roles emit <c>BDC</c> with an MCID.
    /// </summary>
    public static bool IsContainerRole(this SemanticRole role)
    {
        return role switch
        {
            SemanticRole.Table => true,
            SemanticRole.TR => true,
            SemanticRole.THead => true,
            SemanticRole.TBody => true,
            SemanticRole.TFoot => true,
            SemanticRole.Document => true,
            SemanticRole.Sect => true,
            SemanticRole.Art => true,
            SemanticRole.Div => true,
            SemanticRole.TOC => true,
            SemanticRole.L => true,
            SemanticRole.LI => true,
            _ => false,
        };
    }
}
