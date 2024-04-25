using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.ViewModel.PointCloudManager;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
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
            using (Polyline Boundary = ed.GetPolyline("\nSélectionnez une polyligne qui delimite / croise les objects à selectionner", false))
            {
                if (Boundary is null)
                {
                    return;
                }
                Boundary.Cleanup();
                Point3dCollection collection = Boundary.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
                var SavedView = ed.GetCurrentView();
                Boundary?.GetExtents().ZoomExtents();
                var SelectCrossingPolygonResult = ed.SelectCrossingPolygon(collection);
                if (SelectCrossingPolygonResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Une erreur s'est produite lors de la selection");
                    ed.SetCurrentView(SavedView);
                    return;
                }
                var Objects = SelectCrossingPolygonResult?.Value?.GetObjectIds()?.ToList();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Objects.Remove(Boundary.ObjectId);
                    ed.SetImpliedSelection(Objects.ToArray());
                    ed.SetCurrentView(SavedView);
                    tr.Commit();
                }
                ed.Regen();
            }
        }

        public static void InsideStrictPolyline()
        {
            //https://forums.autodesk.com/t5/net/cannot-get-the-entities-using-selectcrossingpolygon-and/td-p/6384137
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Polyline Boundary = ed.GetPolyline("\nSélectionnez une polyligne qui delimite les objects à selectionner", false))
            {
                if (Boundary is null)
                {
                    return;
                }
                Boundary.Cleanup();
                Point3dCollection collection = Boundary.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
                var SavedView = ed.GetCurrentView();
                Boundary?.GetExtents().ZoomExtents();

                var SelectWindowPolygonResult = ed.SelectWindowPolygon(collection);
                if (SelectWindowPolygonResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Une erreur s'est produite lors de la selection");
                    ed.SetCurrentView(SavedView); return;
                }
                var Objects = SelectWindowPolygonResult?.Value;
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
}
