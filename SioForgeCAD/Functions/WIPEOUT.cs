using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Overrules.PolyGripOverrule;
using SioForgeCAD.Commun.Overrules.PolylineGripOverrule;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static void OnHotGripAction(ObjectId objectid, Point3d GripPoint)
        {
            var ed = Generic.GetEditor();
            using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                if (objectid.GetDBObject(OpenMode.ForWrite) is Wipeout WipeoutEnt)
                {
                    Point3dCollection WipeoutEntVertices = WipeoutEnt.GetVertices();

                    using (Polyline WipeoutPoly = new Polyline())
                    {
                        foreach (Point3d WipeoutEntVertice in WipeoutEntVertices)
                        {
                            WipeoutPoly.AddVertex(WipeoutEntVertice);
                        }
                        WipeoutPoly.Closed = true;
                        var jig = new PolylineJig(WipeoutPoly, GripPoint);
                        var JigResult = jig.Drag();
                        if (JigResult.Status == PromptStatus.OK)
                        {
                            Point2dCollection pts = new Point2dCollection();
                            bool HasFound = false;
                            foreach (Point3d WipeoutEntVertice in WipeoutEntVertices)
                            {
                                if (!HasFound && WipeoutEntVertice.IsEqualTo(GripPoint, Generic.MediumTolerance))
                                {
                                    HasFound = true;
                                    pts.Add(JigResult.Value.ToPoint2d());
                                }
                                else
                                {
                                    pts.Add(WipeoutEntVertice.ToPoint2d());
                                }
                            }
                            //Wipeout wo = new Wipeout();
                            WipeoutEnt.SetFrom(pts, Vector3d.ZAxis);
                            WipeoutEnt.RecordGraphicsModified(true);
                            //var DrawWoObjId = wo.AddToDrawing();
                            // WipeoutEnt.CopyPropertiesTo(wo);
                            // WipeoutEnt.CopyDrawOrderTo(wo);
                            // WipeoutEnt.EraseObject();
                        }
                    }
                }
                tr.Commit();
            }
        }

        #endregion

    }
}
