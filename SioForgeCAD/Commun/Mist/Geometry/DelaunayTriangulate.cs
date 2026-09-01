using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public static class DelaunayTriangulate
    {
        public readonly struct Triangle3d
        {
            public Point3d Vertex1 { get; }
            public Point3d Vertex2 { get; }
            public Point3d Vertex3 { get; }

            public Triangle3d(Point3d v1, Point3d v2, Point3d v3)
            {
                Vertex1 = v1;
                Vertex2 = v2;
                Vertex3 = v3;
            }
        }

        /*
         * Triangle interne.
         *
         * N_A = triangle voisin de l'arête BC
         * N_B = triangle voisin de l'arête CA
         * N_C = triangle voisin de l'arête AB
         *
         * Les triangles supprimés restent dans la liste pour éviter
         * les RemoveAll() coûteux. Ils sont simplement marqués Removed.
         */
        private sealed class InternalTriangle
        {
            public int A;
            public int B;
            public int C;

            public int N_A = -1;
            public int N_B = -1;
            public int N_C = -1;

            public bool Removed;
        }

        /*
         * Une arête est toujours stockée dans l'ordre min/max.
         * Cela permet d'utiliser directement Dictionary<Edge, int>.
         */
        private readonly struct Edge : IEquatable<Edge>
        {
            public readonly int A;
            public readonly int B;

            public Edge(int a, int b)
            {
                if (a < b)
                {
                    A = a;
                    B = b;
                }
                else
                {
                    A = b;
                    B = a;
                }
            }

            public bool Equals(Edge other)
            {
                return A == other.A && B == other.B;
            }

            public override bool Equals(object obj)
            {
                return obj is Edge edge && Equals(edge);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (A * 397) ^ B;
                }
            }
        }

        /// <summary>
        /// Triangulation de Delaunay 2D.
        ///
        /// X/Y servent à la triangulation.
        /// Z est conservé et restitué dans les Triangle3d.
        ///
        /// Version optimisée :
        /// - suppression des doublons
        /// - ordre spatial Morton/Z-order
        /// - triangles adjacents
        /// - localisation par marche topologique
        /// - cavity local
        /// - dictionnaire d'arêtes
        /// - aucune double boucle sur les arêtes
        /// - aucun RemoveAll() pendant la triangulation
        /// </summary>
        public static List<Triangle3d> Triangulate(
            List<Point3d> nuagePoints,
            LongOperationProcess op = null)
        {
            List<Triangle3d> resultat = new List<Triangle3d>();

            if (nuagePoints == null || nuagePoints.Count < 3)
            {
                return resultat;
            }

            // ------------------------------------------------------------
            // 1. Suppression des doublons XY
            // ------------------------------------------------------------

            List<Point3d> ptsFiltres = nuagePoints.GroupBy(p => new PointKey(p.X, p.Y)).Select(g => g.First()).ToList();

            int n = ptsFiltres.Count;

            if (n < 3)
            {
                return resultat;
            }

            // ------------------------------------------------------------
            // 2. Stockage compact des coordonnées
            // ------------------------------------------------------------

            double[] xs = new double[n + 3];
            double[] ys = new double[n + 3];
            double[] zs = new double[n + 3];

            double xMin = ptsFiltres[0].X;
            double xMax = xMin;
            double yMin = ptsFiltres[0].Y;
            double yMax = yMin;

            for (int i = 0; i < n; i++)
            {
                Point3d p = ptsFiltres[i];

                xs[i] = p.X;
                ys[i] = p.Y;
                zs[i] = p.Z;

                if (p.X < xMin)
                {
                    xMin = p.X;
                }

                if (p.X > xMax)
                {
                    xMax = p.X;
                }

                if (p.Y < yMin)
                {
                    yMin = p.Y;
                }

                if (p.Y > yMax)
                {
                    yMax = p.Y;
                }

                if (op != null && (i & 1023) == 0)
                {
                    op.CheckCanceled();
                }
            }

            double deltaX = xMax - xMin;
            double deltaY = yMax - yMin;

            double dMax = Math.Max(deltaX, deltaY);

            /*
             * Tous les points ont le même XY.
             * Normalement impossible ici puisque les doublons sont filtrés,
             * mais on protège tout de même le calcul du super triangle.
             */
            if (dMax <= 0.0)
            {
                return resultat;
            }

            double xMid = (xMin + xMax) * 0.5;
            double yMid = (yMin + yMax) * 0.5;

            // ------------------------------------------------------------
            // 3. Super triangle
            //
            // CCW :
            //
            //        ST3
            //       /  \
            //      /    \
            //    ST1----ST2
            // ------------------------------------------------------------

            int st1 = n;
            int st2 = n + 1;
            int st3 = n + 2;

            xs[st1] = xMid - (20.0 * dMax);
            ys[st1] = yMid - dMax;
            zs[st1] = 0.0;

            xs[st2] = xMid + (20.0 * dMax);
            ys[st2] = yMid - dMax;
            zs[st2] = 0.0;

            xs[st3] = xMid;
            ys[st3] = yMid + (20.0 * dMax);
            zs[st3] = 0.0;

            // ------------------------------------------------------------
            // 4. Ordre spatial Morton / Z-order
            //
            // L'idée est de traiter les points proches géographiquement
            // les uns après les autres.
            //
            // Cela rend la marche dans les triangles beaucoup plus courte
            // qu'un ordre arbitraire.
            // ------------------------------------------------------------

            int[] ordre = Enumerable.Range(0, n).ToArray();

            Array.Sort(ordre, (a, b) =>
                {
                    ulong ma = MortonCode(xs[a], ys[a], xMin, xMax, yMin, yMax);
                    ulong mb = MortonCode(xs[b], ys[b], xMin, xMax, yMin, yMax);
                    int c = ma.CompareTo(mb);
                    return c != 0 ? c : a.CompareTo(b);
                });

            // ------------------------------------------------------------
            // 5. Structure de triangulation
            // ------------------------------------------------------------

            List<InternalTriangle> triangles = new List<InternalTriangle>(Math.Max(16, n * 2));

            /*
             * edgeMap contient uniquement les arêtes de la triangulation
             * COURANTE.
             *
             * Pour une arête donnée, une seule référence de triangle est
             * conservée. Le voisin est accessible dans le triangle.
             */
            Dictionary<Edge, int> edgeMap = new Dictionary<Edge, int>(Math.Max(16, n * 3));
            int superTriangleId = AddTriangle(st1, st2, st3, triangles, edgeMap, xs, ys);

            if (superTriangleId < 0)
            {
                return resultat;
            }

            int lastTriangle = superTriangleId;

            // ------------------------------------------------------------
            // 6. Insertion incrémentale
            // ------------------------------------------------------------

            for (int orderIndex = 0; orderIndex < n; orderIndex++)
            {
                int pointIndex = ordre[orderIndex];

                double px = xs[pointIndex];
                double py = ys[pointIndex];

                if (op != null && (orderIndex & 63) == 0)
                {
                    op.CheckCanceled();
                    op.UpdateProgress();
                }

                // --------------------------------------------------------
                // 6.1 Localiser le triangle contenant le point
                // --------------------------------------------------------
                int containingTriangle = LocateTriangle(px, py, lastTriangle, triangles, xs, ys);

                /*
                 * Cette situation ne devrait normalement arriver que
                 * pour des cas dégénérés / problèmes numériques.
                 *
                 * On garde un fallback pour privilégier la robustesse.
                 */
                if (containingTriangle < 0)
                {
                    containingTriangle = FindAnyContainingTriangle(px, py, triangles, xs, ys);
                }

                if (containingTriangle < 0)
                {
                    continue;
                }

                // --------------------------------------------------------
                // 6.2 Recherche du cavity
                // --------------------------------------------------------

                List<int> cavity = FindCavity(containingTriangle, px, py, triangles, xs, ys, op);

                if (cavity.Count == 0)
                {
                    continue;
                }

                HashSet<int> cavitySet = new HashSet<int>(cavity);

                // --------------------------------------------------------
                // 6.3 Extraction de la frontière du cavity
                //
                // Grâce aux voisins, on n'a plus besoin de comparer
                // toutes les arêtes entre elles.
                // --------------------------------------------------------

                List<Edge> boundary =
                    new List<Edge>(cavity.Count + 2);

                for (int i = 0; i < cavity.Count; i++)
                {
                    int triangleId = cavity[i];
                    InternalTriangle t = triangles[triangleId];

                    // Arête BC, voisin N_A
                    AddBoundaryIfNeeded(new Edge(t.B, t.C), t.N_A, cavitySet, boundary);

                    // Arête CA, voisin N_B
                    AddBoundaryIfNeeded(new Edge(t.C, t.A), t.N_B, cavitySet, boundary);

                    // Arête AB, voisin N_C
                    AddBoundaryIfNeeded(new Edge(t.A, t.B), t.N_C, cavitySet, boundary);
                }

                // --------------------------------------------------------
                // 6.4 Retrait logique des triangles du cavity
                //
                // On ne fait PAS de RemoveAll().
                // --------------------------------------------------------

                for (int i = 0; i < cavity.Count; i++)
                {
                    int triangleId = cavity[i];

                    InternalTriangle t = triangles[triangleId];

                    t.Removed = true;

                    RemoveCurrentEdge(new Edge(t.B, t.C), triangleId, t.N_A, cavitySet, edgeMap);

                    RemoveCurrentEdge(new Edge(t.C, t.A), triangleId, t.N_B, cavitySet, edgeMap);

                    RemoveCurrentEdge(new Edge(t.A, t.B), triangleId, t.N_C, cavitySet, edgeMap);
                }

                // --------------------------------------------------------
                // 6.5 Création des nouveaux triangles
                // --------------------------------------------------------

                int newLastTriangle = -1;

                for (int i = 0; i < boundary.Count; i++)
                {
                    Edge e = boundary[i];

                    int newTriangleId = AddTriangle(e.A, e.B, pointIndex, triangles, edgeMap, xs, ys);

                    if (newTriangleId >= 0)
                    {
                        newLastTriangle = newTriangleId;
                    }
                }

                /*
                 * Pour la prochaine recherche, on démarre depuis un
                 * triangle créé récemment, donc généralement très proche
                 * du prochain point dans l'ordre Morton.
                 */
                if (newLastTriangle >= 0)
                {
                    lastTriangle = newLastTriangle;
                }
            }

            // ------------------------------------------------------------
            // 7. Construction du résultat final
            // ------------------------------------------------------------

            resultat = new List<Triangle3d>();

            for (int i = 0; i < triangles.Count; i++)
            {
                InternalTriangle t = triangles[i];

                if (t.Removed)
                {
                    continue;
                }

                /*
                 * Les triangles contenant un sommet du super triangle
                 * sont supprimés du résultat final.
                 */
                if (t.A >= n || t.B >= n || t.C >= n)
                {
                    continue;
                }

                /*
                 * Dernière sécurité contre les triangles dégénérés.
                 */
                double area2 = Orientation(xs[t.A], ys[t.A], xs[t.B], ys[t.B], xs[t.C], ys[t.C]);

                if (Math.Abs(area2) <= GeometricEpsilon(xs[t.A], ys[t.A], xs[t.B], ys[t.B], xs[t.C], ys[t.C]))
                {
                    continue;
                }

                resultat.Add(new Triangle3d(new Point3d(xs[t.A], ys[t.A], zs[t.A]), new Point3d(xs[t.B], ys[t.B], zs[t.B]), new Point3d(xs[t.C], ys[t.C], zs[t.C])));
            }

            op?.CheckCanceled();

            return resultat;
        }

        // ================================================================
        // TRIANGLE MANAGEMENT
        // ================================================================

        private static int AddTriangle(int a, int b, int c, List<InternalTriangle> triangles, Dictionary<Edge, int> edgeMap, double[] xs, double[] ys)
        {
            double orientation = Orientation(xs[a], ys[a], xs[b], ys[b], xs[c], ys[c]);

            /*
             * Triangle dégénéré.
             */
            if (Math.Abs(orientation) <= GeometricEpsilon(xs[a], ys[a], xs[b], ys[b], xs[c], ys[c]))
            {
                return -1;
            }

            /*
             * Tous les triangles sont stockés CCW.
             */
            if (orientation < 0.0)
            {
                int temp = b;
                b = c;
                c = temp;
            }

            InternalTriangle t = new InternalTriangle
            {
                A = a,
                B = b,
                C = c,
                Removed = false
            };

            int id = triangles.Count;

            triangles.Add(t);

            /*
             * BC -> N_A
             * CA -> N_B
             * AB -> N_C
             */
            ConnectEdge(id, new Edge(b, c), NeighborSlot.A, triangles, edgeMap);
            ConnectEdge(id, new Edge(c, a), NeighborSlot.B, triangles, edgeMap);
            ConnectEdge(id, new Edge(a, b), NeighborSlot.C, triangles, edgeMap);

            return id;
        }

        private enum NeighborSlot
        {
            A,
            B,
            C
        }

        private static void ConnectEdge(int triangleId, Edge edge, NeighborSlot slot, List<InternalTriangle> triangles, Dictionary<Edge, int> edgeMap)
        {

            if (edgeMap.TryGetValue(edge, out int existingTriangle))
            {
                if (existingTriangle >= 0 && existingTriangle < triangles.Count && existingTriangle != triangleId)
                {
                    SetNeighbour(triangles[triangleId], slot, existingTriangle);

                    SetNeighbourForEdge(triangles[existingTriangle], edge, triangleId);
                }
            }
            else
            {
                edgeMap.Add(edge, triangleId);
            }
        }

        private static void SetNeighbour(
            InternalTriangle t,
            NeighborSlot slot,
            int neighbour)
        {
            switch (slot)
            {
                case NeighborSlot.A:
                    t.N_A = neighbour;
                    break;

                case NeighborSlot.B:
                    t.N_B = neighbour;
                    break;

                case NeighborSlot.C:
                    t.N_C = neighbour;
                    break;
            }
        }

        private static void SetNeighbourForEdge(
            InternalTriangle t,
            Edge edge,
            int neighbour)
        {
            Edge edgeA = new Edge(t.B, t.C);

            if (edge.Equals(edgeA))
            {
                t.N_A = neighbour;
                return;
            }

            Edge edgeB = new Edge(t.C, t.A);

            if (edge.Equals(edgeB))
            {
                t.N_B = neighbour;
                return;
            }

            Edge edgeC = new Edge(t.A, t.B);

            if (edge.Equals(edgeC))
            {
                t.N_C = neighbour;
            }
        }


        // ================================================================
        // TRIANGLE LOCATION
        // ================================================================

        private static int LocateTriangle(
            double px,
            double py,
            int startTriangle,
            List<InternalTriangle> triangles,
            double[] xs,
            double[] ys)
        {
            if (startTriangle < 0 ||
                startTriangle >= triangles.Count)
            {
                return -1;
            }

            int current = startTriangle;

            /*
             * Sécurité contre une éventuelle boucle topologique.
             */
            int maxSteps = Math.Max(32, triangles.Count * 2);

            for (int step = 0; step < maxSteps; step++)
            {
                InternalTriangle t = triangles[current];

                if (t.Removed)
                {
                    return -1;
                }

                double cAB = Orientation(
                    xs[t.A], ys[t.A],
                    xs[t.B], ys[t.B],
                    px, py);

                double cBC = Orientation(
                    xs[t.B], ys[t.B],
                    xs[t.C], ys[t.C],
                    px, py);

                double cCA = Orientation(
                    xs[t.C], ys[t.C],
                    xs[t.A], ys[t.A],
                    px, py);

                double eps = PointInsideEpsilon(
                    xs[t.A], ys[t.A],
                    xs[t.B], ys[t.B],
                    xs[t.C], ys[t.C],
                    px, py);

                /*
                 * Tous les triangles sont CCW.
                 * Donc un point intérieur a les trois cross >= 0.
                 */
                if (cAB >= -eps &&
                    cBC >= -eps &&
                    cCA >= -eps)
                {
                    return current;
                }

                /*
                 * On traverse l'arête par laquelle le point est sorti.
                 *
                 * On choisit le cross le plus négatif.
                 */
                double min = cAB;
                NeighborSlot slot = NeighborSlot.C;

                if (cBC < min)
                {
                    min = cBC;
                    slot = NeighborSlot.A;
                }

                if (cCA < min)
                {
                    min = cCA;
                    slot = NeighborSlot.B;
                }

                int next;

                switch (slot)
                {
                    case NeighborSlot.A:
                        next = t.N_A;
                        break;

                    case NeighborSlot.B:
                        next = t.N_B;
                        break;

                    default:
                        next = t.N_C;
                        break;
                }

                if (next < 0 ||
                    next >= triangles.Count ||
                    triangles[next].Removed)
                {
                    return -1;
                }

                current = next;
            }

            return -1;
        }

        private static int FindAnyContainingTriangle(
            double px,
            double py,
            List<InternalTriangle> triangles,
            double[] xs,
            double[] ys)
        {
            /*
             * Fallback uniquement.
             *
             * Cette boucle n'est normalement jamais utilisée sur un jeu
             * de données propre.
             */
            for (int i = 0; i < triangles.Count; i++)
            {
                InternalTriangle t = triangles[i];

                if (t.Removed)
                {
                    continue;
                }

                if (PointInsideTriangle(
                    px,
                    py,
                    t,
                    xs,
                    ys))
                {
                    return i;
                }
            }

            return -1;
        }

        // ================================================================
        // CAVITY
        // ================================================================

        private static List<int> FindCavity(
            int startTriangle,
            double px,
            double py,
            List<InternalTriangle> triangles,
            double[] xs,
            double[] ys,
            LongOperationProcess op)
        {
            List<int> cavity = new List<int>();

            Stack<int> stack = new Stack<int>();
            HashSet<int> visited = new HashSet<int>();

            stack.Push(startTriangle);

            int iterations = 0;

            while (stack.Count > 0)
            {
                int id = stack.Pop();

                if (id < 0 ||
                    id >= triangles.Count)
                {
                    continue;
                }

                if (!visited.Add(id))
                {
                    continue;
                }

                InternalTriangle t = triangles[id];

                if (t.Removed)
                {
                    continue;
                }

                /*
                 * Test inCircle.
                 *
                 * Tous les triangles sont CCW.
                 */
                if (!PointInCircumcircle(
                    px,
                    py,
                    t,
                    xs,
                    ys))
                {
                    continue;
                }

                cavity.Add(id);

                if (t.N_A >= 0)
                {
                    stack.Push(t.N_A);
                }

                if (t.N_B >= 0)
                {
                    stack.Push(t.N_B);
                }

                if (t.N_C >= 0)
                {
                    stack.Push(t.N_C);
                }

                iterations++;

                if (op != null &&
                    (iterations & 255) == 0)
                {
                    op.CheckCanceled();
                }
            }

            return cavity;
        }

        // ================================================================
        // BOUNDARY
        // ================================================================

        private static void AddBoundaryIfNeeded(
            Edge edge,
            int neighbour,
            HashSet<int> cavity,
            List<Edge> boundary)
        {
            /*
             * Si le voisin n'est pas dans le cavity,
             * l'arête est une arête frontière.
             */
            if (neighbour < 0 ||
                !cavity.Contains(neighbour))
            {
                boundary.Add(edge);
            }
        }

        /*
         * Met à jour edgeMap lors de la suppression d'un triangle.
         *
         * Cas 1 :
         *
         *       T supprimé | T extérieur
         *       ------------|------------
         *             edge
         *
         * L'arête reste dans la triangulation et doit pointer vers
         * T extérieur.
         *
         * Cas 2 :
         *
         *       T supprimé | T supprimé
         *
         * L'arête disparaît complètement.
         */
        private static void RemoveCurrentEdge( Edge edge, int triangleId, int neighbour, HashSet<int> cavity, Dictionary<Edge, int> edgeMap)
        {

            if (!edgeMap.TryGetValue(edge, out int current))
            {
                return;
            }

            if (current != triangleId)
            {
                return;
            }

            if (neighbour >= 0 &&
                !cavity.Contains(neighbour))
            {
                edgeMap[edge] = neighbour;
            }
            else
            {
                edgeMap.Remove(edge);
            }
        }

        // ================================================================
        // GEOMETRY
        // ================================================================

        private static bool PointInsideTriangle(
            double px,
            double py,
            InternalTriangle t,
            double[] xs,
            double[] ys)
        {
            double c1 = Orientation(
                xs[t.A], ys[t.A],
                xs[t.B], ys[t.B],
                px, py);

            double c2 = Orientation(
                xs[t.B], ys[t.B],
                xs[t.C], ys[t.C],
                px, py);

            double c3 = Orientation(
                xs[t.C], ys[t.C],
                xs[t.A], ys[t.A],
                px, py);

            double eps = PointInsideEpsilon(
                xs[t.A], ys[t.A],
                xs[t.B], ys[t.B],
                xs[t.C], ys[t.C],
                px, py);

            return
                c1 >= -eps &&
                c2 >= -eps &&
                c3 >= -eps;
        }

        private static double Orientation(
            double ax,
            double ay,
            double bx,
            double by,
            double cx,
            double cy)
        {
            return
                ((bx - ax) * (cy - ay)) -
                ((by - ay) * (cx - ax));
        }

        /*
         * Test du cercle circonscrit.
         *
         * Le triangle est CCW.
         *
         * Pour un triangle CCW :
         *
         * determinant > 0
         *
         * signifie que P est à l'intérieur du cercle.
         */
        private static bool PointInCircumcircle(
            double px,
            double py,
            InternalTriangle t,
            double[] xs,
            double[] ys)
        {
            double ax = xs[t.A] - px;
            double ay = ys[t.A] - py;

            double bx = xs[t.B] - px;
            double by = ys[t.B] - py;

            double cx = xs[t.C] - px;
            double cy = ys[t.C] - py;

            double a2 = (ax * ax) + (ay * ay);
            double b2 = (bx * bx) + (by * by);
            double c2 = (cx * cx) + (cy * cy);

            double determinant =
                (a2 * ((bx * cy) - (by * cx)))
                - (b2 * ((ax * cy) - (ay * cx)))
                + (c2 * ((ax * by) - (ay * bx)));

            /*
             * Tolérance relative.
             *
             * On veut éviter qu'une très petite erreur numérique fasse
             * entrer/sortir un point d'un cercle presque tangent.
             */
            double scale =
                a2 + b2 + c2;

            double epsilon =
                1e-14 * Math.Max(1.0, scale * scale);

            return determinant > epsilon;
        }

        private static double GeometricEpsilon(
            double ax,
            double ay,
            double bx,
            double by,
            double cx,
            double cy)
        {
            double dx1 = bx - ax;
            double dy1 = by - ay;

            double dx2 = cx - ax;
            double dy2 = cy - ay;

            double dx3 = cx - bx;
            double dy3 = cy - by;

            double scale =
                Math.Max(
                    (dx1 * dx1) + (dy1 * dy1),
                    Math.Max(
                        (dx2 * dx2) + (dy2 * dy2),
                        (dx3 * dx3) + (dy3 * dy3)));

            return 1e-14 * Math.Max(1.0, scale);
        }

        private static double PointInsideEpsilon(
            double ax,
            double ay,
            double bx,
            double by,
            double cx,
            double cy,
            double px,
            double py)
        {
            double scale =
                Math.Max(
                    Math.Abs(bx - ax) + Math.Abs(by - ay),
                    Math.Max(
                        Math.Abs(cx - ax) + Math.Abs(cy - ay),
                        Math.Abs(px - ax) + Math.Abs(py - ay)));

            return 1e-12 * Math.Max(1.0, scale * scale);
        }

        // ================================================================
        // MORTON / Z-ORDER
        // ================================================================

        /*
         * Transforme les coordonnées XY en code spatial.
         *
         * On utilise 20 bits par dimension :
         *
         * XXXXXXXXX...
         * YYYYYYYYY...
         *
         * entrelacés :
         *
         * XYXYXYXY...
         *
         * Ce n'est pas utilisé pour la géométrie, uniquement pour
         * améliorer la localité des insertions.
         */
        private static ulong MortonCode(
            double x,
            double y,
            double xMin,
            double xMax,
            double yMin,
            double yMax)
        {
            const uint maxValue = (1u << 20) - 1u;

            double nx;

            if (xMax > xMin)
            {
                nx = (x - xMin) / (xMax - xMin);
            }
            else
            {
                nx = 0.0;
            }

            double ny;

            if (yMax > yMin)
            {
                ny = (y - yMin) / (yMax - yMin);
            }
            else
            {
                ny = 0.0;
            }

            nx = Clamp01(nx);
            ny = Clamp01(ny);

            uint ix = (uint)(nx * maxValue);
            uint iy = (uint)(ny * maxValue);

            return InterleaveBits(ix, iy);
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0)
            {
                return 0.0;
            }

            if (value > 1.0)
            {
                return 1.0;
            }

            return value;
        }

        private static ulong InterleaveBits(
            uint x,
            uint y)
        {
            ulong result = 0;

            for (int i = 0; i < 20; i++)
            {
                result |=
                    ((ulong)((x >> i) & 1u))
                    << (2 * i);

                result |=
                    ((ulong)((y >> i) & 1u))
                    << ((2 * i) + 1);
            }

            return result;
        }

        // ================================================================
        // DUPLICATE KEY
        // ================================================================

        private readonly struct PointKey : IEquatable<PointKey>
        {
            private readonly double X;
            private readonly double Y;

            public PointKey(double x, double y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(PointKey other)
            {
                return X.Equals(other.X) && Y.Equals(other.Y);
            }

            public override bool Equals(object obj)
            {
                return obj is PointKey pointKey && Equals(pointKey);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;

                    hash = (hash * 31) + X.GetHashCode();
                    hash = (hash * 31) + Y.GetHashCode();

                    return hash;
                }
            }
        }
    }
}