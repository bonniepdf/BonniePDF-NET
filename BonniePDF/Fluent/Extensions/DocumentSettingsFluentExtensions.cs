using System;

namespace BonniePDF.Fluent;

/// <summary>
/// Fluent extension methods for <see cref="DocumentSettings"/>.
/// </summary>
public static class DocumentSettingsFluentExtensions
{
    extension(DocumentSettings settings)
    {
        /// <summary>
        /// Configures a header template using the fluent builder API.
        /// The action receives the 1-based page number and a container to build the header content.
        /// </summary>
        public void Header(Action<int, IContentContainer> builder)
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
        public void Footer(Action<int, IContentContainer> builder)
        {
            settings.FooterTemplate = pageNumber =>
            {
                var slot = new ContentSlot();
                builder(pageNumber, slot);
                return slot.Child ?? new TextBlock { Text = "" };
            };
        }
    }
}
