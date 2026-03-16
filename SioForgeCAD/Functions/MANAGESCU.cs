using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.JSONParser;
using System.Collections.Generic;
using System.Windows;


namespace SioForgeCAD.Functions
{
    public static class MANAGESCU
    {
        public class UcsData
        {
            public string Name { get; set; }
            public double[] Origin { get; set; }
            public double[] XAxis { get; set; }
            public double[] YAxis { get; set; }
        }

        public static void Menu()
        {
            var ed = Generic.GetEditor();
            var res = ed.GetOptions("Quelle option effectuer pour les SCU ?", false, "COPIER", "COLLER");

            if (res.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                if (res.StringResult == "COPIER")
                {
                    CopyUcs();
                }
                else if (res.StringResult == "COLLER")
                {
                    PasteUcs();
                }
            }
        }
        public static void CopyUcs()
        {
            Database db = Generic.GetDatabase();
            var ucsList = new List<UcsData>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // On accède à la table des SCU
                UcsTable ucsTable = (UcsTable)tr.GetObject(db.UcsTableId, OpenMode.ForRead);

                foreach (ObjectId ucsId in ucsTable)
                {
                    UcsTableRecord ucs = (UcsTableRecord)tr.GetObject(ucsId, OpenMode.ForRead);

                    if (!string.IsNullOrEmpty(ucs.Name))
                    {
                        ucsList.Add(new UcsData
                        {
                            Name = ucs.Name,
                            Origin = new double[] { ucs.Origin.X, ucs.Origin.Y, ucs.Origin.Z },
                            XAxis = new double[] { ucs.XAxis.X, ucs.XAxis.Y, ucs.XAxis.Z },
                            YAxis = new double[] { ucs.YAxis.X, ucs.YAxis.Y, ucs.YAxis.Z }
                        });
                    }
                }
                tr.Commit();
            }

            if (ucsList.Count > 0)
            {
                string json = ucsList.ToJson();
                Clipboard.SetText(json);
                Generic.WriteMessage($"{ucsList.Count} SCU(s) copié(s) dans le presse-papiers (format JSON).");
            }
            else
            {
                Generic.WriteMessage("Aucun SCU personnalisé trouvé à copier dans ce dessin.");
            }
        }

        public static void PasteUcs()
        {
            Database db = Generic.GetDatabase();

            if (!Clipboard.ContainsText())
            {
                Generic.WriteMessage("Le presse-papiers ne contient pas de texte.");
                return;
            }

            string json = Clipboard.GetText();
            List<UcsData> ucsList = json.FromJson<List<UcsData>>();

            if (ucsList == null || ucsList.Count == 0)
            {
                Generic.WriteMessage("Le contenu du presse-papiers n'est pas un JSON valide ou est vide.");
                return;
            }

            int addedCount = 0;
            int skippedCount = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                UcsTable ucsTable = (UcsTable)tr.GetObject(db.UcsTableId, OpenMode.ForWrite);

                foreach (var ucsData in ucsList)
                {
                    // Vérifier si un SCU portant ce nom existe déjà pour éviter les doublons/erreurs
                    if (!ucsTable.Has(ucsData.Name))
                    {
                        UcsTableRecord newUcs = new UcsTableRecord();
                        newUcs.Name = ucsData.Name;
                        newUcs.Origin = new Point3d(ucsData.Origin[0], ucsData.Origin[1], ucsData.Origin[2]);
                        newUcs.XAxis = new Vector3d(ucsData.XAxis[0], ucsData.XAxis[1], ucsData.XAxis[2]);
                        newUcs.YAxis = new Vector3d(ucsData.YAxis[0], ucsData.YAxis[1], ucsData.YAxis[2]);

                        // Ajout à la table et à la transaction
                        ucsTable.Add(newUcs);
                        tr.AddNewlyCreatedDBObject(newUcs, true);
                        addedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                // Indispensable pour valider les modifications dans le dessin
                tr.Commit();
            }

            Generic.WriteMessage($"Opération terminée : {addedCount} SCU(s) collé(s), {skippedCount} ignoré(s) (déjà existants).");
        }
    }
}
