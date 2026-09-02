using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    internal static class SSELEV
    {
        public static void SelectByElevation()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Demande de l'altitude
            PromptDoubleOptions elevOpts =
                new PromptDoubleOptions("\nAltitude à sélectionner : ");

            elevOpts.AllowNegative = true;
            elevOpts.AllowZero = true;

            PromptDoubleResult elevRes =
                ed.GetDouble(elevOpts);

            if (elevRes.Status != PromptStatus.OK)
            {
                return;
            }

            double elevation = elevRes.Value;

            // Tolérance pour les erreurs d'arrondi
            double tolerance = 0.001;

            PromptSelectionResult selRes =
                ed.SelectAll();

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nAucun objet trouvé.");
                return;
            }

            using (Transaction tr =
                db.TransactionManager.StartTransaction())
            {
                ObjectId[] selectedIds = selRes.Value.GetObjectIds();

                var ids = new List<ObjectId>();

                foreach (ObjectId id in selectedIds)
                {
                    Entity entity =
                        tr.GetObject(id, OpenMode.ForRead) as Entity;

                    if (entity == null)
                    {
                        continue;
                    }

                    double? entityElevation =                        entity.GetElevation();

                    if (!entityElevation.HasValue)
                    {
                        continue;
                    }

                    if (Math.Abs(
                        entityElevation.Value - elevation) <= tolerance)
                    {
                        ids.Add(id);
                    }
                }

                tr.Commit();
                ed.SetImpliedSelection(ids.ToArray());

                ed.WriteMessage(                    $"\n{ids.Count} objet(s) trouvé(s) à l'altitude {elevation:0.###}.");
            }
        }

        
    }

}
