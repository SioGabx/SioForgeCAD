using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Functions
{
    public static class DELETESUBGROUP
    {
        public static void Delete()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            short SavedPICKSTYLE = (short)Application.GetSystemVariable("PICKSTYLE");
            Application.SetSystemVariable("PICKSTYLE", 1);
            Application.SystemVariableChanged += CancelPickStyleVariableChange;

            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                PromptSelectionOptions selectionOptions = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelectionnez un groupe",
                    SingleOnly = true,
                    SinglePickInSpace = true,
                    RejectObjectsOnLockedLayers = false
                };
                AllSelectedObject = ed.GetSelection(selectionOptions);
            }

            Application.SystemVariableChanged -= CancelPickStyleVariableChange;
            Application.SetSystemVariable("PICKSTYLE", SavedPICKSTYLE);

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            Dictionary<ObjectId, ObjectId[]> AllGroupIdsInSelection = new Dictionary<ObjectId, ObjectId[]>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var SelectedObjectId in AllSelectedObject.Value.GetObjectIds())
                {
                    DBObject DbObject = SelectedObjectId.GetDBObject(OpenMode.ForRead);
                    foreach (ObjectId ReactorId in DbObject.GetPersistentReactorIds())
                    {
                        DBObject obj = ReactorId.GetDBObject(OpenMode.ForRead);
                        if (obj is Group gp && !AllGroupIdsInSelection.ContainsKey(ReactorId))
                        {
                            AllGroupIdsInSelection.Add(ReactorId, gp.GetAllEntityIds());
                        }
                    }
                }

                //foreach item in selection, we chech if one on its group is inside the list, if not, we search for the biggest group that contains this iteam
                List<ObjectId> AllBiggestGroupIds = new List<ObjectId>();
                foreach (ObjectId item in AllGroupIdsInSelection.Values.SelectMany(obj => obj))
                {
                    bool IsAtLeastInOneGroup = false;
                    DBObject DbObject = item.GetDBObject(OpenMode.ForRead);
                    foreach (ObjectId ReactorId in DbObject.GetPersistentReactorIds())
                    {
                        DBObject obj = ReactorId.GetDBObject(OpenMode.ForRead);
                        if (obj is Group gp && AllBiggestGroupIds.Contains(ReactorId))
                        {
                            IsAtLeastInOneGroup = true;
                            break;
                        }
                    }
                    if (!IsAtLeastInOneGroup)
                    {
                        var Gp = AllGroupIdsInSelection.Where(obj => obj.Value.Contains(item)).OrderByDescending(obj => obj.Value.Length).FirstOrDefault();
                        AllBiggestGroupIds.Add(Gp.Key);
                    }
                }

                //Delete groups that are not in the AllBiggestGroupIds list
                var GroupsToDelete = AllGroupIdsInSelection.Select(obj => obj.Key).Where(key => !AllBiggestGroupIds.Contains(key));
                if (GroupsToDelete.Any())
                {
                    foreach (ObjectId GroupId in GroupsToDelete)
                    {
                        GroupId.EraseObject();
                    }
                    Generic.WriteMessage($"Un total de {GroupsToDelete.Count()} subgroups ont été supprimés");
                }
                else
                {
                    Generic.WriteMessage("Aucun subgroup supprimé");
                }
                ed.SetImpliedSelection(AllGroupIdsInSelection.SelectMany(obj => obj.Value).Distinct().ToArray());
                tr.Commit();
            }
        }

        private static void CancelPickStyleVariableChange(object sender, Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
        {
            if (e.Name == "PICKSTYLE" && (short)Application.GetSystemVariable("PICKSTYLE") != 1)
            {
                //Cancel
                Generic.WriteMessage("Impossible de changer la valeur de PICKSTYLE lors de la sélection d'un groupe");
                Application.SetSystemVariable("PICKSTYLE", 1);
            }
        }
    }
}
