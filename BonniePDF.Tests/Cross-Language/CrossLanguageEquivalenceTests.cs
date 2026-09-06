using System.Text;
using BonniePDF.Tests.Accessibility;
using BonniePDF.Tests.Compliance;
using Xunit;

namespace BonniePDF.Tests.Cross_Language;

/// <summary>
/// Cross-language equivalence. The .NET and TS implementations
/// must produce structurally-equivalent PDF/UA output (not
/// byte-identical PDFs - object IDs differ between runs and between
/// languages - but the same structure-tree shape).
///
/// This test extracts a "canonical fingerprint" from a generated PDF
/// - a sorted list of <c>(role, count)</c> pairs for every
/// <c>StructElem</c> in the output - and asserts the expected
/// fingerprint for each of the three stage-1 exemplar fixtures. The
/// corresponding TS test in
/// <c>core/tests/cross-language-equivalence.test.ts</c> asserts the
/// SAME expected fingerprints, so if both sides pass the structure
/// trees match.
///
/// The fingerprint is derived from <c>/S /Role</c> occurrences, which
/// is stable regardless of object-ID allocation order. Test failures
/// in either repo flag a divergence.
/// </summary>
public class CrossLanguageEquivalenceTests
{
    private static readonly string[] Roles =
    {
        "Document", "Sect", "Art", "Div",
        "H", "H1", "H2", "H3", "H4", "H5", "H6", "P",
        "L", "LI", "Lbl", "LBody",
        "Table", "THead", "TBody", "TFoot", "TR", "TH", "TD",
        "Span", "Quote", "Code",
        "Figure", "Caption", "Form", "Formula",
        "Link", "Reference", "Note", "Annot",
        "TOC", "TOCI", "Index", "BlockQuote",
        "Artifact",
    };

    [Fact]
    public void BusinessInvoice_FingerprintMatchesExpected()
    {
        string pdf = Generate(Stage1CloseoutTests.BuildBusinessInvoiceDocument());
        IDictionary<string, int> fp = Fingerprint(pdf);

        // Same expectations as in the TS sibling test.
        Assert.Equal(new SortedDictionary<string, int>
        {
            ["Document"] = 1,
            ["H1"] = 1,
            ["P"] = 1,
            ["Table"] = 1,
            ["THead"] = 1,     // header row group (PDF/UA 7.2)
            ["TBody"] = 1,     // body row group
            ["TR"] = 4,        // 1 header + 3 body rows
            ["TH"] = 4,        // 4 header cells
            ["TD"] = 12,       // 3 body rows × 4 cells
            ["Link"] = 1,
        }, new SortedDictionary<string, int>(fp));
    }

    [Fact]
    public void DickensNovel_FingerprintMatchesExpected()
    {
        string pdf = Generate(Stage1CloseoutTests.BuildDickensNovelDocument());
        IDictionary<string, int> fp = Fingerprint(pdf);

        Assert.Equal(new SortedDictionary<string, int>
        {
            ["Document"] = 1,
            ["TOC"] = 1,
            ["TOCI"] = 3,
            ["Link"] = 3, // one per TOCI entry - PDF/UA 14.8.4.4.2 requires TOC links to be tagged
            ["H1"] = 3,
            ["P"] = 3,
        }, new SortedDictionary<string, int>(fp));
    }

    [Fact]
    public void MultiPageFlow_FingerprintRolesPresent()
    {
        // Exact role counts depend on Table.RepeatHeadersOnNewPage
        // semantics (re-emitted header rows produce extra TR/TH structs
        // per overflow boundary) and the per-page row-fit math, which
        // varies with font metrics. We assert the *set* of roles
        // matches between languages - that's what equivalence really
        // means - and require minimum counts that hold regardless of
        // pagination details.
        string pdf = Generate(Stage1CloseoutTests.BuildMultiPageFlowDocument());
        IDictionary<string, int> fp = Fingerprint(pdf);

        Assert.Equal(
            new SortedSet<string> { "Document", "Table", "THead", "TBody", "TR", "TH", "TD" },
            new SortedSet<string>(fp.Keys));

        // Body cells are deterministic: 80 rows × 3 columns = 240.
        Assert.Equal(240, fp["TD"]);
        Assert.Equal(1, fp["Document"]);
        Assert.Equal(1, fp["Table"]);
        // TR count = body rows (80) + at least one header render. Each
        // page overflow that re-emits headers adds 1 to TR and 3 to TH.
        Assert.True(fp["TR"] >= 81, $"expected >=81 TR rows, got {fp["TR"]}");
        Assert.True(fp["TH"] >= 3, $"expected >=3 TH cells, got {fp["TH"]}");
        // TH should always be a multiple of 3 (the 3-column header repeats whole).
        Assert.True(fp["TH"] % 3 == 0, $"TH count {fp["TH"]} is not a multiple of 3");
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string Generate(BonniePDF.Document doc)
    {
        using (doc)
        using (var ms = new MemoryStream())
        {
            doc.Save(ms, true);
            return Encoding.ASCII.GetString(ms.ToArray());
        }
    }

    /// <summary>
    /// Build a role → count fingerprint from a generated PDF by
    /// counting <c>/S /Role</c> occurrences across the byte stream.
    /// Object IDs are not part of the fingerprint, so the result is
    /// stable regardless of allocation order.
    /// </summary>
    private static IDictionary<string, int> Fingerprint(string pdf)
    {
        var result = new SortedDictionary<string, int>();
        foreach (string role in Roles)
        {
            int count = SubstringCounter.Count(pdf, $"/S /{role}");
            // Some role names are prefixes of others (e.g. "H" is a
            // prefix of "H1"). Subtract sub-role occurrences so the
            // count reflects only direct "/S /Role" matches.
            foreach (string longer in Roles)
            {
                if (longer.Length > role.Length && longer.StartsWith(role))
                    count -= SubstringCounter.Count(pdf, $"/S /{longer}");
            }
            if (count > 0) result[role] = count;
        }
        return result;
    }
}
