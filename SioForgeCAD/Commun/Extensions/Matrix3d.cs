using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    internal static class Matrix3dExtensions
    {
        public static Matrix3d ToMatrix3d(this Matrix2d matrix2D)
        {
            // Récupère les 9 éléments de la matrice 2D (3x3)
            double[] m = matrix2D.ToArray();

            // Crée une nouvelle matrice 3D (4x4) avec 16 éléments
            return new Matrix3d(new double[]
            {
            m[0], m[1], 0.0, m[2], // Ligne X (Rotation/Echelle X, Cisaillement, 0, Translation X)
            m[3], m[4], 0.0, m[5], // Ligne Y (Cisaillement, Rotation/Echelle Y, 0, Translation Y)
            0.0,  0.0,  1.0, 0.0,  // Ligne Z (Pas de changement sur Z)
            0.0,  0.0,  0.0, 1.0   // Ligne homogène
            });
        }
    }
}
