using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class IMPORT3DGEOMETRYFROMOBJFILE
    {

        public static void Menu()
        {
            GetDataFromObjFile(out List<Point3d> vertices, out List<int[]> faces);

            Editor ed = Generic.GetEditor();
            var ImportChoice = ed.GetOptions("Selectionner l'option d'import", false, "Geometrie", "Altimetries");
            if (ImportChoice.Status == PromptStatus.OK && ImportChoice.StringResult == "Geometrie")
            {
                Import3d(vertices, faces);
            }
            else if (ImportChoice.Status == PromptStatus.OK && ImportChoice.StringResult == "Altimetries")
            {
                ImportCC(vertices);
            }
        }

        public static void GetDataFromObjFile(out List<Point3d> vertices, out List<int[]> faces)
        {
            Editor ed = Generic.GetEditor();

            vertices = new List<Point3d>();
            faces = new List<int[]>();

            PromptOpenFileOptions pfo = new PromptOpenFileOptions("Sélectionnez un fichier OBJ à importer :");
            pfo.Filter = "Fichiers OBJ (*.obj)|*.obj";
            PromptFileNameResult pfr = ed.GetFileNameForOpen(pfo);
            if (pfr.Status != PromptStatus.OK)
            {
                return;
            }

            string filePath = pfr.StringResult;
            if (!File.Exists(filePath))
            {
                ed.WriteMessage("\nFichier introuvable.");
                return;
            }



            // Lecture du fichier .OBJ
            foreach (string line in File.ReadLines(filePath))
            {
                string l = line.Trim();
                if (l.StartsWith("v "))
                {
                    // Exemple : v 1.0 2.0 3.0
                    string[] parts = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        double x = double.Parse(parts[1], CultureInfo.InvariantCulture);
                        double y = double.Parse(parts[2], CultureInfo.InvariantCulture);
                        double z = double.Parse(parts[3], CultureInfo.InvariantCulture);

                        // Conversion Y-up -> Z-up
                        double newX = x;
                        double newY = -z;
                        double newZ = y;

                        vertices.Add(new Point3d(newX, newY, newZ));
                    }
                }
                else if (l.StartsWith("f "))
                {
                    // Exemple : f 1 2 3 ou f 1/1/1 2/2/2 3/3/3
                    string[] parts = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    List<int> faceIndices = new List<int>();

                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] idx = parts[i].Split('/');
                        if (int.TryParse(idx[0], out int vertexIndex))
                        {
                            faceIndices.Add(vertexIndex - 1); // OBJ indexe à partir de 1
                        }
                    }

                    if (faceIndices.Count >= 3)
                    {
                        faces.Add(faceIndices.ToArray());
                    }
                }
            }
            Generic.WriteMessage($"{vertices.Count} sommets et {faces.Count} faces lus.");
        }

        private static void ImportCC(List<Point3d> vertices)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var vertice in vertices)
                {
                    string AltimetrieStr = CotePoints.FormatAltitude(vertice.Z);
                    BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrie, vertice.ToPoints(), ed.GetUSCRotation(AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } });

                }
                tr.Commit();
            }

        }

        private static void Import3d(List<Point3d> vertices, List<int[]> faces)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (var face in faces)
                {
                    using (Polyline3d poly = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection(), true))
                    {
                        btr.AppendEntity(poly);
                        tr.AddNewlyCreatedDBObject(poly, true);

                        foreach (int idx in face)
                        {
                            Point3d pt = vertices[idx];
                            using (PolylineVertex3d vertex = new PolylineVertex3d(pt))
                            {
                                poly.AppendVertex(vertex);
                                tr.AddNewlyCreatedDBObject(vertex, true);
                            }
                        }

                        // Fermeture de la boucle
                        poly.Closed = true;
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nImport terminé (polylignes 3D créées).");
        }

    }
}
