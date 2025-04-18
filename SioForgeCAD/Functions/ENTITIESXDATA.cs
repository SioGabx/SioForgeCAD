using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class ENTITIESXDATA
    {
        public static void Read()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            var result = ed.GetEntity("Selectionnez un object");
            if (result.Status != PromptStatus.OK)
            {
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (result.ObjectId.GetDBObject() is Entity ent)
                {
                    foreach (var item in ent.ReadXData())
                    {
                        Generic.WriteMessage(item.ToString());
                    }
                }
            }
        }

        public static void Remove()
        {
            Editor ed = Generic.GetEditor();

            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                AllSelectedObject = ed.GetSelectionRedraw("Selectionnez des entités pour lequels vous souhaitez supprimer les XDATAs", true, false);
            }
            RemoveAllXDataFromCollection(AllSelectedObject.Value.GetObjectIds());
        }

        public static void RemoveAll()
        {
            Database db = Generic.GetDatabase();
            RemoveAllXDataFromCollection(db.GetAllObjects().Keys.ToList());
        }

        private static void RemoveAllXDataFromCollection(IList<ObjectId> objectIds)
        {
            Database db = Generic.GetDatabase();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var item in objectIds)
                {
                    try
                    {
                        item.GetDBObject().RemoveAllXdata();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
                tr.Commit();
            }
        }

    }
}
