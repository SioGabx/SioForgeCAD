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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class RANDOMPAVEMENT
    {
        public static double PavementFill = 75;
        public static string BaseSettings = "20%{[0,0],[0,1],[1,1],[1,0]},20%{[0,0],[0,1]},80%{[0,0]}";
        public static double PavementColumnsWidth = 0.25;
        public static double PavementRowsWidth = 0.25;
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

                int randomNumber = Random.Next(1, poolSize);
                int accumulatedProbability = 0;
                foreach (var pair in percentageItemsDict)
                {
                    accumulatedProbability += pair.Pourcentage;
                    if (randomNumber <= accumulatedProbability)
                        return pair;
                }
                return default;
            }
        }

        public class Pavement
        {
            public Point3d TopLeft;
            public Point3d TopRight;
            public Point3d BottomLeft;
            public Point3d BottomRight;
            public bool IsSelected;
        }

        public static void Draw()
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!GetDrawingVector(out Line ColumnsLine, out Line RowsLine))
                {
                    return;
                }
                if (!GetPavementsParameters(out ProportionalRandomSelector<List<(int, int)>> randomSelector))
                {
                    return;
                }

                Point3d Origin = ColumnsLine.StartPoint;
                int NumberOfColumns = (int)Math.Ceiling(ColumnsLine.Length / PavementColumnsWidth) - 1;
                int NumberOfRows = (int)Math.Ceiling(RowsLine.Length / PavementRowsWidth) - 1;

                Vector3d ColumnVector = ColumnsLine.GetVector3d();
                Vector3d RowVector = RowsLine.GetVector3d(); 

                List<List<Pavement>> Pavementlist = new List<List<Pavement>>();

                for (int Column = 0; Column < NumberOfColumns; Column++)
                {
                    var ColumnOrigin = Origin.Displacement(ColumnVector, PavementColumnsWidth * Column);
                    for (int Row = 0; Row < NumberOfRows; Row++)
                    {
                        Point3d BottomLeft = ColumnOrigin.Displacement(RowVector, PavementRowsWidth * Row);
                        Point3d BottomRight = BottomLeft.Displacement(ColumnVector, PavementColumnsWidth);

                        Point3d TopLeft = BottomLeft.Displacement(RowVector, PavementRowsWidth);
                        Point3d TopRight = BottomRight.Displacement(RowVector, PavementRowsWidth);
                        Polyline poly = new Polyline();
                        poly.AddVertex(TopLeft);
                        poly.AddVertex(TopRight);
                        poly.AddVertex(BottomRight);
                        poly.AddVertex(BottomLeft);
                        poly.Closed = true;
                        poly.AddToDrawingCurrentTransaction();
                    }
                }


                tr.Commit();
            }
        }

        private static bool GetPavementsParameters(out ProportionalRandomSelector<List<(int, int)>> randomSelector)
        {
            randomSelector = null;
            if (!GetProportionalRandomSelector(out randomSelector)) { return false; }
            if (!GetDouble("Indiquez le taux de remplissage de 0 à 100%", ref PavementFill)) { return false; }
            return true;
        }

        public static bool GetProportionalRandomSelector(out ProportionalRandomSelector<List<(int, int)>> randomSelector)
        {
            randomSelector = null;

            var getStringOption = new PromptStringOptions("Entrez les valeurs :")
            {
                DefaultValue = BaseSettings,
                UseDefaultValue = true,
                AllowSpaces = false
            };
            var GetStringValue = Generic.GetEditor().GetString(getStringOption);
            if (GetStringValue.Status != PromptStatus.OK)
            {
                return false;
            }
            string ResultValue = GetStringValue.StringResult;
            BaseSettings = ResultValue;
            randomSelector = ParseValues(ResultValue);
            return true;
        }

        public static ProportionalRandomSelector<List<(int, int)>> ParseValues(string Values)
        {
            //"20%{[0,0],[0,1],[1,1],[1,0]},20%{[0,0],[0,1]},80%{[0,0]}";
            var randomSelector = new ProportionalRandomSelector<List<(int, int)>>();
            string pattern = @"(\d+)%\{(.*?)\}";

            Regex regex = new Regex(pattern);

            MatchCollection matches = regex.Matches(Values);

            foreach (Match match in matches)
            {
                int percentage = int.Parse(match.Groups[1].Value);
                string arrayString = match.Groups[2].Value;

                List<(int, int)> array = ParseArray(arrayString);
                randomSelector.AddPercentageItem(array, percentage);
            }
            return randomSelector;
        }

        static List<(int, int)> ParseArray(string arrayString)
        {
            List<(int, int)> result = new List<(int, int)>();
            string[] pairs = arrayString.Split(new[] { "],[" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                string cleanedPair = pair.Trim('[', ']');
                string[] elements = cleanedPair.Split(',');

                result.Add((int.Parse(elements[0]), int.Parse(elements[1])));
            }

            return result;
        }





        public static bool GetDouble(string Prompt, ref double Value)
        {
            var getDoubleOption = new PromptDoubleOptions(Prompt)
            {
                DefaultValue = Value,
                UseDefaultValue = true,
                AllowArbitraryInput = true,
                AllowNegative = false,
                AllowNone = false,
            };
            var GetLargeurValue = Generic.GetEditor().GetDouble(getDoubleOption);
            if (GetLargeurValue.Status != PromptStatus.OK)
            {
                return false;
            }
            Value = GetLargeurValue.Value;
            return true;
        }


        private static bool GetDrawingVector(out Line ColumnsLine, out Line RowsLine)
        {
            if (!Points.GetPoint(out Points StartPoint, "Selectionnez le point d'origine") ||
                !Points.GetPoint(out Points LengthEndPoint, "Specifiez la longueur de la zone pavée", StartPoint) ||
                !Points.GetPoint(out Points WidthEndPoint, "Specifiez la largeur de la zone pavée", StartPoint))
            {
                ColumnsLine = new Line();
                RowsLine = new Line();
                return false;
            }

            ColumnsLine = new Line(StartPoint.SCG, WidthEndPoint.SCG);
            RowsLine = new Line(StartPoint.SCG, LengthEndPoint.SCG);

            if (!GetDouble("Indiquez la largeur des pavés", ref PavementColumnsWidth)) { return false; }
            if (!GetDouble("Indiquez la longueur des pavés", ref PavementRowsWidth)) { return false; }

            return true;
        }


    }
}
