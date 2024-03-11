using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CircularArc3dExtension
    {
        public static EllipticalArc3d GetEllipticalArc(this CircularArc3d arc)
        {
            return new EllipticalArc3d(
                arc.Center,
                arc.ReferenceVector,
                (arc.Normal.CrossProduct(arc.ReferenceVector)).GetNormal(),
                arc.Radius,
                arc.Radius,
                arc.StartAngle,
                arc.EndAngle);
        }

      

    }
}
