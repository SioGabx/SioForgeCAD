using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
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
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Functions
{
    public static class CIRCLETOPOLYLIGNE
    {
        public class ContextMenu
        {
            private static ContextMenuExtension cme;

            public static void Attach()
            {
                cme = new ContextMenuExtension();
                MenuItem mi = new MenuItem("Convertir en polyligne");
                mi.Click += new EventHandler(OnConvert);
                cme.MenuItems.Add(mi);
                RXClass rxc = Entity.GetClass(typeof(Circle));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = Entity.GetClass(typeof(Circle));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnConvert(Object o, EventArgs e)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
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
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
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
                                using (Polyline pline = new Polyline())
                                {
                                    double bulge = 1.0;
                                    double halfWidth = 0.0;

                                    pline.AddVertexAt(0, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
                                    pline.AddVertexAt(1, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), bulge, halfWidth, halfWidth);
                                    pline.Elevation = circle.Center.Z;
                                    pline.Closed = true;
                                    pline.Layer = circle.Layer;
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
