using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using static SioForgeCAD.Functions.BATTLEMENTS;

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


        public static void Draw()
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
