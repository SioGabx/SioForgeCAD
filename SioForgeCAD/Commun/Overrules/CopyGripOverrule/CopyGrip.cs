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
            var gripHeight = 3.0 * gripSizeInPixels / unit.X;
            Generic.WriteMessage(gripSizeInPixels + " / " + gripHeight);
            
            var x = GripPoint.X;
            var y = GripPoint.Y;
            var offset = gripHeight / 2.0;

            //var points = new Point3dCollection();
            //points.Add(new Point3d(x - offset, y, 0.0));
            //points.Add(new Point3d(x, y - offset, 0.0));
            //points.Add(new Point3d(x + offset, y, 0.0));
            //points.Add(new Point3d(x, y + offset, 0.0));

            Point3d center = new Point3d(x, y, 0.0);
            var radius = offset;

           worldDraw.SubEntityTraits.FillType = FillType.FillAlways;
            worldDraw.SubEntityTraits.Color = 150;
            worldDraw.Geometry.Circle(center, radius, Vector3d.ZAxis);
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
