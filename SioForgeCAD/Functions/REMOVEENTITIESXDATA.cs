using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class REMOVEENTITIESXDATA
    {
        public static void Remove()
        {

            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();


            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                AllSelectedObject = ed.GetSelectionRedraw("Selectionnez des entités pour lequels vous souhaitez supprimer les XDATAs", true, false);

            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId ObjId in AllSelectedObject.Value.GetObjectIds())
                {
                    try
                    {
                        ObjId.GetDBObject().RemoveAllXdata();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                }
                tr.Commit();
            }
        }

        public static void RemoveAll()
        {
            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var item in db.GetAllObjects())
                {
                    try
                    {
                        item.Key.GetDBObject().RemoveAllXdata();
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
