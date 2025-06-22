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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class PERSPECTIVETRANSFORM
    {
        public static void Transform()
        {

            Overrule.AddOverrule(RXClass.GetClass(typeof(BlockReference)), PerspectiveTransformGrips.Instance, false);

        }

        private static void AddEntitiesToBlock(BlockReference BlockRef, ObjectId[] selectedIds, Transaction tr)
        {
            BlockTableRecord BlockDef = BlockRef.BlockTableRecord.GetDBObject(OpenMode.ForWrite) as BlockTableRecord;
            Matrix3d blockTransform = BlockRef.BlockTransform;
            Matrix3d inverseTransform = blockTransform.Inverse();

            foreach (ObjectId entId in selectedIds)
            {
                if (entId == BlockRef.ObjectId) { continue; }
                if (!(entId.GetDBObject(OpenMode.ForWrite) is Entity SelectedEnt)) { continue; }

                Entity SelectedEntClone = SelectedEnt.Clone() as Entity;
                SelectedEntClone.TransformBy(inverseTransform);
                BlockDef.AppendEntity(SelectedEntClone);
                tr.AddNewlyCreatedDBObject(SelectedEntClone, true);
                SelectedEnt.Erase();
            }
        }

        private static List<Entity> GetEntitiesInBlock(BlockReference BlockRef)
        {
            var result = new List<Entity>();

            BlockTableRecord blockDef = BlockRef.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId EntityInBlockDef in blockDef)
            {
                result.Add(EntityInBlockDef.GetDBObject() as Entity);

            }

            return result;
        }


















        #region PolyGripOverrule







        #endregion








        public class PerspectiveTransformGrips : PolyGripOverrule
        {
            private static PolyGripOverrule _instance = null;
            public static PolyGripOverrule Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new PerspectiveTransformGrips(typeof(BlockReference), FilterFunction, CornerOnHotGripAction, MiddleOnHotGripAction, true);
                    }
                    return _instance;
                }
            }
            public static bool FilterFunction(Entity Entity)
            {
                return Entity is BlockReference;
            }

            public static void MiddleOnHotGripAction(ObjectId objectid, GripData GripData) { }



            public PerspectiveTransformGrips(Type TargetType, Func<Entity, bool> FilterFunction, Action<ObjectId, GripData> CornerOnHotGripAction, Action<ObjectId, GripData> MiddleOnHotGripAction, bool HideOriginals = true) : base(TargetType, FilterFunction, CornerOnHotGripAction, MiddleOnHotGripAction, HideOriginals)
            {
            }


            public static void CornerOnHotGripAction(ObjectId objectid, GripData GripData)
            {
                using (Transaction tr = Generic.GetDocument().TransactionManager.StartTransaction())
                {
                    if (objectid.GetDBObject() is BlockReference blkRef)
                    {
                        Entity Poly = null;
                        foreach (var ent in GetEntitiesInBlock(blkRef))
                        {
                            if (ent is Polyline po)
                            { Poly = po; }
                        }


                        if (Poly is Polyline poly && GripData is PolyCornerGrip polyGrip)
                        {
                            Debug.WriteLine($"Grip CurrentModeId : {(int)polyGrip.CurrentModeId}");
                            Point2dCollection pts = null;
                            if (((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Default ||
                            ((int)polyGrip.CurrentModeId) == (int)PolyGripOverrule.ModeIdAction.Stretch)
                            {
                                pts = StretchPoint(poly, GripData.GripPoint, PolyGripOverrule.ModeIdAction.Stretch);
                            }




                            //Recreate polyline
                            using (Polyline NewPoly = new Polyline())
                            {
                                foreach (var item in pts)
                                {
                                    NewPoly.AddVertex(item.ToPoint3d());
                                }
                                NewPoly.AddToDrawing();
                                poly.EraseObject();
                            }

                        }
                    }
                    tr.Commit();
                }
            }

            private static Point2dCollection StretchPoint(Polyline poly, Point3d Point, PolyGripOverrule.ModeIdAction Action)
            {
                Point3dCollection EntVertices = new Point3dCollection();
                foreach (Point3d EntVertice in poly.GetPoints())
                {
                    EntVertices.Add(EntVertice);
                }

                using (Polyline NewPoly = new Polyline())
                {
                    for (int i = 0; i < EntVertices.Count - 1; i++)
                    {
                        Point3d WipeoutEntVertice = EntVertices[i];
                        NewPoly.AddVertex(WipeoutEntVertice);
                    }

                    NewPoly.Closed = true;
                    var jig = new PolyGripJig(NewPoly, Point, new Point3dCollection() { Point });
                    var JigResult = jig.Drag();
                    if (JigResult?.Status == PromptStatus.OK)
                    {
                        Point2dCollection pts = new Point2dCollection();
                        foreach (Point3d WipeoutEntVertice in EntVertices)
                        {
                            if (WipeoutEntVertice.IsEqualTo(Point, Generic.MediumTolerance))
                            {
                                pts.Add(JigResult.Value.ToPoint2d());
                            }
                        }
                        return pts;
                    }
                    return null;
                }
            }

            public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
            {
                if (IsApplicable(entity))
                {
                    //  Dont use transaction here, this cause AutoCAD to crash when changing properties : An item with the same key has already been added

                    Point3dCollection AlreadyAddedPoints = new Point3dCollection();
                    var blk = entity as BlockReference;
                    //if (entity is BlockReference blk)
                    //{
                    if (blk != null || true)
                    {
                        using (var tran = blk.Database.TransactionManager.StartTransaction())
                        {
                            foreach (var ent in GetEntitiesInBlock(blk))
                            {
                                if (ent is Polyline po)
                                {
                                    int index = 0;
                                    var poPoints = po.GetPoints();
                                    foreach (var o in poPoints)
                                    {
                                        if (!AlreadyAddedPoints.ContainsTolerance(o, Generic.MediumTolerance))
                                        {
                                            index++;
                                            var grip = new PolyCornerGrip()
                                            {
                                                Index = index,
                                                GripPoint = o,
                                                EntityId = entity.ObjectId,
                                                OnHotGripAction = CornerOnHotGripAction
                                            };
                                            grips.Add(grip);
                                            AlreadyAddedPoints.Add(o);
                                        }
                                    }
                                }
                            }

                            tran.Abort();
                        }
                        return;
                    }

                }

                base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
            }

        }




        public static class PerspectiveTransformFunctions
        {
            public static void Caller()
            {
                Point2d[] SourceRect = new Point2d[4]
                {
                new Point2d(0, 0),
                new Point2d(0, 20),
                new Point2d(15, 20),
                new Point2d(15, 0),
                };
                Point2d[] DestinationRect = new Point2d[4]
                {
                new Point2d(0, 0),
                new Point2d(12.5, 20),
                new Point2d(22.5, 20),
                new Point2d(35, 0),
                };

                GetMatrixTransformation(SourceRect, DestinationRect);
            }

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

            private static double[] GetNormalizationCoefficients(Point2d[] SourceRect, Point2d[] DestinationRect)
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
