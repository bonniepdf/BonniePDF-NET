namespace BonniePDF;

/// <summary>Controls how registered emoji fonts are painted.</summary>
public enum EmojiRendering
{
    /// <summary>Use monochrome font outlines (the default).</summary>
    Outline,
    /// <summary>Prefer COLRv0 colour layers or CBDT images, with outline fallback.</summary>
    Colour
}
