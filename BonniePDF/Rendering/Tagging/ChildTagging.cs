using System.Runtime.InteropServices;
using BonniePDF.Elements;

namespace BonniePDF.Rendering.Tagging;

/// <summary>
/// Records whether <see cref="ChildTagging.BeginChildTag"/> opened an /Artifact
/// region or a structure-tagged marked-content scope, so
/// <see cref="ChildTagging.EndChildTag"/> can close the matching one. Both
/// <c>false</c> means the child declared no role and nothing was opened.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ChildTagToken
{
    /// <summary>Whether an /Artifact region was opened.</summary>
    public bool IsArtifact { get; init; }

    /// <summary>Whether a structure-tagged marked-content scope was opened.</summary>
    public bool TagThis { get; init; }
}

/// <summary>
/// Opens and closes the marked-content / structure scope for a child element
/// about to be rendered, exactly as <see cref="PdfRenderPipeline"/> does for
/// page-level elements. Shared by every layout container that renders children
/// itself (stacks, Container, …).
/// </summary>
public static class ChildTagging
{
    /// <summary>
    /// Opens the marked-content / structure scope for a child element about to be
    /// rendered.
    /// </summary>
    /// <remarks>
    /// Every layout container that renders children itself (stacks, Container, …)
    /// must call this (and <see cref="EndChildTag"/> in a <c>finally</c>) around
    /// each child, or a role-bearing leaf whose nearest container is one of those
    /// would emit untagged real content and fail PDF/UA 7.1 ("content shall be
    /// marked as Artifact or tagged as real content"). It is a no-op when tagging
    /// is off or the child declares no role, so untagged documents are unaffected.
    /// </remarks>
    public static ChildTagToken BeginChildTag(RenderContext ctx, IElement child)
    {
        TaggingContext tagging = ctx.Tagging;

        // Inside an /Artifact region (header/footer/slot/decorative subtree) nothing
        // may be tagged as real content - PDF/UA 7.1-2. The child renders as part of
        // the enclosing artifact, so open no structure scope.
        if (tagging != null && ctx.Canvas != null && ctx.Canvas.IsInsideArtifact())
            return new ChildTagToken { IsArtifact = false, TagThis = false };

        SemanticRole? role = tagging != null && child is IRoleProvider rp ? rp.SemanticRole : (SemanticRole?)null;
        // Artifact is a marked-content type, NOT a structure type: a decorative
        // child is wrapped in /Artifact and kept out of the structure tree.
        bool isArtifact = role == SemanticRole.Artifact;
        bool tagThis = role != null && !isArtifact;

        if (isArtifact)
        {
            ctx.Canvas.BeginArtifact();
        }
        else if (tagThis)
        {
            StructureElement elem = tagging!.Tree.Begin(role!.Value);
            if (child is IAltTextProvider alt && !string.IsNullOrEmpty(alt.AltText))
                tagging.Tree.SetAlt(alt.AltText);
            // Wire the struct element back to a Hyperlink so the serializer can write
            // /StructParent + /OBJR on the annotation.
            if (child is Core.Hyperlink hyperlink)
                hyperlink.LinkedStructElement = elem;
            if (role.Value.IsContainerRole())
                ctx.Canvas.BeginContainerContent(role.Value.ToPdfTag());
            else
                ctx.Canvas.BeginMarkedContent(role.Value.ToPdfTag());
        }

        return new ChildTagToken { IsArtifact = isArtifact, TagThis = tagThis };
    }

    /// <summary>Closes the scope opened by <see cref="BeginChildTag"/>. Safe to call with any token.</summary>
    public static void EndChildTag(RenderContext ctx, ChildTagToken token)
    {
        if (token.IsArtifact)
        {
            ctx.Canvas.EndArtifact();
        }
        else if (token.TagThis)
        {
            ctx.Canvas.EndMarkedContent();
            ctx.Tagging!.Tree.End();
        }
    }
}
