using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class BATTLEMENTS
    {
        public class ProportionalRandomSelector<T>
        {
            static readonly Random Random = new Random();
            private readonly List<(T Value, int Pourcentage)> percentageItemsDict;

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
        //public static void Draw()
        //{
        //    Editor ed = Generic.GetEditor();
        //    Database db = Generic.GetDatabase();
        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {
        //        var AskBaseLine = ed.GetEntity("Selectionnez une ligne de base");
        //    if (AskBaseLine.Status != PromptStatus.OK)
        //    {
        //        return;
        //    }
        //    if (!(AskBaseLine.ObjectId.GetEntity() is Line BaseLine))
        //    {
        //        return;
        //    }
        //    var AskDirectionLine = ed.GetEntity("Selectionnez une ligne de direction des creneaux");
        //    if (AskDirectionLine.Status != PromptStatus.OK)
        //    {
        //        return;
        //    }
        //    if (!(AskDirectionLine.ObjectId.GetEntity() is Line DirectionLine))
        //    {
        //        return;
        //    }
        //    if (!GetBattlementsParameters(out ProportionalRandomSelector<double> randomSelector, out double Largeur))
        //    {
        //        return;
        //    }

        //        Vector3d BaseLineVector = BaseLine.GetVector3d();
        //        Vector3d DirectionLineVector = DirectionLine.GetVector3d();
        //        // Point3d diagonalPoint = startPoint + verticalDistance * verticalVector + verticalDistance * diagonalVector;
        //        Point3d LastDiagonalPoint = BaseLine.StartPoint;
        //        for (int i = 0; i < 50; i++)
        //        {

        //            Point3d diagonalPoint = LastDiagonalPoint + Largeur * DirectionLineVector.GetPerpendicularVector() + Largeur * BaseLineVector;
        //            using (DBPoint Point = new DBPoint(diagonalPoint))
        //            {
        //                Point.AddToDrawing();
        //            }
        //            LastDiagonalPoint = diagonalPoint;
        //        }
        //        tr.Commit();
        //    }
        //}

        public static void Draw()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (Polyline polyline = new Polyline())
            {
                var AskBaseLine = ed.GetEntity("Selectionnez une ligne de base");
                if (AskBaseLine.Status != PromptStatus.OK)
                {
                    return;
                }
                if (!(AskBaseLine.ObjectId.GetEntity() is Line BaseLine))
                {
                    return;
                }
                var AskDirectionLine = ed.GetEntity("Selectionnez une ligne de direction des creneaux");
                if (AskDirectionLine.Status != PromptStatus.OK)
                {
                    return;
                }
                if (!(AskDirectionLine.ObjectId.GetEntity() is Line DirectionLine))
                {
                    return;
                }
                if (!GetBattlementsParameters(out ProportionalRandomSelector<double> randomSelector, out double Largeur))
                {
                    return;
                }


                var TriangleVectors = GetTriangleVectorsBasedFromVectors(BaseLine, DirectionLine);

                Point3d LastDiagonalPoint = BaseLine.StartPoint;
                Point3d LastDrawingPoint = BaseLine.StartPoint;
                double BaseLineHeight = (TriangleVectors.ABVector * -1).FindProjectedIntersection(BaseLine.EndPoint, TriangleVectors.BCVector, BaseLine.StartPoint).DistanceTo(BaseLine.EndPoint);
                int NumberOfGrid = (int)Math.Ceiling(BaseLineHeight / Largeur);

                for (int i = 0; i < NumberOfGrid; i++)
                {
                    Point3d DrawingPoint = GetPointOnGridNearestFromBaseLine(TriangleVectors.ACVector, TriangleVectors.ABVector, TriangleVectors.BCVector, Largeur, ref LastDiagonalPoint, ref LastDrawingPoint);

                    double BattlementLength = randomSelector.SelectItem().Value;
                    Point3d BattlementPoint = DrawingPoint.TransformBy(Matrix3d.Displacement(DirectionLine.GetVector3d().SetLength(BattlementLength))); // use DirectionLine.GetVector3d() instead of BCVector to keep the direction of the initial vector (no rotate)
                    Point3d BattlementWidthPoint = BattlementPoint.TransformBy(Matrix3d.Displacement(TriangleVectors.ABVector.SetLength(Largeur)));
                    //Lines.Draw(DrawingPoint, BattlementPoint);
                    //Lines.Draw(BattlementPoint, BattlementWidthPoint);
                    polyline.AddVertexIfNotExist(BattlementPoint);
                    polyline.AddVertexIfNotExist(BattlementWidthPoint);
                }
                polyline.Cleanup();
                polyline.AddToDrawingCurrentTransaction();
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
            using (Polyline Triangle = new Polyline())
            {
                Triangle.AddVertexAt(0, pointA.ToPoint2d(), 0, 0, 0);
                Triangle.AddVertexAt(1, pointB.ToPoint2d(), 0, 0, 0);
                Triangle.AddVertexAt(2, pointC.ToPoint2d(), 0, 0, 0);
                Triangle.Color = Color.FromColorIndex(ColorMethod.ByColor, 100);
                Triangle.Transparency = Generic.GetTransparencyFromAlpha(50);
                Triangle.AddToDrawing();
            }

            Point3d DrawingPoint = LastDrawingPoint.TransformBy(Matrix3d.Displacement(ABVector.SetLength(Largeur)));

            Lines.Draw(pointC, DrawingPoint, 50);
            Lines.Draw(DrawingPoint, DrawingPoint.TransformBy(Matrix3d.Displacement(ABVector.SetLength(5))), 170);
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
            double BaseLineDirectionAngle = ACVector.GetRotationRelativeToSCG();
            double DirectionLineDirectionAngle = DirectionLine.GetVector3d().GetRotationRelativeToSCG();
            Debug.WriteLine("DirectionLineDirectionAngle : " + DirectionLineDirectionAngle);
            Debug.WriteLine("BaseLineDirectionAngle : " + BaseLineDirectionAngle);
            double InvertABVector = 1;
            double InvertBCVector = 1;
            if (BaseLineDirectionAngle > 0 && BaseLineDirectionAngle < 90) { InvertABVector = 1; InvertBCVector = 1; }
            if (BaseLineDirectionAngle > 90 && BaseLineDirectionAngle < 180) { InvertABVector = -1; InvertBCVector = 1; }
            if (BaseLineDirectionAngle > 180 && BaseLineDirectionAngle < 270) { InvertABVector = -1; InvertBCVector = -1; }
            if (BaseLineDirectionAngle > 270 && BaseLineDirectionAngle < 360) { InvertABVector = 1; InvertBCVector = -1; }

            Vector3d ABVector = DirectionLine.GetVector3d().GetPerpendicularVector().MultiplyBy(InvertABVector);
            Vector3d BCVector = DirectionLine.GetVector3d().MultiplyBy(InvertBCVector);

            if (DirectionLineDirectionAngle > 180)
            {
                ACVector = ACVector.MultiplyBy(-1);
                ABVector = ABVector.MultiplyBy(-1);
                BCVector = BCVector.MultiplyBy(-1);
            }

            return (ACVector, ABVector, BCVector);
        } 
        
        
        //public static (Vector3d ACVector, Vector3d ABVector, Vector3d BCVector) GetTriangleVectorsBasedFromVectors(Line BaseLine, Line DirectionLine)
        //{
        //    /*
        //    This is a triangle rectangle at B = A^BC
        //    B_________C
        //    |        /
        //    |      /
        //    |    /
        //    |  /
        //    |/
        //    A

        //    */
        //    Vector3d ACVector = BaseLine.GetVector3d();
        //    double BaseLineDirectionAngle = ACVector.GetRotationRelativeToSCG();
        //    double DirectionLineDirectionAngle = DirectionLine.GetVector3d().GetRotationRelativeToSCG();
        //    Debug.WriteLine("DirectionLineDirectionAngle : " + DirectionLineDirectionAngle);
        //    Debug.WriteLine("BaseLineDirectionAngle : " + BaseLineDirectionAngle);
        //    double InvertABVector = 1;
        //    double InvertBCVector = 1;
        //    if (BaseLineDirectionAngle > 0 && BaseLineDirectionAngle < 90) { InvertABVector = 1; InvertBCVector = 1; }
        //    if (BaseLineDirectionAngle > 90 && BaseLineDirectionAngle < 180) { InvertABVector = -1; InvertBCVector = 1; }
        //    if (BaseLineDirectionAngle > 180 && BaseLineDirectionAngle < 270) { InvertABVector = -1; InvertBCVector = -1; }
        //    if (BaseLineDirectionAngle > 270 && BaseLineDirectionAngle < 360) { InvertABVector = 1; InvertBCVector = -1; }

        //    Vector3d ABVector = DirectionLine.GetVector3d().GetPerpendicularVector().MultiplyBy(InvertABVector);
        //    Vector3d BCVector = DirectionLine.GetVector3d().MultiplyBy(InvertBCVector);

        //    if (DirectionLineDirectionAngle > 180)
        //    {
        //        ACVector = ACVector.MultiplyBy(-1);
        //        ABVector = ABVector.MultiplyBy(-1);
        //        BCVector = BCVector.MultiplyBy(-1);
        //    }

        //    return (ACVector, ABVector, BCVector);
        //}

        private static bool GetBattlementsParameters(out ProportionalRandomSelector<double> randomSelector, out double Largeur)
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


        public static void DrawV1()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            var getStringOption = new PromptStringOptions("Entrez les valeurs : \nex : 25%0.25;75%2")
            {
                DefaultValue = "10%0;25%0.25;25%0.5;25%0.75;10%1",
                UseDefaultValue = true,
                AllowSpaces = false
            };
            var GetStringValue = ed.GetString(getStringOption);
            if (GetStringValue.Status == PromptStatus.OK)
            {
                ProportionalRandomSelector<double> randomSelector = new ProportionalRandomSelector<double>();
                string ResultValue = GetStringValue.StringResult;
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

                var getDoubleOption = new PromptDoubleOptions("Entrez la largeur des creneaux")
                {
                    DefaultValue = 0.25,
                    UseDefaultValue = true,
                    AllowArbitraryInput = true,
                    AllowNegative = false,
                    AllowNone = false,
                };
                var GetLargeurValue = ed.GetDouble(getDoubleOption);
                if (GetLargeurValue.Status == PromptStatus.OK)
                {
                    double LargeurCreneaux = GetLargeurValue.Value;
                    Matrix3d WidthMatrix = Matrix3d.Displacement(new Point3d(LargeurCreneaux, 0, 0).GetAsVector());
                    double Longeur = 50;
                    Point3d LastPoint = new Point3d(0, 0, 0);
                    double LastHeight = 0;

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    using (Polyline polyline = new Polyline())
                    {
                        void AddVertex(Point3d point)
                        {
                            polyline.AddVertexAt(polyline.NumberOfVertices, point.ToPoint2d(), 0, 0, 0);
                        }

                        for (double i = 0; i <= Longeur; i += LargeurCreneaux)
                        {
                            double NewHeight = randomSelector.SelectItem().Value;
                            Matrix3d HeightMatrix = Matrix3d.Displacement(new Point3d(0, NewHeight - LastHeight, 0).GetAsVector());
                            Point3d NewHeightPoint = LastPoint.TransformBy(HeightMatrix);
                            if (LastPoint != NewHeightPoint)
                            {
                                if (NewHeightPoint != LastPoint)
                                {
                                    AddVertex(LastPoint);
                                    AddVertex(NewHeightPoint);
                                }
                                //Lines.Draw(LastPoint, NewHeightPoint);
                            }
                            Point3d NewWidthPoint = NewHeightPoint.TransformBy(WidthMatrix);
                            LastPoint = NewWidthPoint;
                            LastHeight = NewHeight;
                        }
                        polyline.AddToDrawing();
                        tr.Commit();
                    }
                }
            }
        }
    }
}
