using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class COPYGEOMETRYTOCLIPBOARDFORINDESIGN
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

                List<Entity> EntsClones = new List<Entity>();
                foreach (ObjectId id in SelectedEnts.Value.GetObjectIds())
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity ent)) continue;
                    var entClone = ent.Clone() as Entity;
                    EntsClones.Add(entClone);
                }

                //Max width / height : 57600 meters
                //double Scale = 25;
                //var EntsClonesOriginalExtend = EntsClones.GetExtents();
                //var VectorToOrigin = EntsClonesOriginalExtend.MinPoint.GetVectorTo(Point3d.Origin);
                //foreach (var entClone in EntsClones)
                //{
                //    entClone.TransformBy(Matrix3d.Displacement(VectorToOrigin)); //move ents to 0 0
                //    entClone.TransformBy(Matrix3d.Scaling(Scale, Point3d.Origin)); //scale from 0 0

                //}


                //var EntsClonesScaledExtend = EntsClones.GetExtents();
                double Scale = 25;
                double MaxSizeInMeters = 57600.0; // en mètres

                var EntsClonesOriginalExtend = EntsClones.GetExtents();
                var VectorToOrigin = EntsClonesOriginalExtend.MinPoint.GetVectorTo(Point3d.Origin);

                //Move all entities so that the lower-left corner is at (0,0)
                foreach (var entClone in EntsClones)
                {
                    entClone.TransformBy(Matrix3d.Displacement(VectorToOrigin));
                }

                //Apply initial scaling (×25) from the origin
                foreach (var entClone in EntsClones)
                {
                    entClone.TransformBy(Matrix3d.Scaling(Scale, Point3d.Origin));
                }

                //Get new extents after scaling
                var EntsClonesScaledExtend = EntsClones.GetExtents();
                var EntsClonesScaledExtendSize = EntsClonesScaledExtend.Size();

                double width = EntsClonesScaledExtendSize.Width;
                double height = EntsClonesScaledExtendSize.Height;

                //If the scaled drawing exceeds the maximum allowed size, reduce it proportionally
                double maxDimension = Math.Max(width, height);
                if (maxDimension > MaxSizeInMeters)
                {
                    double reductionFactor = MaxSizeInMeters / maxDimension;

                    //Apply the reduction scaling to all entities (from the origin)
                    foreach (var entClone in EntsClones)
                    {
                        entClone.TransformBy(Matrix3d.Scaling(reductionFactor, Point3d.Origin));
                    }
                }
                //Update EntsClonesScaledExtend
                EntsClonesScaledExtend = EntsClones.GetExtents();


                StringBuilder writer = new StringBuilder();
                {
                    writer.AppendLine("%!PS-Adobe-3.0 EPSF-3.0");
                    writer.AppendLine($"%%BoundingBox: {Format(EntsClonesScaledExtend.MinPoint.X)} {Format(EntsClonesScaledExtend.MinPoint.Y)} {Format(EntsClonesScaledExtend.MaxPoint.X)} {Format(EntsClonesScaledExtend.MaxPoint.Y)}");

                    foreach (var ent in EntsClones)
                    {
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

                        writer.AppendLine($"1 setlinewidth");
                        writer.AppendLine("1 setlinejoin"); //0 = biseau; 1 = Arrondi ; 2 = Chanfreiné 

                        writer.AppendLine($"{GetColor(ent)} setrgbcolor");
                        writer.AppendLine("stroke");
                        //writer.AppendLine("fill");
                    }
                    writer.AppendLine("showpage");
                    tr.Commit();
                }
                byte[] EPS = Encoding.ASCII.GetBytes(writer.ToString());
                Clipboard.Clear();
                SioForgeCAD.Commun.Mist.ClipboardHelper.SetRawDataToClipboard("Encapsulated PostScript", EPS);
            }
        }

        private static void WriteLine(StringBuilder w, Point3d p1, Point3d p2)
        {
            w.AppendLine($"{Format(p1)} moveto");
            w.AppendLine($"{Format(p2)} lineto");
        }

        private static void WriteArc(StringBuilder w, Arc arc)
        {
            using (var x = arc.ToCircularArc2d())
            {
                w.AppendLine(GetEpsFromArc(x));
            }
        }

        private static void WritePolyline(StringBuilder w, Polyline pl)
        {
            if (pl.NumberOfVertices < 2)
                return;

            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p0 = pl.GetPoint2dAt(i);
                Point2d p1 = pl.GetPoint2dAt((i + 1) % pl.NumberOfVertices);
                SegmentType type = pl.GetSegmentType(i);


                if (type == SegmentType.Arc)
                {
                    CircularArc2d arc2d = pl.GetArcSegment2dAt(i);
                    w.AppendLine(GetEpsFromArc(arc2d));
                }
                else
                {
                    if (i == 0)
                    {
                        w.AppendLine($"{Format(p0)} moveto");
                    }
                    w.AppendLine($"{Format(p1)} lineto");
                }
            }

            try
            {
                if (pl.Closed || (pl.NumberOfVertices > 2 && pl.GetPoint2dAt(0).IsEqualTo(pl.GetPoint2dAt(pl.NumberOfVertices))))
            {
                w.AppendLine("closepath");
            }
            }catch(Exception ex)
            {
                Generic.WriteMessage("Echec de l'ajout d'une polyligne");
            }

        }

        public static string GetColor(Entity Ent)
        {
            Color Color = Ent.Color;
            if (Ent.Color.IsByLayer)
            {
                string EntityLayer = Ent.Layer;
                ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(EntityLayer);
                Color = Layers.GetLayerColor(LayerTableRecordObjId);
            }
            return $"{Color.ColorValue.R / 255} {Color.ColorValue.G / 255} {Color.ColorValue.B / 255}";
        }

        private static string GetEpsFromArc(CircularArc2d arc2d)
        {
            Interval arc2dInterval = arc2d.GetInterval();
            double startParam = arc2dInterval.LowerBound;
            double endParam = arc2dInterval.UpperBound;
            Point2d sp2d = arc2d.StartPoint;//arc2d.EvaluatePoint(startParam);
            Point2d ep2d = arc2d.EndPoint;//arc2d.EvaluatePoint(endParam);
            Point2d cp2d = arc2d.Center;
            double startAngle = (new Line2d(cp2d, sp2d)).Direction.Angle;
            double endAngle = (new Line2d(cp2d, ep2d)).Direction.Angle;
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}", Format(cp2d.X), Format(cp2d.Y), Format(arc2d.Radius), Format(RadToDeg(startAngle)), Format(RadToDeg(endAngle)), arc2d.IsClockWise ? "arcn" : "arc");
        }


        private static double RadToDeg(double radians)
        {
            return radians * 180.0 / System.Math.PI;
        }
        private static string Format(double value)
        {
            return (value).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Format(Point2d pt)
        {
            return Format(pt.X) + " " + Format(pt.Y);
        }

        private static string Format(Point3d pt)
        {
            return Format(pt.X) + " " + Format(pt.Y);
        }





















    }
}
