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
    public static class LINETOPOLYLIGNE
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
                RXClass rxc = RXObject.GetClass(typeof(Line));
                if (rxc is null) { return; }
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = RXObject.GetClass(typeof(Line));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("SIOFORGECAD.POLYLINE2DTOPOLYLIGNE");
            }
        }

        public static void ConvertLineToPolylines()
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
                        if (selObj.ObjectId.ObjectClass.DxfName == "LINE")
                        {
                            Line line = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Line;
                            using (Polyline pline = line.ToPolyline())
                            {
                                line.CopyPropertiesTo(pline);
                                ConvertionResult.Add(line.ReplaceInDrawing(pline));
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
