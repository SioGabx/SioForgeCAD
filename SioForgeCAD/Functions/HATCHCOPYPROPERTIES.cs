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
    public static class HATCHCOPYPROPERTIES
    {
        public static void CopyProperties()
        {
            Document doc = Generic.GetDocument(); ;
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            if (!ed.GetHatch(out ObjectId SourceHatchId, "\nSélectionnez la hachure source (motif à copier) : "))
            {
                return;
            }

            if (!ed.GetHatch(out ObjectId TargetHatchId, "\nSélectionnez la hachure cible (à remplacer) : "))
            {
                return;
            }

            if (SourceHatchId == TargetHatchId)
            {
                ed.WriteMessage("Vous avez sélectionné deux fois la même hachure.");
                return;
            }
            // 3. Exécuter dans une Transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Hatch originHatch = SourceHatchId.GetDBObject(OpenMode.ForRead) as Hatch;
                Hatch targetHatch = TargetHatchId.GetDBObject(OpenMode.ForWrite) as Hatch;
                originHatch.CopyHatchPropertiesToAndDelete(targetHatch);
                tr.Commit();
            }
        }
    }
}
