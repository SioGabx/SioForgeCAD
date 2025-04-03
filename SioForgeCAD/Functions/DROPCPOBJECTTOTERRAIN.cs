using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class DROPCPOBJECTTOTERRAIN
    {
        public static void Project()
        {
            Editor ed = Generic.GetEditor();

            using (Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne comme base de terrain"))
            {
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
                TransformAlign(AllSelectedObjectIds, TerrainBasePolyline, false);
                var Align = ed.GetOptions("Voullez vous alligner les entités", "Oui", "Non");
                if (Align.Status == PromptStatus.OK && Align.StringResult == "Oui")
                {
                    TransformAlign(AllSelectedObjectIds, TerrainBasePolyline, true);
                }
            }
        }

        private static void TransformAlign(ObjectId[] ObjectIds, Polyline Terrain, bool Align)
        {
            Database db = Generic.GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ObjId in ObjectIds)
                {
                    if (Layers.IsEntityOnLockedLayer(ObjId))
                    {
                        continue;
                    }

                    //check if ent is on a group : then move the group ? https://adndevblog.typepad.com/autocad/2012/04/how-to-detect-whether-entity-is-belong-to-any-group-or-not.html
                    //if ent is block, then we move the point, else if entity, first get extend and then move it
                    using (Entity SelectedEntity = ObjId.GetEntity(OpenMode.ForWrite))
                    {
                        if (SelectedEntity is BlockReference blkRef)
                        {
                            Terrain.DropBlockReference(blkRef, Align);
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

        private static void DropBlockReference(this Polyline TerrainBasePolyline, BlockReference blkRef, bool AlignToSegment)
        {
            List<(Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector)> ListOfPossibleIntersections = new List<(Point3d, Vector3d, Vector3d)>();
            for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < TerrainBasePolyline.GetReelNumberOfVertices(); PolylineSegmentIndex++)
            {
                var PolylineSegment = TerrainBasePolyline.GetSegmentAt(PolylineSegmentIndex);
                Vector3d PerpendicularVector = GetUCSPerpendicularVector(TerrainBasePolyline);
                Vector3d PerpendicularVectorIntersection = PerpendicularVector.MultiplyBy(Lines.GetLength(PolylineSegment.StartPoint, blkRef.Position));
                using (Line SegmentLine = new Line(PolylineSegment.StartPoint.Flatten(), PolylineSegment.EndPoint.Flatten()))
                using (Line PerpendicularLine = new Line(blkRef.Position.Add(-PerpendicularVectorIntersection).Flatten(), blkRef.Position.Add(PerpendicularVectorIntersection).Flatten()))
                {
                    if (!Lines.AreLinesCutting(SegmentLine, PerpendicularLine, out Point3dCollection IntersectionPointsFounds))
                    {
                        continue;
                    }
                    ListOfPossibleIntersections.Add((IntersectionPointsFounds[0], PolylineSegment.StartPoint.GetVectorTo(PolylineSegment.EndPoint), PerpendicularVectorIntersection));
                }
            }
            if (ListOfPossibleIntersections.Count == 0)
            {
                return;
            }
            (Point3d Point, Vector3d SegmentVector, Vector3d PerpendicularVector) FinalIntersection = (Point3d.Origin, Vector3d.ZAxis, Vector3d.ZAxis);
            double MinimalDistance = double.MaxValue;
            foreach (var Intersection in ListOfPossibleIntersections)
            {
                double NewPointDistance = Lines.GetLength(Intersection.Point, blkRef.Position);
                if (NewPointDistance < MinimalDistance)
                {
                    MinimalDistance = NewPointDistance;
                    FinalIntersection = Intersection;
                }
            }
            Vector3d translationVector = FinalIntersection.Point - blkRef.Position;
            blkRef.TransformBy(Matrix3d.Displacement(translationVector));
            if (AlignToSegment)
            {
                AlignEntityToSegment(blkRef, blkRef.Position, FinalIntersection.SegmentVector, FinalIntersection.PerpendicularVector);
            }

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
