using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public static class Generic
    {
        public static readonly Tolerance LowTolerance = new Tolerance(1e-3, 1e-3); //0.001
        public static readonly Tolerance MediumTolerance = new Tolerance(1e-5, 1e-5); //0.00001
        public static void ReadWriteToFileResource(string name, string ToFilePath)
        {
            // Determine path
            byte[] ressource_bytes = Properties.Resources.ResourceManager.GetObject(name) as byte[];
            File.WriteAllBytes(ToFilePath, ressource_bytes);
        }

        public static void WriteMessage(object message)
        {
            Editor ed = GetEditor();
            ed.WriteMessage($"\n{message}\n");
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
            var db = GetDatabase();
            using (Transaction newTransaction = doc.TransactionManager.StartTransaction())
            {
                BlockTable newBlockTable = newTransaction.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord newBlockTableRecord = Generic.GetCurrentSpaceBlockTableRecord(newTransaction);
                TextStyleTable newTextStyleTable = newTransaction.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

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

        public static Transparency GetTransparencyFromAlpha(int Alpha)
        {
            byte AlphaByte = ((byte)(255 * (100 - Alpha) / 100));
            return new Transparency(AlphaByte);
        }

        public static Document GetDocument()
        {
            return Application.DocumentManager.MdiActiveDocument;
        }
        public static Database GetDatabase()
        {
            return HostApplicationServices.WorkingDatabase;
        }

        public static BlockTableRecord GetCurrentSpaceBlockTableRecord(Transaction acTrans)
        {
            //https://spiderinnet1.typepad.com/blog/2012/03/autocad-net-api-modelspacepaperspacecurrentspace-and-entity-creation.html
            //Use db.CurrentSpaceId instead of bt[BlockTableRecord.ModelSpace
            Database db = Generic.GetDatabase();
            return acTrans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
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