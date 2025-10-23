using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun.Overrules
{
    internal class PolyCornerGrip : GripData
    {
        public PolyCornerGrip()
        {
            ForcedPickOn = false;
            GizmosEnabled = true;
            DrawAtDragImageGripPoint = false;
            IsPerViewport = false;
            ModeKeywordsDisabled = false;
            RubberBandLineDisabled = false;
            TriggerGrip = true;
            HotGripInvokesRightClick = false;
            SkipWhenShared = true;

            GripModes = new GripModeCollection();
        }

        public int Index { get; set; }
        public ObjectId EntityId { get; set; } = ObjectId.Null;
        public Action<ObjectId, GripData> OnHotGripAction { get; set; } = (_, __) => Generic.WriteMessage("GRIPPED");
        public GripModeCollection GripModes { get; }
        public virtual GripMode.ModeIdentifier CurrentModeId { get; set; } = GripMode.ModeIdentifier.CustomStart;

        public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
        {
            var gripHeight = worldDraw.GetGripHeight(GripPoint, gripSizeInPixels);
            var offset = gripHeight / 2.5;

            Point3d Origin = new Point3d(GripPoint.X, GripPoint.Y, 0.0);
            if (Generic.GetDocument() == null) { return false; }
            Matrix3d ucs = Generic.GetEditor().CurrentUserCoordinateSystem;
            Vector3d YVector = ucs.CoordinateSystem3d.Yaxis;
            Vector3d XVector = ucs.CoordinateSystem3d.Xaxis;

            Point3d OriginLeft = Origin.Displacement(-XVector, offset);
            Point3d OriginRight = Origin.Displacement(XVector, offset);

            Point3d Transform(Point3d point, Vector3d Vector)
            {
                return point.Displacement(Vector, offset);
            }

            /*
            K++++++D
            +      +
            +      +
            J++++++E
            */

            var points = new Point3dCollection
            {
                Transform(OriginRight, YVector), //D
                Transform(OriginRight, -YVector), //E
                Transform(OriginLeft, -YVector), //J
                Transform(OriginLeft, YVector), //K
            };
            return worldDraw.DrawGrip(points, type);
        }

        public override ReturnValue OnHotGrip(ObjectId entityId, Context contextFlags)
        {
            var doc = Generic.GetDocument();
            using (doc.LockDocument())
            {
                OnHotGripAction(entityId, this);
            }
            return ReturnValue.GetNewGripPoints;
        }

        public bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
        {
            //return false; //disable grip menu
            modes.Add(new GripMode()
            {
                ModeId = (int)PolyGripOverrule.PolyGripOverrule.ModeIdAction.Stretch,
                DisplayString = "Etirer le sommet",
                Action = GripMode.ActionType.DragOn,
                ToolTip = ""
            });

            modes.Add(new GripMode()
            {
                ModeId = (int)PolyGripOverrule.PolyGripOverrule.ModeIdAction.Add,
                DisplayString = "Ajouter sommet",
                Action = GripMode.ActionType.DragOn,
                ToolTip = ""
            });
            modes.Add(new GripMode()
            {
                ModeId = (int)PolyGripOverrule.PolyGripOverrule.ModeIdAction.Remove,
                DisplayString = "Supprimer sommet",
                Action = GripMode.ActionType.DragOn,
                ToolTip = ""
            });

            return true;
        }
    }
}
