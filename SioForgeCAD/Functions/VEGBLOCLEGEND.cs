using Autodesk.AutoCAD.Colors;
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
        public static void AddOld()
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
                                if (blkRef?.IsXref() == false)
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

                //TODO : Create a table of block, hauteur entre = 1.25, faire groupe a chaque changement de types
                //Title font : .35, position : .9 above block, 2.85 bellow previous
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

                            //TODO : set same color of block, search color for good color ratio
                            MText text = new MText
                            {
                                Contents = name.Split('_').Last(),
                                Location = textPosition,
                                Height = 0,
                                TextHeight = 0.25,
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



        public static void Add()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            Dictionary<string, List<string>> categorizedBlocks = new Dictionary<string, List<string>>();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj != null && selObj.ObjectId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                        {
                            BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                            if (blkRef?.IsXref() == false)
                            {
                                string blockName = blkRef.GetBlockReferenceName();
                                var Infos = VEGBLOC.GetDataStore(blkRef);
                                string Type = Infos[VEGBLOC.DataStore.Type];

                                if (!categorizedBlocks.TryGetValue(Type, out List<string> value))
                                {
                                    value = new List<string>();
                                    categorizedBlocks[Type] = value;
                                }
                                if (!value.Contains(blockName))
                                {
                                    value.Add(blockName);
                                }
                            }
                        }
                    }
                    tr.Commit();
                }

                double yPosition = 0.0;
                double rowSpacing = 1;
                double titleSpacing = 2.85;
                DBObjectCollection Legende = new DBObjectCollection();

                string CartoucheName = SymbolUtilityServices.RepairSymbolName($"{Settings.VegblocLayerPrefix}_CARTOUCHE", false);
                Layers.CreateLayer(CartoucheName, Color.FromColorIndex(ColorMethod.ByAci, 1), LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(0), true);

                foreach (var category in categorizedBlocks.OrderBy(c => c.Key))
                {
                    yPosition -= titleSpacing;
                    MText CategoryName = new MText
                    {
                        Contents = category.Key.UcFirst(),
                        Location = new Point3d(0, yPosition, 0),
                        Height = 0.35,
                        Attachment = AttachmentPoint.MiddleLeft,
                        Normal = Vector3d.ZAxis,
                        Layer = CartoucheName
                    };
                    Legende.Add(CategoryName);
                    yPosition -= rowSpacing;
                    foreach (var blockName in category.Value.OrderBy(n => n))
                    {
                        MText LatinName = new MText
                        {
                            Contents = blockName.Split('_').Last(),
                            Location = new Point3d(1, yPosition, 0),
                            Height = 0.25,
                            Attachment = AttachmentPoint.MiddleLeft,
                            Normal = Vector3d.ZAxis,
                            Layer = blockName,
                            ColorIndex = 256
                        };

                        Legende.Add(LatinName);

                        var BlockReference = BlockReferences.GetBlockReference(blockName, new Point3d(0, yPosition, 0));
                        var Extend = BlockReference.GetExtents();
                        double scale = 1.0 / Extend.Size().Width;

                        // Créer une matrice de transformation d'échelle
                        Matrix3d scaleMatrix = Matrix3d.Scaling(scale, BlockReference.Position);
                        BlockReference.TransformBy(scaleMatrix);
                        BlockReference.Layer = blockName;
                        BlockReference.ColorIndex = 256;
                        Legende.Add(BlockReference);
                        yPosition -= rowSpacing;
                    }
                }

                Legende.GetExtents().GetGeometry().AddToDrawing();
                Legende.AddToDrawing();
            }
        }
    }
}
