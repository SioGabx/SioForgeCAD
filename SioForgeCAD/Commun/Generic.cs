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
        public static Tolerance Tolerance = new Tolerance(1e-3, 1e-3);
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








        public static Transparency GetTransparencyFromAlpha(int Alpha)
        {
            byte AlphaByte = ((byte)(255 * (100 - Alpha) / 100));
            return new Autodesk.AutoCAD.Colors.Transparency(AlphaByte);
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