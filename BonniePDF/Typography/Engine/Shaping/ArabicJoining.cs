// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
using System;

namespace BonniePDF.Typography.Engine
{
    internal sealed class ArabicJoining
    {
        internal string Type { get; }
        internal string Group { get; }
        internal bool IsTatweel { get; }
        internal bool JoinsBefore { get; }
        internal bool JoinsAfter { get; }
        private ArabicJoining(int scalar)
        {
            Type = ArabicData.JoiningType(scalar); Group = LayoutUnicodeData.JoiningGroup(scalar);
            IsTatweel = scalar == 0x640; JoinsBefore = "DRC".Contains(Type); JoinsAfter = "DLC".Contains(Type);
        }
        internal static ArabicJoining Describe(int scalar)
        {
            if (scalar < 0 || scalar > 0x10ffff || (scalar >= 0xd800 && scalar <= 0xdfff)) throw new ArgumentOutOfRangeException("scalar");
            return new ArabicJoining(scalar);
        }
    }
}
