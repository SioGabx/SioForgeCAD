using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public static class Generic
    {
        public static void ReadResource(string name, string tofile)
        {
            // Determine path
            byte[] ressource_bytes = Properties.Resources.ResourceManager.GetObject(name) as byte[];
            File.WriteAllBytes(tofile, ressource_bytes);
        }

        public static ObjectId AddFontStyle(string font)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            using (Transaction newTransaction = doc.TransactionManager.StartTransaction())
            {
                BlockTable newBlockTable;
                newBlockTable = newTransaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord newBlockTableRecord;
                newBlockTableRecord = (BlockTableRecord)newTransaction.GetObject(newBlockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                TextStyleTable newTextStyleTable = newTransaction.GetObject(doc.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

                if (!newTextStyleTable.Has(font.ToUpperInvariant()))  //The TextStyle is currently not in the database
                {
                    newTextStyleTable.UpgradeOpen();
                    TextStyleTableRecord newTextStyleTableRecord = new TextStyleTableRecord
                    {
                        FileName = font,
                        Name = font.ToUpperInvariant()
                    };
                    newTextStyleTable.Add(newTextStyleTableRecord);
                    newTransaction.AddNewlyCreatedDBObject(newTextStyleTableRecord, true);
                }

                newTransaction.Commit();
                return newTextStyleTable[font];
            }
        }

        public static DBObjectCollection Explode(IEnumerable<ObjectId> ObjectsToExplode, bool EraseOriginal = true)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tr = db.TransactionManager;

            // Collect our exploded objects in a single collection

            DBObjectCollection objs = new DBObjectCollection();

            // Loop through the selected objects
            foreach (ObjectId ObjectToExplode in ObjectsToExplode)
            {
                Entity ent = (Entity)tr.GetObject(ObjectToExplode, OpenMode.ForRead);
                // Explode the object into our collection
                ent.Explode(objs);
                // Erase the original, if requested
                if (EraseOriginal)
                {
                    ent.UpgradeOpen();
                    ent.Erase();
                }
            }
            return objs;
        }

        public static void Erase(ObjectId ObjectToErase)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                Entity ent = (Entity)tr.GetObject(ObjectToErase, OpenMode.ForWrite);
                ent.Erase(true);
                tr.Commit();
            }
        }

        public enum AngleUnit { Radians, Degrees }
        public static double GetUSCRotation(AngleUnit angleUnit)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            Matrix3d ucsCur = ed.CurrentUserCoordinateSystem;
            CoordinateSystem3d cs = ucsCur.CoordinateSystem3d;
            double ucs_rotAngle = cs.Xaxis.AngleOnPlane(new Plane(Point3d.Origin, Vector3d.ZAxis));
            if (angleUnit == AngleUnit.Radians)
            {
                return ucs_rotAngle;
            }
            double ucs_angle_degres = ucs_rotAngle * 180 / Math.PI;
            return ucs_angle_degres;
        }




    }
}
