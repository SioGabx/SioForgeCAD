using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Functions
{
    public static class ARRAYCOPY
    {
        public const string MODE_DIVIDE = "Diviser";
        public const string MODE_MULTIPLY = "Multiplier";

        public static void Execute()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDocument().Database;

            // 1. Sélection et Point de base
            var selRes = ed.GetSelectionRedraw("Sélectionnez les objets à copier :", true, false, null);
            if (selRes.Status != PromptStatus.OK) return;

            var ptRes = ed.GetPoint("\nPoint de base : ");
            if (ptRes.Status != PromptStatus.OK) return;
            Point3d basePt = ptRes.Value;

            // 2. Extraction des entités sources (Clonage initial)
            DBObjectCollection sourceEntities = GetSourceEntities(db, selRes.Value.GetObjectIds());
            if (sourceEntities.Count == 0) return;

            try
            {
                // 3. Point cible
                if (!TryGetTargetPoint(basePt, sourceEntities, out Point3d targetPt)) return;

                Vector3d vec = basePt.GetVectorTo(targetPt);
                if (vec.IsZeroLength())
                {
                    Generic.WriteMessage("Le point de copie doit être différent du point de base.");
                    return;
                }

                // 4. Boucle interactive "Sketchup"
                DBObjectCollection finalEntities = RunInteractiveArrayLoop(sourceEntities, vec);

                // 5. Validation finale (Injection dans la DB)
                if (finalEntities != null && finalEntities.Count > 0)
                {
                    CommitEntities(db, finalEntities);
                    finalEntities.DeepDispose(); // Nettoyage après écriture
                }
            }
            finally
            {
                sourceEntities?.DeepDispose();
            }
        }



        private static DBObjectCollection GetSourceEntities(Database db, ObjectId[] ids)
        {
            var source = new DBObjectCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    {
                        source.Add(ent.Clone() as Entity);
                    }
                }
                tr.Commit();
            }
            return source;
        }

        private static bool TryGetTargetPoint(Point3d basePt, DBObjectCollection sourceEntities, out Point3d targetPt)
        {
            targetPt = Point3d.Origin;

            // Préparation de l'aperçu au point 0,0,0 pour le Transient
            DBObjectCollection previewEntities = sourceEntities.DeepClone();
            var transMatrix = Matrix3d.Displacement(basePt.GetVectorTo(Point3d.Origin));
            foreach (Entity ent in previewEntities)
            {
                ent.TransformBy(transMatrix);
            }

            using (var getPtTrans = new GetPointTransientNoColorChange(previewEntities, null))
            {
                var targetRes = getPtTrans.GetPoint("Point de copie : ", basePt.ToPoints(), false);
                if (targetRes.PromptPointResult.Status != PromptStatus.OK)
                {
                    return false;
                }

                targetPt = targetRes.PromptPointResult.Value;
                return true;
            }
        }

        private static DBObjectCollection RunInteractiveArrayLoop(DBObjectCollection sourceEntities, Vector3d vec)
        {
            string currentMode = MODE_MULTIPLY;
            int currentCount = 1;
            DBObjectCollection previewEntities = null;

            while (true)
            {
                // Nettoyage de l'ancien aperçu
                previewEntities?.DeepDispose();

                // Génération du nouveau
                previewEntities = GenerateArrayEntities(sourceEntities, vec, currentMode, currentCount);

                using (var getStringTrans = new GetStringTransientNoColorChange(null))
                {
                    getStringTrans.SetStaticEntities = previewEntities;

                    string msg = $"Tapez une distance, ou *5, /3, ou Entrée pour valider\u2028Copie {currentMode} x{currentCount} | Dist: {vec.Length:F2} : ";

                    var strRes = getStringTrans.GetString(msg);

                    if (strRes.Status == PromptStatus.None || string.IsNullOrWhiteSpace(strRes.StringResult))
                    {
                        return previewEntities.DeepClone();
                    }

                    // Annulation
                    if (strRes.Status != PromptStatus.OK)
                    {
                        Generic.WriteMessage("Opération annulée.");
                        previewEntities?.DeepDispose();
                        return null;
                    }

                    // Mise à jour des paramètres selon la saisie
                    string input = strRes.StringResult.Trim().ToUpper();
                    ParseSketchUpInput(input, ref currentMode, ref currentCount, ref vec);
                }
            }
        }

        private static void CommitEntities(Database db, DBObjectCollection entities)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                foreach (Entity ent in entities)
                {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }
                tr.Commit();
            }
        }

        private static DBObjectCollection GenerateArrayEntities(DBObjectCollection source, Vector3d vec, string mode, int count)
        {
            var result = new DBObjectCollection();
            foreach (Entity ent in source)
            {
                for (int i = 1; i <= count; i++)
                {
                    Entity clone = ent.Clone() as Entity;
                    double factor = mode == MODE_MULTIPLY ? i : ((double)i / count);
                    clone.TransformBy(Matrix3d.Displacement(vec * factor));
                    result.Add(clone);
                }
            }
            return result;
        }

        private static void ParseSketchUpInput(string input, ref string currentMode, ref int currentCount, ref Vector3d vec)
        {
            string numberPart = input;
            bool isArrayModifier = false;

            if (input.StartsWith("*") || input.StartsWith("X", StringComparison.InvariantCultureIgnoreCase))
            {
                currentMode = MODE_MULTIPLY;
                numberPart = input.Substring(1);
                isArrayModifier = true;
            }
            else if (input.EndsWith("X", StringComparison.InvariantCultureIgnoreCase))
            {
                currentMode = MODE_MULTIPLY;
                numberPart = input.Substring(0, input.Length - 1);
                isArrayModifier = true;
            }
            else if (input.StartsWith("/"))
            {
                currentMode = MODE_DIVIDE;
                numberPart = input.Substring(1);
                isArrayModifier = true;
            }

            numberPart = numberPart.Replace(',', '.');

            if (isArrayModifier)
            {
                if (int.TryParse(numberPart, out int parsedCount))
                {
                    currentCount = Math.Max(1, parsedCount);
                }
                else
                {
                    Generic.WriteMessage($"Quantité invalide ignorée : {input}");
                }
            }
            else
            {
                if (double.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedDistance))
                {
                    if (parsedDistance > 0)
                    {
                        vec = vec.GetNormal() * parsedDistance;
                    }
                    else
                    {
                        Generic.WriteMessage("La distance doit être strictement positive.");
                    }
                }
                else
                {
                    Generic.WriteMessage($"Saisie invalide ignorée : {input}");
                }
            }
        }
    }
}