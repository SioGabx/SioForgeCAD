using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCLEGEND
    {

        public static void Add()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            // Liste pour stocker les noms de blocs uniques
            List<string> blockNames = new List<string>();

            // Demander à l'utilisateur de sélectionner des blocs
            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    SelectionSet selSet = selResult.Value;
                    foreach (SelectedObject selObj in selSet)
                    {
                        if (selObj != null)
                        {
                            // Vérifier si l'objet sélectionné est un bloc
                            if (selObj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                            {
                                BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                                if (blkRef != null)
                                {
                                    string blockName = blkRef.GetBlockReferenceName();
                                    // Ajouter le nom du bloc à la liste s'il n'y est pas déjà
                                    if (!blockNames.Contains(blockName))
                                    {
                                        blockNames.Add(blockName);
                                    }
                                }
                            }
                        }
                    }
                    tr.Commit();
                }



                // Trie par ordre alphabétique
                blockNames.Sort();

                foreach (string name in blockNames)
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        var Object = VEGBLOC.AskInsertVegBloc(name);
                        if (Object != ObjectId.Null)
                        {
                            var BlockReference = Object.GetDBObject(OpenMode.ForWrite) as BlockReference;
                            var Extend = BlockReference.GetExtents();
                            double scale = 1.0 / Extend.Size().Width;

                            // Créer une matrice de transformation d'échelle
                            Matrix3d scaleMatrix = Matrix3d.Scaling(scale, BlockReference.Position);
                            BlockReference.TransformBy(scaleMatrix);

                            Point3d textPosition = BlockReference.Position + new Vector3d(1.5, 0, 0);
                            MText text = new MText
                            {
                                Contents = name.Split('_').Last(),
                                Location = textPosition,
                                Height = 0.1,
                                TextHeight = 0.1,
                                Attachment = AttachmentPoint.MiddleLeft,
                                Normal = Vector3d.ZAxis,
                                Rotation = 0
                            };
                            text.AddToDrawingCurrentTransaction();

                        }
                        tr.Commit();
                    }
                }

            }
        }


    }
}
