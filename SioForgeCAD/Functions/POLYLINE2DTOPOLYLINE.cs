using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class POLYLINE2DTOPOLYLIGNE
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
                RXClass rxc = Entity.GetClass(typeof(Polyline2d));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = Entity.GetClass(typeof(Polyline2d));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("SIOFORGECAD.POLYLINE2DTOPOLYLIGNE");
            }
        }

        public static void ConvertPolyline2dToPolylines()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

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
                            Polyline2d poly2d = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Polyline2d;
                            using (Polyline pline = poly2d.ToPolyline())
                            {
                                poly2d.CopyPropertiesTo(pline);
                                ConvertionResult.Add(pline.AddToDrawing());
                                poly2d.Erase();
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
