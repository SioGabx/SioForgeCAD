using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun
{
    public static partial class PolygonOperation
    {
        /// <summary>
        /// A fast algorithm for finding polygon pole of inaccessibility, the most distant
        /// internal point from the polygon outline (not to be confused with centroid).
        /// //Adapted to AutoCAD by SioGabx
        ///From : https://github.com/eqmiller/polylabel-csharp
        ///Original : https://github.com/mapbox/polylabel
        /// </summary>
        public static Point3d GetInnerCentroid(Polyline PolylinePolygon, double precision = 1.0)
        {
            var PolygonPtnsCollection = PolylinePolygon.GetPoints().ToPoint3dCollection();
            PolygonPtnsCollection.Add(PolygonPtnsCollection[0]);

            var Extend = PolylinePolygon.GetExtents();
            var ExtendSize = Extend.Size();
            var width = ExtendSize.Width;
            var height = ExtendSize.Height;
            var cellSize = Math.Min(width, height);
            var h = cellSize / 2;

            if (cellSize == 0) return Extend.MinPoint;

            //a priority queue of cells in order of their "potential" (max distance to polygon)
            var cellQueue = new Queue<Cell>();

            //cover polygon with initial cells
            for (var x = Extend.MinPoint.X; x < Extend.MaxPoint.X; x += cellSize)
            {
                for (var y = Extend.MinPoint.Y; y < Extend.MaxPoint.Y; y += cellSize)
                {
                    Point3d CellCenter = new Point3d(x + h, y + h, 0);
                    cellQueue.Enqueue(new Cell(CellCenter, h, PolylinePolygon, PolygonPtnsCollection, null));
                }
            }

            //take centroid as the first best guess
            var bestCell = GetCentroidCell(PolylinePolygon, PolygonPtnsCollection);

            //special case for rectangular polygons
            Point3d bboxCellPoint = new Point3d(Extend.MinPoint.X + (width / 2), Extend.MinPoint.Y + (height / 2), 0);
            var bboxCell = new Cell(bboxCellPoint, 0, PolylinePolygon, PolygonPtnsCollection, null);
            if (bboxCell.DistanceFromCenterToPolygon > bestCell.DistanceFromCenterToPolygon) { bestCell = bboxCell; }

            var numProbes = cellQueue.Count;

            while (cellQueue.Count > 0)
            {
                //pick the most promising cell from the queue

                var cell = cellQueue.Dequeue();

                //update the best cell if we found a better one
                if (cell.DistanceFromCenterToPolygon > bestCell.DistanceFromCenterToPolygon)
                {
                    bestCell = cell;
                }

                //do not drill down further if there's no chance of a better solution
                if (cell.MaxDistanceToPolygonWithingACell - bestCell.DistanceFromCenterToPolygon <= precision) { continue; }

                //split the cell into four cells
                h = cell.HalfCellSize / 2;
                cellQueue.Enqueue(new Cell(new Point3d(cell.CenterPoint.X - h, cell.CenterPoint.Y - h, 0), h, PolylinePolygon, PolygonPtnsCollection, cell.IsFullyInside));
                cellQueue.Enqueue(new Cell(new Point3d(cell.CenterPoint.X + h, cell.CenterPoint.Y - h, 0), h, PolylinePolygon, PolygonPtnsCollection, cell.IsFullyInside));
                cellQueue.Enqueue(new Cell(new Point3d(cell.CenterPoint.X - h, cell.CenterPoint.Y + h, 0), h, PolylinePolygon, PolygonPtnsCollection, cell.IsFullyInside));
                cellQueue.Enqueue(new Cell(new Point3d(cell.CenterPoint.X + h, cell.CenterPoint.Y + h, 0), h, PolylinePolygon, PolygonPtnsCollection, cell.IsFullyInside));
                numProbes += 4;
            }

            return bestCell.CenterPoint;
        }

        private static Cell GetCentroidCell(Polyline polygon, Point3dCollection PolygonPtnsCollection)
        {
            var area = 0.0;
            var x = 0.0;
            var y = 0.0;

            var len = PolygonPtnsCollection.Count;
            var j = len - 1;
            for (var i = 0; i < len; j = i++)
            {
                var a = PolygonPtnsCollection[i];
                var b = PolygonPtnsCollection[j];
                var f = (a.X * b.Y) - (b.X * a.Y);
                x += (a.X + b.X) * f;
                y += (a.Y + b.Y) * f;
                area += f * 3;
            }
            if (area == 0) { return new Cell(PolygonPtnsCollection[0], 0, polygon, PolygonPtnsCollection, false); }
            return new Cell(new Point3d(x / area, y / area, 0), 0, polygon, PolygonPtnsCollection, false);
        }

        private class Cell
        {
            public Cell(Point3d pt, double h, Polyline polygon, Point3dCollection PtnsCollection, bool? FullyInside)
            {
                CenterPoint = pt;
                HalfCellSize = h;
                DistanceFromCenterToPolygon = PointToPolygonDist(pt, polygon, PtnsCollection);
                MaxDistanceToPolygonWithingACell = DistanceFromCenterToPolygon + (HalfCellSize * Math.Sqrt(2));
                if (FullyInside is null)
                {
                    IsFullyInside = CheckFullyInside(polygon);
                }
                else
                {
                    IsFullyInside = (bool)FullyInside;
                }
            }

            public bool? IsFullyInside { get; }
            public Point3d CenterPoint { get; }
            public double HalfCellSize { get; }
            public double DistanceFromCenterToPolygon { get; }
            public double MaxDistanceToPolygonWithingACell { get; }

            private bool? CheckFullyInside(Polyline Boundary)
            {
                using (Polyline polyline = GetBox())
                {
                    if (Boundary.IsSegmentIntersecting(polyline, out _, Intersect.OnBothOperands))
                    {
                        return null;
                    }
                    else
                    {
                        return CenterPoint.IsInsidePolyline(Boundary);
                    }
                }
            }

            public Polyline GetBox()
            {
                var BL = new Point3d(CenterPoint.X - HalfCellSize, CenterPoint.Y - HalfCellSize, 0);
                var BR = new Point3d(CenterPoint.X + HalfCellSize, CenterPoint.Y - HalfCellSize, 0);
                var TR = new Point3d(CenterPoint.X + HalfCellSize, CenterPoint.Y + HalfCellSize, 0);
                var TL = new Point3d(CenterPoint.X - HalfCellSize, CenterPoint.Y + HalfCellSize, 0);
                Polyline polyline = new Polyline();
                polyline.AddVertex(BL);
                polyline.AddVertex(BR);
                polyline.AddVertex(TR);
                polyline.AddVertex(TL);
                polyline.Closed = true;
                return polyline;
            }

            //private void DebugDraw()
            //{
            //    Polyline poly = GetBox();
            //    poly.AddToDrawing();
            //    MText text = new MText();
            //    text.Location = CenterPoint;
            //    text.TextHeight = 0.1;
            //    text.Contents = DistanceFromCenterToPolygon.ToString();
            //    text.AddToDrawing();
            //}

            //distance from point to polygon outline (negative if point is outside)
            private double PointToPolygonDist(Point3d Point, Polyline polygon, Point3dCollection PtnsCollection)
            {
                bool inside = IsFullyInside == null ? Point.ToPoint2d().IsPointInsidePolygonMcMartin(PtnsCollection) : (bool)IsFullyInside;
                var ClosestPoint = polygon.GetClosestPointTo(Point, false);
                var minDistSq = ClosestPoint.DistanceTo(Point);
                return (inside ? 1 : -1) * minDistSq;
            }
        }
    }
}