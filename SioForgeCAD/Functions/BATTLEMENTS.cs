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
    public static class BATTLEMENTS
    {
        public class ProportionalRandomSelector<T>
        {
            static readonly Random Random = new Random();
            private readonly List<(T Value, int Pourcentage)> percentageItemsDict;
            public string BaseSettings = string.Empty;


            public ProportionalRandomSelector() => percentageItemsDict = new List<(T, int)>() { };

            public void AddPercentageItem(T item, int percentage) => percentageItemsDict.Add((item, percentage));

            public (T Value, int Pourcentage) SelectItem()
            {
                // Calculate the summa of all portions.
                int poolSize = 0;
                foreach (var i in percentageItemsDict)
                {
                    poolSize += i.Pourcentage;
                }

                // Get a random integer from 1 to PoolSize.
                int randomNumber = Random.Next(1, poolSize);

                // Detect the item, which corresponds to current random number.
                int accumulatedProbability = 0;
                foreach (var pair in percentageItemsDict)
                {
                    accumulatedProbability += pair.Pourcentage;
                    if (randomNumber <= accumulatedProbability)
                        return pair;
                }
                return default;  // this code will never come while you use this programm right :)
            }
        }

        public static void Draw()
        {
            Database db = Generic.GetDatabase();


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!getDrawingVector(out Line BaseLine, out Line DirectionLine))
                {
                    return;
                }

                if (!GetBattlementsParameters(out ProportionalRandomSelector<double> randomSelector, out double Largeur))
                {
                    return;
                }

                var TriangleVectors = GetTriangleVectorsBasedFromVectors(BaseLine, DirectionLine);


                while (true)
                {
                    Point3d StartPoint = BaseLine.StartPoint.TransformBy(Matrix3d.Displacement(BaseLine.GetVector3d().Inverse().SetLength(Largeur)));

                    Point3d LastDiagonalPoint = StartPoint;
                    Point3d LastDrawingPoint = StartPoint;
                    double BaseLineHeight = (TriangleVectors.ABVector).FindProjectedIntersection(BaseLine.EndPoint, TriangleVectors.BCVector, StartPoint).DistanceTo(BaseLine.EndPoint);


                    int NumberOfGrid = (int)Math.Ceiling(BaseLineHeight / Largeur);

                    using (Polyline polyline = new Polyline())
                    {
                        for (int i = 1; i < NumberOfGrid; i++)
                        {
                            Point3d DrawingPoint = GetPointOnGridNearestFromBaseLine(TriangleVectors.ACVector, TriangleVectors.ABVector, TriangleVectors.BCVector, Largeur, ref LastDiagonalPoint, ref LastDrawingPoint);

                            double BattlementLength = randomSelector.SelectItem().Value;
                            Point3d BattlementPoint = DrawingPoint.TransformBy(Matrix3d.Displacement(DirectionLine.GetVector3d().SetLength(BattlementLength))); // use DirectionLine.GetVector3d() instead of BCVector to keep the direction of the initial vector (no rotate)
                            Point3d BattlementWidthPoint = BattlementPoint.TransformBy(Matrix3d.Displacement(TriangleVectors.ABVector.SetLength(Largeur)));

                            polyline.AddVertexIfNotExist(BattlementPoint);
                            polyline.AddVertexIfNotExist(BattlementWidthPoint);
                        }

                        polyline.Cleanup();

                        using (GetPointTransient ValidateDrawingTransient = new GetPointTransient(new DBObjectCollection(), null)
                        {
                            SetStaticEntities = new DBObjectCollection() { polyline.Clone() as DBObject }
                        })
                        {
                            var InsertionTransientPointsValues = ValidateDrawingTransient.GetPoint("Cliquez pour valider", Points.Null, "Randomiser", "Paramètres");

                            var Status = InsertionTransientPointsValues.PromptPointResult.Status;
                            if (Status == PromptStatus.OK)
                            {
                                //Validate current polyligne
                                polyline.AddToDrawingCurrentTransaction();
                                break;
                            }
                            if (Status == PromptStatus.Keyword)
                            {
                                if (InsertionTransientPointsValues.PromptPointResult.StringResult == "Paramètres")
                                {
                                    if (!GetBattlementsParameters(out randomSelector, out Largeur))
                                    {
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                //Insertion aborded
                                tr.Commit();
                                return;
                            }
                        }
                    }
                }

                tr.Commit();
            }
        }



        public static Point3d GetPointOnGridNearestFromBaseLine(Vector3d ACVector, Vector3d ABVector, Vector3d BCVector, double Largeur, ref Point3d LastDiagonalPoint, ref Point3d LastDrawingPoint)
        {
            // Calcul du point C
            Point3d pointA = LastDiagonalPoint;
            Point3d pointB = LastDiagonalPoint.TransformBy(Matrix3d.Displacement(ABVector.SetLength(Largeur)));
            Point3d pointC = ACVector.FindProjectedIntersection(pointA, BCVector, pointB);

            LastDiagonalPoint = pointC;
            Point3d DrawingPoint = LastDrawingPoint.TransformBy(Matrix3d.Displacement(ABVector.SetLength(Largeur)));

            //Lines.Draw(pointC, DrawingPoint, 50);
            //Lines.Draw(DrawingPoint, DrawingPoint.TransformBy(Matrix3d.Displacement(ABVector.SetLength(5))), 170);
            double GabBetweenBaseLineAndGrid = DrawingPoint.DistanceTo(pointC);

            if (GabBetweenBaseLineAndGrid > Largeur)
            {
                var GabNumberOfTimeWidth = Math.Floor(GabBetweenBaseLineAndGrid / Largeur);
                DrawingPoint = DrawingPoint.TransformBy(Matrix3d.Displacement(BCVector.SetLength(GabNumberOfTimeWidth * Largeur)));
            }
            LastDrawingPoint = DrawingPoint;
            return DrawingPoint;
        }

        public static (Vector3d ACVector, Vector3d ABVector, Vector3d BCVector) GetTriangleVectorsBasedFromVectors(Line BaseLine, Line DirectionLine)
        {
            /*
            This is a triangle rectangle at B = A^BC
            B_________C
            |        /
            |      /
            |    /
            |  /
            |/
            A

            */
            Vector3d ACVector = BaseLine.GetVector3d();
            Vector3d ABVector = DirectionLine.GetVector3d().GetPerpendicularVector();
            Vector3d BCVector = DirectionLine.GetVector3d();

            if (ABVector.GetAngleTo(ACVector) > ABVector.MultiplyBy(-1).GetAngleTo(ACVector))
            {
                ABVector = ABVector.MultiplyBy(-1);
            }

            //Check if BCVector AND ACVector is going to the same direction. ABVector is the axe vector
            bool IsBCVectorDirectionToRight = BCVector.IsVectorOnRightSide(ABVector);
            bool IsACVectorDirectionToRight = ACVector.IsVectorOnRightSide(ABVector);
            if (IsBCVectorDirectionToRight != IsACVectorDirectionToRight)
            {
                BCVector = BCVector.MultiplyBy(-1);
            }
            //TriangleVectors.ACVector.DrawVector(BaseLine.EndPoint, 1);//rouge
            //TriangleVectors.BCVector.DrawVector(BaseLine.EndPoint, 2);//jaune
            //TriangleVectors.ABVector.DrawVector(BaseLine.EndPoint, 3);//vert
            return (ACVector, ABVector, BCVector);
        }

        private static bool GetBattlementsParameters(out ProportionalRandomSelector<double> randomSelector, out double Largeur, string BaseSettings = "")
        {
            Editor ed = Generic.GetEditor();
            //Battlement length
            randomSelector = new ProportionalRandomSelector<double>();
            Largeur = 0.25;

            var getStringOption = new PromptStringOptions("Entrez les valeurs : \nex : 25%0.25;75%2")
            {
                DefaultValue = "10%0;25%0.25;25%0.5;25%0.75;10%1",
                UseDefaultValue = true,
                AllowSpaces = false
            };
            var GetStringValue = ed.GetString(getStringOption);
            if (GetStringValue.Status != PromptStatus.OK)
            {
                return false;
            }
            string ResultValue = GetStringValue.StringResult;
            randomSelector.BaseSettings = ResultValue;
            var InputValuePourcentage = ResultValue.Split(';');
            foreach (var ValuePourcentage in InputValuePourcentage)
            {
                string[] SplittedValuePourcentage = ValuePourcentage.Split('%');
                string PourcentageStr = SplittedValuePourcentage.First();
                string ValueStr = SplittedValuePourcentage.Last();

                if (!double.TryParse(ValueStr, out var ValueDbl) || !double.TryParse(PourcentageStr, out var PourcentageDbl))
                {
                    continue;
                }
                randomSelector.AddPercentageItem(ValueDbl, (int)Math.Floor(PourcentageDbl));
            }

            //Battlement width
            var getDoubleOption = new PromptDoubleOptions("Entrez la largeur des creneaux")
            {
                DefaultValue = Largeur,
                UseDefaultValue = true,
                AllowArbitraryInput = true,
                AllowNegative = false,
                AllowNone = false,
            };
            var GetLargeurValue = ed.GetDouble(getDoubleOption);
            if (GetLargeurValue.Status != PromptStatus.OK)
            {
                return false;
            }
            Largeur = GetLargeurValue.Value;
            return true;
        }

        private static bool getDrawingVector(out Line BaseLine, out Line DirectionLine)
        {
            if (!Points.GetPoint(out Points StartPoint, "Selectionnez le point de départ") ||
                !Points.GetPoint(out Points EndPoint, "Selectionnez le point de fin", StartPoint) ||
                !Points.GetPoint(out Points Direction, "Definissez l'angle des créneaux", StartPoint))
            {
                BaseLine = new Line();
                DirectionLine = new Line();
                return false;
            }

            BaseLine = new Line(StartPoint.SCG, EndPoint.SCG);
            DirectionLine = new Line(StartPoint.SCG, Direction.SCG);
            return true;
        }
    }














}
