using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
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

            Dictionary<string, HashSet<string>> VegTypes = new Dictionary<string, HashSet<string>>();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Matrix3d ucsMatrix = ed.CurrentUserCoordinateSystem;
                    double ucsAngle = System.Math.Atan2(ucsMatrix.CoordinateSystem3d.Xaxis.Y, ucsMatrix.CoordinateSystem3d.Xaxis.X);
                    Matrix3d ucsRotationMatrix = Matrix3d.Rotation(ucsAngle, Vector3d.ZAxis, Point3d.Origin);

                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        if (selObj?.ObjectId.IsDerivedFrom(typeof(BlockReference)) == true)
                        {
                            BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                            if (!blkRef.IsXref())
                            {
                                string blockName = blkRef.GetBlockReferenceName();
                                var Infos = VEGBLOC.GetDataStore(blkRef);
                                if (Infos is null) { continue; }
                                string Type = Infos[VEGBLOC.DataStore.Type].UcFirst();

                                if (!VegTypes.TryGetValue(Type, out HashSet<string> value))
                                {
                                    value = new HashSet<string>();
                                    VegTypes[Type] = value;
                                }
                                value.Add(blockName);
                            }
                        }
                    }
                    if (VegTypes.Count == 0)
                    {
                        Generic.WriteMessage("Aucun bloc compatible sélectionné.");
                        tr.Commit();
                        return;
                    }

                    const double rowSpacing = 1;
                    const double titleSpacing = rowSpacing;
                    const double blockSizeInMetters = 0.8;

                    double yPosition = Point3d.Origin.Y + titleSpacing;
                    double xPosition = Point3d.Origin.X + (blockSizeInMetters / 2);
                    string CartoucheLayerName = SymbolUtilityServices.RepairSymbolName($"{Settings.CADLayerPrefix}CARTOUCHE", false);
                    Layers.CreateLayer(CartoucheLayerName, Color.FromColorIndex(ColorMethod.ByAci, 7), LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(0), true);
                    Dictionary<string, DBObjectCollection> LegendeByCategories = new Dictionary<string, DBObjectCollection>();
                    foreach (var Type in VegTypes.OrderBy(c =>
                    {
                        if (System.Enum.TryParse(c.Key, true, out VEGBLOC.VegblocTypes vegEnum))
                        {
                            return (int)vegEnum;
                        }
                        return int.MaxValue;
                    }).ThenBy(c => c.Key))
                    {
                        var LegendPart = new DBObjectCollection();
                        yPosition -= titleSpacing;
                        var CategoryName = Type.Key;

                        var CategoryNameMText = new MText
                        {
                            Contents = CategoryName,
                            Location = new Point3d(xPosition - (blockSizeInMetters / 2), yPosition, Point3d.Origin.Z),
                            Height = 0.35,
                            TextHeight = 0.35,
                            Attachment = AttachmentPoint.MiddleLeft,
                            Normal = Vector3d.ZAxis,
                            Layer = CartoucheLayerName,
                            Rotation = -ucsAngle,
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
                            var Layer = blockName;
                            if (!Layers.CheckIfLayerExist(Layer))
                            {
                                Generic.WriteMessage($"Erreur détéctée sur le calque du bloc {blockName}, veuillez corriger le calque manuellement");
                                ObjectIdCollection objectIdCollection = BlockReferences.GetAllBlockReferenceInstances(blockName, tr, db);
                                var MostUsedLayer = Layers.GetLayerCountsFromCollection(objectIdCollection).FirstOrDefault();
                                if (string.IsNullOrEmpty(MostUsedLayer.Key)) continue;//should never happen
                                Layer = MostUsedLayer.Key;
                            }


                            LegendPart.Add(new MText
                            {
                                Contents = Infos[VEGBLOC.DataStore.CompleteName],
                                Location = new Point3d(xPosition + rowSpacing, yPosition, Point3d.Origin.Z),
                                Height = 0.25,
                                TextHeight = 0.25,
                                Attachment = AttachmentPoint.MiddleLeft,
                                Normal = Vector3d.ZAxis,
                                Layer = Layer,
                                Rotation = -ucsAngle,
                                Transparency = Generic.GetTransparencyFromAlpha(0),
                                ColorIndex = 256
                            });

                            var Extend = BlockReference.GetExtents();
                            double blockWidth = Extend.Size().Width;
                            double scale = blockWidth > Generic.LowTolerance.EqualPoint ? blockSizeInMetters / blockWidth : 1.0;

                            Matrix3d scaleMatrix = Matrix3d.Scaling(scale, BlockReference.Position);
                            BlockReference.TransformBy(scaleMatrix);
                            BlockReference.Layer = Layer;
                            BlockReference.ColorIndex = 256;
                            LegendPart.Add(BlockReference);
                            yPosition -= rowSpacing;
                        }
                        foreach (Entity ent in LegendPart)
                        {
                            ent.TransformBy(ucsRotationMatrix);
                        }

                        LegendeByCategories.TryAdd(CategoryName, LegendPart);
                    }

                    DBObjectCollection EntsForTransients = new DBObjectCollection();
                    foreach (var LegendPart in LegendeByCategories)
                    {
                        EntsForTransients.AddRange(LegendPart.Value);
                    }

                    VEGBLOC.GetVegBlocPointTransient insertionTransientPoints = new VEGBLOC.GetVegBlocPointTransient(EntsForTransients, null);
                    var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du bloc", Points.Null, false);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return;
                    }

                    Matrix3d DisplacementMatrix = Point3d.Origin.GetDisplacementMatrixTo(NewPointLocation.SCG);
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
