using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Commun.Overrules.PolylineGripOverrule
{
    public class PolyGripJig
    {
        private readonly Editor _ed;
        private readonly Autodesk.AutoCAD.DatabaseServices.Polyline _polyline;

        private TransientManager _tsManager = TransientManager.CurrentTransientManager;
        private Autodesk.AutoCAD.DatabaseServices.Polyline _tspolyline;

        private readonly Point3dCollection _points;
        private readonly Point3d _basePoint;

        public PolyGripJig(Autodesk.AutoCAD.DatabaseServices.Polyline Polyline, Point3d InitPoint, Point3dCollection Points)
        {
            _ed = Generic.GetEditor();
            _polyline = Polyline;
            _basePoint = InitPoint;
            _points = Points;
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

                var Result = _ed.GetPoint(opt);

                return Result;
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
                Vector3d TransformVector = _basePoint.GetVectorTo(mousePoint);
                var TransformMatrix = Matrix3d.Displacement(TransformVector);
                for (int i = 0; i < _polyline.GetReelNumberOfVertices(); i++)
                {
                    if (_points.ContainsTolerance(_polyline.GetPoint3dAt(i), Generic.MediumTolerance))
                    {
                        _tspolyline.AddVertex(_polyline.GetPoint3dAt(i).TransformBy(TransformMatrix));
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
