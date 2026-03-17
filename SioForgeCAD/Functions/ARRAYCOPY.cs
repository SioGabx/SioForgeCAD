using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class ARRAYCOPY
    {
        public const string MODE_DIVIDE = "Diviser";
        public const string MODE_MULTIPLY = "Multiplier";

        public static void Execute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            Database db = doc.Database;

            // 1. Sélection des objets
            var selRes = ed.GetSelectionRedraw("Sélectionnez les objets à copier :", true, false, null);
            if (selRes.Status != PromptStatus.OK) return;

            // 2. Point de base
            var ptRes = ed.GetPoint("\nPoint de base : ");
            if (ptRes.Status != PromptStatus.OK) return;
            Point3d basePt = ptRes.Value;

            DBObjectCollection sourceEntities = new DBObjectCollection();
            DBObjectCollection previewEntities = new DBObjectCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    {
                        sourceEntities.Add(ent.Clone() as Entity);
                        previewEntities.Add(ent.Clone() as Entity);
                    }
                }
                tr.Commit();
            }

            // Déplacement des entités de preview à l'origine pour le Transient
            var TransMatrix = Matrix3d.Displacement(basePt.GetVectorTo(Point3d.Origin));
            foreach (Entity entprev in previewEntities)
            {
                entprev.TransformBy(TransMatrix);
            }

            // 3. Point de copie (avec aperçu dynamique de la première copie)
            Point3d targetPt;
            // Assure-toi que GetPointTransientNoColorChange est bien accessible ici
            using (var getPtTrans = new GetPointTransientNoColorChange(previewEntities, null))
            {
                var targetRes = getPtTrans.GetPoint("Point de copie : ", basePt.ToPoints(), false);
                if (targetRes.PromptPointResult.Status != PromptStatus.OK)
                {
                    sourceEntities.DeepDispose();
                    return;
                }
                targetPt = targetRes.PromptPointResult.Value;
            }

            Vector3d vec = basePt.GetVectorTo(targetPt);
            if (vec.IsZeroLength())
            {
                Generic.WriteMessage("\nLe point de copie doit être différent du point de base.");
                sourceEntities.DeepDispose();
                return;
            }

            // 4. Initialisation du mode d'Array "Sketchup"
            string currentMode = MODE_MULTIPLY;
            int currentCount = 1;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                previewEntities = new DBObjectCollection(); // Reset pour la boucle interactive

                while (true)
                {
                    // Nettoyage de la RAM : on supprime l'ancien aperçu avant d'en créer un nouveau
                    previewEntities.DeepDispose();

                    // Génération des nouvelles entités clonées en fonction du vecteur et de la quantité
                    previewEntities = GenerateArrayEntities(sourceEntities, vec, currentMode, currentCount);

                    // 5. Affichage dynamique et demande de la valeur
                    using (var getStringTrans = new GetStringTransientNoColorChange(null))
                    {
                        getStringTrans.SetStaticEntities = previewEntities; //GetStringTransientNoColorChange dispose ents after using

                        string msg = $"Copie [{currentMode} x{currentCount} | Dist: {vec.Length:F2}] (Tapez une distance, ou *5, /3, ou Entrée pour valider) : ";
                        var strRes = getStringTrans.GetString(msg);

                        // Validation de la commande si l'utilisateur appuie sur Espace ou Entrée à vide
                        if (strRes.Status == PromptStatus.None || string.IsNullOrWhiteSpace(strRes.StringResult))
                        {
                            sourceEntities.DeepDispose();
                            sourceEntities = new DBObjectCollection();
                            previewEntities.ToList().ForEach(t => sourceEntities.Add(t.Clone() as Entity));
                            break;
                        }
                        else if (strRes.Status != PromptStatus.OK)
                        {
                            Generic.WriteMessage("\nOpération annulée.");
                            sourceEntities.DeepDispose();
                            previewEntities.DeepDispose();
                            return;
                        }

                        // Analyse de la chaîne tapée
                        string input = strRes.StringResult.Trim().ToUpper();
                        ParseSketchUpInput(input, ref currentMode, ref currentCount, ref vec);
                    }
                }

                // 6. Validation finale : Écriture dans la base de données
                // On injecte simplement la dernière collection previewEntities validée
                foreach (Entity ent in sourceEntities)
                {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent, true);
                }

                // Nettoyage des objets sources
                sourceEntities.DeepDispose();

                tr.Commit();
            }
        }

        // --- Méthodes utilitaires ---

        private static DBObjectCollection GenerateArrayEntities(DBObjectCollection source, Vector3d vec, string mode, int count)
        {
            DBObjectCollection result = new DBObjectCollection();
            foreach (Entity ent in source)
            {
                // i commence à 1 pour ne pas superposer une copie sur l'original au point de base
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

            // Détection des modificateurs d'Array
            if (input.StartsWith("*") || input.StartsWith("X"))
            {
                currentMode = MODE_MULTIPLY;
                numberPart = input.Substring(1);
                isArrayModifier = true;
            }
            else if (input.EndsWith("X"))
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

            // Remplacement sécurisé de la virgule pour la culture Invariant
            numberPart = numberPart.Replace(',', '.');

            if (isArrayModifier)
            {
                if (int.TryParse(numberPart, out int parsedCount))
                {
                    currentCount = Math.Max(1, parsedCount); // Empêche le 0 ou négatif
                }
                else
                {
                    Generic.WriteMessage($"\nQuantité invalide ignorée : {input}");
                }
            }
            else
            {
                // S'il n'y a pas de modificateur, on considère que c'est une nouvelle distance
                if (double.TryParse(numberPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedDistance))
                {
                    if (parsedDistance > 0)
                    {
                        // On redéfinit la longueur du vecteur en conservant sa direction
                        vec = vec.GetNormal() * parsedDistance;
                    }
                    else
                    {
                        Generic.WriteMessage("\nLa distance doit être strictement positive.");
                    }
                }
                else
                {
                    Generic.WriteMessage($"\nSaisie invalide ignorée : {input}");
                }
            }
        }
    }
}