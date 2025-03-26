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

            Dictionary<string, List<string>> VegTypes = new Dictionary<string, List<string>>();

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

                                if (!VegTypes.TryGetValue(Type, out List<string> value))
                                {
                                    value = new List<string>();
                                    VegTypes[Type] = value;
                                }
                                if (!value.Contains(blockName))
                                {
                                    value.Add(blockName);
                                }
                            }
                        }
                    }


                    double rowSpacing = 1;
                    double titleSpacing = rowSpacing;
                    double blockSizeInMetters = 0.8;

                    double yPosition = Point3d.Origin.Y + titleSpacing;
                    double xPosition = Point3d.Origin.X + (blockSizeInMetters / 2);
                    string CartoucheLayerName = SymbolUtilityServices.RepairSymbolName($"{Settings.CADLayerPrefix}CARTOUCHE", false);
                    Layers.CreateLayer(CartoucheLayerName, Color.FromColorIndex(ColorMethod.ByAci, 7), LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(0), true);
                    Dictionary<string, DBObjectCollection> LegendeByCategories = new Dictionary<string, DBObjectCollection>();
                    foreach (var Type in VegTypes.OrderBy(c => c.Key))
                    {
                        var LegendPart = new DBObjectCollection();
                        yPosition -= titleSpacing;
                        var CategoryName = Type.Key.UcFirst();

                        var CategoryNameMText = new MText
                        {
                            Contents = CategoryName,
                            Location = new Point3d(xPosition - (blockSizeInMetters / 2), yPosition, Point3d.Origin.Z),
                            Height = 0.35,
                            TextHeight = 0.35,
                            Attachment = AttachmentPoint.MiddleLeft,
                            Normal = Vector3d.ZAxis,
                            Layer = CartoucheLayerName,
                            ColorIndex = 256
                        };
                        TextEditor te = TextEditor.CreateTextEditor(CategoryNameMText);
                        if (te == null) { return; }
                        te.SelectAll();
                        te.Selection.Bold = true;
                        te.Close(TextEditor.ExitStatus.ExitSave);
                        LegendPart.Add(CategoryNameMText);

                        yPosition -= rowSpacing;
                        foreach (var blockName in Type.Value.OrderBy(n => n))
                        {
                            var BlockReference = BlockReferences.GetBlockReference(blockName, new Point3d(xPosition, yPosition, Point3d.Origin.Z));
                            var Infos = VEGBLOC.GetDataStore(BlockReference);

                            LegendPart.Add(new MText
                            {
                                Contents = Infos[VEGBLOC.DataStore.CompleteName],
                                Location = new Point3d(xPosition + rowSpacing, yPosition, Point3d.Origin.Z),
                                Height = 0.25,
                                TextHeight = 0.25,
                                Attachment = AttachmentPoint.MiddleLeft,
                                Normal = Vector3d.ZAxis,
                                Layer = blockName,
                                Transparency = Generic.GetTransparencyFromAlpha(0),
                                ColorIndex = 256
                            });

                            var Extend = BlockReference.GetExtents();
                            double scale = blockSizeInMetters / Extend.Size().Width;

                            // Créer une matrice de transformation d'échelle
                            Matrix3d scaleMatrix = Matrix3d.Scaling(scale, BlockReference.Position);
                            BlockReference.TransformBy(scaleMatrix);
                            BlockReference.Layer = blockName;
                            BlockReference.ColorIndex = 256;
                            LegendPart.Add(BlockReference);
                            yPosition -= rowSpacing;
                        }

                        LegendeByCategories.Add(CategoryName, LegendPart);
                    }

                    DBObjectCollection EntsForTransients = new DBObjectCollection();
                    foreach (var LegendPart in LegendeByCategories)
                    {
                        EntsForTransients.AddRange(LegendPart.Value);
                    }

                    VEGBLOC.GetVegBlocPointTransient insertionTransientPoints = new VEGBLOC.GetVegBlocPointTransient(EntsForTransients, null);
                    var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du bloc", Points.Null);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }

                    Vector3d DisplacementVector = Point3d.Origin.GetVectorTo(NewPointLocation.SCG);
                    Matrix3d DisplacementMatrix = Matrix3d.Displacement(DisplacementVector);
                    foreach (var LegendPart in LegendeByCategories)
                    {
                        ObjectIdCollection CategoryObjIdCollection = new ObjectIdCollection();
                        foreach (var DbObjEnt in LegendPart.Value)
                        {
                            if (DbObjEnt is Entity ent)
                            {
                                ent.TransformBy(DisplacementMatrix);
                                CategoryObjIdCollection.Add(ent.AddToDrawingCurrentTransaction());
                            }
                        }
                        Groups.Create(Settings.InfoLayerPrefix + "Legende_" + LegendPart.Key, "", CategoryObjIdCollection);
                    }

                    tr.Commit();
                }
            }
        }
    }
}
