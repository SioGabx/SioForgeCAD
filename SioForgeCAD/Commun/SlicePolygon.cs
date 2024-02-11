using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun
{
    public static class SlicePolygon
    {
        private static bool IsSegmentIntersecting(this Polyline polyline, Line CutLine, out Point3dCollection IntersectionPointsFounds)
        {
            IntersectionPointsFounds = new Point3dCollection();
            polyline.IntersectWith(CutLine, Intersect.ExtendArgument, IntersectionPointsFounds, IntPtr.Zero, IntPtr.Zero);
            return IntersectionPointsFounds.Count > 0;
        }

        public static List<Polyline> Cut(this Polyline polyline, Line CutLine)
        {
            polyline.IsSegmentIntersecting(CutLine, out Point3dCollection IntersectionPointsFounds);
            DBObjectCollection cutedDBObjectsCollection = polyline.GetSplitCurves(IntersectionPointsFounds);
            Point3dCollection OrderedIntersectionPointsFounds = IntersectionPointsFounds.OrderByDistance(CutLine.StartPoint);

            DBObjectCollection ValidCutLines = new DBObjectCollection();
            for (var segIndex = 0; segIndex < OrderedIntersectionPointsFounds.Count; segIndex++)
            {
                Point3d StartPoint = OrderedIntersectionPointsFounds[segIndex];
                Point3d EndPoint = OrderedIntersectionPointsFounds[segIndex + 1];

                Point3d MiddlePoint = StartPoint.GetMiddlePoint(EndPoint);

                DBObjectCollection Reg = Region.CreateFromCurves(new DBObjectCollection() { polyline });
                Brep brepEnt = new Brep(Reg[0] as Region);
                brepEnt.GetPointContainment(MiddlePoint, out PointContainment pointCont);
                if (pointCont != PointContainment.Outside)
                {
                    Polyline PolySegment = new Polyline();
                    PolySegment.AddVertex(StartPoint);
                    PolySegment.AddVertex(EndPoint);
                    ValidCutLines.Add(PolySegment);
                }

            }


            foreach (Polyline cutedPolyligne in cutedDBObjectsCollection)
            {
                if (!cutedPolyligne.IsClockwise())
                {
                    cutedPolyligne.ReverseCurve();
                }


                foreach (Polyline PolySegment in ValidCutLines)
                {

                    if ((cutedPolyligne.StartPoint.IsEqualTo(PolySegment.StartPoint) && cutedPolyligne.EndPoint.IsEqualTo(PolySegment.EndPoint)) ||
                        (cutedPolyligne.EndPoint.IsEqualTo(PolySegment.StartPoint) && cutedPolyligne.StartPoint.IsEqualTo(PolySegment.EndPoint)))
                    {
                        cutedPolyligne.Closed = true;
                    }
                }
            }

            List<DBObject> Polylines = cutedDBObjectsCollection.Join(ValidCutLines).ToList();
            List<DBObject> ClosedPolylines = Polylines.Where((poly) => (poly as Polyline).Closed == true).ToList();
            List<DBObject> NotClosedPolylines = Polylines.Where((poly) => (poly as Polyline).Closed == false).ToList();
            int index = 0;
            while (NotClosedPolylines.Count > index)
            {
                if (!(NotClosedPolylines[index] is Polyline cutedPolyligne))
                {
                    continue;
                }

                foreach (Polyline subCutedPolyligne in NotClosedPolylines.ToArray().Cast<Polyline>())
                {
                    if (subCutedPolyligne == cutedPolyligne) { continue; }
                    if (subCutedPolyligne.StartPoint.IsEqualTo(cutedPolyligne.StartPoint) || subCutedPolyligne.StartPoint.IsEqualTo(cutedPolyligne.EndPoint) || subCutedPolyligne.EndPoint.IsEqualTo(cutedPolyligne.StartPoint) || subCutedPolyligne.EndPoint.IsEqualTo(cutedPolyligne.EndPoint))
                    {
                        cutedPolyligne.JoinEntity(subCutedPolyligne);
                        NotClosedPolylines.Remove(subCutedPolyligne);
                    }
                }

                cutedPolyligne.Cleanup();
                if (cutedPolyligne.Closed)
                {
                    ClosedPolylines.Add(cutedPolyligne);
                }
                index++;

            }


            //foreach (Polyline cutedPolyligne in NotClosedPolylines)
            //{
            //    foreach (Polyline subCutedPolyligne in NotClosedPolylines)
            //    {
            //        if (subCutedPolyligne == cutedPolyligne) { continue; }
            //        if (subCutedPolyligne.StartPoint.IsEqualTo(cutedPolyligne.StartPoint) || subCutedPolyligne.StartPoint.IsEqualTo(cutedPolyligne.EndPoint) || subCutedPolyligne.EndPoint.IsEqualTo(cutedPolyligne.StartPoint) || subCutedPolyligne.EndPoint.IsEqualTo(cutedPolyligne.EndPoint))
            //        {
            //            try {
            //            cutedPolyligne.JoinEntity(subCutedPolyligne);
            //            }catch (Exception ex)
            //            {
            //                Debug.WriteLine(ex.ToString());
            //            }
            //        }
            //    }
            //    cutedPolyligne.Cleanup();
            //    if (cutedPolyligne.Closed)
            //    {
            //        ClosedPolylines.Add(cutedPolyligne);
            //    }
            //}



            foreach (DBObject cutedDbObject in ClosedPolylines)
            {
                if (cutedDbObject is Entity ent)
                {
                    ent.ColorIndex = 5;
                    ent.AddToDrawing();
                }
            }
            return new List<Polyline>() { polyline };

        }


















    }
}
