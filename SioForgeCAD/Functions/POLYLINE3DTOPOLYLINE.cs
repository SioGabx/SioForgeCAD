using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class POLYLINE3DTOPOLYLIGNE
    {
        public static class ContextMenu
        {
            private static ContextMenuExtension cme;

            public static void Attach()
            {
                cme = new ContextMenuExtension();
                MenuItem mi = new MenuItem("Convertir en polyligne");
                mi.Click += OnExecute;
                cme.MenuItems.Add(mi);
                RXClass rxc = RXObject.GetClass(typeof(Polyline3d));
                if (rxc is null) { return; }
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = RXObject.GetClass(typeof(Polyline3d));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("SIOFORGECAD.POLYLINE3DTOPOLYLIGNE");
            }
        }

        public static void ConvertPolyline3dToPolylines()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            // Demander si l'altitude doit être conservée
            PromptKeywordOptions options = new PromptKeywordOptions("\nGarder l'altitude des sommets ? [Oui/Non] <Oui> : ", "Oui Non")
            {
                AllowNone = true
            };
            options.Keywords.Default = "Oui";

            PromptResult altitudeResult = ed.GetKeywords(options);

            if (altitudeResult.Status != PromptStatus.OK && altitudeResult.Status != PromptStatus.None)
            {
                return;
            }

            bool garderAltitude = altitudeResult.Status == PromptStatus.None || altitudeResult.StringResult == "Oui";

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!ed.GetImpliedSelection(out PromptSelectionResult selResult))
                {
                    selResult = ed.GetSelection();
                }

                if (selResult.Status == PromptStatus.OK)
                {
                    List<ObjectId> ConvertionResult = new List<ObjectId>();

                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj.ObjectId.ObjectClass.DxfName == "POLYLINE")
                        {
                            Polyline3d poly3d = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Polyline3d;

                            using (Polyline pline = poly3d.ToPolyline())
                            {
                                poly3d.CopyPropertiesTo(pline);

                                if (garderAltitude)
                                {
                                    List<Point3d> pts3d = new List<Point3d>();

                                    foreach (ObjectId vertexId in poly3d)
                                    {
                                        PolylineVertex3d vertex = tr.GetObject(vertexId, OpenMode.ForRead) as PolylineVertex3d;

                                        if (vertex != null)
                                        {
                                            pts3d.Add(vertex.Position);
                                        }

                                    }
                                    if (pts3d.Count > 0)
                                    {
                                        pline.Elevation = (double)pts3d.First().Z;
                                    }
                                }

                                ConvertionResult.Add(poly3d.ReplaceInDrawing(pline));
                            }
                        }
                    }
                    ed.SetImpliedSelection(ConvertionResult.ToArray());
                }
                tr.Commit();
            }
        }
    }
}
