using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Point3d FirstSCUPoint = First.Points.SCU;
            Point3d SecondSCUPoint = Second.Points.SCU;

            //calcul distance :
            double AI_dist_horizontal = Math.Pow(Intermediaire.SCU.X - FirstSCUPoint.X, 2);
            double AI_dist_vertical = Math.Pow(Intermediaire.SCU.Y - FirstSCUPoint.Y, 2);
            double AI_dist_total = Math.Sqrt(AI_dist_horizontal + AI_dist_vertical);

            double IB_dist_horizontal = Math.Pow(SecondSCUPoint.X - Intermediaire.SCU.X, 2);
            double IB_dist_vertical = Math.Pow(SecondSCUPoint.Y - Intermediaire.SCU.Y, 2);
            double IB_dist_total = Math.Sqrt(IB_dist_horizontal + IB_dist_vertical);

            double AB_dist_horizontal = Math.Pow(SecondSCUPoint.X - FirstSCUPoint.X, 2);
            double AB_dist_vertical = Math.Pow(SecondSCUPoint.Y - FirstSCUPoint.Y, 2);
            double AB_dist_total = Math.Sqrt(AB_dist_horizontal + AB_dist_vertical);

            double AIB_dist_total = AI_dist_total + IB_dist_total;
            //ed.WriteMessage("Distance : " + Math.Round(AIB_dist_total, 2) + "\n");

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
            I_cote = Math.Round(I_cote, 2);
            return (I_cote, pente);
        }





    }
}
