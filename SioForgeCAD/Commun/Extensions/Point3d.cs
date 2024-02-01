using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SioForgeCAD.Commun.Extensions
{
    public static class Point3dExtensions
    {
        public static Point2d ToPoint2d(this Point3d p)
        {
            return new Point2d(p.X, p.Y);
        }
        public static Point3d ToPoint3d(this Point2d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }
        public static Point3d Flatten(this Point3d p)
        {
            return new Point3d(p.X, p.Y, 0);
        }

        public static Points ToPoints(this Point3d p)
        {
            return Points.From3DPoint(p);
        }

        public static Point3d GetMiddlePoint(this Point3d A, Point3d B)
        {
            double X = (A.X + B.X) / 2;
            double Z = (A.Z + B.Z) / 2;
            double Y = (A.Y + B.Y) / 2;
            return new Point3d(X, Y, Z);
        }

        public static double GetAngleWith(this Point3d A, Point3d B)
        {
            using (Line line = new Line(A, B))
            {
                return line.Angle;
            }
        }

        public static Point3d TranformToBlockReferenceTransformation(this Point3d OriginPoint, BlockReference blkRef)
        {
            Point3d selectedPointInBlockRefSpace = OriginPoint.TransformBy(blkRef.BlockTransform.Inverse());
            // Matrix3d rotationMatrix = Matrix3d.Rotation(Math.PI, Vector3d.ZAxis, Point3d.Origin);
            return selectedPointInBlockRefSpace;//.TransformBy(rotationMatrix);
        }

    }
}
