namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// Carries the per-document tagging state through the render pipeline:
/// the in-memory <see cref="StructureTreeBuilder"/> being populated, and
/// a per-page MCID counter that the canvas uses to label each
/// marked-content sequence (this feature / this feature stage 0).
///
/// One instance per <c>Document</c> render; <see cref="RenderContext"/>
/// hands it to each page's <c>PdfCanvas</c>. When tagging is disabled,
/// the context is null and the canvas's
/// <c>BeginMarkedContent/EndMarkedContent</c> calls are no-ops.
/// </summary>
public sealed class TaggingContext
{
    private int _nextMcidOnPage;

    /// <summary>Creates a new tagging context wrapping <paramref name="tree"/>.</summary>
    public TaggingContext(StructureTreeBuilder tree)
    {
        Tree = tree;
    }

    /// <summary>The structure-tree builder being populated.</summary>
    public StructureTreeBuilder Tree { get; }

    /// <summary>
    /// Allocate the next MCID for the current page. MCIDs restart at 0
    /// on every new page per ISO 32000-1 §14.7.4.
    /// </summary>
    public int AllocateMcid() => _nextMcidOnPage++;

    /// <summary>
    /// Reset the per-page MCID counter to 0. Called by
    /// <c>RenderContext</c> on every <c>NewPage</c>.
    /// </summary>
    public void ResetPage() => _nextMcidOnPage = 0;
}
