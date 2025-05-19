using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class REGIONFORSKETCHUP
    {
        public static void GenerateRegionFromBoundaries()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            PromptSelectionResult psr = ed.GetSelectionRedraw(
                "Sélectionnez les entités fermées (polylignes, cercles...) :",
                false,
                false,
                new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,CIRCLE,ELLIPSE,POLYLINE") }));

            if (psr.Status != PromptStatus.OK)
            {
                Generic.WriteMessage("Aucune entité valide sélectionnée.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                int successCount = 0;

                foreach (SelectedObject selObj in psr.Value)
                {
                    Entity ent = selObj?.ObjectId.GetDBObject() as Entity;
                    if (ent == null) continue;

                    DBObjectCollection curves = new DBObjectCollection
                    {
                        ent.Clone() as Entity
                    };

                    DBObjectCollection regions = Region.CreateFromCurves(curves);
                    curves.DeepDispose();
                    if (regions.Count == 0)
                    {
                        Generic.WriteMessage($"Échec : Impossible de créer une région pour l'entité {ent.GetType().Name} (ID: {ent.ObjectId}).");
                        continue;
                    }
                    foreach (Region region in regions)
                    {
                        if (region == null) continue;
                        ;
                        string EntityLayer = ent.Layer;
                        ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(EntityLayer);
                        Autodesk.AutoCAD.Colors.Color LayerColor = Layers.GetLayerColor(LayerTableRecordObjId);
                        if (ent.Color.IsByLayer)
                        {
                            region.Color = LayerColor;
                        }
                        else
                        {
                            region.Color = ent.Color;
                        }
                        region.Layer = "0";
                        region.AddToDrawingCurrentTransaction();
                        successCount++;
                    }
                }
                tr.Commit();
                ed.WriteMessage($"\n{successCount} région(s) créée(s) avec succès.");
            }
        }
    }
}
