using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class GETOBJECTBYTESIZE
    {
        public static void GetObjectByteSize()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            var selResult = ed.GetSelectionRedraw("Selectionnez des entités à inclure au bloc", false, false);

            if (selResult.Status != PromptStatus.OK)
            {
                return;
            }
            Dictionary<ObjectId, long> objectSizes = new Dictionary<ObjectId, long>();
            ObjectId[] selectedIds = selResult.Value.GetObjectIds();
            try
            {
                foreach (ObjectId id in selectedIds)
                {
                    long byteSize = id.GetObjectByteSize();
                    objectSizes[id] = byteSize;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Erreur lors de la récupération de la taille des objets : {ex.Message}");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--- Taille des objets sélectionnés ---");
            foreach (var item in objectSizes)
            {
                sb.Append(item.Key.ObjectClass.DxfName)
                    .Append($"({item.Key}) : ")
                    .Append(" - Taille: ")
                    .Append(Files.FormatFileSizeFromByte(item.Value))
                    .AppendLine();
            }

            sb.AppendLine("Taille totale : " + Files.FormatFileSizeFromByte(objectSizes.Values.Sum()));

            Generic.WriteMessage(sb.ToString());
        }
    }
}
