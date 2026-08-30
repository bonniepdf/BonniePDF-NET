using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Rendering;

namespace BonniePDF.Compliance;

// ---------------------------------------------------------------------------
// Accessibility / compliance preflight.
//
// A pure evaluator that walks an enriched LayoutSnapshot — the tree the
// LayoutRecorder captures during a render pass, carrying each element's intrinsic
// accessibility facts (NodeAccessibility) plus document-level context
// (SnapshotMeta) — and reports per-element findings against the PDF/UA-1 rules
// (ISO 14289-1).
//
// Because BonniePDF *generates* the document, it knows the semantics of every element at authoring time and can
// tell an author which element on which page is inaccessible and how to fix it,
// before the PDF is even written. External validators (veraPDF) can only inspect a
// finished file, and crucially they *pass* image-like content that silently fell
// back to /Artifact for want of alt text; this evaluator flags exactly that.
//
// Mirrors the TypeScript implementation; both conform to the shared
// compliance-preflight specification and the shared conformance fixtures.
//
// NOTE: authored without a .NET toolchain in the build environment — must be
// compiled and run against the shared fixtures before it is considered landed.
// ---------------------------------------------------------------------------

/// <summary>Severity of a compliance finding, most-severe first.</summary>
public enum ComplianceSeverity
{
    /// <summary>A PDF/UA-1 requirement is violated; the document is not conformant.</summary>
    Error,
    /// <summary>Likely-inaccessible authoring the spec strongly discourages.</summary>
    Warning,
    /// <summary>Advisory context (e.g. the document opted out of tagging entirely).</summary>
    Info
}

/// <summary>One reported accessibility/compliance issue, resolved to an element when applicable.</summary>
public sealed class ComplianceFinding
{
    /// <summary>Stable rule identifier, e.g. "figure-missing-alt".</summary>
    public string Code { get; init; } = "";
    public ComplianceSeverity Severity { get; init; }
    /// <summary>The standard the rule derives from. Currently always "PDF/UA-1".</summary>
    public string Standard { get; init; } = "PDF/UA-1";
    /// <summary>Spec clause citation, e.g. "ISO 14289-1:2014 §7.3".</summary>
    public string Clause { get; init; } = "";
    /// <summary>Human-readable, author-facing description of the problem.</summary>
    public string Message { get; init; } = "";
    /// <summary>How to fix it, phrased as an authoring action.</summary>
    public string? FixHint { get; init; }
    /// <summary>The snapshot node this finding is anchored to; null for document-level findings.</summary>
    public string? NodeId { get; init; }
    /// <summary>The element's resolved role, when node-scoped.</summary>
    public string? Role { get; init; }
    /// <summary>0-based page index of the element's first rectangle, for navigation.</summary>
    public int? Page { get; init; }
}

/// <summary>The result of evaluating a snapshot.</summary>
public sealed class ComplianceReport
{
    /// <summary>Findings, ordered document-level first, then in document order.</summary>
    public IReadOnlyList<ComplianceFinding> Findings { get; init; } = new List<ComplianceFinding>();
    /// <summary>True when there are no <see cref="ComplianceSeverity.Error"/> findings.</summary>
    public bool Passed { get; init; }
    public int ErrorCount { get; init; }
    public int WarningCount { get; init; }
    public int InfoCount { get; init; }
}

/// <summary>
/// Evaluates a layout snapshot against the PDF/UA-1 accessibility rules. Pure and
/// deterministic. See <c>the specification</c> for the rule set.
/// </summary>
public static class CompliancePreflight
{
    private const string Ua = "PDF/UA-1";

    /// <summary>
    /// Evaluates a layout snapshot and returns a per-element report. The rules are
    /// checked unconditionally (independent of AccessibilityMode), so the report
    /// answers "is this document accessible, and if not, where?" for any document.
    /// </summary>
    public static ComplianceReport Evaluate(LayoutSnapshot snapshot)
    {
        var findings = new List<ComplianceFinding>();
        SnapshotMeta? meta = snapshot.Meta;
        bool tagged = (meta?.AccessibilityMode ?? 0) != 0;

        // ── Document-level findings ────────────────────────────────────────────
        if (!tagged)
        {
            findings.Add(new ComplianceFinding
            {
                Code = "doc-not-tagged",
                Severity = ComplianceSeverity.Info,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §5",
                Message =
                    "Document is not tagged (AccessibilityMode.None). It carries no structure " +
                    "tree, so assistive technology cannot interpret its reading order. The " +
                    "findings below show what accessible authoring would require.",
                FixHint = "Set Settings.Accessibility = AccessibilityMode.PdfUa1 to emit a tagged PDF."
            });
        }

        if (string.IsNullOrEmpty(meta?.Language))
        {
            findings.Add(new ComplianceFinding
            {
                Code = "doc-lang-missing",
                Severity = tagged ? ComplianceSeverity.Error : ComplianceSeverity.Warning,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.2",
                Message =
                    "The document does not declare what language it is written in. Screen readers cannot " +
                    "choose the right pronunciation rules for the text.",
                FixHint = "Set Settings.Language to a BCP-47 tag, e.g. \"en-GB\"."
            });
        }

        if (string.IsNullOrEmpty(meta?.Title))
        {
            findings.Add(new ComplianceFinding
            {
                Code = "doc-title-missing",
                Severity = tagged ? ComplianceSeverity.Error : ComplianceSeverity.Warning,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.1",
                Message =
                    "The document has no title in its metadata. PDF/UA requires a title and that " +
                    "viewers display it instead of the file name.",
                FixHint = "Set the document metadata title (doc.Metadata.Title)."
            });
        }

        // ── Node walk (document order, pre-order DFS) ──────────────────────────
        int headingCount = 0;
        int lastHeadingLevel = 0;

        void Visit(LayoutNode node)
        {
            NodeAccessibility? a11y = node.A11y;
            if (a11y != null)
            {
                CheckFigure(node, a11y, findings);
                if (a11y.HeadingLevel is int level)
                {
                    headingCount++;
                    CheckHeadingOrder(node, a11y, level, headingCount, lastHeadingLevel, findings);
                    lastHeadingLevel = level;
                }
                if (string.Equals(a11y.Role, "Table", StringComparison.Ordinal)) CheckTableHeaders(node, findings);
            }
            foreach (LayoutNode child in node.Children) Visit(child);
        }
        Visit(snapshot.Root);

        if (headingCount == 0 && HasContent(snapshot.Root))
        {
            findings.Add(new ComplianceFinding
            {
                Code = "doc-no-headings",
                Severity = ComplianceSeverity.Info,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.4",
                Message =
                    "The document contains no headings. Headings give assistive-technology users " +
                    "a navigable outline of the content.",
                FixHint = "Use Heading elements to structure the document."
            });
        }

        int errorCount = findings.Count(f => f.Severity == ComplianceSeverity.Error);
        int warningCount = findings.Count(f => f.Severity == ComplianceSeverity.Warning);
        int infoCount = findings.Count(f => f.Severity == ComplianceSeverity.Info);

        return new ComplianceReport
        {
            Findings = findings,
            Passed = errorCount == 0,
            ErrorCount = errorCount,
            WarningCount = warningCount,
            InfoCount = infoCount
        };
    }

    private static int? PageOf(LayoutNode node) => node.Rects.Count > 0 ? node.Rects[0].Page : (int?)null;

    /// <summary>
    /// Image-like content (Image, QR code, barcode, …) that fell back to /Artifact
    /// for want of alt text is invisible to assistive tech — and external validators
    /// pass it silently. This is the highest-value rule in the set.
    /// </summary>
    private static void CheckFigure(LayoutNode node, NodeAccessibility a11y, List<ComplianceFinding> outList)
    {
        if (a11y.ImageLike != true) return;
        if (a11y.Artifact && a11y.Decorative != true)
        {
            outList.Add(new ComplianceFinding
            {
                Code = "figure-missing-alt",
                Severity = ComplianceSeverity.Error,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.3",
                Message =
                    $"{node.Type} has no alternate text, so it is exported as an artifact and " +
                    "assistive technology will skip it entirely.",
                FixHint = "Set the element's AltText to describe it, or mark it decorative if it conveys no meaning.",
                NodeId = node.Id,
                Role = a11y.Role,
                Page = PageOf(node)
            });
        }
        else if (string.Equals(a11y.Role, "Figure", StringComparison.Ordinal) && string.IsNullOrEmpty(a11y.Alt))
        {
            outList.Add(new ComplianceFinding
            {
                Code = "figure-missing-alt",
                Severity = ComplianceSeverity.Error,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.3",
                Message = $"{node.Type} is tagged as a figure but has no alternative text.",
                FixHint = "Set the element's AltText.",
                NodeId = node.Id,
                Role = a11y.Role,
                Page = PageOf(node)
            });
        }
    }

    /// <summary>
    /// Headings must not skip levels (H1→H3) and the outline should open at H1.
    /// Skipping breaks the navigable structure assistive-tech users rely on.
    /// </summary>
    private static void CheckHeadingOrder(
        LayoutNode node, NodeAccessibility a11y, int level, int ordinal, int lastLevel, List<ComplianceFinding> outList)
    {
        if (ordinal == 1)
        {
            if (level != 1)
            {
                outList.Add(new ComplianceFinding
                {
                    Code = "heading-first-not-h1",
                    Severity = ComplianceSeverity.Warning,
                    Standard = Ua,
                    Clause = "ISO 14289-1:2014 §7.4",
                    Message = $"The first heading is {a11y.Role}, not H1; a document outline should begin at H1.",
                    FixHint = "Start the heading hierarchy at level 1.",
                    NodeId = node.Id,
                    Role = a11y.Role,
                    Page = PageOf(node)
                });
            }
            return;
        }
        if (level > lastLevel + 1)
        {
            outList.Add(new ComplianceFinding
            {
                Code = "heading-skip",
                Severity = ComplianceSeverity.Warning,
                Standard = Ua,
                Clause = "ISO 14289-1:2014 §7.4",
                Message = $"Heading level jumps from H{lastLevel} to {a11y.Role}, skipping a level.",
                FixHint = $"Use H{lastLevel + 1} here, or add the intermediate heading level.",
                NodeId = node.Id,
                Role = a11y.Role,
                Page = PageOf(node)
            });
        }
    }

    /// <summary>A data table with no header cells (TH) cannot be navigated by assistive tech.</summary>
    private static void CheckTableHeaders(LayoutNode table, List<ComplianceFinding> outList)
    {
        if (HasRole(table, "TH")) return;
        outList.Add(new ComplianceFinding
        {
            Code = "table-no-header",
            Severity = ComplianceSeverity.Warning,
            Standard = Ua,
            Clause = "ISO 14289-1:2014 §7.5",
            Message = "Table has no header cells (TH); screen readers cannot associate data cells with headers.",
            FixHint = "Mark the header row/column cells as headers (e.g. Table.AddHeaderRow / cell.IsHeader).",
            NodeId = table.Id,
            Role = "Table",
            Page = PageOf(table)
        });
    }

    private static bool HasRole(LayoutNode node, string role)
    {
        foreach (LayoutNode child in node.Children)
        {
            if (string.Equals(child.A11y?.Role, role, StringComparison.Ordinal)) return true;
            if (HasRole(child, role)) return true;
        }
        return false;
    }

    private static bool HasContent(LayoutNode node)
    {
        foreach (LayoutNode child in node.Children)
        {
            if (child.A11y != null) return true;
            if (HasContent(child)) return true;
        }
        return false;
    }
}
