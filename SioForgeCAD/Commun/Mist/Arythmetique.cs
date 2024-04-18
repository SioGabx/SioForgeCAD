using Autodesk.AutoCAD.Geometry;
using System;

namespace SioForgeCAD.Commun
{
    public static class Arythmetique
    {
        public static (double Altitude, double Slope) ComputeSlopeAndIntermediate(CotePoints First, CotePoints Second, Points Intermediaire)
        {
            if (First is null || Second is null)
            {
                return (0, 0);
            }
            Point3d FirstPointSCG = First.Points.SCG;
            Point3d SecondPointSCG = Second.Points.SCG;
            Point3d IntermediairePointInSCG = Intermediaire.SCG;

            //calcul distance :
            double AI_dist_horizontal = Math.Pow(IntermediairePointInSCG.X - FirstPointSCG.X, 2);
            double AI_dist_vertical = Math.Pow(IntermediairePointInSCG.Y - FirstPointSCG.Y, 2);
            double AI_dist_total = Math.Sqrt(AI_dist_horizontal + AI_dist_vertical);

            double IB_dist_horizontal = Math.Pow(SecondPointSCG.X - IntermediairePointInSCG.X, 2);
            double IB_dist_vertical = Math.Pow(SecondPointSCG.Y - IntermediairePointInSCG.Y, 2);
            double IB_dist_total = Math.Sqrt(IB_dist_horizontal + IB_dist_vertical);

            double AB_dist_horizontal = Math.Pow(SecondPointSCG.X - FirstPointSCG.X, 2);
            double AB_dist_vertical = Math.Pow(SecondPointSCG.Y - FirstPointSCG.Y, 2);
            double AB_dist_total = Math.Sqrt(AB_dist_horizontal + AB_dist_vertical);

            double AIB_dist_total = AI_dist_total + IB_dist_total;

            double AI_pourcent = AI_dist_total / AIB_dist_total;
            double AB_cote_dif = Math.Abs(First.Altitude - Second.Altitude);
            double I_dif_to_add_sus = AB_cote_dif * AI_pourcent;
            double I_cote = First.Altitude;

            double pente = Math.Round((AB_cote_dif / AB_dist_total) * 100.00, 2);

            if (First.Altitude > Second.Altitude)
            {
                I_cote -= I_dif_to_add_sus;
            }
            else
            {
                I_cote += I_dif_to_add_sus;
            }
            //I_cote = Math.Round(I_cote, 3);

            if (double.IsNaN(I_cote))
            {
                I_cote = First.Altitude;
            }
            if (double.IsNaN(pente))
            {
                pente = 0;
            }

            return (I_cote, pente);
        }

        public static double ComputePointFromSlopePourcentage(double OriginAltitude, double DistanceFromOrigin, double Slope)
        {
            const double PourcentageToDecimalRatio = 0.01;
            double Altimetrie = Math.Abs(OriginAltitude) + (Slope * PourcentageToDecimalRatio * Math.Abs(DistanceFromOrigin));
            return Altimetrie;
        }
    }
}
