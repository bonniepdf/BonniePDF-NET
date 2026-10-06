using System;

namespace BonniePDF.Fluent;

/// <summary>
/// Internal bridge methods that make the fluent API chain pattern work.
/// </summary>
internal static class ContainerBridgeExtensions
{
    /// <summary>
    /// Sets the child of this container and returns the child if it's also a container (for chaining).
    /// This is the core method that enables: .Padding(10).Border(1).Text("Hello")
    /// </summary>
    internal static IContentContainer Element(this IContentContainer parent, IElement child)
    {
        if (parent.Child != null)
            throw new InvalidOperationException(
                "This container already has a child element. " +
                "Each container slot can only hold one child. " +
                "Use Column() or Row() to add multiple elements.");

        parent.Child = child;

        // If the child is also a content container, return it for continued chaining
        if (child is IContentContainer cc)
            return cc;

        // Otherwise return a dead-end (shouldn't happen with proper chaining)
        return parent;
    }

    /// <summary>
    /// Sets the child and returns the child element typed for further styling.
    /// Used by terminal methods like .Text() that return the concrete element type.
    /// </summary>
    internal static T Element<T>(this IContentContainer parent, T child) where T : IElement
    {
        if (parent.Child != null)
            throw new InvalidOperationException(
                "This container already has a child element. " +
                "Each container slot can only hold one child. " +
                "Use Column() or Row() to add multiple elements.");

        parent.Child = child;
        return child;
    }
}
