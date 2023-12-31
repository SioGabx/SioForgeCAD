﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class DROPCPOBJECTTOTERRAIN
    {
        public static void Project()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Polyline TerrainBasePolyline = LinesExtentions.AskForSelection("Sélectionnez une polyligne comme base de terrain");
            if (TerrainBasePolyline == null)
            {
                return;
            }
            var PromptSelectEntitiesOptions = new PromptSelectionOptions()
            {
                MessageForAdding = "Selectionnez les entités à projeter sur le terrain via le UCS courrant"
            };

            var AllSelectedObject = ed.GetSelection(PromptSelectEntitiesOptions);

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }
            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();

            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId SelectedObjectId in AllSelectedObjectIds)
                {
                    if (Layers.IsLayerLocked(SelectedObjectId))
                    {
                        continue;
                    }
                    using (Entity SelectedEntity = SelectedObjectId.GetEntity(OpenMode.ForWrite))
                    {
                        if (SelectedEntity is BlockReference blkRef)
                        {
                            TerrainBasePolyline.DropBlockReference(blkRef);
                        }
                    }
                }
                acTrans.Commit();
            }
        }

        private static Vector3d GetUCSPerpendicularVector(Polyline TerrainBasePolyline)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Matrix3d ucsMatrix = ed.CurrentUserCoordinateSystem;
            return ucsMatrix.CoordinateSystem3d.Yaxis.MultiplyBy(-TerrainBasePolyline.Length);
        }

        private static void DropBlockReference(this Polyline TerrainBasePolyline, BlockReference blkRef)
        {
            List<Point3d> ListOfPossibleIntersections = new List<Point3d>();
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < Polylines.GetVerticesMaximum(TerrainBasePolyline); PolylineSegmentIndex++)
            {
                var PolylineSegment = Polylines.GetSegmentPoint(TerrainBasePolyline, PolylineSegmentIndex);
                Vector3d PerpendicularVector = GetUCSPerpendicularVector(TerrainBasePolyline);
                Vector3d PerpendicularVectorIntersection = PerpendicularVector.MultiplyBy(Lines.GetLength(PolylineSegment.PolylineSegmentStart, blkRef.Position));
                using (Line SegmentLine = new Line(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd))
                using (Line PerpendicularLine = new Line(blkRef.Position.Add(-PerpendicularVectorIntersection), blkRef.Position.Add(PerpendicularVectorIntersection)))
                {
                    if (!Lines.AreLinesCutting(SegmentLine, PerpendicularLine, out Point3dCollection IntersectionPointsFounds))
                    {
                        continue;
                    }
                    ListOfPossibleIntersections.Add(IntersectionPointsFounds[0]);
                }
            }
            if (ListOfPossibleIntersections.Count == 0)
            {
                return;
            }
            Point3d FinalPoint = Point3d.Origin;
            double MinimalDistance = double.MaxValue;
            foreach (Point3d IntersectionPointFound in ListOfPossibleIntersections)
            {
                double NewPointDistance = Lines.GetLength(IntersectionPointFound, blkRef.Position);
                if (NewPointDistance < MinimalDistance)
                {
                    MinimalDistance = NewPointDistance;
                    FinalPoint = IntersectionPointFound;
                }
            }
            Vector3d translationVector = FinalPoint - blkRef.Position;
            blkRef.TransformBy(Matrix3d.Displacement(translationVector));

        }

    }
}
