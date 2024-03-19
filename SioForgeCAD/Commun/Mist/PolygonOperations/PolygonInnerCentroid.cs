using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    //Adapted to AutoCAD by SioGabx
    //From : https://github.com/eqmiller/polylabel-csharp
    //Original : https://github.com/mapbox/polylabel

    public static class PolygonInnerCentroid
    {
        /// <summary>
        /// A fast algorithm for finding polygon pole of inaccessibility, the most distant
        /// internal point from the polygon outline (not to be confused with centroid).
        /// Useful for optimal placement of a text label on a polygon.
        /// </summary>
        /// <param name="polygon">GeoJson like format</param>
        /// <param name="precision"></param>
        /// <param name="debug"></param>
        /// <returns></returns>
        public static Point3d GetInnerCentroid(Point3dCollection polygon, double precision = 1.0)
        {
            //get bounding box of the outer ring
            var minX = polygon.Cast<Point3d>().Min(p => p.X);
            var minY = polygon.Cast<Point3d>().Min(p => p.Y);
            var maxX = polygon.Cast<Point3d>().Max(p => p.X);
            var maxY = polygon.Cast<Point3d>().Max(p => p.Y);

            var width = maxX - minX;
            var height = maxY - minY;
            var cellSize = Math.Min(width, height);
            var h = cellSize / 2;

            if (cellSize == 0) return new Point3d(minX, minY, 0);

            //a priority queue of cells in order of their "potential" (max distance to polygon)
            var cellQueue = new Queue<Cell>();

            //cover polygon with initial cells
            for (var x = minX; x < maxX; x += cellSize)
            {
                for (var y = minY; y < maxY; y += cellSize)
                {
                    Point3d CellCenter = new Point3d(x + h, y + h, 0);
                    cellQueue.Enqueue(new Cell(CellCenter, h, polygon));
                }
            }

            //take centroid as the first best guess
            var bestCell = GetCentroidCell(polygon);

            //special case for rectangular polygons
            Point3d bboxCellPoint = new Point3d(minX + width / 2, minY + height / 2, 0);
            var bboxCell = new Cell(bboxCellPoint, 0, polygon);
            if (bboxCell.D > bestCell.D) { bestCell = bboxCell; }

            var numProbes = cellQueue.Count;

            while (cellQueue.Count > 0)
            {
                //pick the most promising cell from the queue
                var cell = cellQueue.Dequeue();

                //update the best cell if we found a better one
                if (cell.D > bestCell.D)
                {
                    bestCell = cell;
                    //Debug.WriteLine($"found best {Math.Round(1e4 * cell.D) / 1e4} after {numProbes}");
                }

                //do not drill down further if there's no chance of a better solution
                if (cell.Max - bestCell.D <= precision) { continue; }

                //split the cell into four cells
                h = cell.H / 2;
                cellQueue.Enqueue(new Cell(new Point3d(cell.Point.X - h, cell.Point.Y - h, 0), h, polygon));
                cellQueue.Enqueue(new Cell(new Point3d(cell.Point.X + h, cell.Point.Y - h, 0), h, polygon));
                cellQueue.Enqueue(new Cell(new Point3d(cell.Point.X - h, cell.Point.Y + h, 0), h, polygon));
                cellQueue.Enqueue(new Cell(new Point3d(cell.Point.X + h, cell.Point.Y + h, 0), h, polygon));
                numProbes += 4;
            }

            //Debug.WriteLine($"Number probes: {numProbes}");
            //Debug.WriteLine($"Best distance: {bestCell.D}");

            return bestCell.Point;
        }

        private static Cell GetCentroidCell(Point3dCollection polygon)
        {
            var area = 0.0;
            var x = 0.0;
            var y = 0.0;

            var len = polygon.Count;
            var j = len - 1;
            for (var i = 0; i < len; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                var f = a.X * b.Y - b.X * a.Y;
                x += (a.X + b.X) * f;
                y += (a.Y + b.Y) * f;
                area += f * 3;
            }
            if (area == 0) { return new Cell(polygon[0], 0, polygon); }
            return new Cell(new Point3d(x / area, y / area, 0), 0, polygon);
        }


        public class Cell
        {
            public Cell(Point3d pt, double h, Point3dCollection polygon)
            {
                Point = pt;
                H = h;
                D = PointToPolygonDist(pt, polygon);
                Max = D + H * Math.Sqrt(2);

                //Polyline poly = new Polyline();
                //poly.AddVertexIfNotExist(new Point3d(pt.X - h, pt.Y - h, 0));
                //poly.AddVertexIfNotExist(new Point3d(pt.X + h, pt.Y - h, 0));
                //poly.AddVertexIfNotExist(new Point3d(pt.X + h, pt.Y + h, 0));
                //poly.AddVertexIfNotExist(new Point3d(pt.X - h, pt.Y + h, 0));
                //poly.Closed = true;
                //poly.AddToDrawing();
                //MText text = new MText();
                //text.Location = pt;
                //text.TextHeight = 0.05;
                //text.Contents = D.ToString();
                //text.AddToDrawing();
            }

            /// <summary>
            /// Cell center X
            /// </summary>
            public Point3d Point { get; }

            /// <summary>
            /// Half the cell size
            /// </summary>
            public double H { get; }

            /// <summary>
            /// Distance from cell center to polygon
            /// </summary>
            public double D { get; }

            /// <summary>
            /// Max distance to polygon within a cell
            /// </summary>
            public double Max { get; }

            /// <summary>
            /// Signed distance from point to polygon outline (negative if point is outside)
            /// </summary>
            /// <param name="x">Cell center x</param>
            /// <param name="y">Cell center y</param>
            /// <param name="polygon">Full GeoJson like Polygon</param>
            private double PointToPolygonDist(Point3d Point, Point3dCollection polygon)
            {
                var inside = Point.ToPoint2d().IsPointInsidePolygonMcMartin(polygon);
                var minDistSq = double.PositiveInfinity;

                for (var k = 0; k < polygon.Count; k++)
                {
                    var ring = polygon;

                    var len = ring.Count;
                    var j = len - 1;
                    for (var i = 0; i < len; j = i++)
                    {
                        Point3d a = ring[i];
                        Point3d b = ring[j];

                        //using (Line ln = new Line(a, b))
                        //{
                        //    var ClosestPt = ln.GetClosestPointTo(Point, false);
                        //    minDistSq = Math.Min(minDistSq, ClosestPt.DistanceTo(Point));
                        //}

                        minDistSq = Math.Min(minDistSq, GetSegDistSq(Point.X, Point.Y, a, b));
                    }
                }

                return (inside ? 1 : -1) * Math.Sqrt(minDistSq);
            }






            private double GetSegDistSq(double px, double py, Point3d a, Point3d b)
            {
                var x = a.X;
                var y = a.Y;
                var dx = b.X - x;
                var dy = b.Y - y;

                if (dx != 0 || dy != 0)
                {
                    var t = ((px - x) * dx + (py - y) * dy) / (dx * dx + dy * dy);

                    if (t > 1)
                    {
                        x = b.X;
                        y = b.Y;
                    }
                    else if (t > 0)
                    {
                        x += dx * t;
                        y += dy * t;
                    }
                }

                dx = px - x;
                dy = py - y;

                return dx * dx + dy * dy;
            }
        }
    }
}