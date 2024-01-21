using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BLKINSEDIT
    {
        public static void MoveBasePoint()
        {
            Editor editor = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            TypedValue[] filterList = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez un bloc",
                SingleOnly = true,
                SinglePickInSpace = true,
                RejectObjectsOnLockedLayers = true
            };

            PromptSelectionResult promptResult;

            while (true)
            {
                promptResult = editor.GetSelection(selectionOptions, new SelectionFilter(filterList));

                if (promptResult.Status == PromptStatus.Cancel)
                {
                    return;
                }
                else if (promptResult.Status == PromptStatus.OK)
                {
                    if (promptResult.Value.Count > 0)
                    {
                        break;
                    }
                }
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId blockRefId = promptResult.Value.GetObjectIds().First();
                blockRefId.RegisterHighlight();
                PromptPointOptions pointOptions = new PromptPointOptions("Veuillez sélectionner son nouveau point de base : ");
                PromptPointResult pointResult = editor.GetPoint(pointOptions);
                blockRefId.RegisterUnhighlight();
                if (pointResult.Status != PromptStatus.OK)
                {
                    return;
                }
                Point3d selectedPoint = pointResult.Value;

                Vector3d FixPosition;
                if (!(tr.GetObject(blockRefId, OpenMode.ForWrite) is BlockReference blockRef))
                {
                    return;
                }
                Point3d selectedPointInBlockRefSpace = selectedPoint.TransformBy(blockRef.BlockTransform.Inverse());
                Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
                Point3d rotatedPoint = selectedPointInBlockRefSpace.TransformBy(rotationMatrix);
                BlockTableRecord blockDef = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForWrite) as BlockTableRecord;

                foreach (ObjectId entId in blockDef)
                {
                    Entity entity = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
                    entity?.TransformBy(Matrix3d.Displacement(rotatedPoint.GetAsVector()));
                }
                FixPosition = selectedPoint - blockRef.Position;
                blockRef.DowngradeOpen();

                ObjectIdCollection iter = blockDef.GetBlockReferenceIds(true, false);
                foreach (ObjectId entId in iter)
                {
                    if (entId.GetDBObject(OpenMode.ForWrite) is BlockReference otherBlockRef)
                    {
                        otherBlockRef.TransformBy(Matrix3d.Displacement(FixPosition));
                        otherBlockRef.RecordGraphicsModified(true);
                    }
                }
                tr.Commit();
            }
        }
    }
}
