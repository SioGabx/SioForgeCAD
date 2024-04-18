using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;
using System;

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
        public Action<ObjectId> OnHotGripAction { get; set; } = (_) => Generic.WriteMessage("GRIPPED");

        public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
        {
            var unit = worldDraw.Viewport.GetNumPixelsInUnitSquare(GripPoint);
            var gripHeight = 2.5 * gripSizeInPixels / unit.X;
            var x = GripPoint.X;
            var y = GripPoint.Y;
            var offset = gripHeight / 2.0;

            double WidthCross = (gripHeight / 3 / 2) * 0.8;

            Point3d Origin = new Point3d(x, y, 0.0);

            Matrix3d ucs = Generic.GetEditor().CurrentUserCoordinateSystem;
            Vector3d YVector = ucs.CoordinateSystem3d.Yaxis;
            Vector3d XVector = ucs.CoordinateSystem3d.Xaxis;

            Point3d OriginTop = Origin.Displacement(YVector, offset);
            Point3d OriginBottom = Origin.Displacement(-YVector, offset);
            Point3d OriginLeft = Origin.Displacement(-XVector, offset);
            Point3d OriginRight = Origin.Displacement(XVector, offset);

            Point3d Transform(Point3d point, Vector3d Vector)
            {
                return point.Displacement(Vector, WidthCross);
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
                //GRIPHOVER (System Variable) -> Obsolete
                worldDraw.SubEntityTraits.Color = 11;
            }
            else if (type == DrawType.HotGrip)
            {
                //GRIPHOT (System Variable)
                worldDraw.SubEntityTraits.Color = 12;
            }
            else
            {
                //GRIPCONTOUR
                worldDraw.SubEntityTraits.Color = 150;
            }
            worldDraw.Geometry.Polygon(points);

            if (type == DrawType.WarmGrip)
            {
                worldDraw.SubEntityTraits.FillType = FillType.FillNever;
                worldDraw.SubEntityTraits.Color = 251;
                worldDraw.Geometry.Polygon(points);
            }

            return true;
        }

        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            var doc = Generic.GetDocument();
            using (doc.LockDocument())
            {
                OnHotGripAction(entityId);
            }

            return ReturnValue.Ok;
        }
    }
}
