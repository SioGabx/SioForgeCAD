using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    internal static class SSELEV
    {
        public static void SelectByElevation()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            PromptDoubleOptions elevOpts = new PromptDoubleOptions("\nAltitude à sélectionner : ")
            {
                AllowNegative = true,
                AllowZero = true
            };

            PromptDoubleResult elevRes = ed.GetDouble(elevOpts);

            if (elevRes.Status != PromptStatus.OK)
            {
                return;
            }

            double elevation = elevRes.Value;

            const double tolerance = 0.001;

            PromptSelectionResult selRes = ed.SelectAll();

            if (selRes.Status != PromptStatus.OK)
            {
                Generic.WriteMessage("Aucun objet trouvé.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId[] selectedIds = selRes.Value.GetObjectIds();

                var ids = new List<ObjectId>();

                foreach (ObjectId id in selectedIds)
                {
                    if (!(tr.GetObject(id, OpenMode.ForRead) is Entity entity))
                    {
                        continue;
                    }
                    double? entityElevation = entity.GetElevation();

                    if (!entityElevation.HasValue)
                    {
                        continue;
                    }

                    if (Math.Abs(entityElevation.Value - elevation) <= tolerance)
                    {
                        ids.Add(id);
                    }
                }

                tr.Commit();
                ed.SetImpliedSelection(ids.ToArray());

                Generic.WriteMessage($"{ids.Count} objet(s) trouvé(s) à l'altitude {elevation:0.###}.");
            }
        }
    }

}
