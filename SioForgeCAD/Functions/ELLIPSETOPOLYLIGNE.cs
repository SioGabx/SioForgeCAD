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
    public static class ELLIPSETOPOLYLIGNE
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
                RXClass rxc = RXObject.GetClass(typeof(Ellipse));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = RXObject.GetClass(typeof(Ellipse));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("SIOFORGECAD.ELLIPSETOPOLYLIGNE");
            }
        }

        public static void ConvertEllipseToPolylines()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
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
                            if (selObj.ObjectId.ObjectClass.DxfName == "ELLIPSE")
                            {
                                Ellipse ellipse = tr.GetObject(selObj.ObjectId, OpenMode.ForWrite) as Ellipse;
                                using (Polyline pline = ellipse.ToPolyline())
                                {
                                    pline.Elevation = ellipse.Center.Z;
                                    ellipse.CopyPropertiesTo(pline);
                                    pline.Cleanup();
                                    ConvertionResult.Add(ellipse.ReplaceInDrawing(pline));
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
