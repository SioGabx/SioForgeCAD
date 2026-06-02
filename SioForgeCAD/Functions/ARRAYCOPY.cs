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
            Point3d basePt = Points.ToSCGFromCurentSCU(ptRes.Value);

            ObjectId[] sourceIds = selRes.Value.GetObjectIds();
            if (sourceIds.Length == 0) return;

            DBObjectCollection previewSources = GetTransientPreviewEntities(db, sourceIds);

            try
            {
                if (!TryGetTargetPoint(basePt, previewSources, out Point3d targetPt)) return;

                Vector3d vec = basePt.GetVectorTo(targetPt);
                if (vec.IsZeroLength())
                {
                    Generic.WriteMessage("Le point de copie doit être différent du point de base.");
                    return;
                }

                // Boucle interactive : on récupère les paramètres finaux (et non plus des entités)
                if (RunInteractiveArrayLoop(previewSources, ref vec, out string finalMode, out int finalCount))
                {
                    CommitWithDeepCloneObjects(db, sourceIds, vec, finalMode, finalCount);
                }
            }
            finally
            {
                previewSources?.DeepDispose();
            }
        }

        private static DBObjectCollection GetTransientPreviewEntities(Database db, ObjectId[] ids)
        {
            var source = new DBObjectCollection();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    {
                        // Un simple clone suffit pour le fantôme visuel
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
                    previewEntities.DeepDispose();
                    return false;
                }

                targetPt = Points.GetFromPromptPointResult(targetRes.PromptPointResult).SCG;
                previewEntities.DeepDispose();
                return true;
            }
        }

        private static bool RunInteractiveArrayLoop(DBObjectCollection sourceEntities, ref Vector3d vec, out string finalMode, out int finalCount)
        {
            finalMode = MODE_MULTIPLY;
            finalCount = 1;
            DBObjectCollection previewEntities = null;

            while (true)
            {
                previewEntities?.DeepDispose();
                previewEntities = GenerateTransientArray(sourceEntities, vec, finalMode, finalCount);

                using (var getStringTrans = new GetStringTransientNoColorChange(null))
                {
                    getStringTrans.SetStaticEntities = previewEntities;
                    double interdistance = finalMode == MODE_MULTIPLY ? vec.Length : (vec.Length / finalCount);
                    string msg = $"Tapez une distance, ou *5, /3, ou Entrée pour valider\u2028 | {finalMode} x {finalCount}\u2028 | Dist: {vec.Length:F2}\u2028 | Interdistance: {interdistance:F2} : ";

                    var strRes = getStringTrans.GetString(msg);

                    if (strRes.Status == PromptStatus.None || string.IsNullOrWhiteSpace(strRes.StringResult))
                    {
                        previewEntities?.DeepDispose();
                        return true;
                    }

                    if (strRes.Status != PromptStatus.OK)
                    {
                        Generic.WriteMessage("Opération annulée.");
                        previewEntities?.DeepDispose();
                        return false;
                    }

                    string input = strRes.StringResult.Trim().ToUpper();
                    ParseSketchUpInput(input, ref finalMode, ref finalCount, ref vec);
                }
            }
        }

        private static DBObjectCollection GenerateTransientArray(DBObjectCollection source, Vector3d vec, string mode, int count)
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

        private static void CommitWithDeepCloneObjects(Database db, ObjectId[] sourceIds, Vector3d vec, string mode, int count)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId ownerId = db.CurrentSpaceId; // On clone dans l'espace courant (Objet ou Papier)
                ObjectIdCollection idsToClone = new ObjectIdCollection(sourceIds);

                for (int i = 1; i <= count; i++)
                {
                    double factor = mode == MODE_MULTIPLY ? i : ((double)i / count);
                    var transMatrix = Matrix3d.Displacement(vec * factor);

                    IdMapping mapping = new IdMapping();

                    db.DeepCloneObjects(idsToClone, ownerId, mapping, false);

                    // On récupère les nouvelles entités clonées via l'IdMapping pour appliquer le déplacement géométrique
                    foreach (ObjectId originalId in sourceIds)
                    {
                        IdPair pair = mapping.Lookup(originalId);

                        if (pair.Value != ObjectId.Null && pair.IsCloned)
                        {
                            if (tr.GetObject(pair.Value, OpenMode.ForWrite) is Entity clonedEnt)
                            {
                                clonedEnt.TransformBy(transMatrix);
                            }
                        }
                    }
                }
                tr.Commit();
            }
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