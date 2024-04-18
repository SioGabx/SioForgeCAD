using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;

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
                mi.Click += OnConvert;
                cme.MenuItems.Add(mi);
                RXClass rxc = Entity.GetClass(typeof(Polyline3d));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = Entity.GetClass(typeof(Polyline3d));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnConvert(object o, EventArgs e)
            {
                Document doc = Generic.GetDocument();
                doc.SendStringToExecute("_.POLYLINE3DTOPOLYLIGNE ", true, false, false);
            }
        }

        public static void ConvertPolyline3dToPolylines()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = Generic.GetCurrentSpaceBlockTableRecord(tr);
                using (btr)
                {
                    PromptSelectionResult selResult = ed.GetSelection();
                    if (selResult.Status == PromptStatus.OK)
                    {
                        SelectionSet selSet = selResult.Value;

                        foreach (SelectedObject selObj in selSet)
                        {
                            if (selObj.ObjectId.ObjectClass.DxfName == "POLYLINE")
                            {
                                Polyline3d poly3d = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Polyline3d;
                                using (Polyline pline = poly3d.ToPolyline())
                                {
                                    poly3d.CopyPropertiesTo(pline);
                                    btr.AppendEntity(pline);
                                    tr.AddNewlyCreatedDBObject(pline, true);
                                    poly3d.Erase();
                                }
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}
