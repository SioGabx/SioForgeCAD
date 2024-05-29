using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class GETINNERCENTROID
    {
        public static void Get()
        {
            Editor ed = Generic.GetEditor();
            using (var poly = ed.GetPolyline("Selectionnez une polyline", false, false))
            using (var polygon = poly.ToPolygon(10))
            {
                var PtnsCollection = polygon.GetPoints().ToPoint3dCollection();
                PtnsCollection.Add(PtnsCollection[0]);
                var pnts = PolygonOperation.GetInnerCentroid(polygon, 5);
                pnts.AddToDrawing();
            }
        }
    }
}
