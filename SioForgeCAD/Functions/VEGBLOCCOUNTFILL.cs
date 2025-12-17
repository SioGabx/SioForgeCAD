using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var psr = ed.GetSelectionRedraw("Selectionnez les blocs à numeroter", true, false, filter);
            if (psr.Status != PromptStatus.OK)
            {
                return;
            }

            var ss = psr.Value.GetSelectionSet().ToArray();
            if (ss.Length == 0)
            {
                return;
            }

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
                        ed.WriteMessage("\nAucun attribut trouvé dans le()s bloc(s).");
                        return;
                    }

                    var pr = ed.GetOptions("Attribut à numéroter :", true, attributeTags.ToArray());
                    if (pr.Status != PromptStatus.OK)
                    {
                        return;
                    }

                    string selectedTag = pr.StringResult;


                    int index = 0;
                    foreach (var so in ss)
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
