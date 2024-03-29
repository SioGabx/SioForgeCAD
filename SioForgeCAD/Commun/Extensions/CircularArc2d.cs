﻿using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun.Extensions
{
    public static class CircularArc2dExtensions
    {
        public static double GetArea(this CircularArc2d arc)
        {
            double rad = arc.Radius;
            double ang = arc.IsClockWise ?
                arc.StartAngle - arc.EndAngle :
                arc.EndAngle - arc.StartAngle;
            return rad * rad * (ang - Math.Sin(ang)) / 2.0;
        }
    }
}
