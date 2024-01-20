using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace SioForgeCAD.Functions
{
    public static class RRR
    {
        public static void Rotate()
        {
            Editor editor = Generic.GetEditor();
            PromptSelectionOptions options = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez les entités sur lesquels effectuer une rotation"
            };
            var Selection = editor.GetSelection(options);

            PromptPointResult basePointResult = editor.GetPoint("Select the base point: ");
            if (basePointResult.Status != PromptStatus.OK) { return; }
            Points basePoint = Points.GetFromPromptPointResult(basePointResult);

            PromptPointOptions secondPointOptions = new PromptPointOptions("Select the second point: ")
            {
                BasePoint = basePoint.SCG,
                UseBasePoint = true
            };
            PromptPointResult secondPointResult = editor.GetPoint(secondPointOptions);
            if (secondPointResult.Status != PromptStatus.OK) { return; }
            Points secondPoint = Points.GetFromPromptPointResult(secondPointResult);

            // Calculate the initial angle
            double initialAngle = basePoint.SCG.GetAngleWith(secondPoint.SCG);

            editor.WriteMessage($"Initial Angle: {initialAngle}\n");
            Database db = Generic.GetDatabase();
            ObjectId[] SelectedObjectIds = Selection.Value.GetObjectIds();
            List<DBObject> Objects = new List<DBObject>();
            List<DBObject> StaticObjects = new List<DBObject>();
            try
            {
                using (Transaction acTrans = db.TransactionManager.StartTransaction())
                {
                    foreach (ObjectId SelectedObjectId in SelectedObjectIds)
                    {
                        DBObject DbObject = SelectedObjectId.GetDBObject(OpenMode.ForWrite);
                        Objects.Add(DbObject.Clone() as DBObject);
                        StaticObjects.Add(DbObject.Clone() as DBObject);
                        SelectedObjectId.EraseObject();
                    }
                    acTrans.Commit();
                }
                using (Transaction acTrans = db.TransactionManager.StartTransaction())
                {
                    GetAngleTransient getAngleTransient = new GetAngleTransient(basePoint, secondPoint, Objects.ToDBObjectCollection(), null)
                    {
                        SetStaticEntities = StaticObjects.ToDBObjectCollection()
                    };
                    var Angle = getAngleTransient.GetAngle("New anfle prompt");

                    foreach (DBObject SelectedObjectId in Objects)
                    {
                        if (SelectedObjectId is Entity SelectedEntity)
                        {
                            if (Angle.PromptAngleResult.Status == PromptStatus.OK)
                            {
                                Matrix3d rotationMatrix = Matrix3d.Rotation(Angle.Angle - initialAngle, Vector3d.ZAxis, basePoint.SCG);
                                SelectedEntity.TransformBy(rotationMatrix);
                            }
                            SelectedEntity.AddToDrawing();
                        }
                    }
                    acTrans.Commit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

    }
}
