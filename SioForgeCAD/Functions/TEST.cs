using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace SioForgeCAD.Functions
{
    public static class TEST
    {
        public static void OffsetSegmentsCustom()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez une polyligne fermée : ");

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                return;
            }

            const double offsetDist = .5; // Distance de décalage

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline pline = (Polyline)tr.GetObject(per.ObjectId, OpenMode.ForWrite);
                bool isClockwise = IsClockwise(pline);



                int numSegments = pline.NumberOfVertices;

                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                for (int i = 0; i < numSegments; i++)
                {
                    // 1. Isoler le segment
                    LineSegment2d seg = pline.GetLineSegment2dAt(i);

                    // 2. Calculer le vecteur directeur et la normale
                    Vector2d v = seg.EndPoint - seg.StartPoint;
                    double len = v.Length;
                    Vector2d unitNormal = isClockwise ? new Vector2d(v.Y, -v.X) / len : new Vector2d(-v.Y, v.X) / len;

                    // 3. Créer le vecteur de translation
                    Vector2d translationVector = new Vector2d(unitNormal.X * offsetDist, unitNormal.Y * offsetDist);

                    // 4. Créer une matrice de translation (Garanti le parallélisme)
                    Matrix2d mat = Matrix2d.Displacement(translationVector);

                    // 5. Créer une copie du segment et appliquer la transformation
                    LineSegment2d movedSeg = (LineSegment2d)seg.Clone();
                    v.TransformBy(mat);

                    // Dessiner le nouveau segment dans le dessin (Entity Line)
                    Line line = new Line(movedSeg.StartPoint.ToPoint3d(), movedSeg.EndPoint.ToPoint3d());
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }

                tr.Commit();
            }
        }

        private static bool IsClockwise(Polyline pl)
        {
            double area = 0;
            for (int i = 0; i < pl.NumberOfVertices; i++)
            {
                Point2d p1 = pl.GetPoint2dAt(i);
                Point2d p2 = (i == pl.NumberOfVertices - 1) ? pl.GetPoint2dAt(0) : pl.GetPoint2dAt(i + 1);
                area += (p2.X - p1.X) * (p2.Y + p1.Y);
            }
            return area > 0; // Si l'aire signée est positive, c'est CW
        }
    }
}


namespace SioForgeCAD.Functions
{
    public static class TEST2
    {
        public static void P3D2DTrim()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // ------------------------------------------------------------
            // 1. Sélection des Polyline3d
            // ------------------------------------------------------------

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSélectionnez les polylignes 3D à convertir : ";

            SelectionFilter filter = new SelectionFilter(
                new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, "POLYLINE")
                });

            PromptSelectionResult psr = ed.GetSelection(pso, filter);

            if (psr.Status != PromptStatus.OK)
            {
                return;
            }

            // ------------------------------------------------------------
            // 2. Sélection de la polyligne fermée de découpe
            // ------------------------------------------------------------

            PromptEntityOptions peo =
                new PromptEntityOptions(
                    "\nSélectionnez la polyligne fermée de découpe : ");

            peo.SetRejectMessage(
                "\nVous devez sélectionner une polyligne 2D fermée.");

            peo.AddAllowedClass(typeof(Polyline), true);

            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline limite =
                    tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;

                if (limite?.Closed != true)
                {
                    ed.WriteMessage(
                        "\nErreur : la polyligne de découpe doit être fermée.");

                    return;
                }

                BlockTableRecord btr =
                    (BlockTableRecord)tr.GetObject(
                        db.CurrentSpaceId,
                        OpenMode.ForWrite);

                int nbConverties = 0;
                int nbCrees = 0;

                foreach (SelectedObject so in psr.Value)
                {
                    if (so == null)
                    {
                        continue;
                    }

                    Polyline3d pl3d =
                        tr.GetObject(
                            so.ObjectId,
                            OpenMode.ForRead) as Polyline3d;

                    if (pl3d == null)
                    {
                        continue;
                    }

                    // ----------------------------------------------------
                    // Récupération des sommets 3D
                    // ----------------------------------------------------

                    List<Point3d> pts3d = new List<Point3d>();

                    foreach (ObjectId vertexId in pl3d)
                    {
                        PolylineVertex3d vertex =
                            tr.GetObject(
                                vertexId,
                                OpenMode.ForRead) as PolylineVertex3d;

                        if (vertex != null)
                        {
                            pts3d.Add(vertex.Position);
                        }
                    }

                    if (pts3d.Count < 2)
                    {
                        continue;
                    }

                    // ----------------------------------------------------
                    // Elevation de la nouvelle polyligne
                    //
                    // On prend l'altitude du premier sommet.
                    // ----------------------------------------------------

                    double elevation = pts3d[0].Z;

                    // ----------------------------------------------------
                    // Découpage de la polyline suivant la limite
                    // ----------------------------------------------------

                    List<List<Point2d>> morceaux =
                        ClipPolyline(pts3d, limite);

                    // ----------------------------------------------------
                    // Création des nouvelles polylignes
                    // ----------------------------------------------------

                    foreach (List<Point2d> morceau in morceaux)
                    {
                        if (morceau.Count < 2)
                        {
                            continue;
                        }

                        // Évite les morceaux quasi nuls
                        if (Distance2D(morceau[0], morceau[1]) < Tolerance.Global.EqualPoint)
                        {
                            continue;
                        }

                        Polyline newPl = new Polyline();

                        newPl.SetDatabaseDefaults(db);

                        // Reprise des propriétés principales
                        newPl.Layer = pl3d.Layer;
                        newPl.Color = pl3d.Color;
                        newPl.Linetype = pl3d.Linetype;
                        newPl.LineWeight = pl3d.LineWeight;

                        newPl.Elevation = elevation;

                        for (int i = 0; i < morceau.Count; i++)
                        {
                            newPl.AddVertexAt(
                                i,
                                morceau[i],
                                0.0,
                                0.0,
                                0.0);
                        }

                        btr.AppendEntity(newPl);
                        tr.AddNewlyCreatedDBObject(newPl, true);

                        nbCrees++;
                    }

                    // ----------------------------------------------------
                    // Suppression de la 3D polyline originale
                    // ----------------------------------------------------

                    pl3d.UpgradeOpen();
                    pl3d.Erase();

                    nbConverties++;
                }

                tr.Commit();

                ed.WriteMessage(
                    $"\nTerminé : {nbConverties} polylignes 3D traitées, " +
                    $"{nbCrees} polylignes 2D créées.");
            }
        }


        // ================================================================
        // CLIP D'UNE POLYLINE 3D CONTRE UNE POLYLINE FERMÉE
        // ================================================================

        private static List<List<Point2d>> ClipPolyline(
            List<Point3d> pts3d,
            Polyline limite)
        {
            List<List<Point2d>> result =
                new List<List<Point2d>>();

            List<Point2d> current =
                new List<Point2d>();

            for (int i = 0; i < pts3d.Count - 1; i++)
            {
                Point3d p1 = pts3d[i];
                Point3d p2 = pts3d[i + 1];

                Point3d a =
                    new Point3d(p1.X, p1.Y, limite.Elevation);

                Point3d b =
                    new Point3d(p2.X, p2.Y, limite.Elevation);

                Vector3d direction = b - a;

                double length = direction.Length;

                if (length < Tolerance.Global.EqualPoint)
                {
                    continue;
                }

                // --------------------------------------------------------
                // Paramètres de découpe du segment
                // 0 = début du segment
                // 1 = fin du segment
                // --------------------------------------------------------

                List<double> parameters = new List<double>
                    {
                        0.0,
                        1.0
                    };

                // --------------------------------------------------------
                // Recherche des intersections avec la limite
                // --------------------------------------------------------

                using (Line segment = new Line(a, b))
                {
                    Point3dCollection intersections =
                        new Point3dCollection();

                    segment.IntersectWith(
                        limite,
                        Intersect.OnBothOperands,
                        intersections,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    foreach (Point3d ip in intersections)
                    {
                        double param =
                            GetSegmentParameter(a, b, ip);

                        if (param > 0.0 && param < 1.0)
                        {
                            parameters.Add(param);
                        }
                    }
                }

                // --------------------------------------------------------
                // Tri des paramètres
                // --------------------------------------------------------

                parameters.Sort();

                // Suppression des doublons
                parameters = RemoveDuplicateParameters(parameters);

                // --------------------------------------------------------
                // Chaque sous-segment est testé.
                // Son point milieu permet de savoir s'il est intérieur.
                // --------------------------------------------------------

                for (int j = 0; j < parameters.Count - 1; j++)
                {
                    double t1 = parameters[j];
                    double t2 = parameters[j + 1];

                    if (t2 - t1 < 1e-10)
                    {
                        continue;
                    }

                    Point2d q1 = Interpolate2D(p1, p2, t1);
                    Point2d q2 = Interpolate2D(p1, p2, t2);

                    double tm = (t1 + t2) / 2.0;

                    Point2d middle =
                        Interpolate2D(p1, p2, tm);

                    Point3d testPoint =
                        new Point3d(
                            middle.X,
                            middle.Y,
                            limite.Elevation);

                    bool inside = testPoint.IsInsidePolyline(limite);

                    if (inside)
                    {
                        AddToCurrent(
                            current,
                            q1,
                            q2,
                            result);
                    }
                    else
                    {
                        FlushCurrent(current, result);
                    }
                }
            }

            FlushCurrent(current, result);

            return result;
        }


        // ================================================================
        // AJOUT D'UN SEGMENT AU MORCEAU COURANT
        // ================================================================

        private static void AddToCurrent(
            List<Point2d> current,
            Point2d p1,
            Point2d p2,
            List<List<Point2d>> result)
        {
            if (current.Count == 0)
            {
                current.Add(p1);
                current.Add(p2);
                return;
            }

            Point2d last =
                current[current.Count - 1];

            if (Distance2D(last, p1) < 1e-7)
            {
                current.Add(p2);
            }
            else
            {
                FlushCurrent(current, result);

                current.Add(p1);
                current.Add(p2);
            }
        }


        // ================================================================
        // FINALISATION D'UN MORCEAU
        // ================================================================

        private static void FlushCurrent(
            List<Point2d> current,
            List<List<Point2d>> result)
        {
            if (current.Count >= 2)
            {
                result.Add(
                    new List<Point2d>(current));
            }

            current.Clear();
        }


        // ================================================================
        // TEST POINT DANS POLYLIGNE
        // ================================================================




        // ================================================================
        // PARAMETRE D'UN POINT SUR UN SEGMENT
        // ================================================================

        private static double GetSegmentParameter(
            Point3d a,
            Point3d b,
            Point3d p)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            double denominator =
                (dx * dx) + (dy * dy);

            if (denominator < 1e-20)
            {
                return 0.0;
            }

            return
                (((p.X - a.X) * dx) +
                 ((p.Y - a.Y) * dy))
                / denominator;
        }


        // ================================================================
        // INTERPOLATION 2D
        // ================================================================

        private static Point2d Interpolate2D(
            Point3d p1,
            Point3d p2,
            double t)
        {
            return new Point2d(
                p1.X + ((p2.X - p1.X) * t),
                p1.Y + ((p2.Y - p1.Y) * t));
        }


        // ================================================================
        // DISTANCE 2D
        // ================================================================

        private static double Distance2D(
            Point2d a,
            Point2d b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;

            return Math.Sqrt((dx * dx) + (dy * dy));
        }


        // ================================================================
        // SUPPRESSION DES PARAMETRES DUPLIQUES
        // ================================================================

        private static List<double>
            RemoveDuplicateParameters(
                List<double> values)
        {
            List<double> result =
                new List<double>();

            foreach (double value in values)
            {
                if (result.Count == 0 ||
                    Math.Abs(
                        value -
                        result[result.Count - 1]) > 1e-9)
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}