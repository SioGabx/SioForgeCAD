using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public static class Generic
    {
        public static void ReadWriteToFileResource(string name, string ToFilePath)
        {
            // Determine path
            byte[] ressource_bytes = Properties.Resources.ResourceManager.GetObject(name) as byte[];
            File.WriteAllBytes(ToFilePath, ressource_bytes);
        }

        public static void WriteMessage(object message)
        {
            Editor ed = GetEditor();
            ed.WriteMessage(message.ToString() + "\n");
        }

        public static void LoadLispFromStringCommand(string lispCode)
        {
            Document doc = Generic.GetDocument();
            string loadCommand = $"(eval '{lispCode})";
            doc.SendStringToExecute(loadCommand, true, false, false);
        }



        public static string GetExtensionDLLName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        public static ObjectId AddFontStyle(string font)
        {
            var doc = GetDocument();
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

        public static Size GetCurrentViewSize()
        {
            //https://drive-cad-with-code.blogspot.com/2013/04/how-to-get-current-view-size.html
            //Get current view height
            double h = (double)Application.GetSystemVariable("VIEWSIZE");
            //Get current view width,
            //by calculate current view's width-height ratio
            Point2d screen = (Point2d)Application.GetSystemVariable("SCREENSIZE");
            double w = h * (screen.X / screen.Y);
            return new Size(w, h);
        }



        public static DBObjectCollection Explode(IEnumerable<ObjectId> ObjectsToExplode, bool EraseOriginal = true)
        {
            Database db = GetDatabase();
            Autodesk.AutoCAD.DatabaseServices.TransactionManager tr = db.TransactionManager;

            // Collect our exploded objects in a single collection

            DBObjectCollection objs = new DBObjectCollection();

            // Loop through the selected objects
            foreach (ObjectId ObjectToExplode in ObjectsToExplode)
            {
                Entity ent = (Entity)tr.GetObject(ObjectToExplode, OpenMode.ForRead);
                // Explode the object into our collection
                ent.Explode(objs);
                if (EraseOriginal)
                {
                    ent.UpgradeOpen();
                    ent.Erase();
                }
            }
            return objs;
        }


        public static Transparency GetTransparencyFromAlpha(int Alpha)
        {
            byte AlphaByte = ((byte)(255 * (100 - Alpha) / 100));
            return new Autodesk.AutoCAD.Colors.Transparency(AlphaByte);
        }

        public enum AngleUnit { Radians, Degrees }
        public static double GetUSCRotation(AngleUnit angleUnit)
        {
            var ed = GetEditor();
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

        public static ObjectId AddToDrawing(this Entity entity)
        {
            var db = GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Check if the entity is already in the database
                if (entity.IsErased)
                {
                    acTrans.Abort();
                    return ObjectId.Null;
                }
                acBlkTblRec.AppendEntity(entity);
                acTrans.AddNewlyCreatedDBObject(entity, true);
                acTrans.Commit();
                return entity.ObjectId;
            }
        }


        public static ObjectId AddToDrawingCurrentTransaction(this Entity entity)
        {
            var db = GetDatabase();
            Transaction acTrans = db.TransactionManager.TopTransaction;

            BlockTable acBlkTbl = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

            var objid = acBlkTblRec.AppendEntity(entity);
            acTrans.AddNewlyCreatedDBObject(entity, true);
            return objid;

        }

        /// <summary>
        /// For each loop.
        /// </summary>
        /// <typeparam name="T">The element type of source.</typeparam>
        /// <param name="source">The source collection.</param>
        /// <param name="action">The action.</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }

        public static Document GetDocument()
        {
            return Application.DocumentManager.MdiActiveDocument;
        }
        public static Database GetDatabase()
        {
            return GetDocument().Database;
        }
        public static Editor GetEditor()
        {
            return GetDocument().Editor;
        }

        public static void Command(params object[] args)
        {
            short cmdecho = (short)Application.GetSystemVariable("CMDECHO");
            Application.SetSystemVariable("CMDECHO", 0);
            Editor ed = GetEditor();
            ed.Command(args);
            Application.SetSystemVariable("CMDECHO", cmdecho);
        }

        public static async Task CommandAsync(params object[] args)
        {
            short cmdecho = (short)Application.GetSystemVariable("CMDECHO");
            Application.SetSystemVariable("CMDECHO", 0);
            Editor ed = GetEditor();
            await ed.CommandAsync(args);
            Application.SetSystemVariable("CMDECHO", cmdecho);
        }


    }
}