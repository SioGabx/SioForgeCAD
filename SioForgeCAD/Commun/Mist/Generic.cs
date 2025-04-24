using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Mist;
using System.Diagnostics;
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
            if (!Files.IsFileLockedOrReadOnly(ToFilePath))
            {
                File.WriteAllBytes(ToFilePath, ressource_bytes);
            }
        }

        public static string GetCurrentDocumentPath()
        {
            Document doc = GetDocument();
            if (Path.GetDirectoryName(doc.Name).Equals(string.Empty)) { return ""; }
            HostApplicationServices hs = HostApplicationServices.Current;
            string FilePath = hs.FindFile(doc.Name, doc.Database, FindFileHint.Default);
            string directory = new FileInfo(FilePath).Directory.FullName;
            Debug.WriteLine(directory);
            return directory;
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

        public static string GetExtensionDLLLocation()
        {
            return Assembly.GetExecutingAssembly().Location;
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

        public static DwgVersion GetSaveVersion()
        {
            //AC1015 = AutoCAD 2000 
            //AC1018 = AutoCAD 2004 
            //AC1021 = AutoCAD 2007 
            //AC1024 = AutoCAD 2010 
            //AC1027 = AutoCAD 2013 
            //AC1032 = AutoCAD 2018 
            //var ucm = Application.UserConfigurationManager;
            //var profile = ucm.OpenCurrentProfile();
            //var section = profile.OpenSubsection("General");
            //var format = section.ReadProperty("DefaultFormatForSave", 0);

            var db = Generic.GetDatabase();
            return db.OriginalFileSavedByVersion;

            // return Application.DocumentManager.DefaultFormatForSave;
        }
        public static DocumentLock GetLock()
        {
            var doc = Generic.GetDocument();
            return GetLock(doc);
        }

        public static DocumentLock GetLock(this Document doc)
        {
            if (doc.LockMode() == DocumentLockMode.None)
            {
                return doc.LockDocument();
            }
            return null;
        }

        public static Database GetDatabase()
        {
            return HostApplicationServices.WorkingDatabase;
        }

        public static BlockTableRecord GetCurrentSpaceBlockTableRecord(Transaction acTrans, OpenMode openMode = OpenMode.ForWrite)
        {
            //https://spiderinnet1.typepad.com/blog/2012/03/autocad-net-api-modelspacepaperspacecurrentspace-and-entity-creation.html
            //Use db.CurrentSpaceId instead of bt[BlockTableRecord.ModelSpace
            Database db = Generic.GetDatabase();
            return acTrans.GetObject(db.CurrentSpaceId, openMode) as BlockTableRecord;
        }

        public static Editor GetEditor()
        {
            return GetDocument().Editor;
        }

        public static void SendStringToExecute(string Command, bool Echo = true)
        {
            Document doc = Generic.GetDocument();
            doc.SendStringToExecute(string.Concat(Command, ' '), true, false, Echo);
        }

        public static void SetSystemVariable(string Name, object Value, bool EchoChanges = true)
        {
            var OldValue = Application.TryGetSystemVariable(Name);
            if (OldValue.ToString() != Value.ToString())
            {
                if (EchoChanges) { Generic.WriteMessage($"Changement de la variable {Name} de {OldValue} à {Value}."); }
                Application.SetSystemVariable(Name, Value);
            }
        }

        public static void Command(params object[] args)
        {
            short cmdecho = (short)Application.GetSystemVariable("CMDECHO");
            Application.SetSystemVariable("CMDECHO", 0);
            Editor ed = GetEditor();
            ed.Command(args);
            Application.SetSystemVariable("CMDECHO", cmdecho);
        }

        public static void Regen()
        {
            SendStringToExecute("_.REGEN", false);
        }
        public static void UpdateScreen()
        {
            Generic.GetEditor().UpdateScreen();
            Autodesk.AutoCAD.ApplicationServices.Core.Application.UpdateScreen();
        }

        public static async Task CommandAsync(params object[] args)
        {
            short cmdecho = (short)Application.GetSystemVariable("CMDECHO");
            Application.SetSystemVariable("CMDECHO", 0);
            Editor ed = GetEditor();
            await ed.CommandAsync(args);
            Application.SetSystemVariable("CMDECHO", cmdecho);
        }

        public static void CommandInApplicationContext(params object[] args)
        {
            try
            {
                Application.DocumentManager.ExecuteInApplicationContext((_) => Command(args), null);
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage($"Exception: {ex.Message}");
            }
        }

        public static async Task CommandAsyncInCommandContext(params object[] args)
        {
            //Method from https://through-the-interface.typepad.com/through_the_interface/2015/03/autocad-2016-calling-commands-from-autocad-events-using-net.html
            //Replace Document.SendStringToExecute()
            try
            {
                // Ask AutoCAD to execute our command in the right context
                await Application.DocumentManager.ExecuteInCommandContextAsync(async (_) => await CommandAsync(args), null);
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage($"Exception: {ex.Message}");
            }
        }
    }
}