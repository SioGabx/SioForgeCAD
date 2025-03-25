using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Commun.Overrules.PolylineGripOverrule
{
    public class PolyMiddleGripJig
    {
        private readonly Editor _ed;
        private readonly Autodesk.AutoCAD.DatabaseServices.Polyline _polyline;

        private TransientManager _tsManager = TransientManager.CurrentTransientManager;
        private Autodesk.AutoCAD.DatabaseServices.Polyline _tspolyline;

        private Point3d _basePoint;

        public PolyMiddleGripJig(Autodesk.AutoCAD.DatabaseServices.Polyline polyline, Point3d initPoint)
        {
            _ed = Generic.GetEditor();
            _polyline = polyline;
            _basePoint = initPoint;
        }


        public PromptPointResult Drag()
        {
            try
            {
                _ed.PointMonitor += Editor_PointMonitor;

                var opt = new PromptPointOptions("\nSpécifiez un nouveau point de sommet")
                {
                    AllowNone = false,
                    UseBasePoint = true,
                    BasePoint = _basePoint,
                    UseDashedLine = false
                };

                return _ed.GetPoint(opt);
            }
            finally
            {
                ClearGhosts();
                _ed.PointMonitor -= Editor_PointMonitor;
            }
        }

        private void Editor_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            ClearGhosts();
            var pt = e.Context.ComputedPoint;
            DrawGhosts(pt);
        }

        private void ClearGhosts()
        {
            if (_tspolyline != null)
            {
                _tsManager.EraseTransient(_tspolyline, TransientManager.CurrentTransientManager.GetViewPortsNumbers());
                _tspolyline.Dispose();
            }
        }

        private void DrawGhosts(Point3d mousePoint)
        {
            try
            {
                _tspolyline = new Autodesk.AutoCAD.DatabaseServices.Polyline();

                for (int i = 0; i < _polyline.GetReelNumberOfVertices(); i++)
                {
                    if (_polyline.GetPoint3dAt(i).IsEqualTo(_basePoint, Generic.MediumTolerance))
                    {
                        _tspolyline.AddVertex(mousePoint);
                    }
                    else
                    {
                        _tspolyline.AddVertex(_polyline.GetPoint3dAt(i));
                    }
                }
                _tspolyline.Closed = _polyline.Closed;
                _tsManager.AddTransient(_tspolyline, TransientDrawingMode.Highlight, 126, TransientManager.CurrentTransientManager.GetViewPortsNumbers());
            }
            catch { }
        }
    }
}
