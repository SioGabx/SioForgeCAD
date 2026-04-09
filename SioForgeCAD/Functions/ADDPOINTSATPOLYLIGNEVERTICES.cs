using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class ADDPOINTSATPOLYLIGNEVERTICES
    {
        public static void Execute()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();


            // 1. Demander à l'utilisateur de sélectionner une polyligne
            PromptEntityOptions opt = new PromptEntityOptions("\nSélectionnez une polyligne : ");
            opt.SetRejectMessage("\nL'objet doit être une polyligne.");
            opt.AddAllowedClass(typeof(Polyline), exactMatch: true);

            PromptEntityResult res = ed.GetEntity(opt);

            if (res.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 2. Ouvrir la polyligne pour lecture
                    Polyline pline = tr.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;

                    if (pline != null)
                    {
                        // 3. Ouvrir l'espace courant (Model Space) pour écriture
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        int vn = pline.NumberOfVertices;
                        for (int i = 0; i < vn; i++)
                        {
                            Point3d pt = pline.GetPoint3dAt(i);
                            DBPoint dbPoint = new DBPoint(pt);
                            dbPoint.AddToDrawing();
                        }

                        tr.Commit();
                        Generic.WriteMessage($"Succès : {vn} points ajoutés sur les sommets.");
                    }
                }
                catch (System.Exception ex)
                {
                    Generic.WriteMessage("Erreur : " + ex.Message);
                    tr.Abort();
                }
            }
        }
    }
}
