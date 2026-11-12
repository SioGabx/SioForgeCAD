using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCLEGEND
    {
        private const double RowSpacing = 1.0;
        private const double TitleSpacing = RowSpacing;
        private const double BlockSizeInMeters = 0.8;
        private const double HatchRectHeight = 0.5;

        private class LegendItemInfo : IEquatable<LegendItemInfo>
        {
            public bool IsHatch { get; set; }
            public string SourceName { get; set; }
            public ObjectId SourceId { get; set; }

            public override bool Equals(object obj) => Equals(obj as LegendItemInfo);
            public bool Equals(LegendItemInfo other) => other != null && IsHatch == other.IsHatch && SourceName == other.SourceName;

            public override int GetHashCode() => (IsHatch.ToString() + SourceName).GetHashCode();
        }

        public static void Add()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.Start, "HATCH"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            });
            PromptSelectionResult selResult = ed.GetSelection(filter);

            if (selResult.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var vegTypes = CollectLegendItems(selResult.Value, tr);
                if (vegTypes.Count == 0)
                {
                    Generic.WriteMessage("Aucun bloc ou hachure compatible sélectionné.");
                    tr.Commit();
                    return;
                }

                double ucsAngle = GetUcsAngle(ed);
                var legendByCategories = BuildLegendEntities(vegTypes, ucsAngle, tr, db);

                if (legendByCategories != null)
                {
                    PlaceLegendInDrawing(legendByCategories);
                }

                tr.Commit();
            }
        }


        private static Dictionary<string, HashSet<LegendItemInfo>> CollectLegendItems(SelectionSet selectionSet, Transaction tr)
        {
            var vegTypes = new Dictionary<string, HashSet<LegendItemInfo>>();

            foreach (SelectedObject selObj in selectionSet)
            {
                if (selObj?.ObjectId == null)
                {
                    continue;
                }

                if (!(tr.GetObject(selObj.ObjectId, OpenMode.ForRead) is Entity ent))
                {
                    continue;
                }

                if (TryExtractItemInfo(ent, selObj.ObjectId, out string categoryType, out LegendItemInfo itemInfo))
                {
                    if (!vegTypes.TryGetValue(categoryType, out var items))
                    {
                        items = new HashSet<LegendItemInfo>();
                        vegTypes[categoryType] = items;
                    }
                    items.Add(itemInfo);
                }
            }

            return vegTypes;
        }

        private static bool TryExtractItemInfo(Entity ent, ObjectId objId, out string categoryType, out LegendItemInfo itemInfo)
        {
            categoryType = null;
            itemInfo = null;

            if (ent is BlockReference blkRef && !blkRef.IsXref())
            {
                var infos = VEGBLOC.GetDataStore(blkRef);
                if (infos != null)
                {
                    categoryType = VEGBLOC.GetVegblocType(infos[VEGBLOC.DataStore.Type]).UcFirst();
                    itemInfo = new LegendItemInfo
                    {
                        IsHatch = false,
                        SourceName = blkRef.GetBlockReferenceName(),
                        SourceId = objId
                    };
                    return true;
                }
            }
            else if (ent is Hatch hatch && !string.IsNullOrEmpty(hatch.Layer) && hatch.Layer.StartsWith(Settings.PrefixVegblocLayer, StringComparison.OrdinalIgnoreCase))
            {
                var infos = VEGBLOC.GetDataStoreFromLayerName(hatch.Layer);
                if (infos != null)
                {
                    categoryType = VEGBLOC.GetVegblocType(infos[VEGBLOC.DataStore.Type]).UcFirst();
                    itemInfo = new LegendItemInfo
                    {
                        IsHatch = true,
                        SourceName = hatch.Layer,
                        SourceId = objId
                    };
                    return true;
                }
            }

            return false;
        }

        private static double GetUcsAngle(Editor ed)
        {
            Matrix3d ucsMatrix = ed.CurrentUserCoordinateSystem;
            return Math.Atan2(ucsMatrix.CoordinateSystem3d.Xaxis.Y, ucsMatrix.CoordinateSystem3d.Xaxis.X);
        }

        private static Dictionary<string, DBObjectCollection> BuildLegendEntities(Dictionary<string, HashSet<LegendItemInfo>> vegTypes, double ucsAngle, Transaction tr, Database db)
        {
            double yPosition = Point3d.Origin.Y + TitleSpacing;
            double xPosition = Point3d.Origin.X + (BlockSizeInMeters / 2.0);
            Matrix3d ucsRotationMatrix = Matrix3d.Rotation(ucsAngle, Vector3d.ZAxis, Point3d.Origin);

            string cartoucheLayerName = SymbolUtilityServices.RepairSymbolName($"{Settings.PrefixCAD}CARTOUCHE", false);
            Layers.CreateLayer(cartoucheLayerName, Color.FromColorIndex(ColorMethod.ByAci, 7), LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(0), true);

            var legendByCategories = new Dictionary<string, DBObjectCollection>();

            var orderedCategories = vegTypes.OrderBy(c => Enum.TryParse(VEGBLOC.GetVegblocType(c.Key), true, out VEGBLOC.VegblocTypes vegEnum) ? (int)vegEnum : int.MaxValue).ThenBy(c => c.Key);

            foreach (var categoryPair in orderedCategories)
            {
                var legendPart = new DBObjectCollection();
                yPosition -= TitleSpacing;
                string categoryName = categoryPair.Key;

                MText headerMText = CreateCategoryHeader(categoryName, new Point3d(xPosition - (BlockSizeInMeters / 2.0), yPosition, 0), cartoucheLayerName, ucsAngle);
                if (headerMText == null)
                {
                    return null;
                }

                legendPart.Add(headerMText);
                yPosition -= RowSpacing;

                foreach (var item in categoryPair.Value.OrderBy(n => n.SourceName))
                {
                    Point3d itemCenter = new Point3d(xPosition, yPosition, Point3d.Origin.Z);
                    string completeName;
                    string layerName;

                    if (item.IsHatch)
                    {
                        BuildHatchLegendRow(item, itemCenter, legendPart, tr, out completeName, out layerName);
                    }
                    else
                    {
                        BuildBlockLegendRow(item, itemCenter, legendPart, tr, db, out completeName, out layerName);
                    }

                    legendPart.Add(CreateItemLabel(completeName, new Point3d(xPosition + RowSpacing, yPosition, 0), layerName, ucsAngle));
                    yPosition -= RowSpacing;
                }

                foreach (Entity ent in legendPart)
                {
                    ent.TransformBy(ucsRotationMatrix);
                }

                legendByCategories.TryAdd(categoryName, legendPart);
            }

            return legendByCategories;
        }

        private static MText CreateCategoryHeader(string categoryName, Point3d location, string layerName, double ucsAngle)
        {
            var mText = new MText
            {
                Contents = categoryName.Replace('_', ' '),
                Location = location,
                Height = 0.35,
                TextHeight = 0.35,
                Attachment = AttachmentPoint.MiddleLeft,
                Normal = Vector3d.ZAxis,
                Layer = layerName,
                Rotation = -ucsAngle,
                ColorIndex = 256
            };

            TextEditor te = TextEditor.CreateTextEditor(mText);
            if (te == null)
            {
                return null;
            }

            te.SelectAll();
            te.Selection.Bold = true;
            te.Close(TextEditor.ExitStatus.ExitSave);
            return mText;
        }

        private static MText CreateItemLabel(string text, Point3d location, string layerName, double ucsAngle)
        {
            return new MText
            {
                Contents = text,
                Location = location,
                Height = 0.25,
                TextHeight = 0.25,
                Attachment = AttachmentPoint.MiddleLeft,
                Normal = Vector3d.ZAxis,
                Layer = layerName,
                Rotation = -ucsAngle,
                Transparency = Generic.GetTransparencyFromAlpha(0),
                ColorIndex = 256
            };
        }

        private static void BuildBlockLegendRow(LegendItemInfo item, Point3d center, DBObjectCollection legendPart, Transaction tr, Database db, out string completeName, out string layerName)
        {
            string blockName = item.SourceName;
            layerName = blockName;

            var blockRef = BlockReferences.GetBlockReference(blockName, center);
            var infos = VEGBLOC.GetDataStore(blockRef);
            completeName = infos != null ? infos[VEGBLOC.DataStore.CompleteName] : string.Empty;

            if (!Layers.CheckIfLayerExist(layerName))
            {
                Generic.WriteMessage($"Erreur détéctée sur le calque du bloc {blockName}, veuillez corriger le calque manuellement");
                ObjectIdCollection instances = BlockReferences.GetAllBlockReferenceInstances(blockName, tr, db);
                var mostUsedLayer = Layers.GetLayerCountsFromCollection(instances).FirstOrDefault();
                if (!string.IsNullOrEmpty(mostUsedLayer.Key))
                {
                    layerName = mostUsedLayer.Key;
                }
            }

            double blockWidth = blockRef.GetExtents().Size().Width;
            double scale = blockWidth > Generic.LowTolerance.EqualPoint ? BlockSizeInMeters / blockWidth : 1.0;
            blockRef.TransformBy(Matrix3d.Scaling(scale, blockRef.Position));
            blockRef.Layer = layerName;
            blockRef.ColorIndex = 256;
            legendPart.Add(blockRef);
        }

        private static void BuildHatchLegendRow(LegendItemInfo item, Point3d center, DBObjectCollection legendPart, Transaction tr, out string completeName, out string layerName)
        {
            layerName = item.SourceName;
            var infos = VEGBLOC.GetDataStoreFromLayerName(layerName);
            completeName = infos != null ? infos[VEGBLOC.DataStore.CompleteName] : string.Empty;

            double halfW = BlockSizeInMeters / 2.0;
            double halfH = HatchRectHeight / 2.0;

            Point2d[] rectPoints =
            {
                new Point2d(center.X - halfW, center.Y - halfH),
                new Point2d(center.X + halfW, center.Y - halfH),
                new Point2d(center.X + halfW, center.Y + halfH),
                new Point2d(center.X - halfW, center.Y + halfH),
                new Point2d(center.X - halfW, center.Y - halfH)
            };

            Polyline rect = new Polyline { Closed = true, Layer = layerName, ColorIndex = 251 };
            for (int i = 0; i < rectPoints.Length - 1; i++)
            {
                rect.AddVertexAt(i, rectPoints[i], 0, 0, 0);
            }
            legendPart.Add(rect);

            if (tr.GetObject(item.SourceId, OpenMode.ForRead) is Hatch sourceHatch)
            {
                Hatch legendHatch = new Hatch();
                legendHatch.SetDatabaseDefaults();
                legendHatch.AppendLoop(HatchLoopTypes.Default, new Point2dCollection(rectPoints), new DoubleCollection(new double[rectPoints.Length])                );
                legendHatch.EvaluateHatch(true);
                sourceHatch.CopyPropertiesTo(legendHatch);

                legendHatch.SetHatchPattern(sourceHatch.PatternType, sourceHatch.PatternName);
                legendHatch.PatternScale = sourceHatch.PatternScale;
                legendHatch.PatternAngle = sourceHatch.PatternAngle;
                legendHatch.HatchStyle = sourceHatch.HatchStyle;

                legendPart.Add(legendHatch);
            }
        }

        private static void PlaceLegendInDrawing(Dictionary<string, DBObjectCollection> legendByCategories)
        {
            DBObjectCollection entsForTransients = new DBObjectCollection();
            foreach (var legendPart in legendByCategories.Values)
            {
                entsForTransients.AddRange(legendPart);
            }

            using (var transientPoints = new VEGBLOC.GetVegBlocPointTransient(entsForTransients, null))
            {
                var insertionResult = transientPoints.GetPoint("\nIndiquez l'emplacement de la légende", Points.Null, false);

                if (insertionResult.Point == null || insertionResult.PromptPointResult.Status != PromptStatus.OK)
                {
                    return;
                }

                Matrix3d displacementMatrix = Point3d.Origin.GetDisplacementMatrixTo(insertionResult.Point.SCG);

                foreach (var legendPart in legendByCategories)
                {
                    ObjectIdCollection categoryObjIds = new ObjectIdCollection();
                    foreach (var dbObj in legendPart.Value)
                    {
                        if (dbObj is Entity ent)
                        {
                            ent.TransformBy(displacementMatrix);
                            categoryObjIds.Add(ent.AddToDrawingCurrentTransaction());
                        }
                    }
                    Groups.Create(Settings.PrefixInfoLayer + "Legende_" + legendPart.Key, "", categoryObjIds);
                }
            }
        }
    }
}