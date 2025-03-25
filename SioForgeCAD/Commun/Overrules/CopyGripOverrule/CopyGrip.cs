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
            var gripHeight = worldDraw.GetGripHeight(GripPoint, gripSizeInPixels);
            var offset = gripHeight / 2.0;
            double Width = (gripHeight * 0.8) / 6;

            Point3d Origin = new Point3d(GripPoint.X, GripPoint.Y, 0.0);

            Matrix3d ucs = Generic.GetEditor().CurrentUserCoordinateSystem;
            Vector3d YVector = ucs.CoordinateSystem3d.Yaxis;
            Vector3d XVector = ucs.CoordinateSystem3d.Xaxis;

            Point3d OriginTop = Origin.Displacement(YVector, offset);
            Point3d OriginBottom = Origin.Displacement(-YVector, offset);
            Point3d OriginLeft = Origin.Displacement(-XVector, offset);
            Point3d OriginRight = Origin.Displacement(XVector, offset);

            Point3d Transform(Point3d point, Vector3d Vector)
            {
                return point.Displacement(Vector, Width);
            }

            /*
                  A++++++B
                  +      +
                  +      +
            K+++++L      C+++++D
            +                  +
            +                  +
            J+++++I      F+++++E
                  +      +
                  +      +
                  H++++++G
            */

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

            return worldDraw.DrawGrip(points, type);
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
