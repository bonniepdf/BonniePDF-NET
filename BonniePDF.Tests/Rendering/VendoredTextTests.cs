using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// <c>BonniePDF/Typography/Engine/</c> is vendored: maintained upstream and copied here
/// by tooling, never edited in this repository. Its <c>VENDORED.json</c> lists a
/// SHA-256 for every file, taken with a leading BOM removed and line endings
/// normalised to LF, so a checkout's line endings do not matter. A change belongs
/// upstream; these checks fail on a hand edit, an added or removed file, or a copy
/// synced from uncommitted work. The TypeScript implementation checks its copy the
/// same way.
/// </summary>
public class VendoredTextTests
{
    private static string Folder([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "BonniePDF", "Typography", "Engine"));

    private static JsonElement Stamp() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder(), "VENDORED.json"))).RootElement;

    private static Dictionary<string, string> ListedFiles() =>
        Stamp().GetProperty("files").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);

    private static string Hash(string path)
    {
        string text = File.ReadAllText(path);
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];
        string normalised = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalised))).ToLowerInvariant();
    }

    [Fact]
    public void MatchesItsStampFileForFile()
    {
        foreach ((string file, string expected) in ListedFiles())
        {
            string actual = Hash(Path.Combine(Folder(), file));
            Assert.True(string.Equals(expected, actual, StringComparison.Ordinal),
                $"{file} was edited here; change it upstream and re-sync");
        }
    }

    [Fact]
    public void HoldsNoFileTheStampDoesNotList()
    {
        Dictionary<string, string> listed = ListedFiles();
        string[] unlisted = Directory.GetFiles(Folder(), "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(Folder(), path).Replace('\\', '/'))
            .Where(file => file != "VENDORED.json" && !listed.ContainsKey(file))
            .ToArray();
        Assert.Empty(unlisted);
    }

    [Fact]
    public void WasSyncedFromACommittedSource()
    {
        JsonElement source = Stamp().GetProperty("source");
        Assert.False(source.GetProperty("dirty").GetBoolean());
        Assert.Matches("^[0-9a-f]{40}$", source.GetProperty("commit").GetString());
    }
}
