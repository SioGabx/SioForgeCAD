using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using MenuItem = Autodesk.AutoCAD.Windows.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;

namespace SioForgeCAD.Functions
{
    public static class CONVERTIMAGETOOLE
    {
        public static class ContextMenu
        {
            private static ContextMenuExtension cme;

            public static void Attach()
            {
                cme = new ContextMenuExtension();
                MenuItem mi = new MenuItem("Convertir en OLE (embed)");
                mi.Click += OnExecute;
                cme.MenuItems.Add(mi);
                RXClass rxc = RXObject.GetClass(typeof(RasterImage));
                Application.AddObjectContextMenuExtension(rxc, cme);
            }

            public static void Detach()
            {
                RXClass rxc = RXObject.GetClass(typeof(RasterImage));
                Application.RemoveObjectContextMenuExtension(rxc, cme);
            }

            private static void OnExecute(object o, EventArgs e)
            {
                Generic.SendStringToExecute("SIOFORGECAD.CONVERTIMAGETOOLE");
            }
        }

        public static void RasterToOle()
        {
            Editor ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "Selectionnez une image",
                SinglePickInSpace = false,
                SingleOnly = false,
                RejectObjectsOnLockedLayers = true
            };
            TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Start, "IMAGE")
                };
            var ent = ed.GetSelection(selectionOptions, new SelectionFilter(filterList));
            if (ent.Status != PromptStatus.OK) { return; }

            foreach (ObjectId RasterObjectId in ent.Value.GetObjectIds())
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    if (RasterObjectId.GetDBObject() is RasterImage rasterImage)
                    {
                        var rasterImageColor = rasterImage.GetSystemDrawingColor();
                        System.Drawing.Image bitmap = System.Drawing.Image.FromFile(rasterImage.Path);
                        bool ImageHasAlpha = bitmap.PixelFormat.HasFlag(PixelFormat.Alpha);
                        const string HasAlphaWarningMessage = "la transparence de l'image sera supprimée et remplacée par la couleur de l'object raster";
                        bool ImageIsRotated = rasterImage.Rotation > 0;
                        const string IsRotatedWarningMessage = "les OLE ne supportent pas les rotations. Un fond sera appliqué de la couleur de l'object raster";
                        if (ImageHasAlpha || ImageIsRotated)
                        {
                            string JoinedMessage = "Attention : ";
                            if (ImageHasAlpha && ImageIsRotated)
                            {
                                JoinedMessage += $"\n - {HasAlphaWarningMessage}\n - {IsRotatedWarningMessage}";
                            }
                            else if (ImageHasAlpha)
                            {
                                JoinedMessage += HasAlphaWarningMessage;
                            }
                            else if (ImageIsRotated)
                            {
                                JoinedMessage += IsRotatedWarningMessage;
                            }
                            JoinedMessage += $"\nVoullez-vous utiliser un fond de la couleur de l'object raster ? ({rasterImageColor.R},{rasterImageColor.G},{rasterImageColor.B}). Un fond blanc sera appliqué dans le cas contraire";
                            var AskContinue = MessageBox.Show(JoinedMessage, Generic.GetExtensionDLLName(), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                            if (AskContinue != DialogResult.Cancel)
                            {
                                return;
                            }
                            else if (AskContinue != DialogResult.No) { rasterImageColor = System.Drawing.Color.White; }
                        }
                        Debug.WriteLine("Bitmap Size :" + bitmap.GetImageFileSize());
                        var ClipBackup = System.Windows.Clipboard.GetDataObject();
                        using (var RotatedImage = bitmap.RotateImage(rasterImage.Rotation, rasterImageColor))
                        {
                            try
                            {
                                System.Windows.Clipboard.Clear();
                                System.Windows.Clipboard.SetImage(RotatedImage.ToBitmapSource());
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception ex)
                            {
                                Generic.WriteMessage(ex.Message);
                                continue;
                            }
                        }
                        var RasterImagePosition = rasterImage.Position;
                        //Paste into the drawing because we cannot create a Ole2Frame in NET
                        Generic.Command("_pasteclip", RasterImagePosition);
                        try
                        {
                            System.Windows.Clipboard.SetDataObject(ClipBackup);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                        //Get last created entity of type Ole2Frame
                        var InsertedOLEObjectId = db.EntLast(typeof(Ole2Frame));
                        if (InsertedOLEObjectId.GetDBObject(OpenMode.ForWrite) is Ole2Frame InsertedOLE)
                        {
                            //Move OLE at the right position
                            // Positionner l'OLE à l'emplacement de l'image raster
                            var rasterExtent = rasterImage.GetExtents();
                            InsertedOLE.Position3d = rasterExtent.ToRectangle3d();

                            // Définir les propriétés de base de l'OLE
                            InsertedOLE.Layer = "0";
                            InsertedOLE.ColorIndex = 0; // ByBlock
                            InsertedOLE.Transparency = new Autodesk.AutoCAD.Colors.Transparency(TransparencyMethod.ByBlock);
                            InsertedOLE.Linetype = "BYBLOCK";
                            InsertedOLE.LineWeight = LineWeight.ByBlock;

                            // Créer un bloc unique contenant l'OLE
                            string oleFileName = new System.IO.FileInfo(rasterImage.Path).Name;
                            string blockName = BlockReferences.GetUniqueBlockName($"OLE_{oleFileName}");

                            var blockOrigin = Points.From3DPoint(rasterExtent.MinPoint);
                            var oleClone = InsertedOLE.Clone() as DBObject;

                            BlockReferences.Create(blockName, "OLE Definition", new DBObjectCollection { oleClone }, blockOrigin, false, BlockScaling.Uniform);

                            // Insérer le bloc et copier les propriétés de l'image raster
                            var blkObj = BlockReferences.InsertFromName(blockName, blockOrigin, 0, null, rasterImage.Layer, rasterImage.Database.BlockTableId.GetDBObject(OpenMode.ForWrite) as BlockTableRecord);

                            var blkRef = blkObj.GetDBObject(OpenMode.ForWrite) as BlockReference;
                            rasterImage.CopyPropertiesTo(blkRef);

                            // Nettoyer les objets sources
                            rasterImage.EraseObject();
                            InsertedOLE.EraseObject();

                        }
                        else
                        {
                            Generic.WriteMessage("Une erreur s'est produite lors de la convertion.");
                            tr.Abort();
                        }
                    }
                    tr.Commit();
                }
            }
        }

        public static void TransformToFitBoundingBox(Ole2Frame ent, Extents3d FitBoundingBox)
        {
            var FitBoundingBoxSize = FitBoundingBox.Size();
            var EntExtendSize = ent.GetExtents().Size();
            double HeightRatio = FitBoundingBoxSize.Height / EntExtendSize.Height;
            double WidthRatio = FitBoundingBoxSize.Width / EntExtendSize.Width;
            ent.LockAspect = Math.Abs(HeightRatio - WidthRatio) <= Generic.LowTolerance.EqualPoint;
            ent.WcsHeight = FitBoundingBoxSize.Height;
            ent.WcsWidth = FitBoundingBoxSize.Width;
        }
    }
}
