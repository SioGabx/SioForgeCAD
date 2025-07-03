using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class COPYGEOMETRYFORINDESIGN
    {
        public static void Copy()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var SelectedEnts = ed.GetSelectionRedraw(RejectObjectsOnLockedLayers: true);
                if (SelectedEnts.Status != PromptStatus.OK)
                {
                    return;
                }
                StringBuilder writer = new StringBuilder();
                {
                    writer.AppendLine("%!PS-Adobe-3.0 EPSF-3.0");
                    var extend = SelectedEnts.Value.GetObjectIds().GetExtents();
                    writer.AppendLine($"%%BoundingBox: {extend.MinPoint.X} {extend.MinPoint.Y} {extend.MaxPoint.X} {extend.MaxPoint.Y}");


                    foreach (ObjectId id in SelectedEnts.Value.GetObjectIds())
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;



                        writer.AppendLine("newpath");
                        switch (ent)
                        {
                            case Line line:
                                WriteLine(writer, line.StartPoint, line.EndPoint);
                                break;

                            case Arc arc:
                                WriteArc(writer, arc);
                                break;

                            case Polyline pl:
                                WritePolyline(writer, pl);
                                break;
                        }

                        writer.AppendLine("closepath");
                        writer.AppendLine("1 setlinewidth "); // epaisseur
                        writer.AppendLine("0 setgray"); // de 0 à 1 -> noir à blanc
                        writer.AppendLine("stroke");
                        // writer.AppendLine("1 0 0 setrgbcolor");
                        //writer.AppendLine("fill");


                    }
                    tr.Commit();


                    writer.AppendLine("showpage");



                }
                byte[] EPS = Encoding.ASCII.GetBytes(writer.ToString());

                bool ok = SioForgeCAD.Commun.Mist.ClipboardHelper.SetRawDataToClipboard("Encapsulated PostScript", EPS);
            }



            //var EPS = "%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 100 100\nnewpath\n0 0 moveto\n100 0 lineto\n100 100 lineto\n0 100 lineto\n0 66.7 33.3 33.3 0 0 curveto\nclosepath\n0.3 setgray\nfill\nshowpage";

        }






















        private static void WriteLine(StringBuilder w, Point3d p1, Point3d p2)
        {
            w.AppendLine($"{ToPS(p1)} moveto");
            w.AppendLine($"{ToPS(p2)} lineto");
        }

        private static void WriteArc(StringBuilder w, Arc arc)
        {
            Point3d center = arc.Center;
            double radius = arc.Radius;
            double startAngle = RadToDeg(arc.StartAngle);
            double endAngle = RadToDeg(arc.EndAngle);

            // Sens horaire ou antihoraire ?
            bool clockwise = arc.Normal.Z < 0;

            w.AppendLine($"{center.X.ToString("0.###", CultureInfo.InvariantCulture)} {center.Y.ToString("0.###", CultureInfo.InvariantCulture)} {radius.ToString("0.###", CultureInfo.InvariantCulture)} {startAngle.ToString("0.###", CultureInfo.InvariantCulture)} {endAngle.ToString("0.###", CultureInfo.InvariantCulture)} {(clockwise ? "arc" : "arcn")}");
        }

        private static void WritePolyline(StringBuilder w, Polyline pl)
        {
            if (pl.NumberOfVertices < 2)
                return;
            Point2d start = pl.GetPoint2dAt(0);
            w.AppendLine($"{ToPS(start)} moveto");

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p0 = pl.GetPoint2dAt(i);
                Point2d p1 = pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices);
                SegmentType type = pl.GetSegmentType(i);

                if (type == SegmentType.Arc)
                {
                    w.AppendLine(ArcFromBulge(pl, i));
                }
                else
                {
                    w.AppendLine($"{ToPS(p1)} lineto");
                }
            }

        }
        // 📐 Convertir bulge en arc : renvoie centre, rayon, angles
        private static string ArcFromBulge(Polyline pline, int index)
        {
            CircularArc2d arc2d = pline.GetArcSegment2dAt(index);



            Interval arc2dInterval = arc2d.GetInterval();

            double startParam = arc2dInterval.LowerBound;

            double endParam = arc2dInterval.UpperBound;

            Point2d sp2d = arc2d.EvaluatePoint(startParam);

            Point2d ep2d = arc2d.EvaluatePoint(endParam);

            Point2d cp2d = arc2d.Center;

            double startAngle =

               (new Line2d(cp2d, sp2d)).Direction.Angle;

            double endAngle =

                (new Line2d(cp2d, ep2d)).Direction.Angle;



            Point3d cp3d = new Point3d(

                                        cp2d.X,

                                        cp2d.Y,

                                        pline.Elevation

                                      );



            // if this polyline does not lie in WCS, get its ECS

            // and tranform the center point back to WCS

            if (pline.Normal != Vector3d.ZAxis)

            {

                Matrix3d ecsMatrix = pline.Ecs;

                cp3d = cp3d.TransformBy(ecsMatrix);

            }


            string epsLine = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}", cp2d.X, cp2d.Y, arc2d.Radius, RadToDeg(startAngle), RadToDeg(endAngle), arc2d.IsClockWise ? "arcn" : "arc");
            return epsLine;
        }

        private static string ToPS(Point2d pt)
        {
            return pt.X.ToString("0.###", CultureInfo.InvariantCulture) + " " +
                   pt.Y.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string ToPS(Point3d pt)
        {
            return pt.X.ToString("0.###", CultureInfo.InvariantCulture) + " " +
                   pt.Y.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static double RadToDeg(double radians)
        {
            return radians * 180.0 / System.Math.PI;
        }
        private static string Format(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }























    }
}
