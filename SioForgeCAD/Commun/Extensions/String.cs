using System;
using System.Collections.Generic;
using System.Linq;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun.Extensions
{
    static class StringExtensions
    {
        public static IEnumerable<int> AllIndexesOf(this string OriginalString, string SearchedString)
        {
            int minIndex = OriginalString.IndexOf(SearchedString);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = OriginalString.IndexOf(SearchedString, minIndex + SearchedString.Length);
            }
        }

        public static string CapitalizeFirstLetters(this string input, int x)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            x = Math.Min(x, input.Length);
            string firstXLetters = input.Substring(0, x).ToUpper();
            string restOfTheString = input.Substring(x);

            return firstXLetters + restOfTheString;
        }
        public static bool IgnoreCaseEquals(this string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static double? ExtractDoubleInStringFromPoint(this string OriginalString)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            if (OriginalString.Contains("%"))
            {
                ed.WriteMessage("Par mesure de sécurité, les textes contenant des % ne peuvent être converti en côte.");
                return null;
            }

            int[] StringPointPosition = OriginalString.AllIndexesOf(".").ToArray();
            string NumberValueBeforePoint = "";
            string NumberValueAfterPoint = "";

            foreach (int index in StringPointPosition)
            {

                int n = index;
                while (n > 0 && char.IsDigit(OriginalString[n - 1]))
                {
                    NumberValueBeforePoint = OriginalString[n - 1].ToString() + NumberValueBeforePoint;
                    n--;
                }

                n = index;
                while (OriginalString.Length > n + 1 && char.IsDigit(OriginalString[n + 1]))
                {
                    NumberValueAfterPoint += OriginalString[n + 1].ToString();
                    n++;
                }

                if (string.IsNullOrWhiteSpace(NumberValueBeforePoint) || string.IsNullOrWhiteSpace(NumberValueAfterPoint))
                {
                    //Not sure if this is a cote
                    return null;
                }

                string FinalNumberString = $"{NumberValueBeforePoint}.{NumberValueAfterPoint}";
                bool IsValidNumber = double.TryParse(FinalNumberString, out double FinalNumberDouble);
                if (IsValidNumber)
                {
                    ed.WriteMessage($"Côte détéctée : {FinalNumberString}\n");
                    return FinalNumberDouble;
                }
                else
                {
                    //No number found
                    return null;
                }
            }
            //Foreach return 0 element
            return null;
        }
    }
}
