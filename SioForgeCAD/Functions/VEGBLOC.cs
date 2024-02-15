using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows;
using Color = Autodesk.AutoCAD.Colors.Color;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOC
    {
        public static void Create()
        {
            VegblocDialog vegblocDialog = new VegblocDialog();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModalDialog(null, vegblocDialog, true);
            var Values = vegblocDialog.GetDataGridValues();
            foreach (var Rows in Values)
            {
                string Name = Rows["NAME"];
                if (string.IsNullOrWhiteSpace(Name))
                {
                    continue;
                }
                NumberStyles NumberStyle = NumberStyles.Integer | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite;
                string StrHeight = Rows["HEIGHT"] ?? "0";
                StrHeight = StrHeight.Replace(",", ".");
                string StrWidth = Rows["WIDTH"] ?? "1";
                StrWidth = StrWidth.Replace(",", ".");

                string Type = Rows["TYPE"] ?? "ARBRES";
                const string ErrorParseDoubleMessage = "Génération du bloc \"{0}\" ignorée : Impossible de convertir \"{1}\" en nombre.";

                if (!double.TryParse(StrHeight, NumberStyle, CultureInfo.InvariantCulture, out double Height))
                {
                    Generic.WriteMessage(string.Format(ErrorParseDoubleMessage, Name, StrHeight));
                    continue;
                }
                if (!double.TryParse(StrWidth, NumberStyle, CultureInfo.InvariantCulture, out double Width))
                {
                    Generic.WriteMessage(string.Format(ErrorParseDoubleMessage, Name, StrWidth));
                    continue;
                }

                if (Height > 0)
                {
                    Layers.CreateLayer(Settings.VegblocLayerHeightName, Color.FromRgb(0, 0, 0), LineWeight.LineWeight120, Generic.GetTransparencyFromAlpha(0), false);
                }

                string ShortType = Type.Trim().Substring(0, Math.Min(Type.Length, 4)).ToUpperInvariant();
                GetBlocDisplayName(Name, out string ShortName, out string CompleteName);
                string BlocName = $"{Settings.VegblocLayerPrefix}{ShortType}_{CompleteName}";


                Color BlocColor = GetRandomColor();
                int Transparence = 20;
                if (ShortType == "ARBR")
                {
                    Transparence = 90;
                }
                Layers.CreateLayer(BlocName, BlocColor, LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(Transparence), true);
                Color HeightColorIndicator = GetColorFromHeight(Height);
                var BlocEntities = GetBlocGeometry(ShortName, ShortType, Width, Height, BlocColor, HeightColorIndicator);
                if (!BlockReferences.IsBlockExist(BlocName))
                {
                    BlockReferences.Create(BlocName, $"{CompleteName}\nLargeur : {Width}\nHauteur : {Height}", BlocEntities, Points.Empty);
                }
                AskInsertVegBloc(BlocName);
            }

        }

        public static bool AskInsertVegBloc(string BlocName, string Layer = null, Points Origin = Points.Null)
        {

            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            DBObjectCollection ents = BlockReferences.InitForTransient(BlocName, null, Layer ?? BlocName);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                GetVegBlocPointTransient insertionTransientPoints = new GetVegBlocPointTransient(ents, null);
                var InsertionTransientPointsValues = insertionTransientPoints.GetPoint($"\nIndiquez l'emplacements du bloc", Origin);
                Points NewPointLocation = InsertionTransientPointsValues.Point;
                PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return false;
                }
                BlockReferences.InsertFromNameImportIfNotExist(BlocName, NewPointLocation, Generic.GetUSCRotation(Generic.AngleUnit.Radians), null, Layer ?? BlocName);
                tr.Commit();
            }
            return true;
        }


        private static Color GetColorFromHeight(double HeightInMeters)
        {
            Bitmap RainBowRamp = Properties.Resources.RainBowRamp;
            int HeightInPixel = (int)Math.Floor(HeightInMeters * 100);
            System.Drawing.Color couleurPixel = RainBowRamp.GetPixel(Math.Min(HeightInPixel + 1, RainBowRamp.Width - 1), 0);

            byte rouge = couleurPixel.R;
            byte vert = couleurPixel.G;
            byte bleu = couleurPixel.B;

            Debug.WriteLine($"Pixel à la position ({HeightInPixel}) - R:{rouge}, G:{vert}, B:{bleu}");
            return Color.FromRgb(rouge, vert, bleu);
        }

        private static DBObjectCollection GetBlocGeometry(string DisplayName, string ShortType, double WidthDiameter, double Height, Color BlocColor, Color HeightColorIndicator)
        {
            DBObjectCollection BlocGeometry = new DBObjectCollection();
            double WidthRadius = WidthDiameter / 2;
            var FirstCircle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, WidthRadius);
            ObjectId FirstCircleId = FirstCircle.AddToDrawing();
            Hatch acHatch = new Hatch();
            acHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            acHatch.Associative = false;
            acHatch.Layer = "0";
            acHatch.ColorIndex = 0;
            acHatch.Transparency = new Transparency(TransparencyMethod.ByLayer);
            acHatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { FirstCircleId });
            acHatch.EvaluateHatch(true);
            FirstCircleId.EraseObject();
            BlocGeometry.Add(acHatch);

            GetCircle(0, 0);

            if (Settings.VegblocGeneratePeripheryCircles)
            {
                GetCircle(0.084, -0.05);
                GetCircle(0.015, -0.1);
                GetCircle(-0.1, 0.065);
                GetCircle(-0.09, -0.06);
                GetCircle(0.045, 0.05);
            }

            ObjectId GetCircle(double x, double y)
            {
                Point3d cerle_periph_position = new Point3d(0 + (x * WidthRadius), 0 + (y * WidthRadius), 0);
                var Circle = new Circle(cerle_periph_position, Vector3d.ZAxis, WidthRadius)
                {
                    Layer = "0",
                    LineWeight = LineWeight.ByLineWeightDefault,
                    ColorIndex = 7,
                    Transparency = new Transparency((byte)255)
                };
                BlocGeometry.Add(Circle);
                return Circle.ObjectId;
            }

            var TextBlocDisplayName = new MText
            {
                Contents = DisplayName,
                Layer = "0",
                Location = new Point3d(0, 0, 0),
                Attachment = AttachmentPoint.MiddleCenter,
                Width = WidthRadius,
                TextHeight = WidthRadius / 5,
                Transparency = new Transparency(255),
                Color = GetTextColorFromBackgroundColor(BlocColor, ShortType)
            };
            BlocGeometry.Add(TextBlocDisplayName);


            if (Height > 0)
            {
                var CircleHeightColorIndicator = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, WidthRadius)
                {
                    Layer = Settings.VegblocLayerHeightName,
                    Color = HeightColorIndicator,
                    LineWeight = LineWeight.ByLayer,
                    Transparency = new Transparency(255)
                };
                BlocGeometry.Add(CircleHeightColorIndicator);

                MText TextHeightColorIndicator = new MText
                {
                    Contents = Height.ToString(),
                    Layer = Settings.VegblocLayerHeightName,

                    Location = new Point3d(0, 0 - WidthRadius * 0.6, 0),
                    Attachment = AttachmentPoint.MiddleCenter,
                    Width = WidthRadius,
                    TextHeight = WidthRadius / 10,
                    Transparency = new Transparency(255),
                    Color = HeightColorIndicator
                };
                BlocGeometry.Add(TextHeightColorIndicator);
            }
            return BlocGeometry;
        }

        private static Color GetTextColorFromBackgroundColor(Color BlocColor, string ShortType)
        {
            if (ShortType == "ARBR")
            {
                return Color.FromRgb(0, 0, 0);
            }
            double IsContrasted = (299 * BlocColor.Red + 587 * BlocColor.Green + 114 * BlocColor.Blue) / 1000;
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
            OriginalName = OriginalName.Replace(',', '.');
            OriginalName = OriginalName.Replace("' ", "'");
            OriginalName = OriginalName.Replace("' ", "'");

            //"ssp." == zoologie | "subsp." == botanique
            OriginalName = OriginalName.Replace(" ssp", " subsp"); //(pour sub-species) pour une sous-espèce, ou subsp.
            OriginalName = OriginalName.Replace(" spp", " subsp"); //species pluralis ou plurimae pour désigner plusieurs espèces ou l'ensemble des espèces d'un genre, 
            OriginalName = OriginalName.Replace(" sp", " subsp"); //species venant à la suite du nom du genre, pour une espèce indéterminée ou non décrite, 
            OriginalName = OriginalName.Replace(" sspp", " subspp"); //(pour sub-species pluralis) pour plusieurs ou l'ensemble des sous-espèces d'une espèce, ou subspp
            OriginalName = OriginalName.Replace(" subsp ", " subsp. ");

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
                if (OriginalName.Contains("'"))
                {
                    ShortName += " " + Espece[0];
                }
                else
                {
                    ShortName += " " + Espece;
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
                    ShortName += " " + Cultivar[0];
                }

                CompleteName += " " + Cultivar;
                index++;
            }

            if (!ShortName.EndsWith("'"))
            {
                if (ShortName.Contains("\'"))
                {
                    ShortName += "'";
                    CompleteName += "'";
                }
            }

            return;
        }

        private static Color GetRandomColor()
        {
            Random r = new Random();
            byte Red = (byte)r.Next(20, 220);
            byte Green = (byte)r.Next(50, 198);
            byte Blue = (byte)r.Next(20, 220);
            return Color.FromRgb(Red, Green, Blue);
        }
    }

    public class GetVegBlocPointTransient : GetPointTransient
    {
        public GetVegBlocPointTransient(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
        {
        }

        public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
        {
            return Drawable.Color;
        }
    }

}
