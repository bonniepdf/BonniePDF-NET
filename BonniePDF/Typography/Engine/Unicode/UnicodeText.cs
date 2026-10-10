// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BonniePDF.Typography.Engine
{
    internal sealed class UnicodeText
    {
        internal readonly string Text;
        internal readonly ReadOnlyCollection<int> Points;
        internal readonly ReadOnlyCollection<int> Offsets;
        internal UnicodeText(string text, int maxTextLength = 65536)
        {
            if (text == null) throw new ArgumentNullException(nameof(text), "Text is required.");
            if (maxTextLength < 1 || text.Length > maxTextLength) throw new ArgumentException("Unicode text length limit exceeded.");
            Text = text;
            var points = new List<int>(); var offsets = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                offsets.Add(i); int cp = text[i];
                if (cp >= 0xd800 && cp <= 0xdbff && i + 1 < text.Length && text[i + 1] >= 0xdc00 && text[i + 1] <= 0xdfff)
                    cp = (cp - 0xd800) * 1024 + text[++i] - 0xdc00 + 0x10000;
                else if (cp >= 0xd800 && cp <= 0xdfff) throw new ArgumentException("Text contains an unpaired surrogate.");
                points.Add(cp);
            }
            offsets.Add(text.Length); Points = points.AsReadOnly(); Offsets = offsets.AsReadOnly();
        }
    }
}
