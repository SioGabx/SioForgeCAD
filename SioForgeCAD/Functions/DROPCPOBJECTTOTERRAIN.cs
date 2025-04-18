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
            Editor ed = Generic.GetEditor();

            using (Polyline terrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne comme base de terrain"))
            {
                if (terrainBasePolyline == null) return;

                var promptOptions = new PromptSelectionOptions
                {
                    MessageForAdding = "Sélectionnez les entités à projeter sur le terrain via l'UCS courant"
                };

                var selectedObjects = ed.GetSelection(promptOptions);
                if (selectedObjects.Status != PromptStatus.OK) { return; }

                ObjectId[] objectIds = selectedObjects.Value.GetObjectIds();
                TransformAlign(objectIds, terrainBasePolyline, false);

                var alignChoice = ed.GetOptions("Voulez-vous aligner les entités ?", "Oui", "Non");
                if (alignChoice.Status == PromptStatus.OK && alignChoice.StringResult == "Oui")
                {
                    TransformAlign(objectIds, terrainBasePolyline, true);
                }
            }
        }

        private static void TransformAlign(ObjectId[] objectIds, Polyline terrain, bool align)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId objId in objectIds)
                {
                    if (Layers.IsEntityOnLockedLayer(objId)) { continue; }

                    Entity entity = objId.GetEntity(OpenMode.ForWrite);
                    if (entity is BlockReference blockRef)
                    {
                        terrain.DropBlockReference(blockRef, align);
                    }
                }
                tr.Commit();
            }
        }

        private static Vector3d GetUCSPerpendicularVector(Polyline terrainBasePolyline)
        {
            Matrix3d ucsMatrix = Generic.GetEditor().CurrentUserCoordinateSystem;
            return ucsMatrix.CoordinateSystem3d.Yaxis.MultiplyBy(-terrainBasePolyline.Length);
        }

        private static void DropBlockReference(this Polyline terrain, BlockReference blkRef, bool alignToSegment)
        {
            List<(Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector)> intersections = new List<(Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector)>();

            for (int i = 0; i < terrain.GetReelNumberOfVertices(); i++)
            {
                var segment = terrain.GetSegmentAt(i);
                Vector3d perpendicularVector = GetUCSPerpendicularVector(terrain);
                Vector3d offsetVector = perpendicularVector.MultiplyBy(Lines.GetLength(segment.StartPoint, blkRef.Position));

                using (Line segmentLine = new Line(segment.StartPoint.Flatten(), segment.EndPoint.Flatten()))
                using (Line perpendicularLine = new Line(blkRef.Position.Add(-offsetVector).Flatten(), blkRef.Position.Add(offsetVector).Flatten()))
                {
                    if (Lines.AreLinesCutting(segmentLine, perpendicularLine, out Point3dCollection intersectionPoints))
                    {
                        intersections.Add((intersectionPoints[0], segment.StartPoint.GetVectorTo(segment.EndPoint), offsetVector));
                    }
                }
            }

            if (intersections.Count == 0) { return; }

            var closestIntersection = FindClosestIntersection(intersections, blkRef.Position);
            Vector3d translationVector = closestIntersection.Point - blkRef.Position;
            blkRef.TransformBy(Matrix3d.Displacement(translationVector));

            if (alignToSegment)
            {
                AlignEntityToSegment(blkRef, blkRef.Position, closestIntersection.SegmentVector, closestIntersection.PerpendicularVector);
            }
        }

        private static (Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector) FindClosestIntersection(List<(Point3d Point, Vector3d, Vector3d)> intersections, Point3d referencePoint)
        {
            double minDistance = double.MaxValue;
            (Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector) closestIntersection = (Point3d.Origin, Vector3d.ZAxis, Vector3d.ZAxis);

            foreach (var intersection in intersections)
            {
                double distance = Lines.GetLength(intersection.Point, referencePoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIntersection = intersection;
                }
            }

            return closestIntersection;
        }

        private static void AlignEntityToSegment(BlockReference ent, Point3d basePoint, Vector3d segmentVector, Vector3d perpendicularVector)
        {
            if (ent == null) return;

            Matrix3d resetRotationMatrix = Matrix3d.Rotation(-ent.Rotation, Vector3d.ZAxis, ent.Position);
            ent.TransformBy(resetRotationMatrix);
            ent.Rotation = 0;
            Vector3d BaseOrientVector = Vector3d.ZAxis.GetPerpendicularVector();

            //Always face up, ignore segmentVector direction
            if (segmentVector.IsVectorOnRightSide(perpendicularVector))
            {
                segmentVector = segmentVector.Inverse();
            }
            double rotationAngle = BaseOrientVector.GetAngleTo(segmentVector, Vector3d.ZAxis);
            Matrix3d finalRotation = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, basePoint);
            ent.TransformBy(finalRotation);
        }
    }
}
