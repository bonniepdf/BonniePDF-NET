using System.Collections;
using System.Reflection;
using BonniePDF.Configuration;
using BonniePDF.Elements;
using BonniePDF.Typography;

namespace BonniePDF.Tests.Definition;

/// <summary>
/// Compares two object graphs by their own state, private fields included, and returns the path of the first
/// difference (or null when they are equal). Two documents built from the same definition must be equal under it,
/// which is a stronger statement than "they render the same bytes": it also covers state that only affects a later
/// render (a cascade flag, an unused option).
///
/// Mirrors stateDiff in core/tests/definition/helpers.ts.
/// </summary>
internal static class StateComparer
{
    private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static string? Find(object? a, object? b) => Find(a, b, "$", new HashSet<(object, object)>(PairComparer.Instance));

    private static string? Find(object? a, object? b, string path, HashSet<(object, object)> seen)
    {
        if (ReferenceEquals(a, b)) return null;
        if (a is null || b is null) return $"{path}: {a ?? "null"} vs {b ?? "null"}";
        Type type = a.GetType();
        if (type != b.GetType()) return $"{path}: {type.Name} vs {b.GetType().Name}";
        if (type.IsPrimitive || type.IsEnum || a is string || a is decimal || a is DateTime || a is DateTimeOffset || a is TimeSpan)
            return Equals(a, b) ? null : $"{path}: {a} vs {b}";
        if (a is byte[] bytesA) return bytesA.AsSpan().SequenceEqual((byte[])b) ? null : $"{path}: bytes differ";
        if (a is Delegate)
        {
            var ta = (Func<int, IElement?>?)(a as Func<int, IElement?>);
            var tb = (Func<int, IElement?>?)(b as Func<int, IElement?>);
            bool builtA = PageTemplate.TryGetElements(ta, out PageTemplateElements ea);
            bool builtB = PageTemplate.TryGetElements(tb, out PageTemplateElements eb);
            if (builtA != builtB) return $"{path}: a template built from elements vs a function";
            return builtA ? Find(ea, eb, path + "(template)", seen) : null;
        }
        if (!seen.Add((a, b))) return null;

        if (a is IDictionary da)
        {
            var db = (IDictionary)b;
            if (da.Count != db.Count) return $"{path}: {da.Count} entries vs {db.Count}";
            foreach (DictionaryEntry entry in da)
            {
                if (!db.Contains(entry.Key)) return $"{path}: missing key {entry.Key}";
                string? d = Find(entry.Value, db[entry.Key], $"{path}[{entry.Key}]", seen);
                if (d is not null) return d;
            }
            return null;
        }
        if (a is IEnumerable ea2 && type.Namespace?.StartsWith("BonniePDF", StringComparison.Ordinal) != true)
        {
            object?[] left = ea2.Cast<object?>().ToArray();
            object?[] right = ((IEnumerable)b).Cast<object?>().ToArray();
            if (left.Length != right.Length) return $"{path}: length {left.Length} vs {right.Length}";
            for (int i = 0; i < left.Length; i++)
            {
                string? d = Find(left[i], right[i], $"{path}[{i}]", seen);
                if (d is not null) return d;
            }
            return null;
        }
        // Types from elsewhere (locks, async locals, tasks) have no state a document's meaning depends on.
        if (type.Namespace?.StartsWith("BonniePDF", StringComparison.Ordinal) != true && !type.IsGenericType) return null;
        if (type.Namespace?.StartsWith("BonniePDF", StringComparison.Ordinal) != true) return null;

        var skip = new HashSet<string>(StringComparer.Ordinal);
        if (a is DocumentMetadata ma && b is DocumentMetadata mb)
        {
            // The dates a metadata gave itself are the time it was made.
            if (ma.CreationDateIsBuiltIn && mb.CreationDateIsBuiltIn) { skip.Add("_creationDate"); }
            if (ma.ModifiedDateIsBuiltIn && mb.ModifiedDateIsBuiltIn) { skip.Add("_modifiedDate"); }
        }
        for (Type? t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            foreach (FieldInfo field in t.GetFields(AllInstance))
            {
                if (skip.Contains(field.Name)) continue;
                // A document's font faces are named after the document (spec 16 §4): their identity is family, weight, italic and bytes.
                if (a is DocumentFontFace && field.Name is "<FontName>k__BackingField" or "<Font>k__BackingField") continue;
                if (field.Name is "_serial" or "<Serial>k__BackingField") continue;
                string? d = Find(field.GetValue(a), field.GetValue(b), $"{path}.{field.Name}", seen);
                if (d is not null) return d;
            }
        }
        return null;
    }

    private sealed class PairComparer : IEqualityComparer<(object, object)>
    {
        public static readonly PairComparer Instance = new();

        public bool Equals((object, object) x, (object, object) y) => ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

        public int GetHashCode((object, object) obj) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Item1) * 31 + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Item2);
    }
}
