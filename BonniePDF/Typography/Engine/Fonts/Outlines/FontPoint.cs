// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
#nullable disable
namespace BonniePDF.Typography.Engine
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    internal struct FontPoint
    {
        public double X, Y;
        public bool OnCurve;
        public FontPoint(double x, double y, bool onCurve)
        {
            X = x;
            Y = y;
            OnCurve = onCurve;
        }
    }
}
