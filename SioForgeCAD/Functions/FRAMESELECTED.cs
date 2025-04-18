using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class FRAMESELECTED
    {
        public static void FrameEntitiesToView()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetImpliedSelection(out PromptSelectionResult selResult))
            {
                selResult = ed.GetSelection();
            }
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Extents3d Extend = new Extents3d();
                    int NotInCurrentSpace = 0;
                    int InCurrentSpace = 0;
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj.ObjectId.GetDBObject() is Entity ent)
                        {
                            if (ent.OwnerId == db.CurrentSpaceId)
                            {
                                Extend.AddExtents(ent.GetExtents());
                                if (ent.Bounds is Extents3d EntBound)
                                {
                                    Extend.AddExtents(EntBound);
                                }
                                InCurrentSpace++;
                            }
                            else
                            {
                                NotInCurrentSpace++;
                            }
                        }
                    }

                    if (InCurrentSpace > 0)
                    {
                        ed.SetImpliedSelection(selResult.Value);
                        Extend.Expand(1.25);
                        Extend.ZoomExtents();
                    }
                    if (NotInCurrentSpace > 0)
                    {
                        Generic.WriteMessage($"{NotInCurrentSpace}/{NotInCurrentSpace + InCurrentSpace} entité(s) n'étaient pas dans l'espace courant.");
                    }
                    tr.Commit();
                }
                if (selResult.Value.Count > 1)
                {
                    var Options = ed.GetOptions("Voullez-vous zoomer sur chaque entités individuellement ?", "Oui", "Non");
                    if (Options.Status == PromptStatus.OK && Options.StringResult == "Oui")
                    {
                        FrameEachIndividualEntityToView(selResult.Value);
                    }
                }
            }
        }

        public static void FrameEachIndividualEntityToView(SelectionSet sel)
        {
            if (sel == null || sel.Count == 0)
            {
                return;
            }

            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < sel.Count; i++)
                {
                    SelectedObject selObj = sel[i];
                    if (selObj.ObjectId == ObjectId.Null || !(selObj.ObjectId.GetDBObject() is Entity ent))
                    {
                        continue;
                    }
                    BlockTableRecord EntSpace = ent.BlockId.GetDBObject() as BlockTableRecord;
                    if (EntSpace?.IsLayout == true && ent.OwnerId != db.CurrentSpaceId)
                    {
                        LayoutManager.Current.SetCurrentLayoutId(EntSpace.LayoutId);
                        ed.SetImpliedSelection(sel.GetObjectIds().Where(objid => objid.GetDBObject() is Entity selectent && selectent.OwnerId == db.CurrentSpaceId).ToArray());
                    }

                    Extents3d ext = ent.GetExtents();
                    ext.Expand(1.2);
                    ext.ZoomExtents();

                    if (i < (sel.Count - 1))
                    {
                        var Options = new PromptPointOptions($"\nCliquez pour afficher l'entité suivante - {i + 1}/{sel.Count} ...")
                        {
                            AllowNone = true,
                            AllowArbitraryInput = true
                        };
                        var Wait = ed.GetPoint(Options);
                        if (Wait.Status != PromptStatus.OK && Wait.Status != PromptStatus.None)
                        {
                            break;
                        }
                    }
                }
                ed.SetImpliedSelection(Array.Empty<ObjectId>());
                tr.Commit();
            }
        }
    }
}
