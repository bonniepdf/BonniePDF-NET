// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;

namespace BonniePDF.Typography.Engine
{
    internal static class EmojiClusters
    {
        internal static List<int> Scalars(string text, bool hebrew = false, bool arabic = false)
        {
            var scalars = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                int scalar;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    scalar = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else if (char.IsSurrogate(text[i]))
                    throw new ArgumentException("Text contains an unpaired surrogate.", "text");
                else
                    scalar = text[i];
                if (scalar < 32 || (scalar >= 127 && scalar <= 159))
                    throw new NotSupportedException("Only one printable text line is supported.");
                // Emoji sequence support does not imply a complete complex-script or bidi engine.
                if ((!hebrew && scalar >= 0x590 && scalar <= 0x5ff) || (scalar >= 0x600 && scalar <= 0x8ff && !(arabic && (scalar <= 0x6ff || (scalar >= 0x750 && scalar <= 0x77f) || scalar >= 0x870))) || (scalar >= 0x900 && scalar <= 0x109f) ||
                    (!hebrew && ((scalar >= 0x200e && scalar <= 0x200f) || (scalar >= 0x202a && scalar <= 0x202e) || (scalar >= 0x2066 && scalar <= 0x2069))))
                    throw new NotSupportedException("Complex-script and bidirectional layout are not implemented in the independent emoji shaper.");
                scalars.Add(scalar);
            }
            return scalars;
        }
        internal static List<ScalarCluster> Split(List<int> points, bool joinLeading = true)
        {
            var bounds = new List<int> { 0 };
            int regional = points.Count > 0 && EmojiData.IsRegional(points[0]) ? 1 : 0;
            bool lastPictographic = points.Count > 0 && EmojiData.IsExtendedPictographic(points[0]);
            bool pictographicBeforeJoiner = false;
            for (int i = 1; i < points.Count; i++)
            {
                int current = points[i], prior = points[i - 1];
                bool extend = EmojiData.IsExtend(current);
                bool joined = prior == 0x200d && ((pictographicBeforeJoiner && EmojiData.IsExtendedPictographic(current)) || (joinLeading && i == 1));
                bool attaches = extend || EmojiData.IsSpacingMark(current) || UnicodeData.IsMark(current) || current == 0x200d || joined ||
                    (EmojiData.IsRegional(current) && EmojiData.IsRegional(prior) && regional % 2 == 1);
                if (!attaches) bounds.Add(i);
                if (current == 0x200d) pictographicBeforeJoiner = lastPictographic;
                if (!extend) lastPictographic = EmojiData.IsExtendedPictographic(current);
                regional = EmojiData.IsRegional(current) ? regional + 1 : 0;
            }
            if (points.Count > 0)
                bounds.Add(points.Count);
            var result = new List<ScalarCluster>();
            for (int i = 0; i + 1 < bounds.Count; i++)
            {
                var cluster = new ScalarCluster { Start = bounds[i], End = bounds[i + 1] };
                for (int j = cluster.Start; j < cluster.End; j++)
                {
                    int scalar = points[j];
                    cluster.Emoji |= EmojiData.IsPresentation(scalar) ||
                        (scalar == 0xfe0f && j > cluster.Start && EmojiData.IsEmoji(points[j - 1]));
                    cluster.TextPresentation |= scalar == 0xfe0e;
                }
                cluster.SequenceKind = EmojiData.SequenceKind(points, cluster.Start, cluster.End);
                cluster.Emoji |= cluster.SequenceKind != 0;
                cluster.Emoji &= !cluster.TextPresentation;
                result.Add(cluster);
            }
            return result;
        }
        internal static bool Ignorable(int scalar)
        {
            return UnicodeData.IsIgnorable(scalar);
        }
    }
}
