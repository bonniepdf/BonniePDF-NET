// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
namespace BonniePDF.Rendering.Svg.Sink;

/// <summary>An image for <see cref="IVectorSink.DrawImage"/>: encoded PNG bytes.</summary>
/// <remarks>
/// Deliberately not a copy of BonniePDF's <c>ImageInfo</c>, which is a
/// <em>decoded</em> image - pixel streams, colour space, ICC profile - and a
/// BonniePDF type the IR must not carry. The BonniePDF adapter decodes these bytes
/// with its own PNG reader, as it would any PNG a document embeds. See AGENTS.md,
/// deliberate deviations.
/// </remarks>
/// <param name="png">The encoded PNG.</param>
internal sealed class SinkImage(byte[] png)
{
    /// <summary>Gets the encoded PNG bytes.</summary>
    public byte[] Png { get; } = png;
}
