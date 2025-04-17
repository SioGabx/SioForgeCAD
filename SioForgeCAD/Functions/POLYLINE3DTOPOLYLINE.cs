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
                RXClass rxc = Entity.GetClass(typeof(Polyline3d));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = Entity.GetClass(typeof(Polyline3d));
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
                                    ConvertionResult.Add(pline.AddToDrawing());
                                    poly3d.Erase();
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
