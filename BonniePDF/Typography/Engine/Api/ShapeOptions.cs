// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ShapeOptions
    {
        internal readonly string Script, Language;
        internal readonly TextDirection Direction;
        internal readonly ReadOnlyCollection<ShapeFeature> Features;
        internal readonly ShapingLimits Limits;

        internal ShapeOptions(string script = "latn", string language = "dflt",
            TextDirection direction = TextDirection.LeftToRight, IEnumerable<ShapeFeature> features = null, ShapingLimits limits = null)
        {
            ValidateTag(script, "script"); ValidateTag(language, "language");
            if (direction < TextDirection.LeftToRight || direction > TextDirection.BottomToTop)
                throw new ArgumentOutOfRangeException("direction");
            Script = script == "DFLT" ? script : script.ToLowerInvariant();
            Language = language;
            Direction = direction; Limits = limits;
            var copy = new List<ShapeFeature>();
            if (features != null)
                foreach (var feature in features)
                {
                    if (feature == null) throw new ArgumentException("A feature cannot be null.", "features");
                    if (copy.Count == 256) throw new ArgumentException("Feature count limit exceeded.", "features");
                    copy.Add(feature);
                }
            Features = copy.AsReadOnly();
        }

        internal static void ValidateTag(string tag, string parameter)
        {
            if (tag == null || tag.Length != 4) throw new ArgumentException("An OpenType tag must contain four ASCII characters.", parameter);
            foreach (char c in tag)
                if (c < 32 || c > 126) throw new ArgumentException("An OpenType tag must contain four ASCII characters.", parameter);
        }

        internal void ValidateText(string text)
        {
            if (Direction == TextDirection.TopToBottom || Direction == TextDirection.BottomToTop)
                throw new NotSupportedException("Vertical shaping is unsupported.");
            foreach (var feature in Features)
            {
                int end = feature.End == int.MaxValue ? text.Length : feature.End;
                if (feature.Start > text.Length || end > text.Length)
                    throw new ArgumentException("A feature range exceeds the UTF-16 text length.", "features");
                if ((feature.Start < text.Length && char.IsLowSurrogate(text[feature.Start])) ||
                    (end < text.Length && char.IsLowSurrogate(text[end])))
                    throw new ArgumentException("A feature range splits a surrogate pair.", "features");
            }
        }

        internal int FeatureValue(string tag, int cluster, int defaultValue, Action tick = null)
        {
            int value = defaultValue;
            foreach (var feature in Features)
            {
                if (tick != null) tick();
                if (feature.Tag == tag && cluster >= feature.Start && cluster < feature.End) value = feature.Value;
            }
            return value;
        }
    }
}
