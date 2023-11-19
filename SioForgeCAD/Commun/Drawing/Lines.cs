using Autodesk.AutoCAD.DatabaseServices;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Lines
    {
        public static ObjectId SingleLine(Points start, Points end, int ColorIndex = 256)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            ObjectId returnObjectId;
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                // Define the new line
                using (Line acLine = new Line(start.SCG, end.SCG))
                {
                    //angleR = Vector3d.XAxis.GetAngleTo(acLine.GetFirstDerivative(start), Vector3d.ZAxis);
                    acLine.ColorIndex = ColorIndex;
                    // Add the line to the drawing
                    acBlkTblRec.AppendEntity(acLine);
                    acTrans.AddNewlyCreatedDBObject(acLine, true);
                    returnObjectId = acLine.ObjectId;
                }
                // Commit the changes and dispose of the transaction
                acTrans.Commit();
            }
            return returnObjectId;
        }
    }
}
