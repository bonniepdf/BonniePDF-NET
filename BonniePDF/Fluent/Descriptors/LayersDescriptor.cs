
namespace BonniePDF.Fluent;

/// <summary>
/// Fluent builder for the <see cref="Layers"/> inline z-stacking primitive,
/// used by <c>Container.Layers(...)</c>. Layers paint in declaration order:
/// background behind, the primary layer in the middle (it defines the flow
/// height), and foreground on top.
/// </summary>
public sealed class LayersDescriptor
{
    private readonly Layers _layers = new();

    /// <summary>Adds a layer painted behind the primary layer.</summary>
    public LayersDescriptor Background(IElement element)
    {
        _layers.AddBackground(element);
        return this;
    }

    /// <summary>
    /// Sets the primary content layer - the layer that determines the flow
    /// height of the stack.
    /// </summary>
    public LayersDescriptor PrimaryLayer(IElement element)
    {
        _layers.PrimaryLayer = element;
        return this;
    }

    /// <summary>Adds a layer painted on top of the primary layer.</summary>
    public LayersDescriptor Foreground(IElement element)
    {
        _layers.AddForeground(element);
        return this;
    }

    internal Layers Build() => _layers;
}
