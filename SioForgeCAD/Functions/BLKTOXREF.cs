using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class BLKTOXREF
    {
        public static void Convert()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, true, true))
            {
                return;
            }
            using (Generic.GetLock())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId blockRefId in ObjectIds)
                {
                    if (!(blockRefId.GetDBObject(OpenMode.ForRead) is BlockReference blockRef))
                    {
                        return;
                    }

                    BlockTableRecord blockTableRecord = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (blockTableRecord == null)
                    {
                        ed.WriteMessage("\nFailed to get the block table record.");
                        return;
                    }

                    SaveFileDialog saveFileDialog = new SaveFileDialog()
                    {
                        AddExtension = true,
                        DefaultExt = "dwg",
                        ValidateNames = true,
                        FileName = blockTableRecord.Name + ".dwg",
                        Filter = "Fichiers DWG (.dwg)|*.dwg"
                    };

                    if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }
                    string dwgFileName = Path.GetFullPath(saveFileDialog.FileName);

                    using (Database newDb = new Database(true, true))
                    {
                        db.Wblock(newDb, new ObjectIdCollection() { blockRefId }, Point3d.Origin, DuplicateRecordCloning.Replace);

                        newDb.SaveAs(dwgFileName, DwgVersion.Current);
                        ed.WriteMessage($"\nBlock saved as {dwgFileName}");
                    }

                    const int MaxWaitMs = 5000;
                    int CurrentWaitMs = 0;
                    while (!File.Exists(dwgFileName) && CurrentWaitMs < MaxWaitMs && Files.IsFileLockedOrReadOnly(new FileInfo(dwgFileName)))
                    {
                        const int WaitTimeIncrement = 100;
                        CurrentWaitMs += WaitTimeIncrement;
                        Thread.Sleep(WaitTimeIncrement);
                    }
                    //TODO : Check name if in drawing

                    ObjectId xg = db.AttachXref(dwgFileName, Path.GetFileNameWithoutExtension(dwgFileName));
                    if (xg == ObjectId.Null)
                    {
                        Generic.WriteMessage("\nFailed to attach Xref.");
                        return;
                    }
                    var bref = new BlockReference(Point3d.Origin, xg);
                    bref.AddToDrawing();
                    blockRef.EraseObject();
                    tr.Commit();
                }
            }
        }
    }
}
