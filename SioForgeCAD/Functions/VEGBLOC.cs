using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using SioForgeCAD.Forms;
using SioForgeCAD.JSONParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using Color = Autodesk.AutoCAD.Colors.Color;

#pragma warning disable CS0618 

namespace SioForgeCAD.Functions
{
    public static class VEGBLOC
    {
        public enum DataStore
        {
            BlocName, CompleteName, Height, Width, Type, VegblocVersion
        }
        public static void Create()
        {
            VegblocDialog vegblocDialog = new VegblocDialog();
            var DialogResult = Application.ShowModalDialog(null, vegblocDialog, true);
            if (DialogResult != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            var Values = vegblocDialog.GetDataGridValues();

            string CurrentLayerName = Layers.GetCurrentLayerName();
            bool IsCurrentLayerLocked = Layers.IsLayerLocked(CurrentLayerName);
            if (IsCurrentLayerLocked)
            {
                Layers.SetLock(CurrentLayerName, false);
            }
            foreach (var Rows in Values)
            {
                string Name = Rows["NAME"];
                string Height = Rows["HEIGHT"] ?? "0";
                string Width = Rows["WIDTH"] ?? "1";
                string Type = Rows["TYPE"] ?? "ARBRES";
                string BlocName = CreateBlockFromData(Name, Height, Width, Type, out _, out _);
                if (string.IsNullOrEmpty(BlocName))
                {
                    continue;
                }
                AskInsertVegBloc(BlocName);
            }
            if (IsCurrentLayerLocked)
            {
                Layers.SetLock(CurrentLayerName, true);
            }
        }

        public static string CreateBlockFromData(string Name, string StrHeight, string StrWidth, string Type, out string BlockData, out bool WasSuccessfullyCreated)
        {
            WasSuccessfullyCreated = false;
            BlockData = string.Empty;
            if (string.IsNullOrWhiteSpace(Name))
            {
                return string.Empty;
            }
            StrHeight = StrHeight.Replace(",", ".");
            StrWidth = StrWidth.Replace(",", ".");

            const string ErrorParseDoubleMessage = "Génération du bloc \"{0}\" ignorée : Impossible de convertir \"{1}\" en nombre.";

            const NumberStyles NumberStyle = NumberStyles.Integer | NumberStyles.AllowDecimalPoint;
            if (!double.TryParse(StrHeight, NumberStyle, CultureInfo.InvariantCulture, out double Height))
            {
                Generic.WriteMessage(string.Format(ErrorParseDoubleMessage, Name, StrHeight));
                return string.Empty;
            }
            if (!double.TryParse(StrWidth, NumberStyle, CultureInfo.InvariantCulture, out double Width))
            {
                Generic.WriteMessage(string.Format(ErrorParseDoubleMessage, Name, StrWidth));
                return string.Empty;
            }

            if (Height > 0)
            {
                Layers.CreateLayer(Settings.VegblocLayerHeightName, Color.FromRgb(0, 0, 0), LineWeight.LineWeight120, Generic.GetTransparencyFromAlpha(0), false);
            }

            string ShortType = Type.Trim().Substring(0, Math.Min(Type.Length, 4)).ToUpperInvariant();
            GetBlocDisplayName(Name, out string ShortName, out string CompleteName);
            string MaybeIllegalBlocName = $"{Settings.VegblocLayerPrefix}{ShortType}_{CompleteName}";
            string BlocName = SymbolUtilityServices.RepairSymbolName(MaybeIllegalBlocName, false);

            Color BlocColor = GetBestUniqueRandomColorByDistance();
            int Transparence = 20;
            if (ShortType == "ARBR")
            {
                Transparence = 90;
            }
            Layers.CreateLayer(BlocName, BlocColor, LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(Transparence), true);

            if (!BlockReferences.IsBlockExist(BlocName))
            {
                Color HeightColorIndicator = GetColorFromHeight(Height);
                //If the layer arealdy exist; we need to get the real color
                BlocColor = Layers.GetLayerColor(BlocName);
                string Description = GenerateDataStore(BlocName, CompleteName, Height, Width, Type);
                var blkId = BlockReferences.Create(BlocName, Description, new DBObjectCollection(), Points.Empty, false, BlockScaling.Uniform);
                PopulateBlocGeometry(blkId, ShortName, ShortType, Width, Height, BlocColor, HeightColorIndicator);
                BlockData = Description;
                WasSuccessfullyCreated = true;
            }

            return BlocName;
        }

        public static string GenerateDataStore(string BlocName, string CompleteName, double Height, double Width, string Type)
        {
            Dictionary<DataStore, string> data = new Dictionary<DataStore, string>
            {
                { DataStore.BlocName, BlocName },
                { DataStore.CompleteName, CompleteName },
                { DataStore.Height, Height.ToString() },
                { DataStore.Width, Width.ToString() },
                { DataStore.Type, Type },
                { DataStore.VegblocVersion, "4" },
            };
            return data.ToJson();
        }

        public static Dictionary<DataStore, string> GetDataStore(BlockReference BlkRef)
        {
            string BlocDescription = BlkRef.GetDescription();
            if (string.IsNullOrWhiteSpace(BlocDescription))
            {
                return null;
            }
            try
            {
                Dictionary<DataStore, string> BlocDataFromJson = BlocDescription.FromJson<Dictionary<DataStore, string>>();
                if (BlocDataFromJson != null)
                {
                    return BlocDataFromJson;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
            return GetLEGACYDataStore(BlkRef);
            //return null;
        }

        [Obsolete("Old VEGBLOC versions, for compatibility only.", false)]
        private static Dictionary<DataStore, string> GetLEGACYDataStore(BlockReference BlkRef)
        {
            var OldVeg = BlkRef.GetDescription().Split('\n');
            if (OldVeg.Length == 3)
            {
                try
                {
                    return new Dictionary<DataStore, string>() {
                        {DataStore.BlocName, BlkRef.GetBlockReferenceName()},
                        {DataStore.CompleteName, OldVeg[0]},
                        {DataStore.Width,OldVeg[1].Split(':')[1]},
                        {DataStore.Height,OldVeg[2].Split(':')[1]},
                        {DataStore.Type, BlkRef.GetBlockReferenceName().Replace(Settings.VegblocLayerPrefix, "").Replace(OldVeg[0], "").Replace("_","") },
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            return null;
        }

        public static ObjectId AskInsertVegBloc(string BlocName, string Layer = null, Points Origin = Points.Null)
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            DBObjectCollection ents = BlockReferences.InitForTransient(BlocName, null, Layer ?? BlocName);
            ObjectId BlkObjectId = ObjectId.Null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                using (GetVegBlocPointTransient insertionTransientPoints = new GetVegBlocPointTransient(ents, null))
                {
                    var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("\nIndiquez l'emplacements du bloc", Origin);
                    Points NewPointLocation = InsertionTransientPointsValues.Point;
                    PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                    if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                    {
                        tr.Commit();
                        return BlkObjectId;
                    }
                    BlkObjectId = BlockReferences.InsertFromNameImportIfNotExist(BlocName, NewPointLocation, ed.GetUSCRotation(AngleUnit.Radians), null, Layer ?? BlocName);

                    tr.Commit();
                }
            }
            return BlkObjectId;
        }

        private static Color GetColorFromHeight(double HeightInMeters)
        {
            Bitmap ColorRamp = Properties.Resources.VEGBLOC_ColorRamp;
            int HeightInPixel = (int)Math.Floor(HeightInMeters * 100);
            System.Drawing.Color couleurPixel = ColorRamp.GetPixel(Math.Min(HeightInPixel + 1, ColorRamp.Width - 1), 0);

            byte rouge = couleurPixel.R;
            byte vert = couleurPixel.G;
            byte bleu = couleurPixel.B;

            Debug.WriteLine($"Pixel à la position ({HeightInPixel}) - R:{rouge}, G:{vert}, B:{bleu}");
            return Color.FromRgb(rouge, vert, bleu);
        }

        private static void PopulateBlocGeometry(ObjectId blockDefinitionId, string DisplayName, string ShortType, double WidthDiameter, double Height, Color BlocColor, Color HeightColorIndicator)
        {
            var db = Generic.GetDatabase();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = blockDefinitionId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                //TODO : Custom style with romans.shx
                double WidthRadius = WidthDiameter / 2;
                var FirstCircle = GetCircle(0, 0);
                ObjectId FirstCircleId = FirstCircle.ObjectId;
                Hatch acHatch = new Hatch();
                acHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                acHatch.Layer = "0";
                acHatch.PatternScale = 1;
                acHatch.ColorIndex = 0;
                acHatch.Transparency = new Transparency(TransparencyMethod.ByLayer);

                btr.AppendEntity(acHatch);
                tr.AddNewlyCreatedDBObject(acHatch, true);

                acHatch.Associative = true;
                acHatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { FirstCircleId });
                acHatch.EvaluateHatch(true);

                DrawOrderTable drawOrderTable = btr.DrawOrderTableId.GetObject(OpenMode.ForWrite) as DrawOrderTable;
                drawOrderTable.MoveToBottom(new ObjectIdCollection { acHatch.ObjectId });

                if (Settings.VegblocGeneratePeripheryCircles)
                {
                    GetCircle(0.084, -0.05);
                    GetCircle(0.015, -0.1);
                    GetCircle(-0.1, 0.065);
                    GetCircle(-0.09, -0.06);
                    GetCircle(0.045, 0.05);
                }

                Circle GetCircle(double offsetX, double offsetY)
                {
                    Point3d cerle_periph_position = new Point3d(0 + (offsetX * WidthRadius), 0 + (offsetY * WidthRadius), 0);
                    var Circle = new Circle(cerle_periph_position, Vector3d.ZAxis, WidthRadius)
                    {
                        Layer = "0",
                        LineWeight = LineWeight.ByLineWeightDefault,
                        ColorIndex = 7,
                        Transparency = new Transparency((byte)255)
                    };

                    btr.AppendEntity(Circle);
                    tr.AddNewlyCreatedDBObject(Circle, true);
                    return Circle;
                }
                var VegblocTextStyle = GetVegblocTextStyle();
                const double TextBlocDisplayNameSizeReduceRatios = 0.2;
                var TextBlocDisplayNameMaxWidth = WidthDiameter - WidthDiameter * 0.2;
                var TextBlocDisplayNameMaxHeight = WidthDiameter - WidthDiameter * 0.3;

                var TextBlocDisplayName = new MText
                {
                    Contents = DisplayName,
                    Layer = "0",
                    Location = new Point3d(0, 0, 0),
                    Attachment = AttachmentPoint.MiddleCenter,
                    TextHeight = WidthRadius * TextBlocDisplayNameSizeReduceRatios,
                    TextStyleId = VegblocTextStyle,
                    Transparency = new Transparency(255),
                    Color = GetTextColorFromBackgroundColor(BlocColor, ShortType),
                    Width = TextBlocDisplayNameMaxWidth
                };

                btr.AppendEntity(TextBlocDisplayName);
                tr.AddNewlyCreatedDBObject(TextBlocDisplayName, true);

                var TextBlocDisplayNameSize = TextBlocDisplayName.GetExplodedExtents().GetExtents().Size();
                if (TextBlocDisplayNameMaxWidth < TextBlocDisplayNameSize.Width)
                {
                    TextBlocDisplayName.TextHeight *= (TextBlocDisplayNameMaxWidth / TextBlocDisplayNameSize.Width);
                }

                TextBlocDisplayNameSize = TextBlocDisplayName.GetExplodedExtents().GetExtents().Size();
                if (TextBlocDisplayNameMaxHeight < TextBlocDisplayNameSize.Height)
                {
                    TextBlocDisplayName.TextHeight *= (TextBlocDisplayNameMaxHeight / TextBlocDisplayNameSize.Height);
                }

                if (Height > 0)
                {
                    var CircleHeightColorIndicator = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, WidthRadius)
                    {
                        Layer = Settings.VegblocLayerHeightName,
                        Color = HeightColorIndicator,
                        LineWeight = LineWeight.ByLayer,
                        Transparency = new Transparency(255)
                    };
                    btr.AppendEntity(CircleHeightColorIndicator);
                    tr.AddNewlyCreatedDBObject(CircleHeightColorIndicator, true);

                    const double TextHeightColorIndicatorSizeReduceRatio = 0.1;
                    MText TextHeightColorIndicator = new MText
                    {
                        Contents = Height.ToString(),
                        Layer = Settings.VegblocLayerHeightName,
                        Location = new Point3d(0, 0 - (WidthRadius * 0.7), 0),
                        Attachment = AttachmentPoint.MiddleCenter,
                        Width = WidthRadius,
                        TextStyleId = VegblocTextStyle,
                        TextHeight = WidthRadius * TextHeightColorIndicatorSizeReduceRatio,
                        Transparency = new Transparency(255),
                        Color = HeightColorIndicator
                    };

                    btr.AppendEntity(TextHeightColorIndicator);
                    tr.AddNewlyCreatedDBObject(TextHeightColorIndicator, true);
                }
                tr.Commit();
            }
        }

        private static ObjectId GetVegblocTextStyle()
        {
            var TextStyleName = Settings.VegblocLayerPrefix + "TextStyle";
            var db = Generic.GetDatabase();
            using (var tr = db.TransactionManager.StartTransaction())
            {
                ObjectId textStyleId = db.GetObjectIdFromAppDictionary(tr, Generic.GetExtensionDLLName(), TextStyleName);

                if (textStyleId.IsNull || textStyleId.IsErased)
                {
                    TextStyleTable textStyleTable = db.TextStyleTableId.GetDBObject() as TextStyleTable;


                    if (!textStyleTable.Has(TextStyleName))
                    {
                        textStyleTable.UpgradeOpen();
                        TextStyleTableRecord newStyle = new TextStyleTableRecord
                        {
                            Name = TextStyleName,
                            FileName = "romans.shx",
                            TextSize = 0,
                            XScale = 1,
                            ObliquingAngle = 0
                        };
                        textStyleId = textStyleTable.Add(newStyle);
                        tr.AddNewlyCreatedDBObject(newStyle, true);
                    }
                    else
                    {
                        textStyleId = textStyleTable[TextStyleName];
                    }
                    db.StoreObjectIdInAppDictionary(tr, Generic.GetExtensionDLLName(), TextStyleName, textStyleId);
                }
                tr.Commit();
                return textStyleId;
            }
        }

        private static Color GetTextColorFromBackgroundColor(Color BlocColor, string ShortType)
        {
            double IsContrasted = ((299 * BlocColor.Red) + (587 * BlocColor.Green) + (114 * BlocColor.Blue)) / 1000;

            if (IsContrasted > 160)
            {
                return Color.FromRgb(0, 0, 0);
            }
            else
            {
                return Color.FromRgb(255, 255, 255);
            }
        }

        private static void GetBlocDisplayName(string OriginalName, out string ShortName, out string CompleteName)
        {
            ShortName = string.Empty;
            CompleteName = string.Empty;
            if (string.IsNullOrEmpty(OriginalName))
            {
                return;
            }
            OriginalName = ParseNameValue(OriginalName);

            string[] SplittedName = OriginalName.Trim().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            int index = 0;
            //Genre
            if (SplittedName.Length > index)
            {
                var Genre = SplittedName[index].ToLowerInvariant().UcFirst();
                ShortName = Genre;
                CompleteName = Genre;
            }

            //Espece if specified : dont run if Paeonia 'Adzuma Nishiki' for exemple
            index++;
            if (SplittedName.Length > index && !SplittedName[index].StartsWith("'"))
            {
                string Espece = SplittedName[index];
                ShortName += " ";
                if (OriginalName.Contains("'"))
                {
                    ShortName += "";//Espece[0];
                }
                else
                {
                    ShortName += Espece;
                }
                CompleteName += " " + Espece;
                index++;
            }

            //Cultivar
            while (SplittedName.Length > index)
            {
                ShortName += " ";
                string Cultivar;
                if (!ShortName.Contains("\'"))
                {
                    //the name of the vegetal is longer than expected : Sedum telephium subsp. 'Maximum'
                    Cultivar = SplittedName[index].ToLowerInvariant();
                    if (Cultivar.Contains("'"))
                    {
                        Cultivar = "'" + Cultivar.Replace("'", "").UcFirst();
                    }
                    ShortName += Cultivar;
                }
                else
                {
                    Cultivar = SplittedName[index].UcFirst();
                    ShortName += " ";

                    if (Cultivar.Length <= 8 && SplittedName.Length == (index + 1))
                    {
                        //if it is the last one and the length is <= as 8 char then we show complete
                        ShortName += Cultivar;
                    }
                    else
                    {
                        ShortName += Cultivar[0];
                    }
                }

                CompleteName += " " + Cultivar;
                index++;
            }

            if (!ShortName.EndsWith("'"))
            {
                if (ShortName.Contains("\'"))
                {
                    ShortName += "'";
                }
            }
            if (!CompleteName.EndsWith("'"))
            {
                if (CompleteName.Contains("\'"))
                {
                    CompleteName += "'";
                }
            }

            return;
        }

        private static string ParseNameValue(object value)
        {
            string valueStr = value?.ToString();
            if (valueStr is null)
            {
                return null;
            }
            const string IllegalAppostropheChar = "'’ʾ′ˊˈꞌ‘ʿ‵ˋʼ\"“”«»„";
            valueStr = valueStr.Replace(IllegalAppostropheChar.ToCharArray(), '\'');

            valueStr = valueStr.RemoveDiacritics();
            valueStr = valueStr.Replace(":", string.Empty);
            valueStr = valueStr.Replace('\\', '+');

            valueStr = valueStr.Replace(',', '.');
            valueStr = valueStr.Replace("' ", "'");
            valueStr = valueStr.Replace("' ", "'");

            //"ssp." == zoologie | "subsp." == botanique
            valueStr = valueStr.Replace(" ssp", " subsp"); //(pour sub-species) pour une sous-espèce, ou subsp.
            valueStr = valueStr.Replace(" spp", " subsp"); //species pluralis ou plurimae pour désigner plusieurs espèces ou l'ensemble des espèces d'un genre, 
            valueStr = valueStr.Replace(" sp", " subsp"); //species venant à la suite du nom du genre, pour une espèce indéterminée ou non décrite, 
            valueStr = valueStr.Replace(" sspp", " subspp"); //(pour sub-species pluralis) pour plusieurs ou l'ensemble des sous-espèces d'une espèce, ou subspp
            valueStr = valueStr.Replace(" subsp ", " subsp. ");

            return valueStr;
        }

        private static Color GetBestUniqueRandomColorByDistance()
        {
            const int maxAttempts = 50;

            Random r = new Random();

            List<Color> existingColors = GetLayerColors();

            Color bestCandidate = Color.FromRgb(0, 0, 0);
            double bestMinDistance = -1;

            for (int i = 0; i < maxAttempts; i++)
            {
                double hue = r.NextDouble() * 360;
                double saturation = 0.2 + (r.NextDouble() * 0.8);
                double value = 0.1 + (r.NextDouble() * 0.9);

                Color candidate = Colors.FromHSV(hue, saturation, value);

                double minDistance = double.MaxValue;
                foreach (var color in existingColors)
                {
                    if (color.ColorMethod != ColorMethod.ByColor)
                        continue;

                    int dr = color.Red - candidate.Red;
                    int dg = color.Green - candidate.Green;
                    int db = color.Blue - candidate.Blue;
                    double distance = dr * dr + dg * dg + db * db; //avoid Math.Sqrt for performance

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }

                // On cherche à maximiser la distance minimale à toutes les couleurs existantes
                if (minDistance > bestMinDistance)
                {
                    bestMinDistance = minDistance;
                    bestCandidate = candidate;
                }
            }

            Debug.WriteLine($"Best color selected with minimal distance: {bestMinDistance:F2}");
            return bestCandidate;
        }

        private static List<Color> GetLayerColors()
        {
            List<Color> colors = new List<Color>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId layerId in layerTable)
                {
                    LayerTableRecord layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    if (layer.Name.StartsWith(Settings.VegblocLayerPrefix))
                    {
                        colors.Add(layer.Color);
                    }
                }
                tr.Commit();
            }

            return colors;
        }













        public class GetVegBlocPointTransient : GetPointTransient
        {
            public GetVegBlocPointTransient(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(PrepareEntsForTransient(Entities), UpdateFunction) { }

            private static DBObjectCollection PrepareEntsForTransient(DBObjectCollection BaseEnts)
            {
                DBObjectCollection TransientByLayerEnts = new DBObjectCollection();
                foreach (DBObject BaseEnt in BaseEnts)
                {
                    if (BaseEnt is Entity ent)
                    {
                        if (!(ent is BlockReference))
                        {
                            TransientByLayerEnts.Add(ent);
                            continue;
                        }
                        else
                        {
                            DBObjectCollection ExplodedEnts = new DBObjectCollection();
                            ent.Explode(ExplodedEnts);
                            foreach (var ExplodedEnt in ExplodedEnts)
                            {
                                if (ExplodedEnt is Entity ByBlocEnt)
                                {
                                    SetEntityToByLayerIfByBloc(ByBlocEnt, ent.Layer);
                                }
                                TransientByLayerEnts.Add(ExplodedEnt as DBObject);
                            }
                        }
                    }
                    else
                    {
                        TransientByLayerEnts.Add(BaseEnt);
                    }
                }
                return TransientByLayerEnts;
            }

            private static void SetEntityToByLayerIfByBloc(Entity entity, string LayerName)
            {
                if (entity.ColorIndex == 0) { entity.ColorIndex = 256; }
                if (entity.Layer == "0" && Layers.CheckIfLayerExist(LayerName))
                {
                    entity.Layer = LayerName;
                }
                if (entity.Transparency.IsByBlock) { entity.Transparency = new Autodesk.AutoCAD.Colors.Transparency(TransparencyMethod.ByLayer); }
                if (entity.Linetype == "BYBLOCK") { entity.Linetype = "BYLAYER"; }
                if (entity.LineWeight == LineWeight.ByBlock) { entity.LineWeight = LineWeight.ByLayer; }
            }

            public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
            {
                return Drawable.Color;
            }

            public override Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
            {
                if (Drawable is BlockReference blkRef)
                {
                    var BlockName = blkRef.GetBlockReferenceName();
                    if (Layers.CheckIfLayerExist(BlockName))
                    {
                        return Layers.GetTransparency(BlockName);
                    }
                }

                return base.GetTransGraphicsTransparency(Drawable, IsStaticDrawable);
            }
        }
    }
}
#pragma warning restore CS0618