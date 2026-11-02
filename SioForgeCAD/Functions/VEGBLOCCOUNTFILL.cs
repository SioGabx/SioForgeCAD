using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCCOUNTFILL
    {
        public static void CountFill()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Sélection : uniquement des blocs
            TypedValue[] tvs = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            SelectionFilter filter = new SelectionFilter(tvs);
            var psr = ed.GetSelectionRedraw("Sélectionnez les blocs à numéroter", true, false, filter);
            if (psr.Status != PromptStatus.OK)
            {
                return;
            }

            var ss = psr.Value.GetSelectionSet().ToArray();
            if (ss.Length == 0)
            {
                return;
            }

            // Demande du vecteur de direction à l'utilisateur
            PromptPointOptions ppo1 = new PromptPointOptions("\nPoint de départ du vecteur de direction : ");
            PromptPointResult ppr1 = ed.GetPoint(ppo1);
            if (ppr1.Status != PromptStatus.OK)
            {
                return;
            }

            PromptPointOptions ppo2 = new PromptPointOptions("\nPoint final définissant la direction : ")
            {
                UseBasePoint = true,
                BasePoint = ppr1.Value
            };
            PromptPointResult ppr2 = ed.GetPoint(ppo2);
            if (ppr2.Status != PromptStatus.OK)
            {
                return;
            }

            // Calcul du vecteur directionnel (normalisé)
            Vector3d directionVector = ppr2.Value.GetVectorTo(ppr1.Value).Negate();
            if (directionVector.Length == 0)
            {
                Generic.WriteMessage("Le vecteur de direction est nul.");
                return;
            }
            Vector3d unitVector = directionVector.GetNormal();
            Point3d originPoint = ppr1.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Récupération des attributs
                    List<string> attributeTags = new List<string>();
                    foreach (var sso in ss)
                    {
                        if (!(sso.GetDBObject(OpenMode.ForRead) is BlockReference firstBr))
                        {
                            continue;
                        }

                        foreach (ObjectId attId in firstBr.AttributeCollection)
                        {
                            AttributeReference ar = attId.GetDBObject(OpenMode.ForRead) as AttributeReference;
                            if (ar != null && !attributeTags.Contains(ar.Tag))
                            {
                                attributeTags.Add(ar.Tag);
                            }
                        }
                    }

                    if (attributeTags.Count == 0)
                    {
                        Generic.WriteMessage("Aucun attribut trouvé dans le(s) bloc(s).");
                        return;
                    }

                    var pr = ed.GetOptions("Attribut à numéroter :", true, attributeTags.ToArray());
                    if (pr.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    string selectedTag = pr.StringResult;

                    // Récupération des blocs avec leur position pour le tri
                    var blockItems = new List<(ObjectId obj, double projection)>();

                    foreach (var so in ss)
                    {
                        if (so.GetDBObject(OpenMode.ForRead) is BlockReference br)
                        {
                            // Calcul du produit scalaire (projection) le long du vecteur directionnel
                            Vector3d relativePos = br.Position - originPoint;
                            double projection = relativePos.DotProduct(unitVector);
                            blockItems.Add((so, projection));
                        }
                    }

                    // Tri des blocs du plus proche au plus éloigné le long du vecteur
                    var sortedBlocks = blockItems.OrderBy(item => item.projection).Select(item => item.obj);

                    // Numérotation dans l'ordre trié
                    int index = 0;
                    foreach (var so in sortedBlocks)
                    {
                        if (so.GetDBObject(OpenMode.ForWrite) is BlockReference br)
                        {
                            foreach (ObjectId attId in br.AttributeCollection)
                            {
                                AttributeReference ar = attId.GetDBObject(OpenMode.ForWrite) as AttributeReference;
                                if (ar != null && ar.Tag == selectedTag)
                                {
                                    index++;
                                    ar.TextString = index.ToString();
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }
        }
    }
}