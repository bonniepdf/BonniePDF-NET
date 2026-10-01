using System.Runtime.CompilerServices;

namespace BonniePDF.Rendering;

/// <summary>
/// Source attribution for rendered elements.
/// </summary>
/// <remarks>
/// The registry is deliberately weak and separate from element instances: marking an object
/// cannot affect its public shape, serialization, layout, or lifetime. It is how an editor
/// integration, or anything that builds a document from data, says which piece of source an
/// element came from. The value is opaque to the engine, reaches the layout snapshot as
/// <see cref="LayoutNode.SourceId"/>, and is never written into PDF or SVG output.
/// </remarks>
internal static class DebugSource
{
    private static readonly ConditionalWeakTable<object, string> SourceIds = new();

    /// <summary>
    /// Associates an opaque source id with an object and returns the object unchanged.
    /// </summary>
    /// <remarks>
    /// The first id wins: the innermost construction site is the most precise origin, so a
    /// surrounding call that returns the same object does not replace it. A null value or an
    /// empty id is ignored.
    /// </remarks>
    public static T Mark<T>(T value, string sourceId) where T : class?
    {
        if (value is not null && !string.IsNullOrEmpty(sourceId))
            SourceIds.TryAdd(value, sourceId);
        return value;
    }

    /// <summary>The source id previously attached to <paramref name="value"/>, or null.</summary>
    public static string? Of(object value) =>
        SourceIds.TryGetValue(value, out string? sourceId) ? sourceId : null;
}
