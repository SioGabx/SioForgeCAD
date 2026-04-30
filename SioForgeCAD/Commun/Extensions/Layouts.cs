using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Commun.Extensions
{
    public static class LayoutsExtensions
    {
        public static void CloneLayout(this Layout sourceLayout, string newLayoutName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord sourceBtr = (BlockTableRecord)tr.GetObject(sourceLayout.BlockTableRecordId, OpenMode.ForRead);
                LayoutManager lm = LayoutManager.Current;

                ObjectId newLayoutId = lm.CreateLayout(newLayoutName);
                Layout newLayout = (Layout)tr.GetObject(newLayoutId, OpenMode.ForWrite);
                BlockTableRecord newBtr = (BlockTableRecord)tr.GetObject(newLayout.BlockTableRecordId, OpenMode.ForWrite);

                IdMapping mapping = new IdMapping();
                db.DeepCloneObjects(sourceBtr.Cast<ObjectId>().ToObjectIdCollection(), newBtr.ObjectId, mapping, false);

                // 3. Copie les réglages de tracé
                newLayout.CopyFrom(sourceLayout);
                lm.CurrentLayout = newLayoutName;

                tr.Commit();
            }
        }



        public static Bitmap GetLayoutSnapshot(this Layout lay, Extents3d ext, int width, int height)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Manager gsm = doc.GraphicsManager;

            KernelDescriptor descriptor = new KernelDescriptor();
            descriptor.addRequirement(Autodesk.AutoCAD.UniqueString.Intern("3D Drawing"));
            GraphicsKernel kernel = Manager.AcquireGraphicsKernel(descriptor);

            using (var tr = lay.Database.TransactionManager.StartOpenCloseTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(lay.BlockTableRecordId, OpenMode.ForRead);

                using (View view = new View())
                {
                    double w = ext.MaxPoint.X - ext.MinPoint.X;
                    double h = ext.MaxPoint.Y - ext.MinPoint.Y;
                    Point3d center = new Point3d(ext.MinPoint.X + (w / 2), ext.MinPoint.Y + (h / 2), 0);

                    // Position de la caméra (Z+1) pour ne pas être "dans" le dessin
                    Point3d eyePosition = new Point3d(center.X, center.Y, center.Z + 1.0);

                    // Cadrage de la vue
                    view.SetView(eyePosition, center, Vector3d.YAxis, w, h);

                    using (Device dev = gsm.CreateAutoCADOffScreenDevice(kernel))
                    {
                        dev.OnSize(new Size(width, height));
                        dev.BackgroundColor = System.Drawing.Color.White;
                        dev.Add(view);

                        using (Model model = gsm.CreateAutoCADModel(kernel))
                        {
                            view.Add(btr, model);
                            dev.Update();
                            view.Update();

                            return view.GetSnapshot(new Rectangle(0, 0, width, height));
                        }
                    }
                }
            }
        }

        public static Bitmap RenderLayoutSnapshot(this Layout layout)
        {
            var db = Generic.GetDatabase();
            int bmpW = 100;
            int bmpH = 100;
            using (Transaction transaction = db.TransactionManager.StartTransaction())
            {
                try
                {
                    Extents3d ext;
                    if (layout.ModelType)
                    {
                        db.UpdateExt(true);
                        ext = new Extents3d(db.Extmin, db.Extmax);
                    }
                    else
                    {
                        ext = layout.GetPaperExtents().ToExtents3d();
                    }

                    double realW = ext.MaxPoint.X - ext.MinPoint.X;
                    double realH = ext.MaxPoint.Y - ext.MinPoint.Y;

                    if (realW <= 0.001 || realH <= 0.001)
                    {
                        return null;
                    }

                    double ratio = Math.Min(512.0 / realW, 512.0 / realH);

                    bmpW = (int)(realW * ratio);
                    bmpH = (int)(realH * ratio);
                    return GetLayoutSnapshot(layout, ext, bmpW, bmpH);
                }
                catch { }
                finally { transaction.Commit(); }
            }
            return new Bitmap(bmpW, bmpH);
        }

        public static bool IsModel(this Layout layout)
        {
            return layout.LayoutName == "Model";
        }

        public static void ExtractLayoutEntities(this Layout layout, out List<Viewport> viewports, out ObjectIdCollection paperIds)
        {
            viewports = new List<Viewport>();
            paperIds = new ObjectIdCollection();

            BlockTableRecord layoutBtr = (BlockTableRecord)layout.BlockTableRecordId.GetDBObject(OpenMode.ForRead);
            foreach (ObjectId entId in layoutBtr)
            {
                Entity ent = (Entity)entId.GetDBObject(OpenMode.ForRead);
                if (ent is Viewport vp)
                {
                    if (vp.Number != 1)
                    {
                        viewports.Add(vp);
                    }
                }
                else
                {
                    paperIds.Add(entId);
                }
            }
        }

        public static void PublishLayouts(this Layout layout, string outputPdfPath)
        {
            new Layout[1] { layout }.PublishLayouts(outputPdfPath);
        }

        public static void PublishLayouts(this IEnumerable<Layout> layouts, string outputPdfPath)
        {
            //TestPublish(layouts, outputPdfPath);
            //return;
            if (layouts?.Any() != true) return;

            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            string originalDocPath = doc.Name;
            string drawingName = Path.GetFileNameWithoutExtension(doc.Name);
            string outFileName = Path.GetFileNameWithoutExtension(outputPdfPath);

            // 1. GESTION DU FICHIER TEMPORAIRE
            //string tempFileName = $"{drawingName.Truncate(100, "")}_PLOT_{DateTime.Now.Ticks}_{Guid.NewGuid()}";
            //string tempDwgPath = Path.Combine(Path.GetTempPath(), $"{tempFileName}.dwg");
            string tempFileName = $"{drawingName}_PlottingService";
            string tempFolderName = $"{drawingName.Truncate(100, "")}_PLOT_{DateTime.Now.Ticks}_{Guid.NewGuid()}";
            string tempFolderPath = Path.Combine(Path.GetTempPath(), tempFolderName);
            System.IO.Directory.CreateDirectory(tempFolderPath);
            string tempDwgPath = Path.Combine(tempFolderPath, $"{tempFileName}.dwg");





            try
            {
                // On sauvegarde le fichier sous un nom temp pour que PublishExecute le trouve
                db.SaveAs(tempDwgPath, DwgVersion.Current);
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage($"Erreur de sauvegarde temporaire : {ex.Message}");
                return;
            }

            bool isMultiPage = layouts.Count() > 1;
            string dsdFilePath = Path.ChangeExtension(tempDwgPath, ".dsd");

            // 2. FORCER LE TRACÉ AU PREMIER PLAN (Indispensable pour supprimer le tempDwg après)
            short bgPlot = (short)Generic.GetSystemVariable("BACKGROUNDPLOT");
            Generic.SetSystemVariable("BACKGROUNDPLOT", 0, false);

            try
            {
                using (DsdData dsdFileData = new DsdData())
                using (DsdEntryCollection dsdEntries = new DsdEntryCollection())
                {
                    foreach (var layout in layouts)
                    {
                        using (DsdEntry entry = new DsdEntry()
                        {
                            DwgName = tempDwgPath,
                            Layout = layout.LayoutName,
                            Title = isMultiPage ? layout.LayoutName : outFileName,
                            NpsSourceDwg = "",
                            Nps = "",
                        })
                        {
                            dsdEntries.Add(entry);
                        }
                    }
                    dsdFileData.SetDsdEntryCollection(dsdEntries);
                    dsdFileData.ProjectPath = Path.GetDirectoryName(outputPdfPath);
                    dsdFileData.LogFilePath = Path.Combine(tempFolderPath, $"{tempFileName}_publish.log");
                    dsdFileData.SheetType = isMultiPage ? SheetType.MultiPdf : SheetType.SinglePdf;
                    dsdFileData.DestinationName = outputPdfPath;
                    dsdFileData.IsHomogeneous = false;
                    dsdFileData.PromptForDwfName = false;
                    dsdFileData.PlotStampOn = false;

                    if (File.Exists(dsdFilePath)) File.Delete(dsdFilePath);

                    // On écrit le fichier DSD, on le force en texte brut, puis on le relit.
                    dsdFileData.WriteDsd(dsdFilePath);

                    string dsdText = File.ReadAllText(dsdFilePath);

                    dsdText = dsdText.Replace("PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
                    //dsdText = dsdText.Replace("IncludeLayer=FALSE", "IncludeLayer=TRUE");
                    File.WriteAllText(dsdFilePath, dsdText);

                    // On recharge le DSD modifié
                    dsdFileData.ReadDsd(dsdFilePath);

                    PlotConfig plotConfig = null;

                    if (isMultiPage)
                    {
                        string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
                        string currentProfile = Application.GetSystemVariable("CPROFILE") as string;

                        if (!string.IsNullOrEmpty(currentProfile))
                        {
                            string regPath = $@"{sProdKey}\Profiles\{currentProfile}\Dialogs\AcPublishDlg";
                            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                            {
                                object initialDirectory = key?.GetValue("SelectedPdfPlotter");
                                if (initialDirectory != null)
                                {
                                    var configInfo = PlotConfigManager.Devices
                                        .Cast<PlotConfigInfo>()
                                        .FirstOrDefault(t => Path.GetFileNameWithoutExtension(t.DeviceName) == initialDirectory.ToString());

                                    if (configInfo != null)
                                        plotConfig = PlotConfigManager.SetCurrentConfig(configInfo.DeviceName);
                                }
                            }
                        }
                    }
                    else
                    {
                        var firstLayout = layouts.FirstOrDefault();
                        if (firstLayout.IsPlotDeviceAvailable())
                        {
                            plotConfig = PlotConfigManager.SetCurrentConfig(firstLayout.PlotConfigurationName);
                        }
                        else
                        {
                            return;
                        }
                    }


                    if (File.Exists(outputPdfPath))
                    {
                        try
                        {
                            File.Delete(outputPdfPath);
                        }
                        catch (System.Exception ex)
                        {
                            Generic.WriteMessage($"Impossible de remplacer le fichier PDF (il est peut-être ouvert) : {ex.Message}");
                            return;
                        }
                    }


                    Application.Publisher.PublishExecute(dsdFileData, plotConfig);
                }
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage(ex.Message);
            }
            finally
            {
                Generic.SetSystemVariable("BACKGROUNDPLOT", bgPlot, false);
                try { if (File.Exists(dsdFilePath)) File.Delete(dsdFilePath); } catch { }
                try { if (File.Exists(tempDwgPath)) File.Delete(tempDwgPath); } catch { }
            }
        }

        public static bool IsPlotDeviceAvailable(this Layout lay)
        {
            var deviceName = lay.PlotConfigurationName;
            var availableDevices = PlotConfigManager.Devices.ToList<PlotConfigInfo>().ConvertAll(t => t.DeviceName);
            availableDevices.RemoveAt(0); //first one is none
            return !string.IsNullOrEmpty(deviceName) && availableDevices.Contains(deviceName);
        }

        public static List<(string LayoutName, string Device)> IsPlotsDeviceAvailable(this IEnumerable<Layout> lays)
        {
            List<(string LayoutName, string Device)> list = new List<(string LayoutName, string Device)>();
            foreach (var lay in lays)
            {
                if (!IsPlotDeviceAvailable(lay))
                {
                    list.Add((lay.LayoutName, lay.PlotConfigurationName));
                }
            }
            return list;
        }


        public static Extents2d GetPaperExtents(this Layout layout)
        {
            double paperWidth = layout.PlotPaperSize.X;
            double paperHeight = layout.PlotPaperSize.Y;

            Point2d marginMin = layout.PlotPaperMargins.MinPoint; // Marges (Gauche, Bas) en portrait
            Point2d marginMax = layout.PlotPaperMargins.MaxPoint; // Marges (Droite, Haut) en portrait
            Point2d origin = layout.PlotOrigin; // Décalage de l'origine (X, Y)

            double minX;
            double minY;

            // L'origine exacte dépend de la rotation, car les marges "tournent" physiquement avec le papier
            switch (layout.PlotRotation)
            {
                case PlotRotation.Degrees000: // Portrait
                    minX = origin.X - marginMin.X;
                    minY = origin.Y - marginMin.Y;
                    break;

                case PlotRotation.Degrees090: // Paysage
                    paperWidth = layout.PlotPaperSize.Y;
                    paperHeight = layout.PlotPaperSize.X;
                    // Rotation anti-horaire à 90° :
                    minX = origin.X - marginMax.Y; // L'ancienne marge du Haut devient la marge de Gauche
                    minY = origin.Y - marginMin.X; // L'ancienne marge de Gauche devient la marge du Bas
                    break;

                case PlotRotation.Degrees180: // Portrait inversé
                    minX = origin.X - marginMax.X;
                    minY = origin.Y - marginMax.Y;
                    break;

                case PlotRotation.Degrees270: // Paysage inversé
                    paperWidth = layout.PlotPaperSize.Y;
                    paperHeight = layout.PlotPaperSize.X;
                    minX = origin.X - marginMin.Y;
                    minY = origin.Y - marginMax.X;
                    break;

                default:
                    minX = origin.X - marginMin.X;
                    minY = origin.Y - marginMin.Y;
                    break;
            }

            double maxX = minX + paperWidth;
            double maxY = minY + paperHeight;
            return new Extents2d(minX, minY, maxX, maxY);
        }

        public static Polyline GetPaperFrame(this Layout layout)
        {
            var PaperExtents = layout.GetPaperExtents();
            return PaperExtents.GetGeometry();
        }

    }
}
