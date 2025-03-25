using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Overrules;
using SioForgeCAD.Commun.Overrules.PolyGripOverrule;
using SioForgeCAD.Commun.Overrules.PolylineGripOverrule;
using System.Diagnostics;
using System.Windows.Controls;
using static SioForgeCAD.Commun.Overrules.PolyCornerGrip;

namespace SioForgeCAD.Functions
{
    public static class WIPEOUT
    {

        #region PolyGripOverrule
        private static PolyGripOverrule _instance = null;
        public static PolyGripOverrule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PolyGripOverrule(typeof(Wipeout), FilterFunction, CornerOnHotGripAction, MiddleOnHotGripAction, true);
                }
                return _instance;
            }
        }

        public static void ToggleGrip()
        {
            if (!Instance.IsEnabled)
            {
                Instance.EnableOverrule(true);
                Generic.WriteMessage("Grip activé.");
            }
            else
            {
                Instance.EnableOverrule(false);
                Generic.WriteMessage("Grip désactivé.");
            }
        }

        public static bool FilterFunction(Entity Entity)
        {
            return Entity is Wipeout;
        }

        public static void MiddleOnHotGripAction(ObjectId objectid, GripData GripData)
        {
            using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                if (objectid.GetDBObject(OpenMode.ForWrite) is Wipeout WipeoutEnt && GripData is PolyMiddleGrip polyGrip)
                {
                    if (((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Add)
                    {
                        var pts = AddStretchPoint(WipeoutEnt, polyGrip.PreviousPoint, PolyGripOverrule.ModeIdAction.Add);
                        RecreateWipeout(WipeoutEnt, pts);
                    }
                }
                tr.Commit();
            }
        }


        public static void CornerOnHotGripAction(ObjectId objectid, GripData GripData)
        {
            using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                if (objectid.GetDBObject(OpenMode.ForWrite) is Wipeout WipeoutEnt && GripData is PolyCornerGrip polyGrip)
                {
                    Debug.WriteLine($"Grip CurrentModeId : {((int)polyGrip.CurrentModeId)}");
                    Point2dCollection pts = null;
                    if (((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Default ||
                    ((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Stretch)
                    {
                        pts = AddStretchPoint(WipeoutEnt, GripData.GripPoint, PolyGripOverrule.ModeIdAction.Stretch);
                    }
                    else if (((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Add)
                    {
                        pts = AddStretchPoint(WipeoutEnt, GripData.GripPoint, PolyGripOverrule.ModeIdAction.Add);
                    }
                    else if (((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Remove)
                    {
                        pts = RemovePoint(WipeoutEnt, GripData.GripPoint);
                    }
                    RecreateWipeout(WipeoutEnt, pts);
                }
                Generic.Regen();
                tr.Commit();
            }
        }

        #endregion

        private static void RecreateWipeout(Wipeout WipeoutEnt, Point2dCollection pts)
        {
            if (pts == null) { return; }
            WipeoutEnt.SetFrom(pts, Vector3d.ZAxis);
            WipeoutEnt.RecordGraphicsModified(true);
        }

        private static Point2dCollection AddStretchPoint(Wipeout WipeoutEnt, Point3d Point, PolyGripOverrule.ModeIdAction Action)
        {
            Point3dCollection WipeoutEntVertices = new Point3dCollection();
            foreach (Point3d WipeoutEntVertice in WipeoutEnt.GetVertices())
            {
                WipeoutEntVertices.Add(WipeoutEntVertice);
                if (Action == PolyGripOverrule.ModeIdAction.Add && WipeoutEntVertice.IsEqualTo(Point, Generic.MediumTolerance))
                {
                    Point = Point.Displacement(Vector3d.XAxis, 100);
                    WipeoutEntVertices.Add(Point);
                }
            }

            using (Polyline WipeoutPoly = new Polyline())
            {
                for (int i = 0; i < WipeoutEntVertices.Count - 1; i++)
                {
                    Point3d WipeoutEntVertice = WipeoutEntVertices[i];
                    WipeoutPoly.AddVertex(WipeoutEntVertice);
                }

                WipeoutPoly.Closed = true;
                var jig = new PolyCornerGripJig(WipeoutPoly, Point);
                var JigResult = jig.Drag();
                if (JigResult.Status == PromptStatus.OK)
                {
                    Point2dCollection pts = new Point2dCollection();
                    foreach (Point3d WipeoutEntVertice in WipeoutEntVertices)
                    {
                        if (WipeoutEntVertice.IsEqualTo(Point, Generic.MediumTolerance))
                        {
                            pts.Add(JigResult.Value.ToPoint2d());
                        }
                        else
                        {
                            pts.Add(WipeoutEntVertice.ToPoint2d());
                        }
                    }
                    return pts;
                }
                return null;
            }
        }

        private static Point2dCollection RemovePoint(Wipeout WipeoutEnt, Point3d Point)
        {
            Point3dCollection WipeoutEntVertices = new Point3dCollection();
            bool HasAlreadyDeletedOnePoint = false;
            foreach (Point3d WipeoutEntVertice in WipeoutEnt.GetVertices())
            {
                if (HasAlreadyDeletedOnePoint || !WipeoutEntVertice.IsEqualTo(Point, Generic.MediumTolerance))
                {
                    WipeoutEntVertices.Add(WipeoutEntVertice);
                }
                else
                {
                    HasAlreadyDeletedOnePoint = true;
                }
            }

            if (WipeoutEntVertices.RemoveDuplicatePoints(Generic.MediumTolerance).Count <= 2)
            {
                Generic.WriteMessage("Impossible de supprimer ce point, cela entraînerait une géométrie invalide.");
                return null;
            }
            if (WipeoutEntVertices[0] != WipeoutEntVertices[WipeoutEntVertices.Count - 1])
            {
                WipeoutEntVertices.Add(WipeoutEntVertices[0]);
            }
            return WipeoutEntVertices.ToPoint2dCollection();
        }

    }
}
