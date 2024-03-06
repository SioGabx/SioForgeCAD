using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Circles
    {
        public static ObjectId Draw(Point3d center, double radius, int ColorIndex = 256)
        {
            using (Circle acLine = new Circle(center, Vector3d.ZAxis, radius))
            {
                return Draw(acLine, ColorIndex);
            }
            
        }

        public static ObjectId Draw(Points center, double radius, int ColorIndex = 256)
        {
            return Draw(center.SCG, radius, ColorIndex);
        }

        public static ObjectId Draw(Circle acLine, int? ColorIndex = 256)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord acBlkTblRec = Generic.GetCurrentSpaceBlockTableRecord(acTrans);

                if (ColorIndex != null)
                {
                    acLine.ColorIndex = ColorIndex ?? 0;
                }
                // Add the line to the drawing
                acBlkTblRec.AppendEntity(acLine);
                acTrans.AddNewlyCreatedDBObject(acLine, true);

                // Commit the changes and dispose of the transaction
                acTrans.Commit();
                return acLine.ObjectId;
            }
        }
    }
}
