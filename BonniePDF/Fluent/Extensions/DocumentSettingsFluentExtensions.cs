using System;

namespace BonniePDF.Fluent;

/// <summary>
/// Fluent extension methods for <see cref="DocumentSettings"/>.
/// </summary>
public static class DocumentSettingsFluentExtensions
{
    /// <summary>
    /// Configures a header template using the fluent builder API.
    /// The action receives the 1-based page number and a container to build the header content.
    /// </summary>
    public static void Header(this DocumentSettings settings, Action<int, IContentContainer> builder)
    {
        settings.HeaderTemplate = pageNumber =>
        {
            var slot = new ContentSlot();
            builder(pageNumber, slot);
            return slot.Child ?? new TextBlock { Text = "" };
        };
    }

    /// <summary>
    /// Configures a footer template using the fluent builder API.
    /// The action receives the 1-based page number and a container to build the footer content.
    /// </summary>
    public static void Footer(this DocumentSettings settings, Action<int, IContentContainer> builder)
    {
        settings.FooterTemplate = pageNumber =>
        {
            var slot = new ContentSlot();
            builder(pageNumber, slot);
            return slot.Child ?? new TextBlock { Text = "" };
        };
    }
}
