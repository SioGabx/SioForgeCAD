using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun.Overrules
{
    internal class CopyGrip : GripData
    {
        public CopyGrip()
        {
            ForcedPickOn = false;
            GizmosEnabled = false;
            DrawAtDragImageGripPoint = false;
            IsPerViewport = false;
            ModeKeywordsDisabled = true;
            RubberBandLineDisabled = false;
            TriggerGrip = true;
            HotGripInvokesRightClick = false;
        }

        public ObjectId EntityId { get; set; } = ObjectId.Null;

        public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
        {
            var unit = worldDraw.Viewport.GetNumPixelsInUnitSquare(GripPoint);
            var gripHeight = 2.5 * gripSizeInPixels / unit.X;
            Generic.WriteMessage(gripSizeInPixels + " / " + gripHeight);

            var x = GripPoint.X;
            var y = GripPoint.Y;
            var offset = gripHeight / 2.0;

            //var points = new Point3dCollection();
            //points.Add(new Point3d(x - offset, y, 0.0));
            //points.Add(new Point3d(x, y - offset, 0.0));
            //points.Add(new Point3d(x + offset, y, 0.0));
            //points.Add(new Point3d(x, y + offset, 0.0));

            //Point3d center = new Point3d(x, y, 0.0);
            //var radius = offset;

            double WidthCross = ((gripHeight / 3) / 2) * 0.8;

            Point3d Origin = new Point3d(x, y, 0.0);
            Point3d OriginTop = new Point3d(x, y + offset, 0.0);
            Point3d OriginBottom = new Point3d(x, y - offset, 0.0);
            Point3d OriginLeft = new Point3d(x - offset, y, 0.0);
            Point3d OriginRight = new Point3d(x + offset, y, 0.0);

            Vector3d YVector = Vector3d.YAxis;
            Vector3d XVector = Vector3d.XAxis;

            Point3d Transform(Point3d point, Vector3d Vector)
            {
                return point.TransformBy(Matrix3d.Displacement(Vector.GetNormal().MultiplyBy(WidthCross)));
            }

            var points = new Point3dCollection
            {
                Transform(OriginTop, -XVector), //A
                Transform(OriginTop, XVector), //B
                Transform(Transform(Origin, XVector), YVector), //C

                
                Transform(OriginRight, YVector), //D
                Transform(OriginRight, -YVector), //E
                Transform(Transform(Origin, XVector), -YVector), //F

                Transform(OriginBottom, XVector), //G
                Transform(OriginBottom, -XVector), //H
                Transform(Transform(Origin, -XVector), -YVector), //I

                Transform(OriginLeft, -YVector), //J
                Transform(OriginLeft, YVector), //K
                Transform(Transform(Origin, -XVector), YVector), //L
            };


            worldDraw.SubEntityTraits.FillType = FillType.FillAlways;
            if (type == DrawType.HoverGrip)
            {
                worldDraw.SubEntityTraits.Color = 11;
            }
            else if (type == DrawType.HotGrip)
            {
                worldDraw.SubEntityTraits.Color = 12;
            }
            else
            {
                worldDraw.SubEntityTraits.Color = 150;
            }
            worldDraw.Geometry.Polygon(points);

            if (type == DrawType.WarmGrip)
            {
                worldDraw.SubEntityTraits.FillType = FillType.FillNever;
                worldDraw.SubEntityTraits.Color = 0;
                worldDraw.Geometry.Polygon(points);
            }

            return true;
        }

        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            var dwg = CadApp.DocumentManager.MdiActiveDocument;
            using (dwg.LockDocument())
            {
                Generic.WriteMessage("GRIPED");

            }

            return ReturnValue.Ok;
        }
    }
}
