using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class DRAWBOUNDINGBOX
    {
        public static void Draw()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var result = ed.GetSelectionRedraw();
            if (result.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptKeywordOptions pko = new PromptKeywordOptions($"Voullez vous les bounding box pour le SCU courrant ou minimal ?");
                pko.Keywords.Add("SCU");
                pko.Keywords.Add("Minimal");
                pko.Keywords.Default = "SCU";
                pko.AllowNone = false;
                PromptResult res = ed.GetKeywords(pko);
                if (res.Status != PromptStatus.OK)
                {
                    return;
                }

                foreach (ObjectId SelectedEntityObjId in result.Value.GetObjectIds())
                {
                    Entity ent = SelectedEntityObjId.GetEntity();
                    if (res.StringResult == "SCU")
                    {
                        ent.GetExtents().GetGeometry().AddToDrawingCurrentTransaction();
                    }
                    else
                    {
                        if (ent is BlockReference blockRef)
                        {
                            using (var blockRefClone = blockRef.Clone() as BlockReference)
                            {
                                blockRefClone.Rotation = 0;
                                var extents = blockRefClone.GetExtents();
                                var transform = blockRef.Rotation;
                                var Geo = extents.GetGeometry();
                                Geo.TransformBy(Matrix3d.Rotation(transform, Vector3d.ZAxis, blockRef.Position));
                                Geo.AddToDrawingCurrentTransaction();
                            }
                        }

                        //TODO
                        //https://github.com/cansik/LongLiveTheSquare/blob/master/U4LongLiveTheSquare/MinimalBoundingBox.cs
                        else
                        {
                            ent.GetExtents().GetGeometry().AddToDrawingCurrentTransaction();
                        }
                    }
                }

                tr.Commit();
            }
        }

      

        public static void DrawExplodedExtends()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            var result = ed.GetSelectionRedraw();
            if (result.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId SelectedEntityObjId in result.Value.GetObjectIds())
                {
                    var ent = SelectedEntityObjId.GetEntity();
                    foreach (var item in ent.GetExplodedExtents())
                    {
                        item.GetGeometry().AddToDrawing(5);
                    }
                }
                tr.Commit();
            }
        }
    }
}
