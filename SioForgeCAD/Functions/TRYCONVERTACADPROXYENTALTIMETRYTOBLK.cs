using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class TRYCONVERTACADPROXYENTALTIMETRYTOBLK
    {
        private enum TextSide { Gauche, Droit }

        private class ProxyData
        {
            public string TextValue { get; set; }
            public double TextRotation { get; set; }
            public Point3d TextCenter { get; set; }
            public DBObjectCollection ExplodedObjects { get; set; } = new DBObjectCollection();
            public DBObjectCollection ExplodedTransformedObjects { get; set; } = new DBObjectCollection();
            // Nouvelle propriété
            public TextSide Side { get; set; }
        }

        public static void Convert()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            SelectionFilter filter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "ACAD_PROXY_ENTITY") });
            var psr = ed.GetSelectionRedraw(selectionFilter: filter);

            if (psr.Status != PromptStatus.OK)
            {
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId objId in psr.Value.GetObjectIds())
                {
                    if (!(tr.GetObject(objId, OpenMode.ForRead) is Entity proxyEnt)) continue;

                    // 1. Extraction brute des composants du proxy
                    ProxyData data = ExtractDataFromProxy(proxyEnt);
                    if (data == null || string.IsNullOrEmpty(data.TextValue))
                    {
                        data?.ExplodedObjects.DeepDispose();
                        continue;
                    }

                    if (data.TextValue.Contains("%"))
                    {
                        // Cas A : C'est une pente
                        ProcessSlopeBlock(data, ed);
                    }
                    else
                    {
                        // Cas B : C'est une altimétrie (on cherche le point d'insertion optimal)
                        InsertAltimetryBlock(data, ed);
                    }

                    (data.ExplodedObjects).DeepDispose();
                    (data.ExplodedTransformedObjects).DeepDispose();
                }
                tr.Commit();
            }
        }

        private static void InsertAltimetryBlock(ProxyData data, Editor ed)
        {
            Point3d? insertionPoint = FindBestInsertionPoint(data);

            if (insertionPoint.HasValue)
            {
                double altValue = GetDoubleInString(data.TextValue);

                BlockReferences.InsertFromNameImportIfNotExist(
                    Settings.BlkAltimetry,
                    nameof(Settings.BlkAltimetry),
                    new Points(insertionPoint.Value),
                    ed.GetUSCRotation(AngleUnit.Radians),
                    new Dictionary<string, string> { { "ALTIMETRIE", altValue.ToString("#.00") } }
                );
            }
        }

        private static ProxyData ExtractDataFromProxy(Entity proxyEnt)
        {
            var data = new ProxyData();

            try { proxyEnt.Explode(data.ExplodedObjects); }
            catch { return null; }

            // --- PASSE 1 : Identification du texte (Identique) ---
            foreach (DBObject obj in data.ExplodedObjects)
            {
                if (obj is Entity ent && string.IsNullOrEmpty(data.TextValue))
                {
                    if (ent is DBText dbText)
                    {
                        data.TextValue = dbText.TextString;
                        data.TextRotation = dbText.Rotation;
                        data.TextCenter = dbText.GetExtents().Middle();
                    }
                    else if (ent is MText mText)
                    {
                        data.TextValue = mText.Text;
                        data.TextRotation = mText.Rotation;
                        data.TextCenter = mText.GetExtents().Middle();
                    }
                }
            }

            if (string.IsNullOrEmpty(data.TextValue)) return data;

            // --- PASSE 2 : Transformation et Debug ---
            Matrix3d invRot = Matrix3d.Rotation(-data.TextRotation, Vector3d.ZAxis, data.TextCenter);

            foreach (DBObject obj in data.ExplodedObjects)
            {
                if (obj is Entity ent)
                {
                    try
                    {
                        Entity transformedEnt = ent.GetTransformedCopy(invRot);
                        data.ExplodedTransformedObjects.Add(transformedEnt);
                    }
                    catch { }
                }
            }
            // --- PASSE 3 : Calcul du Side (sur les extents à 0,0) ---
            double globalCenterX = data.ExplodedTransformedObjects.GetExtents().Middle().X;
            if (data.TextCenter.X > globalCenterX)
            {
                data.Side = TextSide.Gauche;
            }
            else
            {
                data.Side = TextSide.Droit;
            }
            return data;
        }


        private static Point3d? FindBestInsertionPoint(ProxyData data)
        {
            Point3d? bestPoint = null;
            double maxDistance = -1.0;
            Vector3d textDir = new Vector3d(Math.Cos(data.TextRotation), Math.Sin(data.TextRotation), 0);

            foreach (DBObject obj in data.ExplodedObjects)
            {
                if (obj is Line line)
                {
                    Vector3d lineDir = line.EndPoint - line.StartPoint;
                    if (lineDir.GetNormal().IsParallelTo(textDir, new Tolerance(1e-4, 1e-4)))
                    {
                        double distStart = line.StartPoint.DistanceTo(data.TextCenter);
                        double distEnd = line.EndPoint.DistanceTo(data.TextCenter);
                        Point3d candidate = distEnd > distStart ? line.EndPoint : line.StartPoint;

                        if (IsPointOnExtents(candidate, data.ExplodedObjects.GetExtents()))
                        {
                            double dist = Math.Max(distStart, distEnd);
                            if (dist > maxDistance)
                            {
                                maxDistance = dist;
                                bestPoint = candidate;
                            }
                        }
                    }
                }
            }
            return bestPoint;
        }


        private static void ProcessSlopeBlock(ProxyData data, Editor ed)
        {
            if (!data.TextValue.Contains("%")) return;

            var rotation = data.TextRotation;

            if (data.Side == TextSide.Gauche)
            {
                // On inverse la rotation pour que le bloc pointe dans l'autre sens
                rotation += Math.PI;
            }

            var settings = CCP.AdjustAngleAndInversion(rotation);
            var attributes = new Dictionary<string, string>() {
                {"PENTE", data.TextValue },
                {"ANGLE_PENTE", settings.AdjustedAngleRadians.ToString() },
                {"SENS_PENTE", settings.BlocInverseState.ToString() },
            };

            Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(
                Settings.BlkSlopePercentage,
                nameof(Settings.BlkSlopePercentage),
                data.TextCenter.ToPoints(),
                ed.GetUSCRotation(AngleUnit.Radians),
                attributes);
        }

        private static double GetDoubleInString(string Text)
        {
            if (double.TryParse(Text.Trim(), out double Altimetrie))
            {
                return Altimetrie;
            }
            else
            {
                double? ExtractedAltitude = CotePoints.ExtractDoubleInStringFromPoint(Text.Trim(), false);
                return ExtractedAltitude ?? 0;
            }
        }

        private static bool IsPointOnExtents(Point3d pt, Extents3d ext, double tolerance = 1e-4)
        {
            // Un point est sur le bord s'il partage au moins une coordonnée (X, Y ou Z) 
            // avec le MinPoint ou le MaxPoint de l'extend global (avec une tolérance pour les imprécisions de calcul).
            return Math.Abs(pt.X - ext.MinPoint.X) < tolerance ||
                   Math.Abs(pt.X - ext.MaxPoint.X) < tolerance ||
                   Math.Abs(pt.Y - ext.MinPoint.Y) < tolerance ||
                   Math.Abs(pt.Y - ext.MaxPoint.Y) < tolerance ||
                   Math.Abs(pt.Z - ext.MinPoint.Z) < tolerance ||
                   Math.Abs(pt.Z - ext.MaxPoint.Z) < tolerance;
        }
    }
}
