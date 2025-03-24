using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun.Overrules
{
    internal class PolyGrip : GripData
    {
        public PolyGrip()
        {
            ForcedPickOn = false;
            GizmosEnabled = true;
            DrawAtDragImageGripPoint = false;
            IsPerViewport = false;
            ModeKeywordsDisabled = true;
            RubberBandLineDisabled = false;
            TriggerGrip = true;
            HotGripInvokesRightClick = false;


            GripModes = new GripModeCollection();
        }

        public ObjectId EntityId { get; set; } = ObjectId.Null;
        public Action<ObjectId, Point3d> OnHotGripAction { get; set; } = (objectid, GripPoint) => Generic.WriteMessage("GRIPPED");
        public Vector2d DrawVector { get; set; } = new Vector2d();
        public GripModeCollection GripModes { get; }
        public virtual GripMode.ModeIdentifier CurrentModeId { get; set; } = GripMode.ModeIdentifier.CustomStart;

        public override bool ViewportDraw(ViewportDraw worldDraw, ObjectId entityId, DrawType type, Point3d? imageGripPoint, int gripSizeInPixels)
        {
            var unit = worldDraw.Viewport.GetNumPixelsInUnitSquare(GripPoint);
            var gripHeight = 2.5 * gripSizeInPixels / unit.X;
            var x = GripPoint.X;
            var y = GripPoint.Y;
            var offset = gripHeight / 2.5;

            Point3d Origin = new Point3d(x, y, 0.0);

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
                Generic.WriteMessage("Selected => " + CurrentModeId);
                OnHotGripAction(entityId, GripPoint);
            }
            return ReturnValue.GetNewGripPoints;
        }

        public bool GetGripModes(ref GripModeCollection modes, ref uint curMode)
        {
            //return false; //disable grip menu
            modes.Add(new GripMode()
            {
                ModeId = 1,
                DisplayString = $"Etirer",
                Action = GripMode.ActionType.DragOn,
                ToolTip = $"Hello"
            }); 
            
            modes.Add(new GripMode()
            {
                ModeId = 2,
                DisplayString = $"Ajouter",
                Action = GripMode.ActionType.DragOn,
                ToolTip = $"Hello"
            });

            return true;
        }

    }
}
