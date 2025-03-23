using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Overrules.PolylineGripOverrule
{
    public class PolylineJig
    {
        private readonly Document _doc;
        private readonly Database _db;
        private readonly Editor _ed;
        private readonly Autodesk.AutoCAD.DatabaseServices.Polyline _polyline;


        private TransientManager _tsManager = TransientManager.CurrentTransientManager;
        private Autodesk.AutoCAD.DatabaseServices.Polyline _tspolyline;

        private Point3d _basePoint;
        private Point3d _finalPoint;

        public PolylineJig(Autodesk.AutoCAD.DatabaseServices.Polyline polyline, Point3d initPoint)
        {
            _doc = Generic.GetDocument();
            _db = _doc.Database;
            _ed = _doc.Editor;
            _polyline = polyline;
            _basePoint = initPoint;
            using (var tran = _db.TransactionManager.StartOpenCloseTransaction())
            {

                tran.Commit();
            }
        }


        public PromptPointResult Drag()
        {
            try
            {
                _ed.PointMonitor += Editor_PointMonitor;

                var opt = new PromptPointOptions("\nSelect line's extended end point:");
                opt.AllowNone = false;
                opt.UseBasePoint = true;
                opt.BasePoint = _basePoint;
                opt.UseDashedLine = false;

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
            _finalPoint = pt;
        }

        private void ClearGhosts()
        {
            if (_tspolyline != null)
            {
                _tsManager.EraseTransient(_tspolyline, new IntegerCollection());
                _tspolyline.Dispose();
            }
        }

        private void DrawGhosts(Point3d mousePoint)
        {
            try
            {
                _tspolyline = new Autodesk.AutoCAD.DatabaseServices.Polyline()
                {

                };
                bool HasFound = false;
                for (int i = 0; i<_polyline.GetReelNumberOfVertices(); i++)
                {
                    if (!HasFound && _polyline.GetPoint3dAt(i).IsEqualTo(_basePoint, Generic.MediumTolerance))
                    {
                        HasFound = true;
                        _tspolyline.AddVertex(mousePoint);
                    }
                    else
                    {
                        _tspolyline.AddVertex(_polyline.GetPoint3dAt(i));
                    }
                }
                _tspolyline.Closed = _polyline.Closed;
                _tsManager.AddTransient(_tspolyline, TransientDrawingMode.Highlight, 126, new IntegerCollection());
            }
            catch { }
        }



    }
}
