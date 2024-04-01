using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.MacroRecorder;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace SioForgeCAD.Commun
{
    public class PolyHole
    {
        public Polyline Boundary;
        public List<Polyline> Holes;

        public PolyHole(Polyline boundary, List<Polyline> holes)
        {
            Boundary = boundary;
            if (holes != null)
            {
                Holes = holes;
            }
            else
            {
                Holes = new List<Polyline>();
            }
        }


        public static List<PolyHole> CreateFromList(IEnumerable<Polyline> polylines, IEnumerable<Polyline> PossibleHole = null)
        {
            List<PolyHole> polyholes = new List<PolyHole>();
            foreach (var poly in polylines)
            {
                List<Polyline> holes = new List<Polyline>();
                if (PossibleHole != null)
                {
                    foreach (Polyline Hole in PossibleHole)
                    {
                        if (Hole.IsInside(poly, false))
                        {
                            holes.Add(Hole);
                        }
                    }
                }
                polyholes.Add(new PolyHole(poly, holes));
            }
            return polyholes;
        }
    }

    public static class PolyHoleExtensions
    {
        public static List<Polyline> GetBoundaries(this IEnumerable<PolyHole> polyHolesList)
        {
            List<Polyline> holes = new List<Polyline>();
            foreach (var item in polyHolesList)
            {
                holes.Add(item.Boundary);
            }
            return holes;
        }



    }
}
