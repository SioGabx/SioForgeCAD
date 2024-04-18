using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class SPECIALSSELECTIONS
    {
        public static void AllOnCurrentLayer()
        {
            Editor ed = Generic.GetEditor();
            TypedValue[] tvs = new TypedValue[] {
                new TypedValue((int)DxfCode.LayerName,Layers.GetCurrentLayerName()),
               // new TypedValue((int)DxfCode.Start,"LINE"),
            };
            SelectionFilter sf = new SelectionFilter(tvs);
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.SetImpliedSelection(psr.Value);
        }

        public static void InsideCrossingPolyline()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne qui delimite / croise les objects à selectionner", false);
            if (TerrainBasePolyline is null)
            {
                return;
            }
            Point3dCollection collection = TerrainBasePolyline.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
            var SavedView = ed.GetCurrentView();
            TerrainBasePolyline?.GetExtents().ZoomExtents();
            var Objects = ed.SelectCrossingPolygon(collection).Value.GetObjectIds().ToList();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Objects.Remove(TerrainBasePolyline.ObjectId);
                ed.SetImpliedSelection(Objects.ToArray());
                ed.SetCurrentView(SavedView);
                tr.Commit();
            }
            ed.Regen();
        }

        public static void InsideStrictPolyline()
        {
            //https://forums.autodesk.com/t5/net/cannot-get-the-entities-using-selectcrossingpolygon-and/td-p/6384137
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne qui delimite les objects à selectionner", false);
            if (TerrainBasePolyline is null)
            {
                return;
            }
            Point3dCollection collection = TerrainBasePolyline.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
            var SavedView = ed.GetCurrentView();
            TerrainBasePolyline?.GetExtents().ZoomExtents();
            var Objects = ed.SelectWindowPolygon(collection).Value;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ed.SetImpliedSelection(Objects);
                ed.SetCurrentView(SavedView);
                tr.Commit();
            }
            ed.Regen();
        }
    }
}
