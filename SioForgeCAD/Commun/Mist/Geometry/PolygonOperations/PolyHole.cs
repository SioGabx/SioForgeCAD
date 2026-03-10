using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public class PolyHole : IDisposable
    {
        public Polyline Boundary;
        public List<Polyline> Holes;

        public PolyHole(Polyline boundary, IEnumerable<Polyline> holes)
        {
            Boundary = boundary;
            if (holes != null)
            {
                Holes = holes.ToList();
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
                        if (Hole?.IsDisposed != true && Hole.IsInside(poly, false))
                        {
                            holes.Add(Hole);
                        }
                    }
                }
                polyholes.Add(new PolyHole(poly, holes));
            }
            return polyholes;
        }

        public void Dispose()
        {
            Boundary.Dispose();
            Holes.DeepDispose();
            GC.SuppressFinalize(this);
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
        public static List<Polyline> GetAllHoles(this IEnumerable<PolyHole> polyHolesList)
        {
            List<Polyline> holes = new List<Polyline>();
            foreach (var item in polyHolesList)
            {
                holes.AddRange(item.Holes);
            }
            return holes;
        }
    }
}
