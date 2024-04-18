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
    public static class CIRCLETOPOLYLIGNE
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
                RXClass rxc = Entity.GetClass(typeof(Circle));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = Entity.GetClass(typeof(Circle));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnConvert(object o, EventArgs e)
            {
                Document doc = Generic.GetDocument();
                doc.SendStringToExecute("_.CIRCLETOPOLYLIGNE ", true, false, false);
            }
        }

        public static void ConvertCirclesToPolylines()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

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
                            if (selObj.ObjectId.ObjectClass.DxfName == "CIRCLE")
                            {
                                Circle circle = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Circle;
                                using (Polyline pline = circle.ToPolyline())
                                {
                                    pline.Elevation = circle.Center.Z;
                                    pline.Closed = true;
                                    circle.CopyPropertiesTo(pline);
                                    btr.AppendEntity(pline);
                                    tr.AddNewlyCreatedDBObject(pline, true);
                                    circle.Erase();
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
