using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Overrules;
using SioForgeCAD.Commun.Overrules.PolyGripOverrule;
using SioForgeCAD.Commun.Overrules.PolylineGripOverrule;
using System.Windows.Controls;

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
                    _instance = new PolyGripOverrule(typeof(Wipeout), FilterFunction, OnHotGripAction, true);
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
            if (Entity is Wipeout wipeout)
            {
                return true;
            }
            return false;
        }

        public static void OnHotGripAction(ObjectId objectid, GripData GripData)
        {
            var ed = Generic.GetEditor();
            using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                if (objectid.GetDBObject(OpenMode.ForWrite) is Wipeout WipeoutEnt && GripData is PolyGrip polyGrip)
                {
                    Point3dCollection WipeoutEntVertices = new Point3dCollection();
                    foreach (Point3d WipeoutEntVertice in WipeoutEnt.GetVertices())
                    {
                        if (WipeoutEntVertice.IsEqualTo(GripData.GripPoint, Generic.MediumTolerance))
                        {
                            if (((int)polyGrip.CurrentModeId) != 3)
                            {
                                WipeoutEntVertices.Add(WipeoutEntVertice);
                            }
                            if (((int)polyGrip.CurrentModeId) == 2)
                            {
                                GripData.GripPoint = GripData.GripPoint.Displacement(Vector3d.XAxis, 1);
                                WipeoutEntVertices.Add(GripData.GripPoint);
                            }
                        }
                        else
                        {
                            WipeoutEntVertices.Add(WipeoutEntVertice);
                        }


                    }
                    if (((int)polyGrip.CurrentModeId) == 3 && WipeoutEntVertices.RemoveDuplicatePoints(Generic.MediumTolerance).Count <= 2)
                    {
                        Generic.WriteMessage("Impossible de supprimer ce point, cela entraînerait une géométrie invalide.");
                        tr.Abort();
                        return;
                    }
                    if (WipeoutEntVertices[0] != WipeoutEntVertices[WipeoutEntVertices.Count - 1])
                    {
                        WipeoutEntVertices.Add(WipeoutEntVertices[0]);
                    }
                    if (((int)polyGrip.CurrentModeId) == 3)
                    {
                        WipeoutEnt.SetFrom(WipeoutEntVertices.ToPoint2dCollection(), Vector3d.ZAxis);
                        WipeoutEnt.RecordGraphicsModified(true);
                    }
                    else
                    {
                        using (Polyline WipeoutPoly = new Polyline())
                        {
                            for (int i = 0; i < WipeoutEntVertices.Count - 1; i++)
                            {
                                Point3d WipeoutEntVertice = WipeoutEntVertices[i];
                                WipeoutPoly.AddVertex(WipeoutEntVertice);

                            }
                            WipeoutPoly.Closed = true;
                            var jig = new PolylineJig(WipeoutPoly, GripData.GripPoint);
                            var JigResult = jig.Drag();
                            if (JigResult.Status == PromptStatus.OK)
                            {
                                Point2dCollection pts = new Point2dCollection();
                                foreach (Point3d WipeoutEntVertice in WipeoutEntVertices)
                                {
                                    if (WipeoutEntVertice.IsEqualTo(GripData.GripPoint, Generic.MediumTolerance))
                                    {
                                        pts.Add(JigResult.Value.ToPoint2d());
                                    }
                                    else
                                    {
                                        pts.Add(WipeoutEntVertice.ToPoint2d());
                                    }
                                }

                                WipeoutEnt.SetFrom(pts, Vector3d.ZAxis);
                                WipeoutEnt.RecordGraphicsModified(true);
                            }
                        }
                    }
                }
                Generic.Regen();
                tr.Commit();
            }
        }

        #endregion

    }
}
