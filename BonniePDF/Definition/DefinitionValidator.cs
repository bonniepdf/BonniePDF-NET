using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BonniePDF;

/// <summary>Checks a definition completely against the format and the reader's limits. Constructs nothing.</summary>
internal sealed partial class DefinitionValidator
{
    private static readonly DescriptionData D = ElementDescription.Data;

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_.:-]{0,127}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"^\s*[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?\s*(?:pt|in|cm|mm)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LengthToken();

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex HexColor();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}[Tt ]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:[Zz]|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DateTimePattern();

    [GeneratedRegex(@"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Base64Pattern();

    [GeneratedRegex(@"^[0-9a-f]{64}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Sha256Pattern();

    private static readonly string[] SideKeys = ["top", "right", "bottom", "left"];
    private static readonly string[] TableSlots = ["header", "rows", "footer"];
    private static readonly string[] ChartDuplicateKeys = ["title", "xAxis", "yAxis"];

    private sealed record Ref(string Kind, JsonElement Value, string Path, string? Id);

    private readonly DefinitionValidateOptions _options;
    private readonly DefinitionLimits _limits;
    private readonly List<DefinitionIssue> _errors = [];
    private readonly List<DefinitionIssue> _warnings = [];
    private readonly Dictionary<string, string> _ids = new(StringComparer.Ordinal);
    private readonly HashSet<string> _anchors = new(StringComparer.Ordinal);
    private readonly HashSet<string> _tocIdentifiers = new(StringComparer.Ordinal);
    private readonly List<Ref> _refs = [];
    private readonly List<string> _listTypes = [];
    private HashSet<string> _resourceNames = new(StringComparer.Ordinal);
    private bool _newerMinor;
    private bool _aborted;
    private int _pageCount;
    private int _nodeCount;
    private int _chartPoints;
    private long _resourceBytes;

    public DefinitionValidator(DefinitionValidateOptions options)
    {
        _options = options;
        _limits = options.Limits ?? new DefinitionLimits();
    }

    public IReadOnlyList<DefinitionIssue> Errors => _errors;

    public IReadOnlyList<DefinitionIssue> Warnings => _warnings;

    // ── reporting ────────────────────────────────────────────────────────────

    private static string Escape(string key) => key.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    private void Error(string path, string code, string message, string? id = null, string? expected = null, string? found = null) =>
        _errors.Add(new DefinitionIssue(path, code, message) { Id = id, Expected = expected, Found = found });

    private void Warn(string path, string code, string message, string? id = null) =>
        _warnings.Add(new DefinitionIssue(path, code, message) { Id = id });

    private void Unrecognised(string path, string what, string code, string? id = null)
    {
        if (_newerMinor)
        {
            string message = $"{what} is not recognised by this release, which reads definitions up to 1.{DocumentDefinition.SupportedVersions.MaxMinor}.";
            if (_options.BestEffort) Warn(path, "newer-minor", message, id);
            else Error(path, "newer-minor", message, id);
        }
        else if (string.Equals(code, "unknown-key", StringComparison.Ordinal))
        {
            string message = $"{what} is not a key of this object; it is ignored.";
            if (_options.Strict) Error(path, code, message, id);
            else Warn(path, code, message, id);
        }
        else
        {
            Error(path, code, $"{what} is not recognised.", id);
        }
    }

    private void LimitExceeded(string path, string what)
    {
        if (!_aborted) Error(path, "limit-exceeded", $"{what} exceeds the limit for this reader.");
        _aborted = true;
    }

    private static string TypeName(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Null => "null",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.Number => "number",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        _ => "object",
    };

    private static bool IsNumber(JsonElement v) => v.ValueKind == JsonValueKind.Number;

    private static double Num(JsonElement v) => v.GetDouble();

    private static bool Has(JsonElement obj, string key) => obj.TryGetProperty(key, out _);

    // ── entry ────────────────────────────────────────────────────────────────

    public void Run(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            Error("", "invalid-json", $"A definition is a JSON object; found {TypeName(root)}.");
            return;
        }
        if (!CheckVersion(root)) return;
        ValidateShape(root, D.Root, "", null, "unknown-key", 0);
        if (_aborted) return;
        CheckRoot(root);
        ResolveRefs();
    }

    private bool CheckVersion(JsonElement root)
    {
        bool hasVersion = root.TryGetProperty("version", out JsonElement version);
        bool hasMinor = root.TryGetProperty("minorVersion", out JsonElement minor);
        if (!hasVersion || !hasMinor)
        {
            Error("", "missing-version", "A definition needs both `version` and `minorVersion`.");
            return false;
        }
        foreach ((string key, JsonElement v) in new[] { ("version", version), ("minorVersion", minor) })
        {
            if (!IsInteger(v) || Num(v) < 0)
            {
                Error($"/{key}", "invalid-type", $"`{key}` is a non-negative integer; found {v.GetRawText()}.");
                return false;
            }
        }

        DefinitionVersions supported = DocumentDefinition.SupportedVersions;
        if (Num(version) != supported.Version)
        {
            Error("/version", "unsupported-version",
                $"Version {version.GetRawText()} is not supported; this release reads version {supported.Version}, minor versions {supported.MinMinor} to {supported.MaxMinor}.",
                expected: supported.Version.ToString(CultureInfo.InvariantCulture), found: version.GetRawText());
            return false;
        }
        _newerMinor = Num(minor) > supported.MaxMinor;
        return true;
    }

    private static bool IsInteger(JsonElement v) => IsNumber(v) && Math.Floor(Num(v)) == Num(v);

    public void Finish()
    {
        if (_newerMinor && _errors.TrueForAll(e => !string.Equals(e.Code, "newer-minor", StringComparison.Ordinal)))
        {
            Warn("/minorVersion", "newer-minor",
                $"The document is marked 1.x newer than this release reads (1.{DocumentDefinition.SupportedVersions.MaxMinor}), but nothing in it needed anything newer.");
        }
        else if (_newerMinor && _options.BestEffort)
        {
            Warn("/minorVersion", "newer-minor", "The document was written for a newer minor version; unrecognised items were ignored.");
        }
    }

    // ── root ─────────────────────────────────────────────────────────────────

    private void CheckRoot(JsonElement root)
    {
        if (root.TryGetProperty("resources", out JsonElement resources) && resources.ValueKind == JsonValueKind.Object)
            _resourceNames = new HashSet<string>(resources.EnumerateObject().Select(p => p.Name), StringComparer.Ordinal);

        if (root.TryGetProperty("pages", out JsonElement pages) && pages.ValueKind == JsonValueKind.Array)
        {
            _pageCount = pages.GetArrayLength();
            if (_pageCount == 0) Error("/pages", "invalid-value", "A definition needs at least one page.", expected: "at least one page");
            if (_pageCount > _limits.Pages) LimitExceeded("/pages", "The number of pages");
        }

        if (resources.ValueKind == JsonValueKind.Object) CheckResources(resources);
        if (root.TryGetProperty("attachments", out JsonElement attachments) && attachments.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement a in attachments.EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.Object && a.TryGetProperty("resource", out JsonElement r) && r.ValueKind == JsonValueKind.String)
                    _refs.Add(new Ref("resource", r, $"/attachments/{i}/resource", null));
                i++;
            }
        }
        CheckSettingsRefs(root);
        CheckSettingsRules(root);
    }

    private void CheckResources(JsonElement resources)
    {
        var fontIdentities = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty entry in resources.EnumerateObject())
        {
            JsonElement value = entry.Value;
            if (value.ValueKind != JsonValueKind.Object) continue;
            if (value.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.String)
            {
                string text = data.GetString()!;
                long padding = text.EndsWith("==", StringComparison.Ordinal) ? 2 : text.EndsWith('=') ? 1 : 0;
                long bytes = text.Length / 4L * 3 - padding;
                _resourceBytes += bytes;
                if (bytes > _limits.ResourceBytes) LimitExceeded($"/resources/{Escape(entry.Name)}/data", "A resource");
                if (_resourceBytes > _limits.ResourceBytesTotal) LimitExceeded("/resources", "The total size of resources");
            }
            if (value.TryGetProperty("kind", out JsonElement kind) && kind.ValueKind == JsonValueKind.String && kind.GetString() == "font"
                && value.TryGetProperty("family", out JsonElement family) && family.ValueKind == JsonValueKind.String)
            {
                string weight = value.TryGetProperty("weight", out JsonElement w)
                    ? (w.ValueKind == JsonValueKind.Number ? w.GetRawText() : w.ValueKind == JsonValueKind.String ? w.GetString()! : "auto")
                    : "auto";
                bool italic = value.TryGetProperty("italic", out JsonElement it) && it.ValueKind == JsonValueKind.True;
                string identity = $"{family.GetString()!.Trim().ToLowerInvariant()}:{weight}:{(italic ? 'i' : 'n')}";
                if (fontIdentities.TryGetValue(identity, out string? earlier))
                    Error($"/resources/{Escape(entry.Name)}", "duplicate-font-face", $"Resource `{entry.Name}` has the same family, weight and italic as `{earlier}`.");
                else fontIdentities[identity] = entry.Name;
            }
        }
    }

    /// <summary>
    /// The cross-key rules of <c>settings</c> (spec 15 §5.2), from the description. A combination the document
    /// would refuse at generation is refused here, so a definition that reads always generates.
    /// </summary>
    private void CheckSettingsRules(JsonElement root)
    {
        if (!root.TryGetProperty("settings", out JsonElement settings) || settings.ValueKind != JsonValueKind.Object) return;
        SettingsRulesData data = ElementDescription.Data.SettingsRules;
        var raised = new Dictionary<string, string>(StringComparer.Ordinal);

        string? Lookup(string key)
        {
            if (raised.TryGetValue(key, out string? r)) return r;
            JsonElement value = settings;
            foreach (string part in key.Split('.'))
            {
                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value)) return DefaultOf(key);
            }
            return value.ValueKind == JsonValueKind.String ? value.GetString() : DefaultOf(key);
        }
        string? DefaultOf(string key) => data.Defaults.TryGetValue(key, out string? d) ? d : null;

        bool Given(string key)
        {
            JsonElement value = settings;
            foreach (string part in key.Split('.'))
                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(part, out value)) return false;
            return value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => false,
                JsonValueKind.Array => value.GetArrayLength() > 0,
                _ => true,
            };
        }

        bool Holds(SettingsCondition c)
        {
            if (c.Present is bool wanted) return Given(c.Key) == wanted;
            string? value = Lookup(c.Key);
            if (c.In != null) return value != null && c.In.Contains(value);
            if (c.NotIn != null) return !(value != null && c.NotIn.Contains(value));
            return true;
        }

        foreach (SettingsRule rule in data.Rules)
        {
            if (!rule.When.TrueForAll(Holds)) continue;
            if (rule.Outcome == "sets")
            {
                foreach (KeyValuePair<string, string> pair in rule.Set ?? []) raised[pair.Key] = pair.Value;
            }
            else if (!(rule.Require ?? []).TrueForAll(Holds))
            {
                Error("/settings", "incompatible-settings", rule.Text);
            }
        }
    }

    private void CheckSettingsRefs(JsonElement root)
    {
        if (!root.TryGetProperty("settings", out JsonElement settings) || settings.ValueKind != JsonValueKind.Object) return;
        if (settings.TryGetProperty("bookmarks", out JsonElement bookmarks)) WalkBookmarks(bookmarks, "/settings/bookmarks");
        if (settings.TryGetProperty("namedDestinations", out JsonElement destinations) && destinations.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty p in destinations.EnumerateObject())
                if (IsNumber(p.Value)) _refs.Add(new Ref("pageIndex", p.Value, $"/settings/namedDestinations/{Escape(p.Name)}", null));
        }
    }

    private void WalkBookmarks(JsonElement list, string path)
    {
        if (list.ValueKind != JsonValueKind.Array) return;
        int i = 0;
        foreach (JsonElement b in list.EnumerateArray())
        {
            if (b.ValueKind == JsonValueKind.Object)
            {
                if (b.TryGetProperty("pageIndex", out JsonElement index) && IsNumber(index))
                    _refs.Add(new Ref("pageIndex", index, $"{path}/{i}/pageIndex", null));
                if (b.TryGetProperty("children", out JsonElement children)) WalkBookmarks(children, $"{path}/{i}/children");
            }
            i++;
        }
    }

    private void ResolveRefs()
    {
        foreach (Ref r in _refs)
        {
            bool ok = true;
            string message = "";
            switch (r.Kind)
            {
                case "resource":
                    ok = r.Value.ValueKind == JsonValueKind.String && _resourceNames.Contains(r.Value.GetString()!);
                    message = $"No resource named {r.Value.GetRawText()}.";
                    break;
                case "anchor":
                    ok = r.Value.ValueKind == JsonValueKind.String && _anchors.Contains(r.Value.GetString()!);
                    message = $"No Section or Heading defines the anchor {r.Value.GetRawText()}.";
                    break;
                case "tocIdentifier":
                    ok = r.Value.ValueKind == JsonValueKind.String && _tocIdentifiers.Contains(r.Value.GetString()!);
                    message = $"No Heading has the tocIdentifier {r.Value.GetRawText()}.";
                    break;
                case "pageIndex":
                    ok = IsNumber(r.Value) && Num(r.Value) < _pageCount;
                    message = $"Page index {r.Value.GetRawText()} is beyond the last page ({_pageCount} pages).";
                    break;
            }
            if (!ok) Error(r.Path, "dangling-reference", message, r.Id);
        }
    }

    // ── shapes and values ────────────────────────────────────────────────────

    private bool ValidateShape(JsonElement value, ShapeSpec spec, string path, string? owner, string unknownCode, int depth)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            Error(path, "invalid-type", $"Expected an object; found {TypeName(value)}.", owner);
            return false;
        }
        Dictionary<string, KeySpec> keys = ElementDescription.KeysOf(spec);

        foreach (JsonProperty p in value.EnumerateObject())
        {
            string key = p.Name;
            if (key.StartsWith('_') || string.Equals(key, "$schema", StringComparison.Ordinal)) continue;
            if (spec.Secrets is { } secrets && secrets.Contains(key))
            {
                Error($"{path}/{Escape(key)}", "secret-in-definition",
                    $"`{key}` is a secret and is never stored in a definition; the host supplies it when reading.", owner);
                continue;
            }
            if (!keys.TryGetValue(key, out KeySpec? keySpec))
            {
                Unrecognised($"{path}/{Escape(key)}", $"`{key}`", unknownCode, owner);
                continue;
            }
            CheckValue(keySpec, p.Value, $"{path}/{Escape(key)}", owner, depth);
            if (_aborted) return true;
        }

        foreach ((string key, KeySpec keySpec) in keys)
        {
            if (keySpec.Required && !value.TryGetProperty(key, out _))
                Error(path, "missing-required", $"Missing required key `{key}`.", owner, expected: key);
        }
        CheckGroups(value, spec, path, owner);
        return true;
    }

    private void CheckGroups(JsonElement value, ShapeSpec spec, string path, string? owner)
    {
        foreach (List<string> group in spec.ExactlyOne ?? [])
        {
            int present = group.Count(k => Has(value, k));
            if (present != 1)
                Error(path, "exactly-one-of", $"Give exactly one of {string.Join(", ", group.Select(k => $"`{k}`"))}; found {present}.", owner);
        }
        foreach (List<string> group in spec.Conflicts ?? [])
        {
            List<string> present = group.Where(k => Has(value, k)).ToList();
            if (present.Count > 1)
                Error(path, "conflicting-keys", $"{string.Join(" and ", present.Select(k => $"`{k}`"))} cannot both be set.", owner);
        }
        if (spec.AtLeastOne && !spec.Keys.Keys.Any(k => Has(value, k)))
            Error(path, "invalid-value", $"Give at least one of {string.Join(", ", spec.Keys.Keys.Select(k => $"`{k}`"))}.", owner);
    }

    private void CheckValue(KeySpec spec, JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            Error(path, "invalid-value", "null is not a value in a definition; leave the key out instead.", owner);
            return;
        }
        CheckKind(spec.Kind, spec, value, path, owner, depth);
    }

    private void WrongType(string path, string expected, JsonElement value, string? owner) =>
        Error(path, "invalid-type", $"Expected {expected}; found {TypeName(value)}.", owner, expected, TypeName(value));

    private void BadValue(string path, string message, string? owner, string? expected = null) =>
        Error(path, "invalid-value", message, owner, expected);

    private void CheckKind(string kind, KeySpec spec, JsonElement value, string path, string? owner, int depth)
    {
        int colon = kind.IndexOf(':', StringComparison.Ordinal);
        string head = colon < 0 ? kind : kind[..colon];
        string arg = colon < 0 ? "" : kind[(colon + 1)..];
        switch (head)
        {
            case "string":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a string", value, owner);
                else if (value.GetString()!.Length > _limits.StringLength) LimitExceeded(path, "A string");
                return;
            case "boolean":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) WrongType(path, "a boolean", value, owner);
                return;
            case "number":
            case "integer":
                CheckNumber(head, spec, value, path, owner);
                return;
            case "integerOrUnlimited":
                if (value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), "unlimited", StringComparison.Ordinal)) return;
                if (value.ValueKind == JsonValueKind.String) BadValue(path, $"{JsonSerializer.Serialize(value.GetString())} is not \"unlimited\" or an integer.", owner, "an integer or \"unlimited\"");
                else CheckNumber("integer", spec, value, path, owner);
                return;
            case "length":
                CheckLength(value, path, owner);
                return;
            case "spacing":
                CheckSpacing(value, path, owner);
                return;
            case "border":
                CheckBorder(value, path, owner);
                return;
            case "color":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a colour string", value, owner);
                else if (!IsColor(value.GetString()!))
                    BadValue(path, $"{JsonSerializer.Serialize(value.GetString())} is not a palette token ('slate-800') or a hex colour ('#1e293b').", owner, "palette token or #hex");
                return;
            case "pageSize":
                CheckPageSize(value, path, owner);
                return;
            case "fontWeight":
                if (IsNumber(value))
                {
                    if (!IsInteger(value) || Num(value) < 1 || Num(value) > 1000) BadValue(path, $"Font weight {value.GetRawText()} is outside 1 to 1000.", owner, "1 to 1000");
                }
                else if (value.ValueKind == JsonValueKind.String) CheckEnum("FontWeightToken", value.GetString()!, path, owner);
                else WrongType(path, "a weight token or a number", value, owner);
                return;
            case "decoration":
                CheckTextDecoration(value, path, owner);
                return;
            case "permissions":
                CheckPermissions(value, path, owner);
                return;
            case "enum":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a token string", value, owner);
                else CheckEnum(arg, value.GetString()!, path, owner);
                return;
            case "dateTime":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a date-time string", value, owner);
                else if (!DateTimePattern().IsMatch(value.GetString()!)) BadValue(path, $"{JsonSerializer.Serialize(value.GetString())} is not an RFC 3339 date-time with an offset.", owner, "RFC 3339 with offset");
                return;
            case "base64":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a base64 string", value, owner);
                else if (!Base64Pattern().IsMatch(value.GetString()!)) BadValue(path, "Not standard, padded base64.", owner, "RFC 4648 base64");
                return;
            case "sha256":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a hex string", value, owner);
                else if (!Sha256Pattern().IsMatch(value.GetString()!)) BadValue(path, "Expected 64 lower-case hex digits.", owner, "64 lower-case hex digits");
                return;
            case "chartNumberFormat":
                if (value.ValueKind != JsonValueKind.String) WrongType(path, "a format string", value, owner);
                else if (!ChartNumberFormat.IsSupported(value.GetString()))
                    Error(path, "unsupported-format", $"Chart number format {JsonSerializer.Serialize(value.GetString())} is not supported. Use G, F<n>, N<n> or P<n>, with n from 0 to 15.", owner);
                return;
            case "array":
                CheckArray(arg, spec, value, path, owner, depth);
                return;
            case "map":
                CheckMap(arg, spec, value, path, owner, depth);
                return;
            case "object":
                ValidateShape(value, ElementDescription.Shape(arg), path, owner, "unknown-key", depth);
                if (string.Equals(arg, "ChartData", StringComparison.Ordinal)) CountChartPoints(value, path);
                return;
            case "element":
                CheckElement(value, path, depth + 1, null);
                return;
            case "elements":
                CheckElements(value, path, depth);
                return;
            default:
                CheckSpecial(head, value, path, owner, depth);
                return;
        }
    }

    private void CheckNumber(string head, KeySpec spec, JsonElement value, string path, string? owner)
    {
        if (!IsNumber(value))
        {
            WrongType(path, head == "integer" ? "an integer" : "a number", value, owner);
            return;
        }
        double n = Num(value);
        if (head == "integer" && Math.Floor(n) != n) BadValue(path, $"Expected an integer; found {value.GetRawText()}.", owner, "an integer");
        else if (spec.Min is { } min && n < min) BadValue(path, $"{value.GetRawText()} is below the minimum {min.ToString(CultureInfo.InvariantCulture)}.", owner, $">= {min.ToString(CultureInfo.InvariantCulture)}");
        else if (spec.Max is { } max && n > max) BadValue(path, $"{value.GetRawText()} is above the maximum {max.ToString(CultureInfo.InvariantCulture)}.", owner, $"<= {max.ToString(CultureInfo.InvariantCulture)}");
    }

    private static bool IsColor(string s)
    {
        if (HexColor().IsMatch(s)) return true;
        int dash = s.LastIndexOf('-');
        if (dash <= 0) return false;
        return D.Palette.Families.Contains(s[..dash], StringComparer.Ordinal)
            && int.TryParse(s[(dash + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out int shade)
            && D.Palette.Shades.Contains(shade)
            && string.Equals(shade.ToString(CultureInfo.InvariantCulture), s[(dash + 1)..], StringComparison.Ordinal);
    }

    private void CheckEnum(string name, string token, string path, string? owner)
    {
        EnumSpec spec = D.Enums[name];
        if (spec.Tokens.Contains(token, StringComparer.Ordinal)) return;
        if (_newerMinor)
        {
            Unrecognised(path, $"Token {JsonSerializer.Serialize(token)}", "invalid-value", owner);
            return;
        }
        BadValue(path, $"{JsonSerializer.Serialize(token)} is not a {name} token. Expected one of: {string.Join(", ", spec.Tokens)}.", owner, string.Join(" | ", spec.Tokens));
    }

    private void CheckLength(JsonElement value, string path, string? owner)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                return;
            case JsonValueKind.String:
                if (!LengthToken().IsMatch(value.GetString()!))
                    BadValue(path, $"{JsonSerializer.Serialize(value.GetString())} is not a length. Write points (72), a token ('1in', '10mm', '2.5cm', '72pt') or {{ value, unit }}.", owner, "points, unit token or { value, unit }");
                return;
            case JsonValueKind.Object:
                bool extras = value.EnumerateObject().Any(p => p.Name is not ("value" or "unit"));
                if (!value.TryGetProperty("value", out JsonElement v) || !IsNumber(v) || !value.TryGetProperty("unit", out JsonElement u) || u.ValueKind != JsonValueKind.String || extras)
                {
                    BadValue(path, "A length object is { value: number, unit: \"pt\" | \"in\" | \"cm\" | \"mm\" }.", owner);
                    return;
                }
                CheckEnum("Unit", u.GetString()!, $"{path}/unit", owner);
                return;
            default:
                WrongType(path, "a length", value, owner);
                return;
        }
    }

    private void CheckSpacing(JsonElement value, string path, string? owner)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
            case JsonValueKind.String:
                CheckLength(value, path, owner);
                return;
            case JsonValueKind.Array:
                int count = value.GetArrayLength();
                if (count is not (2 or 4))
                {
                    BadValue(path, "Spacing as an array is [h, v] or [left, top, right, bottom].", owner, "2 or 4 lengths");
                    return;
                }
                int i = 0;
                foreach (JsonElement item in value.EnumerateArray()) CheckLength(item, $"{path}/{i++}", owner);
                return;
            case JsonValueKind.Object:
                foreach (JsonProperty p in value.EnumerateObject())
                {
                    if (p.Name is not ("top" or "right" or "bottom" or "left")) BadValue($"{path}/{Escape(p.Name)}", $"`{p.Name}` is not a side; use top, right, bottom or left.", owner);
                    else CheckLength(p.Value, $"{path}/{p.Name}", owner);
                }
                return;
            default:
                WrongType(path, "a spacing value", value, owner);
                return;
        }
    }

    private void CheckBorder(JsonElement value, string path, string? owner)
    {
        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.String)
        {
            CheckLength(value, path, owner);
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            WrongType(path, "a border", value, owner);
            return;
        }
        foreach (JsonProperty p in value.EnumerateObject())
        {
            if (p.Name == "color") CheckKind("color", new KeySpec { Kind = "color" }, p.Value, $"{path}/color", owner, 0);
            else if (p.Name is "width" or "top" or "right" or "bottom" or "left") CheckLength(p.Value, $"{path}/{p.Name}", owner);
            else BadValue($"{path}/{Escape(p.Name)}", $"`{p.Name}` is not a border key.", owner);
        }
        if (Has(value, "width") && SideKeys.Any(k => Has(value, k)))
            Error(path, "conflicting-keys", "`width` and per-side widths cannot both be set.", owner);
    }

    private void CheckPageSize(JsonElement value, string path, string? owner)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            if (!D.PageSizes.Contains(value.GetString()!, StringComparer.Ordinal))
                BadValue(path, $"{JsonSerializer.Serialize(value.GetString())} is not a page size. Expected one of: {string.Join(", ", D.PageSizes)}.", owner, string.Join(" | ", D.PageSizes));
            return;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            WrongType(path, "a page size token or { width, height }", value, owner);
            return;
        }
        bool extras = value.EnumerateObject().Any(p => p.Name is not ("width" or "height" or "name"));
        if (!value.TryGetProperty("width", out JsonElement w) || !IsNumber(w) || !value.TryGetProperty("height", out JsonElement h) || !IsNumber(h) || Num(w) <= 0 || Num(h) <= 0 || extras)
            BadValue(path, "A page size object is { width, height, name? } with positive numbers of points.", owner);
    }

    private void CheckTextDecoration(JsonElement value, string path, string? owner)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            CheckEnum("TextDecorationToken", value.GetString()!, path, owner);
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "a token or an array of tokens", value, owner);
            return;
        }
        int i = 0;
        bool none = false;
        foreach (JsonElement t in value.EnumerateArray())
        {
            if (t.ValueKind == JsonValueKind.String)
            {
                CheckEnum("TextDecorationToken", t.GetString()!, $"{path}/{i}", owner);
                none |= t.GetString() == "none";
            }
            else WrongType($"{path}/{i}", "a string", t, owner);
            i++;
        }
        if (none && value.GetArrayLength() > 1) BadValue(path, "`none` cannot be combined with other decorations.", owner);
    }

    private void CheckPermissions(JsonElement value, string path, string? owner)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            if (value.GetString() is not ("all" or "none")) CheckEnum("PdfPermission", value.GetString()!, path, owner);
            return;
        }
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array of permission tokens", value, owner);
            return;
        }
        int i = 0;
        foreach (JsonElement t in value.EnumerateArray())
        {
            if (t.ValueKind == JsonValueKind.String) CheckEnum("PdfPermission", t.GetString()!, $"{path}/{i}", owner);
            else WrongType($"{path}/{i}", "a string", t, owner);
            i++;
        }
    }

    private void CheckArray(string itemKind, KeySpec spec, JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array", value, owner);
            return;
        }
        if (value.GetArrayLength() > _limits.ArrayLength)
        {
            LimitExceeded(path, "An array");
            return;
        }
        int i = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Null) BadValue($"{path}/{i}", "null is not a value in a definition.", owner);
            else CheckKind(itemKind, new KeySpec { Kind = itemKind, Min = spec.Min, Max = spec.Max }, item, $"{path}/{i}", owner, depth);
            if (_aborted) return;
            i++;
        }
    }

    private void CheckMap(string valueKind, KeySpec spec, JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            WrongType(path, "an object", value, owner);
            return;
        }
        foreach (JsonProperty p in value.EnumerateObject())
        {
            if (p.Name.StartsWith('_')) continue;
            if (p.Value.ValueKind == JsonValueKind.Null) BadValue($"{path}/{Escape(p.Name)}", "null is not a value in a definition.", owner);
            else CheckKind(valueKind, new KeySpec { Kind = valueKind, Min = spec.Min, Max = spec.Max }, p.Value, $"{path}/{Escape(p.Name)}", owner, depth);
        }
    }

    private void CountChartPoints(JsonElement chart, string path)
    {
        if (chart.ValueKind != JsonValueKind.Object || !chart.TryGetProperty("series", out JsonElement series) || series.ValueKind != JsonValueKind.Array) return;
        foreach (JsonElement s in series.EnumerateArray())
            if (s.ValueKind == JsonValueKind.Object && s.TryGetProperty("dataPoints", out JsonElement points) && points.ValueKind == JsonValueKind.Array)
                _chartPoints += points.GetArrayLength();
        if (_chartPoints > _limits.ChartDataPoints) LimitExceeded(path, "The number of chart data points");
    }

    // ── elements and slots ───────────────────────────────────────────────────

    private void CheckElements(JsonElement value, string path, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array of elements", value, null);
            return;
        }
        if (value.GetArrayLength() > _limits.ArrayLength)
        {
            LimitExceeded(path, "An array");
            return;
        }
        int i = 0;
        foreach (JsonElement node in value.EnumerateArray())
        {
            CheckElement(node, $"{path}/{i++}", depth + 1, null);
            if (_aborted) return;
        }
    }

    private void CheckElement(JsonElement node, string path, int depth, string? allowedType)
    {
        if (_aborted) return;
        if (node.ValueKind != JsonValueKind.Object)
        {
            WrongType(path, "an element object", node, null);
            return;
        }
        if (depth > _limits.ElementDepth)
        {
            LimitExceeded(path, "The nesting depth of elements");
            return;
        }
        if (++_nodeCount > _limits.ElementNodes)
        {
            LimitExceeded(path, "The number of elements");
            return;
        }

        if (!node.TryGetProperty("type", out JsonElement typeValue))
        {
            Error(path, "missing-required", "Missing required key `type`.", expected: "type");
            return;
        }
        if (typeValue.ValueKind != JsonValueKind.String)
        {
            WrongType($"{path}/type", "a string", typeValue, null);
            return;
        }
        string type = typeValue.GetString()!;
        if (!D.Elements.TryGetValue(type, out ShapeSpec? spec))
        {
            Unrecognised(path, $"Element type {JsonSerializer.Serialize(type)}", "unknown-type");
            return;
        }
        if (allowedType is not null && !string.Equals(type, allowedType, StringComparison.Ordinal))
        {
            Error($"{path}/type", "invalid-value", $"A nested list has the same type as its parent ({allowedType}); found {type}.", expected: allowedType);
            return;
        }

        string? id = null;
        if (node.TryGetProperty("id", out JsonElement idValue))
        {
            if (idValue.ValueKind != JsonValueKind.String || !IdPattern().IsMatch(idValue.GetString()!))
            {
                BadValue($"{path}/id", "An id starts with a letter or digit and continues with letters, digits and _ . : - (128 characters at most).", null, IdPattern().ToString());
            }
            else
            {
                id = idValue.GetString()!;
                if (_ids.TryGetValue(id, out string? first)) Error($"{path}/id", "duplicate-id", $"The id {JsonSerializer.Serialize(id)} is already used at {first}.", id);
                else _ids[id] = path;
            }
        }

        bool isList = type is "UnorderedList" or "OrderedList";
        if (isList) _listTypes.Add(type);
        ValidateElementBody(node, spec, path, id, depth);
        if (isList) _listTypes.RemoveAt(_listTypes.Count - 1);

        foreach ((string key, string refKind) in spec.Refs ?? [])
            if (node.TryGetProperty(key, out JsonElement refValue)) CollectRef(refKind, refValue, $"{path}/{key}", id);
        foreach ((string key, string defKind) in spec.Defines ?? [])
        {
            if (node.TryGetProperty(key, out JsonElement defValue) && defValue.ValueKind == JsonValueKind.String)
                (defKind == "anchor" ? _anchors : _tocIdentifiers).Add(defValue.GetString()!);
        }
        foreach (string rule in spec.Rules ?? [])
        {
            if (rule == "rotation" || rule == "transformWidth")
            {
                if (node.TryGetProperty("mode", out var mode) && mode.ValueKind == JsonValueKind.String && mode.GetString() == "constrained"
                    && node.TryGetProperty("angle", out var angle) && angle.ValueKind == JsonValueKind.Number && angle.TryGetDouble(out var a) && double.IsFinite(a) && a % 90 != 0)
                    Error($"{path}/angle", "invalid-value", "Constrained rotation requires a multiple of 90 degrees.", id);
                if (node.TryGetProperty("contentWidth", out var width))
                {
                    double? w = null;
                    if (width.ValueKind == JsonValueKind.Number && width.TryGetDouble(out double n)) w = n;
                    else if (width.ValueKind == JsonValueKind.String && LengthToken().IsMatch(width.GetString()!))
                    {
                        string token = width.GetString()!.Trim();
                        if (double.TryParse(token[..^2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out n)) w = n;
                    }
                    else if (width.ValueKind == JsonValueKind.Object && width.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out n)) w = n;
                    if (w is double size && (!double.IsFinite(size) || size <= 0)) Error($"{path}/contentWidth", "invalid-value", "Transform contentWidth must be positive.", id);
                }
            }
            else if (rule == "singleSeries") SingleSeries(node, path, id);
            else if (rule == "chartDuplicates") ChartDuplicates(node, path, id);
        }
        if (type is "Table" or "Grid") TableWidth(node, path, id);
    }

    /// <summary>Validates an element's keys, leaving out its own `type` and `id`.</summary>
    private void ValidateElementBody(JsonElement node, ShapeSpec spec, string path, string? id, int depth)
    {
        // `type` and `id` belong to the node itself; everything else is checked against the element.
        Dictionary<string, KeySpec> keys = ElementDescription.KeysOf(spec);
        foreach (JsonProperty p in node.EnumerateObject())
        {
            string key = p.Name;
            if (key is "type" or "id" || key.StartsWith('_') || key == "$schema") continue;
            if (!keys.TryGetValue(key, out KeySpec? keySpec))
            {
                Unrecognised($"{path}/{Escape(key)}", $"`{key}`", "unknown-key", id);
                continue;
            }
            CheckValue(keySpec, p.Value, $"{path}/{Escape(key)}", id, depth);
            if (_aborted) return;
        }
        foreach ((string key, KeySpec keySpec) in keys)
        {
            if (keySpec.Required && !node.TryGetProperty(key, out _))
                Error(path, "missing-required", $"Missing required key `{key}`.", id, expected: key);
        }
        CheckGroups(node, spec, path, id);
    }

    private void CollectRef(string kind, JsonElement value, string path, string? id)
    {
        if (value.ValueKind == JsonValueKind.Null) return;
        if (value.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement v in value.EnumerateArray())
            {
                if (v.ValueKind == JsonValueKind.String) _refs.Add(new Ref(kind, v, $"{path}/{i}", id));
                i++;
            }
        }
        else _refs.Add(new Ref(kind, value, path, id));
    }

    private void SingleSeries(JsonElement body, string path, string? id)
    {
        if (body.TryGetProperty("data", out JsonElement data) && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("series", out JsonElement series) && series.ValueKind == JsonValueKind.Array && series.GetArrayLength() > 1)
        {
            Error($"{path}/data/series", "invalid-value", "A pie chart draws its first series only; give it one series.", id, expected: "one series");
        }
    }

    /// <summary>A chart's own title and axes and the copies inside `data` may both be given, but must agree.</summary>
    private void ChartDuplicates(JsonElement body, string path, string? id)
    {
        if (!body.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object) return;
        foreach (string key in ChartDuplicateKeys)
        {
            if (!body.TryGetProperty(key, out JsonElement own) || !data.TryGetProperty(key, out JsonElement copy)) continue;
            if (!string.Equals(Canonical(own), Canonical(copy), StringComparison.Ordinal))
                Error(path, "conflicting-keys", $"`{key}` is given on the chart and in `data`, and the two differ.", id);
        }
    }

    /// <summary>JSON text with object keys in a fixed order, so equal values compare equal whatever order they were written in.</summary>
    private static string Canonical(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(Canonical)) + "]",
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => JsonSerializer.Serialize(p.Name) + ":" + Canonical(p.Value))) + "}",
        _ => value.GetRawText(),
    };

    /// <summary>Under `strict`, a row that occupies more columns than the table declares.</summary>
    private void TableWidth(JsonElement body, string path, string? id)
    {
        if (!_options.Strict || !body.TryGetProperty("columns", out JsonElement columns) || columns.ValueKind != JsonValueKind.Array) return;
        int declared = columns.GetArrayLength();
        foreach (string slot in TableSlots)
        {
            if (!body.TryGetProperty(slot, out JsonElement rows) || rows.ValueKind != JsonValueKind.Array) continue;
            int r = 0;
            foreach (JsonElement row in rows.EnumerateArray())
            {
                JsonElement cells = row;
                if (row.ValueKind == JsonValueKind.Object) row.TryGetProperty("cells", out cells);
                int width = 0;
                if (cells.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement c in cells.EnumerateArray())
                    {
                        width += c.ValueKind == JsonValueKind.Object && c.TryGetProperty("type", out JsonElement t) && t.GetString() == "TableCell"
                            && c.TryGetProperty("columnSpan", out JsonElement span) && IsNumber(span) ? (int)Num(span) : 1;
                    }
                }
                if (width > declared) Error($"{path}/{slot}/{r}", "table-row-width", $"Row occupies {width} columns; the table declares {declared}.", id);
                r++;
            }
        }
    }

    // ── named special kinds ──────────────────────────────────────────────────

    private void CheckSpecial(string kind, JsonElement value, string path, string? owner, int depth)
    {
        switch (kind)
        {
            case "stackChildren":
                CheckStackChildren(value, path, owner, depth);
                return;
            case "listItems":
                CheckListItems(value, path, owner, depth);
                return;
            case "spans":
                CheckSpans(value, path, owner);
                return;
            case "rows":
                CheckRows(value, path, owner, depth);
                return;
            case "cells":
                CheckCells(value, path, owner, depth);
                return;
            case "cellContent":
                if (value.ValueKind == JsonValueKind.String) return;
                CheckElement(value, path, depth + 1, null);
                return;
            default:
                Error(path, "invalid-value", $"Internal: unknown value kind {kind}.", owner);
                return;
        }
    }

    private void CheckStackChildren(JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array", value, owner);
            return;
        }
        int i = 0;
        foreach (JsonElement child in value.EnumerateArray())
        {
            string at = $"{path}/{i++}";
            if (child.ValueKind == JsonValueKind.Object && !Has(child, "type") && (Has(child, "element") || Has(child, "weight")))
            {
                foreach (JsonProperty p in child.EnumerateObject())
                    if (p.Name is not ("weight" or "element") && !p.Name.StartsWith('_'))
                        BadValue($"{at}/{Escape(p.Name)}", "`weight` and `element` are the only keys of a weighted child.", owner);
                if (!child.TryGetProperty("weight", out JsonElement weight) || !IsNumber(weight) || Num(weight) <= 0)
                    BadValue($"{at}/weight", "A weight is a positive number.", owner, "> 0");
                if (!child.TryGetProperty("element", out JsonElement element)) Error(at, "missing-required", "Missing required key `element`.", owner, expected: "element");
                else CheckElement(element, $"{at}/element", depth + 2, null);
            }
            else CheckElement(child, at, depth + 1, null);
        }
    }

    private void CheckListItems(JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array", value, owner);
            return;
        }
        string? parentType = _listTypes.Count > 0 ? _listTypes[^1] : null;
        int i = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            string at = $"{path}/{i++}";
            if (item.ValueKind == JsonValueKind.String) continue;
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("type", out JsonElement t) && t.ValueKind == JsonValueKind.String && t.GetString() is "UnorderedList" or "OrderedList")
            {
                CheckElement(item, at, depth + 1, parentType);
                continue;
            }
            WrongType(at, "a string or a nested list", item, owner);
        }
    }

    private void CheckSpans(JsonElement value, string path, string? owner)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array", value, owner);
            return;
        }
        int i = 0;
        foreach (JsonElement span in value.EnumerateArray())
        {
            string at = $"{path}/{i++}";
            if (!ValidateShape(span, D.Shapes["Span"], at, owner, "unknown-key", 0)) continue;
            bool lineBreak = span.TryGetProperty("lineBreak", out JsonElement lb) && lb.ValueKind == JsonValueKind.True;
            if (lineBreak)
            {
                if (Has(span, "text") || Has(span, "style")) Error(at, "conflicting-keys", "A line break has no text or style.", owner);
            }
            else if (!span.TryGetProperty("text", out JsonElement text) || text.ValueKind != JsonValueKind.String)
            {
                Error(at, "missing-required", "A span needs `text`, or `lineBreak: true`.", owner, expected: "text");
            }
        }
    }

    private void CheckRows(JsonElement value, string path, string? owner, int depth)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array of rows", value, owner);
            return;
        }
        int r = 0;
        foreach (JsonElement row in value.EnumerateArray())
        {
            string at = $"{path}/{r++}";
            if (row.ValueKind == JsonValueKind.Array)
            {
                CheckCells(row, at, owner, depth);
                continue;
            }
            if (ValidateShape(row, D.Shapes["TableRow"], at, owner, "unknown-key", 0) && row.TryGetProperty("cells", out JsonElement cells))
                CheckCells(cells, $"{at}/cells", owner, depth);
        }
    }

    private void CheckCells(JsonElement cells, string path, string? owner, int depth)
    {
        if (cells.ValueKind != JsonValueKind.Array)
        {
            WrongType(path, "an array of cells", cells, owner);
            return;
        }
        int c = 0;
        foreach (JsonElement cell in cells.EnumerateArray())
        {
            string at = $"{path}/{c++}";
            if (cell.ValueKind == JsonValueKind.String) continue;
            if (cell.ValueKind == JsonValueKind.Object && cell.TryGetProperty("type", out JsonElement t) && t.ValueKind == JsonValueKind.String && t.GetString() == "TableCell")
            {
                ValidateShape(cell, D.Shapes["TableCell"], at, owner, "unknown-key", depth + 1);
                continue;
            }
            CheckElement(cell, at, depth + 1, null);
        }
    }

    // ── entry points ─────────────────────────────────────────────────────────

    /// <summary>Reads the raw text's nesting depth without parsing it, so a hostile document cannot reach the parser.</summary>
    internal static int JsonDepth(string text)
    {
        int depth = 0;
        int max = 0;
        bool inString = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
            }
            else if (c == '"') inString = true;
            else if (c is '[' or '{') max = Math.Max(max, ++depth);
            else if (c is ']' or '}') depth--;
        }
        return max;
    }

    internal static int ElementDepth(JsonElement root, int limit)
    {
        var stack = new Stack<(JsonElement Value, int Depth)>();
        stack.Push((root, 1));
        int max = 0;
        while (stack.Count > 0)
        {
            (JsonElement v, int d) = stack.Pop();
            if (v.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array)) continue;
            max = Math.Max(max, d);
            if (max > limit) return max;
            if (v.ValueKind == JsonValueKind.Array) foreach (JsonElement child in v.EnumerateArray()) stack.Push((child, d + 1));
            else foreach (JsonProperty child in v.EnumerateObject()) stack.Push((child.Value, d + 1));
        }
        return max;
    }

    internal static DefinitionValidationResult Validate(string text, DefinitionValidateOptions? options)
    {
        options ??= new DefinitionValidateOptions();
        DefinitionLimits limits = options.Limits ?? new DefinitionLimits();
        if (Encoding.UTF8.GetByteCount(text) > limits.InputBytes) return Fail("limit-exceeded", "The input exceeds the size limit for this reader.");
        if (JsonDepth(text) > limits.JsonDepth) return Fail("limit-exceeded", "The JSON nests deeper than this reader allows.");
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 4096 });
        }
        catch (JsonException e)
        {
            return Fail("invalid-json", $"The input is not valid JSON: {e.Message}");
        }
        return Validate(document.RootElement, options, checkDepth: false);
    }

    internal static DefinitionValidationResult Validate(JsonElement root, DefinitionValidateOptions? options, bool checkDepth = true)
    {
        options ??= new DefinitionValidateOptions();
        DefinitionLimits limits = options.Limits ?? new DefinitionLimits();
        if (checkDepth && ElementDepth(root, limits.JsonDepth) > limits.JsonDepth)
            return Fail("limit-exceeded", "The value nests deeper than this reader allows.");

        var validator = new DefinitionValidator(options);
        validator.Run(root);
        validator.Finish();
        bool valid = validator.Errors.Count == 0;
        return new DefinitionValidationResult(validator.Errors, validator.Warnings, valid ? root : null);
    }

    private static DefinitionValidationResult Fail(string code, string message) =>
        new([new DefinitionIssue("", code, message)], [], null);
}
