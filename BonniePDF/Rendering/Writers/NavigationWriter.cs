using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Writes PDF navigation objects (Outlines/Bookmarks) to PDF.
/// </summary>
internal class NavigationWriter
{
    // Exactly one is non-null, depending on which constructor was used - see
    // StructureTreeWriter for the same pattern.
    private readonly IPdfWriter? _writer;
    private readonly IAsyncPdfWriter? _asyncWriter;

    public NavigationWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    public NavigationWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// Writes the bookmark/outline tree to the PDF document.
    /// This enables navigation via the PDF viewer's bookmark pane.
    /// </summary>
    /// <param name="rootId">The object ID for the root Outlines dictionary.</param>
    /// <param name="bookmarks">The list of bookmarks.</param>
    /// <param name="pageIds">List of object IDs for the document's pages.</param>
    /// <returns>The root ID if outlines were written, 0 otherwise.</returns>
    public int WriteOutlineTree(int rootId, IList<BookmarkItem> bookmarks, IList<int> pageIds)
    {
        if (bookmarks == null || bookmarks.Count == 0)
            return 0;

        // Flatten the bookmark tree to assign IDs
        List<(BookmarkItem item, int id, int parentId, int depth)> flatList = new();
        int nextId = rootId + 1; // rootId is reserved for the /Outlines root

        Flatten(bookmarks, rootId, 0);

        // Group children by parent
        Dictionary<int, List<int>> childrenByParent = new();
        foreach (var entry in flatList)
        {
            if (!childrenByParent.ContainsKey(entry.parentId))
                childrenByParent[entry.parentId] = new List<int>();
            childrenByParent[entry.parentId].Add(flatList.IndexOf(entry));
        }

        // Count total visible items
        int CountVisible(int parentId)
        {
            if (!childrenByParent.TryGetValue(parentId, out List<int>? value))
                return 0;
            int count = 0;
            foreach (int idx in value)
            {
                count++;
                count += CountVisible(flatList[idx].id);
            }
            return count;
        }

        int totalCount = CountVisible(rootId);

        // Write root /Outlines object
        List<int> rootChildren = childrenByParent.GetValueOrDefault(rootId) ?? new();
        Dictionary<string, string> rootDict = new()
        {
            ["/Type"] = "/Outlines",
            ["/Count"] = totalCount.ToString()
        };
        if (rootChildren.Count > 0)
        {
            rootDict["/First"] = $"{flatList[rootChildren[0]].id} 0 R";
            rootDict["/Last"] = $"{flatList[rootChildren[^1]].id} 0 R";
        }
        _writer!.WriteObject(rootId, rootDict);

        // Write each bookmark node
        for (int i = 0; i < flatList.Count; i++)
        {
            var (item, itemId, parentId, depth) = flatList[i];
            List<int> siblings = childrenByParent[parentId];
            int siblingIdx = siblings.IndexOf(i);

            Dictionary<string, string> dict = new()
            {
                ["/Title"] = $"({PdfSyntax.Escape(item.Title)})",
                ["/Parent"] = $"{parentId} 0 R"
            };

            // Destination
            int pageIdx = Math.Min(item.PageIndex, pageIds.Count - 1);
            pageIdx = Math.Max(pageIdx, 0);
            dict["/Dest"] = $"[ {pageIds[pageIdx]} 0 R /Fit ]";

            // Prev/Next siblings
            if (siblingIdx > 0)
                dict["/Prev"] = $"{flatList[siblings[siblingIdx - 1]].id} 0 R";
            if (siblingIdx < siblings.Count - 1)
                dict["/Next"] = $"{flatList[siblings[siblingIdx + 1]].id} 0 R";

            // Children
            List<int>? myChildren = childrenByParent.GetValueOrDefault(itemId);
            if (myChildren is { Count: > 0 })
            {
                int childCount = CountVisible(itemId);
                dict["/Count"] = childCount.ToString();
                dict["/First"] = $"{flatList[myChildren[0]].id} 0 R";
                dict["/Last"] = $"{flatList[myChildren[^1]].id} 0 R";
            }

            _writer!.WriteObject(itemId, dict);
        }

        return rootId;

        void Flatten(IList<BookmarkItem> items, int parentId, int depth)
        {
            foreach (BookmarkItem item in items)
            {
                int itemId = _writer!.GetNewId();
                flatList.Add((item, itemId, parentId, depth));
                if (item.Children.Count > 0)
                    Flatten(item.Children, itemId, depth + 1);
            }
        }
    }

    /// <summary>
    /// Writes the bookmark/outline tree to the PDF document asynchronously.
    /// This enables navigation via the PDF viewer's bookmark pane.
    /// </summary>
    /// <param name="rootId">The object ID for the root Outlines dictionary.</param>
    /// <param name="bookmarks">The list of bookmarks.</param>
    /// <param name="pageIds">List of object IDs for the document's pages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The root ID if outlines were written, 0 otherwise.</returns>
    public async Task<int> WriteOutlineTreeAsync(int rootId, IList<BookmarkItem> bookmarks, IList<int> pageIds, CancellationToken cancellationToken = default)
    {
        if (bookmarks == null || bookmarks.Count == 0)
            return 0;

        List<(BookmarkItem item, int id, int parentId, int depth)> flatList = new();

        Flatten(bookmarks, rootId, 0);

        Dictionary<int, List<int>> childrenByParent = new();
        foreach (var entry in flatList)
        {
            if (!childrenByParent.ContainsKey(entry.parentId))
                childrenByParent[entry.parentId] = new List<int>();
            childrenByParent[entry.parentId].Add(flatList.IndexOf(entry));
        }

        int CountVisible(int parentId)
        {
            if (!childrenByParent.TryGetValue(parentId, out List<int>? value))
                return 0;
            int count = 0;
            foreach (int idx in value)
            {
                count++;
                count += CountVisible(flatList[idx].id);
            }
            return count;
        }

        int totalCount = CountVisible(rootId);

        List<int> rootChildren = childrenByParent.GetValueOrDefault(rootId) ?? new();
        Dictionary<string, string> rootDict = new()
        {
            ["/Type"] = "/Outlines",
            ["/Count"] = totalCount.ToString()
        };
        if (rootChildren.Count > 0)
        {
            rootDict["/First"] = $"{flatList[rootChildren[0]].id} 0 R";
            rootDict["/Last"] = $"{flatList[rootChildren[^1]].id} 0 R";
        }
        await _asyncWriter!.WriteObjectAsync(rootId, rootDict, null, cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < flatList.Count; i++)
        {
            var (item, itemId, parentId, depth) = flatList[i];
            List<int> siblings = childrenByParent[parentId];
            int siblingIdx = siblings.IndexOf(i);

            Dictionary<string, string> dict = new()
            {
                ["/Title"] = $"({PdfSyntax.Escape(item.Title)})",
                ["/Parent"] = $"{parentId} 0 R"
            };

            int pageIdx = Math.Min(item.PageIndex, pageIds.Count - 1);
            pageIdx = Math.Max(pageIdx, 0);
            dict["/Dest"] = $"[ {pageIds[pageIdx]} 0 R /Fit ]";

            if (siblingIdx > 0)
                dict["/Prev"] = $"{flatList[siblings[siblingIdx - 1]].id} 0 R";
            if (siblingIdx < siblings.Count - 1)
                dict["/Next"] = $"{flatList[siblings[siblingIdx + 1]].id} 0 R";

            List<int>? myChildren = childrenByParent.GetValueOrDefault(itemId);
            if (myChildren is { Count: > 0 })
            {
                int childCount = CountVisible(itemId);
                dict["/Count"] = childCount.ToString();
                dict["/First"] = $"{flatList[myChildren[0]].id} 0 R";
                dict["/Last"] = $"{flatList[myChildren[^1]].id} 0 R";
            }

            await _asyncWriter!.WriteObjectAsync(itemId, dict, null, cancellationToken).ConfigureAwait(false);
        }

        return rootId;

        void Flatten(IList<BookmarkItem> items, int parentId, int depth)
        {
            foreach (BookmarkItem item in items)
            {
                int itemId = _asyncWriter!.GetNewId();
                flatList.Add((item, itemId, parentId, depth));
                if (item.Children.Count > 0)
                    Flatten(item.Children, itemId, depth + 1);
            }
        }
    }
}
