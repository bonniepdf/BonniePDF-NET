using System;
using System.Runtime.CompilerServices;
using BonniePDF.Elements;

namespace BonniePDF.Configuration;

/// <summary>
/// The elements an element-based page template chooses among; see
/// <see cref="PageTemplate.FromElements"/>.
/// </summary>
/// <param name="Default">Used on every page no more specific member covers.</param>
/// <param name="FirstPage">Used on page 1 only.</param>
/// <param name="Odd">Used on odd pages (1, 3, 5, …) that <paramref name="FirstPage"/> does not cover.</param>
/// <param name="Even">Used on even pages (2, 4, 6, …).</param>
public sealed record PageTemplateElements(
    IElement? Default = null,
    IElement? FirstPage = null,
    IElement? Odd = null,
    IElement? Even = null);

/// <summary>
/// Builds page templates from elements instead of a function.
/// </summary>
/// <remarks>
/// A page template is a <c>Func&lt;int, IElement?&gt;</c>: given the 1-based page
/// number, the element to draw in a page-chrome slot on that page, or nothing. It is
/// the type of <see cref="DocumentSettings.HeaderTemplate"/>,
/// <see cref="DocumentSettings.FooterTemplate"/>, <see cref="DocumentSettings.PageBackground"/>
/// and <see cref="DocumentSettings.PageForeground"/>. A lambda satisfies it, and so does
/// the result of <see cref="FromElements"/>.
/// </remarks>
public static class PageTemplate
{
    /// <summary>
    /// Elements behind templates made by <see cref="FromElements"/>, keyed by the
    /// delegate. A side table rather than a wrapper type, so the template stays an
    /// ordinary delegate everywhere it is used.
    /// </summary>
    private static readonly ConditionalWeakTable<Func<int, IElement?>, PageTemplateElements> BuiltFrom = new();

    /// <summary>
    /// Builds a page template from elements.
    /// </summary>
    /// <remarks>
    /// For page <i>n</i> (1-based) the template returns the first present of
    /// <paramref name="firstPage"/> (page 1 only), then <paramref name="odd"/> or
    /// <paramref name="even"/> by the parity of <i>n</i>, then <paramref name="default"/>;
    /// when none applies it returns nothing for that page. At least one member is
    /// required.
    ///
    /// The same element instance is returned on every page it applies to, so it must be
    /// reusable across pages. <c>{{CurrentPage}}</c> and <c>{{TotalPages}}</c> in text let
    /// one element show a different number on each.
    /// </remarks>
    /// <example>
    /// <code>
    /// doc.Settings.FooterTemplate = PageTemplate.FromElements(
    ///     @default: new TextBlock { Text = "Page {{CurrentPage}} of {{TotalPages}}" },
    ///     firstPage: new TextBlock { Text = "Confidential" });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">No member was given.</exception>
    public static Func<int, IElement?> FromElements(
        IElement? @default = null,
        IElement? firstPage = null,
        IElement? odd = null,
        IElement? even = null)
    {
        if (@default is null && firstPage is null && odd is null && even is null)
            throw new ArgumentException("PageTemplate.FromElements needs at least one of default, firstPage, odd or even.", nameof(@default));

        Func<int, IElement?> template = pageNumber =>
        {
            if (pageNumber == 1 && firstPage is not null) return firstPage;
            IElement? byParity = pageNumber % 2 == 1 ? odd : even;
            return byParity ?? @default;
        };
        BuiltFrom.Add(template, new PageTemplateElements(@default, firstPage, odd, even));
        return template;
    }

    /// <summary>
    /// Reads back the elements a template was built from.
    /// </summary>
    /// <returns>
    /// <c>true</c> when <paramref name="template"/> was made by <see cref="FromElements"/>;
    /// <c>false</c> for a lambda or method group you wrote yourself, or <c>null</c>.
    /// </returns>
    /// <remarks>
    /// For tools that inspect a document's settings; it changes nothing about how the
    /// template renders.
    /// </remarks>
    public static bool TryGetElements(Func<int, IElement?>? template, out PageTemplateElements elements)
    {
        if (template is not null && BuiltFrom.TryGetValue(template, out PageTemplateElements? found))
        {
            elements = found;
            return true;
        }
        elements = new PageTemplateElements();
        return false;
    }
}
