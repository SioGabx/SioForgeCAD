using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.JSONParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class BLKMANAGEPOSITION
    {
        // Structure de données pour le JSON
        public class PlacementData
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Z { get; set; }
            public double Rotation { get; set; } // En radians pour coller à l'API interne
        }

        public static void Menu()
        {
            Editor ed = Generic.GetEditor();

            if (!ed.GetBlocks(out ObjectId[] ObjectIds, "Sélectionnez un bloc ou une XREF", true, true))
            {
                return;
            }
            if (ObjectIds.Length > 1)
            {
                Generic.WriteMessage($"{ObjectIds.Length} blocks séléctionnés. Veuillez selectionner seulement un bloc ou une XREF à la fois.");
                return;
            }

            ObjectId selectedId = ObjectIds[0];
            // 2. Choix de l'action via votre extension personnalisée
            var res = ed.GetOptions("\nAction sur le placement", false, "COPIER", "COLLER", "MODIFIER");

            if (res.Status == PromptStatus.OK)
            {
                if (res.StringResult == "COPIER")
                {
                    CopyPlacement(selectedId);
                }
                else if (res.StringResult == "COLLER")
                {
                    PastePlacement(selectedId);
                }
                else if (res.StringResult == "MODIFIER")
                {
                    ModifyPlacement(selectedId);
                }
            }
        }

        private static void CopyPlacement(ObjectId blockId)
        {
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blk = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;

                PlacementData data = new PlacementData
                {
                    X = blk.Position.X,
                    Y = blk.Position.Y,
                    Z = blk.Position.Z,
                    Rotation = blk.Rotation
                };

                string json = data.ToJson();
                Clipboard.SetText(json);

                Generic.WriteMessage($"Placement copié dans le presse-papiers : X={data.X:F3}, Y={data.Y:F3}, Z={data.Z:F3}, Rot={data.Rotation * 180.0 / Math.PI:F2}°");

                tr.Commit();
            }
        }

        private static void PastePlacement(ObjectId blockId)
        {
            Database db = Generic.GetDatabase();

            if (!Clipboard.ContainsText())
            {
                Generic.WriteMessage("Le presse-papiers ne contient pas de texte.");
                return;
            }

            string json = Clipboard.GetText();
            PlacementData data;

            try
            {
                data = json.FromJson<PlacementData>();
            }
            catch
            {
                Generic.WriteMessage("Le contenu du presse-papiers n'est pas un JSON de placement valide.");
                return;
            }

            if (data == null)
            {
                Generic.WriteMessage("Données de placement invalides.");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blk = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;

                // Application de la position et de la rotation
                blk.Position = new Point3d(data.X, data.Y, data.Z);
                blk.Rotation = data.Rotation;

                Generic.WriteMessage("Placement collé avec succès.");
                tr.Commit();
            }
        }

        private static void ModifyPlacement(ObjectId blockId)
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blk = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;

                // Modification de X
                PromptDoubleOptions pdoX = new PromptDoubleOptions($"\nNouvelle position X <{blk.Position.X:F4}>: ")
                {
                    UseDefaultValue = true,
                    DefaultValue = blk.Position.X
                };
                PromptDoubleResult pdrX = ed.GetDouble(pdoX);
                if (pdrX.Status != PromptStatus.OK) return;

                // Modification de Y
                PromptDoubleOptions pdoY = new PromptDoubleOptions($"\nNouvelle position Y <{blk.Position.Y:F4}>: ")
                {
                    UseDefaultValue = true,
                    DefaultValue = blk.Position.Y
                };
                PromptDoubleResult pdrY = ed.GetDouble(pdoY);
                if (pdrY.Status != PromptStatus.OK) return;

                // Modification de Z
                PromptDoubleOptions pdoZ = new PromptDoubleOptions($"\nNouvelle position Z <{blk.Position.Z:F4}>: ")
                {
                    UseDefaultValue = true,
                    DefaultValue = blk.Position.Z
                };
                PromptDoubleResult pdrZ = ed.GetDouble(pdoZ);
                if (pdrZ.Status != PromptStatus.OK) return;

                // Modification de la Rotation (conversion radians -> degrés pour la saisie utilisateur)
                double currentRotDeg = blk.Rotation * 180.0 / Math.PI;
                PromptDoubleOptions pdoRot = new PromptDoubleOptions($"\nNouvelle rotation (en degrés) <{currentRotDeg:F4}>: ")
                {
                    UseDefaultValue = true,
                    DefaultValue = currentRotDeg
                };
                PromptDoubleResult pdrRot = ed.GetDouble(pdoRot);
                if (pdrRot.Status != PromptStatus.OK) return;

                // Application des modifications (conversion degrés -> radians pour la base de données)
                blk.Position = new Point3d(pdrX.Value, pdrY.Value, pdrZ.Value);
                blk.Rotation = pdrRot.Value * Math.PI / 180.0;

                Generic.WriteMessage("Placement modifié avec succès.");

                tr.Commit();
            }
        }
    }
}
