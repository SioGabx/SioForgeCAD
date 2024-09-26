using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using System;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class DXFIMPORT
    {
        public static void Import()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (LongOperationProcess LongOperation = new LongOperationProcess())
            {
                foreach (var FileName in GetFiles())
                {
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        try
                        {
                            if (LongOperation.IsCanceled) { return; }
                            Application.DoEvents();
                            string BlocName = System.IO.Path.GetFileNameWithoutExtension(FileName);
                            using (Database dxfDb = new Database(false, true))
                            {
                                if (System.IO.Path.GetExtension(FileName).Equals(".dwg", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dxfDb.ReadDwgFile(FileName, FileOpenMode.OpenForReadAndAllShare, true, null);
                                }
                                else
                                {
                                    dxfDb.DxfIn(FileName, null);
                                    db.Insbase = new Point3d(0, 0, 0);
                                }

                                db.Insert(BlocName, dxfDb, false);
                                BlockReferences.InsertFromName(BlocName, Points.Empty, 0);
                            }
                        }
                        catch (Autodesk.AutoCAD.Runtime.Exception ex)
                        {
                            Generic.WriteMessage($"Impossible d'importer le fichier : {FileName}\n{ex.Message}");
                        }
                        finally
                        {
                            tr.Commit();
                            ed.UpdateScreen();
                        }
                    }
                }
            }
        }

        private static string[] GetFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Fichiers DXF (*.dxf)|*.dxf|Fichiers DWG (*.dwg)|*.dwg",
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                InitialDirectory = Generic.GetCurrentDocumentPath(),
                Title = "Selectionnez des fichiers DXF ou DWG à importer dans le dessin"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileNames;
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }
}
