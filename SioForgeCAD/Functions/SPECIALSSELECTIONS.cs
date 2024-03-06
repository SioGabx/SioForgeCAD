using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

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
            Editor ed = Generic.GetEditor();
            Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne qui delimite / croise les objects à selectionner", false);
            if (TerrainBasePolyline is null)
            {
                return;
            }
            Point3dCollection collection = TerrainBasePolyline.GetPoints().ToPoint3dCollection();
            var Objects = ed.SelectCrossingPolygon(collection);
            ed.SetImpliedSelection(Objects.Value);
        }

        public static void InsideStrictPolyline()
        {
            Editor ed = Generic.GetEditor();
            Polyline TerrainBasePolyline = ed.GetPolyline("\nSélectionnez une polyligne qui delimite les objects à selectionner", false);
            if (TerrainBasePolyline is null)
            {
                return;
            }
            Point3dCollection collection = TerrainBasePolyline.GetPoints().ToPoint3dCollection();
            var Objects = ed.SelectWindowPolygon(collection);
            ed.SetImpliedSelection(Objects.Value);
        }
    }
}

