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
                Generic.SendStringToExecute("SIOFORGECAD.CIRCLETOPOLYLIGNE");
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
                    if (!ed.GetImpliedSelection(out PromptSelectionResult selResult))
                    {
                        selResult = ed.GetSelection();
                    }
                    if (selResult.Status == PromptStatus.OK)
                    {
                        List<ObjectId> ConvertionResult = new List<ObjectId>();
                        foreach (SelectedObject selObj in selResult.Value)
                        {
                            if (selObj.ObjectId.ObjectClass.DxfName == "CIRCLE")
                            {
                                Circle circle = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Circle;
                                using (Polyline pline = circle.ToPolyline())
                                {
                                    pline.Elevation = circle.Center.Z;
                                    pline.Closed = true;
                                    circle.CopyPropertiesTo(pline);
                                    ConvertionResult.Add(pline.AddToDrawing());
                                    circle.EraseObject();
                                }
                            }
                        }
                        ed.SetImpliedSelection(ConvertionResult.ToArray());
                    }
                }
                tr.Commit();
            }
        }
    }
}
