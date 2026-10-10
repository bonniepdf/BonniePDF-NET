// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ColourReadLimits
    {
        internal readonly int MaxLayers, MaxStrikes, MaxRecords, MaxOperations, MaxEncodedBitmapBytes;

        internal ColourReadLimits(int maxLayers = 4096, int maxStrikes = 256, int maxRecords = 65536,
            int maxOperations = 1000000, int maxEncodedBitmapBytes = 16000000)
        {
            if (maxLayers < 1 || maxLayers > 65535) throw new ArgumentOutOfRangeException("maxLayers");
            if (maxStrikes < 1 || maxStrikes > 65536) throw new ArgumentOutOfRangeException("maxStrikes");
            if (maxRecords < 1 || maxRecords > 1000000) throw new ArgumentOutOfRangeException("maxRecords");
            if (maxOperations < 1) throw new ArgumentOutOfRangeException("maxOperations");
            if (maxEncodedBitmapBytes < 8 || maxEncodedBitmapBytes > 16000000) throw new ArgumentOutOfRangeException("maxEncodedBitmapBytes");
            MaxLayers = maxLayers; MaxStrikes = maxStrikes; MaxRecords = maxRecords;
            MaxOperations = maxOperations; MaxEncodedBitmapBytes = maxEncodedBitmapBytes;
        }
    }
}
