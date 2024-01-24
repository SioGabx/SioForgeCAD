using SioForgeCAD.Forms;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using Autodesk.AutoCAD.Colors;
using Color = Autodesk.AutoCAD.Colors.Color;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOC
    {
        public static void Create()
        {
            Editor ed = Generic.GetEditor();
            VegblocDialog vegblocDialog = new VegblocDialog();
            vegblocDialog.ShowDialog();

            var Values = vegblocDialog.GetDataGridValues();
            foreach (var Rows in Values)
            {
                string Name = Rows["NAME"];
                if (string.IsNullOrWhiteSpace(Name))
                {
                    continue;
                }
                string StrHeight = Rows["HEIGHT"] ?? "0";
                string StrWidth = Rows["WIDTH"] ?? "0";
                string Type = Rows["TYPE"] ?? "ARBRES";
                if (!double.TryParse(StrHeight, out double Height) || !double.TryParse(StrWidth, out double Width))
                {
                    continue;
                }

                if (Height > 0)
                {
                    Layers.CreateLayer(Settings.VegblocLayerHeightName, Color.FromRgb(0, 0, 0), LineWeight.LineWeight120, Generic.GetTransparencyFromAlpha(0), false);
                }

                string ShortType = Type.Trim().Substring(0, Math.Min(Type.Length, 4)).ToUpperInvariant();
                string BlocName = $"_APUd_VEG_{ShortType}_{Name}";

                string BlocDisplayName = GetBlocDisplayName(Name);
                Color BlocColor = GetRandomColor();
                int Transparence = 20;
                if (ShortType == "ARBR")
                {
                    Transparence = 90;
                }
                Layers.CreateLayer(BlocName, BlocColor, LineWeight.ByLineWeightDefault, Generic.GetTransparencyFromAlpha(Transparence), true);
                Color HeightColorIndicator = GetColorFromHeight(Height);
                var BlocEntities = GetBlocGeometry(BlocDisplayName, ShortType, Width, Height, BlocColor, HeightColorIndicator);
                if (!BlockReferences.IsBlockExist(BlocName))
                {
                    BlockReferences.Create(BlocName, $"{Name}\nLargeur : {Width}\nHauteur : {Height}", BlocEntities, Points.Empty);
                }
                AskInsertVegBloc(BlocName);
            }

        }

        private static void AskInsertVegBloc(string BlocName)
        {

            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            DBObjectCollection ents = BlockReferences.InitForTransient(BlocName, null, BlocName);
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                GetVegBlocPointTransient insertionTransientPoints = new GetVegBlocPointTransient(ents, null);
                var InsertionTransientPointsValues = insertionTransientPoints.GetPoint($"\nIndiquez l'emplacements du bloc", Points.Null);
                Points NewPointLocation = InsertionTransientPointsValues.Point;
                PromptPointResult NewPointPromptPointResult = InsertionTransientPointsValues.PromptPointResult;

                if (NewPointLocation == null || NewPointPromptPointResult.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return;
                }
                BlockReferences.InsertFromNameImportIfNotExist(BlocName, NewPointLocation, Generic.GetUSCRotation(Generic.AngleUnit.Radians), null, BlocName);
                tr.Commit();
            }
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

        private static DBObjectCollection GetBlocGeometry(string DisplayNamName, string ShortType, double Width, double Height, Color BlocColor, Color HeightColorIndicator)
        {
            DBObjectCollection BlocGeometry = new DBObjectCollection();

            var FirstCircle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, Width);
            ObjectId FirstCircleId = FirstCircle.AddToDrawing();
            Hatch acHatch = new Hatch();
            acHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
            acHatch.Associative = false;
            acHatch.Layer = "0";
            acHatch.ColorIndex = 0;
            acHatch.Transparency = new Transparency(TransparencyMethod.ByBlock);
            acHatch.AppendLoop(HatchLoopTypes.Outermost, new ObjectIdCollection { FirstCircleId });
            acHatch.EvaluateHatch(true);
            FirstCircleId.EraseObject();
            BlocGeometry.Add(acHatch);

            Periph_Circle(0, 0);
            Periph_Circle(0.084, -0.05);
            Periph_Circle(0.015, -0.1);
            Periph_Circle(-0.1, 0.065);
            Periph_Circle(-0.09, -0.06);
            Periph_Circle(0.045, 0.05);

            ObjectId Periph_Circle(double x, double y)
            {
                Point3d cerle_periph_position = new Point3d(0 + (x * Width), 0 + (y * Width), 0);
                var Circle = new Circle(cerle_periph_position, Vector3d.ZAxis, Width);
                Circle.Layer = "0";
                Circle.LineWeight = LineWeight.LineWeight000;
                Circle.ColorIndex = 7;
                Circle.Transparency = new Transparency((byte)255);
                BlocGeometry.Add(Circle);
                return Circle.ObjectId;
            }

            var TextBlocDisplayName = new MText
            {
                Contents = DisplayNamName,
                Layer = "0",
                Location = new Point3d(0, 0, 0),
                Attachment = AttachmentPoint.MiddleCenter,
                Width = Width,
                TextHeight = Width / 5,
                Transparency = new Transparency(255),
                Color = GetTextColorFromBackgroundColor(BlocColor, ShortType)
            };
            BlocGeometry.Add(TextBlocDisplayName);


            if (Height > 0)
            {
                var CircleHeightColorIndicator = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, Width)
                {
                    Layer = Settings.VegblocLayerHeightName,
                    Color = HeightColorIndicator,
                    LineWeight = LineWeight.ByLayer,
                    Transparency = new Transparency((byte)255)
                };
                BlocGeometry.Add(CircleHeightColorIndicator);

                MText TextHeightColorIndicator = new MText
                {
                    Contents = Height.ToString(),
                    Layer = Settings.VegblocLayerHeightName,

                    Location = new Point3d(0, 0 - Width * 0.7, 0),
                    Attachment = AttachmentPoint.MiddleCenter,
                    Width = Width,
                    TextHeight = Width / 10,
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



        private static string GetBlocDisplayName(string Name)
        {
            string BlocName = string.Empty;
            if (string.IsNullOrEmpty(Name))
            {
                return string.Empty;
            }
            string[] SplittedName = Name.Trim().Split(' ');
            if (SplittedName.Length > 0)
            {
                BlocName = SplittedName[0];
            }
            if (SplittedName.Length > 1)
            {
                BlocName += " " + SplittedName[1][0];
            }
            if (SplittedName.Length > 2)
            {
                BlocName += " " + SplittedName[2][0];
            }
            if (SplittedName.Length > 3)
            {
                var SplittedThirdPart = SplittedName[3][0];
                if (SplittedThirdPart != '\'')
                {
                    BlocName += " " + SplittedThirdPart;
                }
            }

            if (BlocName.Substring(BlocName.Length - 1) != "'")
            {
                if (BlocName.Contains("'"))
                {
                    BlocName += "'";
                }
            }

            return BlocName;
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
