using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CircularArc3dExtension
    {
        public static EllipticalArc3d GetEllipticalArc(this CircularArc3d arc)
        {
            return new EllipticalArc3d(
                arc.Center,
                arc.ReferenceVector,
                arc.Normal.CrossProduct(arc.ReferenceVector).GetNormal(),
                arc.Radius,
                arc.Radius,
                arc.StartAngle,
                arc.EndAngle);
        }
    }
}
