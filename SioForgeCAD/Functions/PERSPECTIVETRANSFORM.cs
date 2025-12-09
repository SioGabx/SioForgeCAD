using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Overrules;
using SioForgeCAD.Commun.Overrules.PolyGripOverrule;
using SioForgeCAD.Commun.Overrules.PolylineGripOverrule;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class PERSPECTIVETRANSFORM
    {
        enum BlocDataType { PTMain, PTOriginalEnts, PTTransformedEnts }

        private static PerspectiveTransformGrips _instance = null;
        public static PerspectiveTransformGrips Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PerspectiveTransformGrips(typeof(BlockReference), PerspectiveTransformGrips.FilterFunction, PerspectiveTransformGrips.CornerOnHotGripAction, null, true);
                }
                return _instance;
            }
        }

        public static void Create()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var PromptCurves = ed.GetCurves("Selectionnez des courbes à convertir", false);
                if (PromptCurves.Status == PromptStatus.OK)
                {
                    var curves = PromptCurves.Value.GetObjectIds().Select(id => id.GetDBObject(OpenMode.ForWrite)).OfType<Curve>();
                    var (origColl, transColl, origin) = BuildEntityCollections(curves);
                    var mainBlk = BuildBlockHierarchy(origColl, transColl, origin);
                    mainBlk.AddToDrawing();
                    _ = Instance;
                }
                tr.Commit();
            }
        }
        private static void TransformEnts(BlockReference mainBlk)
        {
            using (var tr = Generic.GetDocument().TransactionManager.StartTransaction())
            {
                if (!TryGetSubBlocks(mainBlk, out var origRef, out var transRef))
                {
                    return;
                }

                var origBoundary = GetBoundary(origRef);
                var transBoundary = GetBoundary(transRef);

                var direct = PerspectiveTransformFunctions.GetNormalizationCoefficients(origBoundary, transBoundary);
                // var inverse = PerspectiveTransformFunctions.GetNormalizationCoefficients(transBoundary, origBoundary); // may be useful later.

                ClearContent(transRef);
                RebuildContent(origRef, transRef, direct, tr);

                mainBlk.RegenAllBlkDefinition();
                tr.Commit();
            }
        }

        private static (DBObjectCollection original, DBObjectCollection transformed, Point3d origin) BuildEntityCollections(IEnumerable<Curve> curves)
        {
            var original = new DBObjectCollection();
            var transformed = new DBObjectCollection();

            foreach (var curve in curves)
            {
                using (var pline = curve.ToPolyline())
                using (var polygon = pline.ToPolygon(5))
                {
                    curve.CopyPropertiesTo(polygon);

                    var exploded = new DBObjectCollection();
                    polygon.Explode(exploded);

                    foreach (Entity ent in exploded)
                    {
                        transformed.Add(ent);

                        var entClone = (Entity)ent.Clone();
                        entClone.Visible = false;
                        original.Add(entClone);
                    }
                }
            }

            Point3d origin = original.GetExtents().MinPoint;

            var oBoundary = original.GetExtents().GetGeometry();
            var tBoundary = transformed.GetExtents().GetGeometry();
            oBoundary.Visible = true;
            tBoundary.Visible = true;
            original.Add(oBoundary);
            transformed.Add(tBoundary);

            return (original, transformed, origin);
        }

        private static BlockReference BuildBlockHierarchy(DBObjectCollection original, DBObjectCollection transformed, Point3d origin)
        {
            var origBlkId = BlockReferences.Create("*U", nameof(BlocDataType.PTOriginalEnts), original, origin.ToPoints(), false, BlockScaling.Uniform);
            var transBlkId = BlockReferences.Create("*U", nameof(BlocDataType.PTTransformedEnts), transformed, origin.ToPoints(), false, BlockScaling.Uniform);

            var origRef = new BlockReference(origin, origBlkId);
            origRef.AddXData(nameof(BlocDataType.PTOriginalEnts)); origRef.Transparency = Generic.GetTransparencyFromAlpha(0);

            var transRef = new BlockReference(origin, transBlkId);
            transRef.AddXData(nameof(BlocDataType.PTTransformedEnts));

            var mainBlkId = BlockReferences.Create("*U", nameof(BlocDataType.PTMain), new DBObjectCollection { origRef, transRef }, origin.ToPoints(), false, BlockScaling.Uniform);
            var mainRef = new BlockReference(origin, mainBlkId);
            mainRef.AddXData(nameof(BlocDataType.PTMain));

            return mainRef;
        }

        private static bool TryGetSubBlocks(BlockReference mainBlk, out BlockReference origRef, out BlockReference transRef)
        {
            origRef = transRef = null;
            foreach (var ent in GetEntitiesInBlock(mainBlk))
            {
                if (!(ent is BlockReference sub))
                {
                    continue;
                }

                var data = sub.ReadXData();
                if (data.Contains(nameof(BlocDataType.PTOriginalEnts)))
                {
                    origRef = sub;
                }
                else if (data.Contains(nameof(BlocDataType.PTTransformedEnts)))
                {
                    transRef = sub;
                }
            }
            return origRef != null && transRef != null;
        }

        private static Point2d[] GetBoundary(BlockReference blk) => GetEntitiesInBlock(blk).OfType<Polyline>().First().GetPolyPoints().ToArray();

        private static void ClearContent(BlockReference transRef)
        {
            foreach (var id in (BlockTableRecord)transRef.BlockTableRecord.GetObject(OpenMode.ForWrite))
            {
                var ent = id.GetDBObject(OpenMode.ForWrite);
                if (!(ent is Polyline))
                {
                    ent.Erase();
                }

            }
        }

        private static void RebuildContent(BlockReference origRef, BlockReference transRef, double[] coeffs, Transaction tr)
        {
            var record = (BlockTableRecord)transRef.BlockTableRecord.GetObject(OpenMode.ForWrite);
            foreach (var ln in GetEntitiesInBlock(origRef).OfType<Line>())
            {
                var clone = (Line)ln.Clone();
                clone.Visible = true;
                clone.StartPoint = PerspectiveTransformFunctions.Transform(coeffs, ln.StartPoint.ToPoint2d()).ToPoint3d();
                clone.EndPoint = PerspectiveTransformFunctions.Transform(coeffs, ln.EndPoint.ToPoint2d()).ToPoint3d();

                record.AppendEntity(clone);
                tr.AddNewlyCreatedDBObject(clone, true);
            }
        }

        private static IEnumerable<Entity> GetEntitiesInBlockNoTr(BlockReference blk)
        {
            DBObjectCollection Exploded = new DBObjectCollection();
            if (blk != null)
            {
                blk.Explode(Exploded);
                foreach (DBObject item in Exploded)
                {
                    if (item is Entity ent)
                    {
                        yield return ent;
                    }
                    else
                    {
                        item.Dispose();
                    }
                }
            }
        }
        private static IEnumerable<Entity> GetEntitiesInBlock(BlockReference blk)
        {
            var def = (BlockTableRecord)blk.BlockTableRecord.GetNoTransactionDBObject(OpenMode.ForRead);
            return def.Cast<ObjectId>().Select(id => id.GetNoTransactionDBObject()).OfType<Entity>();
        }



        public class PerspectiveTransformGrips : PolyGripOverrule
        {


            public PerspectiveTransformGrips(Type TargetType, Func<Entity, bool> FilterFunction, Action<ObjectId, GripData> CornerOnHotGripAction, Action<ObjectId, GripData> MiddleOnHotGripAction, bool HideOriginals = true) : base(TargetType, FilterFunction, CornerOnHotGripAction, MiddleOnHotGripAction, HideOriginals)
            {
                Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), this, false);
            }
            public static bool FilterFunction(Entity e) =>
                e is BlockReference br && br.ReadXData().Contains(nameof(BlocDataType.PTMain));

            private static Polyline GetTransformedPolylineNoTr(BlockReference mainBlk)
            {
                var mainBlkInEnts = GetEntitiesInBlockNoTr(mainBlk);
                BlockReference BlkPTTransformedEnts = mainBlkInEnts.OfType<BlockReference>().FirstOrDefault(br => !br.Transparency.IsByAlpha);
                var BlkPTTransformedEntsInEnts = GetEntitiesInBlockNoTr(BlkPTTransformedEnts);
                var Poly = BlkPTTransformedEntsInEnts.OfType<Polyline>().FirstOrDefault().Clone() as Polyline;
                mainBlkInEnts.DeepDispose();
                BlkPTTransformedEntsInEnts.DeepDispose();
                return Poly;
            }

            private static Polyline GetTransformedPolyline(BlockReference mainBlk)
            {
                BlockReference BlkPTTransformedEnts = GetEntitiesInBlock(mainBlk)
                 .OfType<BlockReference>()
                 .FirstOrDefault(br => br.ReadXData().Contains(nameof(BlocDataType.PTTransformedEnts)));

                return GetEntitiesInBlock(BlkPTTransformedEnts).OfType<Polyline>().FirstOrDefault();
            }

            public static void CornerOnHotGripAction(ObjectId id, GripData gripData)
            {
                using (var tr = Generic.GetDocument().TransactionManager.StartTransaction())
                {
                    if (!(id.GetNoTransactionDBObject() is BlockReference mainBlk))
                    {
                        return;
                    }

                    if (!(GetTransformedPolyline(mainBlk) is Polyline poly))
                    {
                        return;
                    }

                    if (!(gripData is PolyCornerGrip polyGrip))
                    {
                        return;
                    }

                    var blockTransform = mainBlk.BlockTransform;
                    var inverse = blockTransform;
                    using (var workingCopy = (Polyline)poly.Clone())
                    {
                        workingCopy.TransformBy(inverse);

                        if ((int)polyGrip.CurrentModeId <= (int)PolyGripOverrule.ModeIdAction.Stretch)
                        {
                            var pts = StretchPoint(workingCopy, gripData.GripPoint, PolyGripOverrule.ModeIdAction.Stretch);
                            if (pts is null)
                            {
                                return;
                            }

                            poly.UpgradeOpen();
                            for (int i = 0; i < pts.Count - 1; i++)
                            {
                                poly.SetPointAt(i, pts[i].ToPoint3d().TransformBy(blockTransform.Inverse()).ToPoint2d());
                            }

                            TransformEnts(mainBlk);
                        }
                    }
                    tr.Commit();
                }
            }

            private static Point2dCollection StretchPoint(Polyline poly, Point3d gripPt, PolyGripOverrule.ModeIdAction action)
            {
                var origVerts = new Point2dCollection(poly.GetPoints().Select(p => p.ToPoint2d()).ToArray());
                using (var jigPoly = new Polyline { Closed = true })
                {
                    for (int i = 0; i < origVerts.Count - 1; i++)
                    {
                        jigPoly.AddVertex(origVerts[i]);
                    }

                    var jig = new PolyGripJig(jigPoly, gripPt,gripPt, new Point3dCollection { gripPt });
                    var jigRes = jig.Drag();
                    if (jigRes?.Status != PromptStatus.OK)
                    {
                        return null;
                    }

                    var newVerts = new Point2dCollection();
                    foreach (Point2d origVert in origVerts)
                    {
                        newVerts.Add(origVert.IsEqualTo(gripPt.ToPoint2d(), Generic.MediumTolerance) ? jigRes.Value.ToPoint2d() : origVert);
                    }

                    return newVerts;
                }
            }

            public override void GetGripPoints(Entity entity, GripDataCollection grips, double viewUnit, int gripSize, Vector3d viewDir, GetGripPointsFlags flags)
            { //  Dont use transaction here, this cause AutoCAD to crash when changing properties : An item with the same key has already been added

                if (!IsApplicable(entity)) { base.GetGripPoints(entity, grips, viewUnit, gripSize, viewDir, flags); return; }

                if (!(entity is BlockReference blk)) return;


                //using (var tr = blk.Database.TransactionManager.StartOpenCloseTransaction())
                //{
                using (var ent = GetTransformedPolylineNoTr(blk))
                {
                    if (!(ent is Polyline po)) { return; }

                    var added = new HashSet<Point3d>();
                    foreach (var pt in po.GetPolyPoints())
                    {
                        var transformed = pt.ToPoint3d().TransformBy(blk.BlockTransform);
                        if (!added.Contains(transformed))
                        {
                            var grip = new PolyCornerGrip
                            {
                                Index = grips.Count + 1,
                                GripPoint = transformed,
                                EntityId = entity.ObjectId,
                                OnHotGripAction = CornerOnHotGripAction
                            };

                            if (!grips.Contains(grip))
                            {
                                grips.Add(grip);
                            }
                        }
                        //}
                        //tr.Abort();
                    }
                }
            }
        }




        public static class PerspectiveTransformFunctions
        {
            public static Point2d Transform(double[] coeffs, Point2d Point)
            {
                return new Point2d(
                ((coeffs[0] * Point.X) + (coeffs[1] * Point.Y) + coeffs[2]) / ((coeffs[6] * Point.X) + (coeffs[7] * Point.Y) + 1),
                ((coeffs[3] * Point.X) + (coeffs[4] * Point.Y) + coeffs[5]) / ((coeffs[6] * Point.X) + (coeffs[7] * Point.Y) + 1)
                );
            }

            public static void GetMatrixTransformation(Point2d[] SourceRect, Point2d[] DestinationRect)
            {
                double[] TransformCoeffs = GetNormalizationCoefficients(SourceRect, DestinationRect);
                double[] TransformCoeffsInverse = GetNormalizationCoefficients(DestinationRect, SourceRect);
                //Console.WriteLine(string.Join(',', TransformCoeffs));
                //Console.WriteLine(string.Join(',', TransformCoeffsInverse));
                Console.WriteLine(Transform(TransformCoeffs, new Point2d(7.5, 1)));
                Console.WriteLine(Transform(TransformCoeffs, new Point2d(3.5, 8)));

                Console.WriteLine(Transform(TransformCoeffsInverse, new Point2d(17.5, 3.111)));
                Console.WriteLine(Transform(TransformCoeffsInverse, new Point2d(12.833, 14)));

            }


            public static double[][] MatrixMultiplication(double[][] matrixX, double[][] matrixY)
            {
                int numRowsX = matrixX.Length;
                int numColsY = matrixY[0].Length;
                int innerDimension = matrixY.Length;

                double[][] result = new double[numRowsX][];

                for (int i = 0; i < numRowsX; i++)
                {
                    result[i] = new double[numColsY];
                    double[] rowX = matrixX[i];

                    for (int k = 0; k < numColsY; k++)
                    {
                        double dotProduct = 0;

                        for (int j = 0; j < innerDimension; j++)
                        {
                            dotProduct += rowX[j] * matrixY[j][k];
                        }

                        result[i][k] = dotProduct;
                    }
                }

                return result;
            }

            public static double[] MatrixVectorMultiplication(double[][] matrix, double[] vector)
            {
                int numRows = matrix.Length;
                double[] result = new double[numRows];

                for (int i = 0; i < numRows; i++)
                {
                    result[i] = VectorDotProduct(matrix[i], vector);
                }

                return result;
            }

            public static double VectorDotProduct(double[] vectorX, double[] vectorY)
            {
                int vectorLength = vectorX.Length;
                double result = vectorX[vectorLength - 1] * vectorY[vectorLength - 1];

                for (int i = vectorLength - 2; i >= 1; i -= 2)
                {
                    int i1 = i - 1;
                    result += (vectorX[i] * vectorY[i]) + (vectorX[i1] * vectorY[i1]);
                }

                if (vectorLength % 2 == 1)
                {
                    result += vectorX[0] * vectorY[0];
                }

                return result;
            }

            public static double[][] InverseSquareMatrix(double[][] matrix) //InverseSquareMatrix
            {
                int rowCount = matrix.Length;
                int colCount = matrix[0].Length;
                double[][] originalMatrix = (double[][])matrix.Clone();
                double[][] identityMatrix = Identity(rowCount);

                double pivot;
                for (int j = 0; j < colCount; ++j)
                {
                    int maxRowIndex = -1;
                    double maxValue = -1;

                    // Find the row with the maximum absolute value in the current column
                    for (int i = j; i < rowCount; ++i)
                    {
                        double absValue = Math.Abs(originalMatrix[i][j]);
                        if (absValue > maxValue)
                        {
                            maxRowIndex = i;
                            maxValue = absValue;
                        }
                    }

                    double[] pivotRow = originalMatrix[maxRowIndex];
                    originalMatrix[maxRowIndex] = originalMatrix[j];
                    originalMatrix[j] = pivotRow;

                    double[] pivotIdentityRow = identityMatrix[maxRowIndex];
                    identityMatrix[maxRowIndex] = identityMatrix[j];
                    identityMatrix[j] = pivotIdentityRow;

                    pivot = pivotRow[j];
                    for (int k = j; k < colCount; ++k)
                    {
                        pivotRow[k] /= pivot;
                    }

                    for (int k = colCount - 1; k >= 0; --k)
                    {
                        pivotIdentityRow[k] /= pivot;
                    }

                    for (int i = rowCount - 1; i >= 0; --i)
                    {
                        if (i != j)
                        {
                            double[] currentRow = originalMatrix[i];
                            double[] currentIdentityRow = identityMatrix[i];
                            double currentX = currentRow[j];

                            int k;
                            for (k = j + 1; k < colCount; ++k)
                            {
                                currentRow[k] -= pivotRow[k] * currentX;
                            }

                            for (k = colCount - 1; k > 0; --k)
                            {
                                currentIdentityRow[k] -= pivotIdentityRow[k] * currentX;
                                --k;
                                currentIdentityRow[k] -= pivotIdentityRow[k] * currentX;
                            }
                            if (k == 0)
                            {
                                currentIdentityRow[0] -= pivotIdentityRow[0] * currentX;
                            }
                        }
                    }
                }

                return identityMatrix;
            }



            public static double[][] DiagonalMatrix(double[] diagonalValues)
            {
                int size = diagonalValues.Length;
                double[][] diagonalMatrix = new double[size][];

                for (int i = 0; i < size; i++)
                {
                    diagonalMatrix[i] = new double[size];
                    for (int j = 0; j < size; j++)
                    {
                        diagonalMatrix[i][j] = (i == j) ? diagonalValues[i] : 0;
                    }
                }

                return diagonalMatrix;
            }

            public static double[] RepeatValue(int[] sizes, double value, int currentIndex = 0)
            {
                int size = sizes[currentIndex];
                double[] result = new double[size];

                if (currentIndex == sizes.Length - 1)
                {
                    for (int i = 0; i < size; i++)
                    {
                        result[i] = value;
                    }
                    return result;
                }

                for (int i = 0; i < size; i++)
                {
                    result[i] = RepeatValue(sizes, value, currentIndex + 1)[0];
                }

                return result;
            }

            public static double[][] Identity(int size)
            {
                return DiagonalMatrix(RepeatValue(new int[] { size }, 1));
            }

            public static double[] GetNormalizationCoefficients(Point2d[] SourceRect, Point2d[] DestinationRect)
            {
                double[][] coeffs = new double[8][] {
                new double[] { SourceRect[0].X, SourceRect[0].Y, 1, 0, 0, 0, -1 * DestinationRect[0].X * SourceRect[0].X, -1 * DestinationRect[0].X * SourceRect[0].Y },
                new double[] { 0, 0, 0, SourceRect[0].X, SourceRect[0].Y, 1, -1 * DestinationRect[0].Y * SourceRect[0].X, -1 * DestinationRect[0].Y * SourceRect[0].Y },

                new double[] { SourceRect[1].X, SourceRect[1].Y, 1, 0, 0, 0, -1 * DestinationRect[1].X * SourceRect[1].X, -1 * DestinationRect[1].X * SourceRect[1].Y },
                new double[] { 0, 0, 0, SourceRect[1].X, SourceRect[1].Y, 1, -1 * DestinationRect[1].Y * SourceRect[1].X, -1 * DestinationRect[1].Y * SourceRect[1].Y },

                new double[] { SourceRect[2].X, SourceRect[2].Y, 1, 0, 0, 0, -1 * DestinationRect[2].X * SourceRect[2].X, -1 * DestinationRect[2].X * SourceRect[2].Y },
                new double[] { 0, 0, 0, SourceRect[2].X, SourceRect[2].Y, 1, -1 * DestinationRect[2].Y * SourceRect[2].X, -1 * DestinationRect[2].Y * SourceRect[2].Y },

                new double[] { SourceRect[3].X, SourceRect[3].Y, 1, 0, 0, 0, -1 * DestinationRect[3].X * SourceRect[3].X, -1 * DestinationRect[3].X * SourceRect[3].Y },
                new double[] { 0, 0, 0, SourceRect[3].X, SourceRect[3].Y, 1, -1 * DestinationRect[3].Y * SourceRect[3].X, -1 * DestinationRect[3].Y * SourceRect[3].Y }
            };

                var MatrixB = new double[8]
                {
               DestinationRect[0].X, DestinationRect[0].Y,
               DestinationRect[1].X, DestinationRect[1].Y,
               DestinationRect[2].X, DestinationRect[2].Y,
               DestinationRect[3].X, DestinationRect[3].Y
                };

                double[][] MatrixC = InverseSquareMatrix(MatrixMultiplication(TransposeMatrix(coeffs), coeffs));
                double[][] MatrixD = MatrixMultiplication(MatrixC, TransposeMatrix(coeffs));

                var FinalMatrix = MatrixVectorMultiplication(MatrixD, MatrixB);
                for (var i = 0; i < FinalMatrix.Length; i++)
                {
                    FinalMatrix[i] = Round(FinalMatrix[i]);
                }

                return FinalMatrix.Append(1).ToArray();
            }

            public static double Round(double num)
            {
                return Math.Round(num, 11);
            }

            public static double[][] TransposeMatrix(double[][] matrix)
            {
                int numRows = matrix.Length;
                int numCols = matrix[0].Length;
                double[][] transposedMatrix = new double[numCols][];
                for (int j = 0; j < numCols; j++)
                {
                    transposedMatrix[j] = new double[numRows];
                    for (int i = 0; i < numRows; i++)
                    {
                        transposedMatrix[j][i] = matrix[i][j];
                    }
                }
                return transposedMatrix;

            }
        }


    }
}
