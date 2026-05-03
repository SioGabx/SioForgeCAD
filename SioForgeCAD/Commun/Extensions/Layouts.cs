using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        //https://github.com/cadwiki/cadwiki-nuget/blob/a98c59f2715ab9a3dc640986c8599ac47ce07be1/cadwiki-nuget/cadwiki.AC/Plotters/PlotterMultiPage.cs#L12
        public static void MultiSheetPlotterOld(string outputFileName, ObjectIdCollection allLayouts)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null || doc.IsDisposed)
            {
                System.Windows.MessageBox.Show("Drawing is missing or failed to load. Cancelled.");
                return;
            }
            Editor ed = doc.Editor;

            Database db = doc.Database;
            if (db == null || db.IsDisposed)
            {
                System.Windows.MessageBox.Show("Drawing database is missing or failed to load. Cancelled.");
                return;
            }
            using (Transaction tr = db.TransactionManager.StartTransaction())// <--- TODO fix NullReferenceException 
            {
                if (tr == null || tr.IsDisposed)
                {
                    System.Windows.MessageBox.Show("Transaction is missing or failed to initialize. Cancelled.");
                    return;
                }
                PlotInfoValidator plotInfoValidator = new PlotInfoValidator
                {
                    MediaMatchingPolicy = MatchingPolicy.MatchEnabled
                };
                if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
                {
                    ed.WriteMessage("\nCancelled. Printer is busy (another plot job is in progress).\n");
                    tr.Commit();
                    return;
                }

                using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
                {
                    if (allLayouts == null || allLayouts.IsDisposed)
                    {
                        System.Windows.MessageBox.Show("\nPlot object is empty. Skipped.\n");
                        plotEngine.Dispose();
                        tr.Commit();
                        return;
                    }
                    if (allLayouts.Count == 0)
                    {
                        System.Windows.MessageBox.Show("\nThe number of sheets to plot is zero. Skipped.\n");
                        plotEngine.Dispose();
                        tr.Commit();
                        return;
                    }
                    int sheetCount = 0;
                    ObjectIdCollection layoutsToPlot = new ObjectIdCollection();

                    foreach (ObjectId btrId in allLayouts)
                    {
                        ObjectId obj = btrId;
                        try
                        {
                            if (!db.TryGetObjectId(obj.Handle, out obj))
                            {
                                throw new System.Exception();
                            }
                        }
                        catch (System.Exception e) // catch erased layout or whatever shit
                        {
                            char[] r = { '(', ')' };
                            ed.WriteMessage($"\nSheet with id '{btrId.ToString().Trim(r)}' was deleted or created with errors. Removed from plot queue.\n");
                            continue;
                        }
                        layoutsToPlot.Add(obj);
                        sheetCount++;
                    }

                    ed.WriteMessage($"\nTotal sheets to plot: {sheetCount}, total: {allLayouts.Count}, sheets created: {layoutsToPlot.Count}.\n");

                    bool printingError = false;

                    using (PlotProgressDialog plotProcessDialog = new PlotProgressDialog(false, sheetCount, true))
                    {
                        int numSheet = 1;
                        using (doc.LockDocument())
                        {
                            string globalDevice = "DWG To PDF.pc3";

                            foreach (ObjectId btrId in layoutsToPlot)
                            {
                                Layout layout = tr.GetObject(btrId, OpenMode.ForRead) as Layout;

                                // On récupère la taille du papier de la présentation
                                string media = layout.CanonicalMediaName;

                                // 1. Créer un nouveau PlotInfo pour chaque page
                                PlotInfo pagePlotInfo = new PlotInfo();
                                pagePlotInfo.Layout = btrId;

                                // 2. Configurer les Overrides
                                PlotSettings plotSettings = new PlotSettings(layout.ModelType);
                                plotSettings.CopyFrom(layout);

                                PlotSettingsValidator plotSettingsValidator = PlotSettingsValidator.Current;
                                plotSettingsValidator.SetPlotType(plotSettings, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
                                plotSettingsValidator.SetUseStandardScale(plotSettings, true);
                                plotSettingsValidator.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);

                                // APPLIQUER LE MÊME TRACEUR À TOUTES LES PAGES
                                try
                                {
                                    // On tente d'appliquer la taille de papier d'origine sur le traceur global
                                    if (!string.IsNullOrEmpty(media))
                                    {
                                        plotSettingsValidator.SetPlotConfigurationName(plotSettings, globalDevice, media);
                                    }
                                    else
                                    {
                                        // Si le media est vide, on force un format standard
                                        plotSettingsValidator.SetPlotConfigurationName(plotSettings, globalDevice, "ISO_A4_(210.00_x_297.00_MM)");
                                    }
                                }
                                catch (Autodesk.AutoCAD.Runtime.Exception)
                                {
                                    // SÉCURITÉ : Si la taille de papier n'est pas supportée par le traceur global
                                    // (ex: format exotique), on évite le crash en forçant un format A4 par défaut.
                                    plotSettingsValidator.SetPlotConfigurationName(plotSettings, globalDevice, "ISO_A4_(210.00_x_297.00_MM)");
                                    ed.WriteMessage($"\nAttention : Format de papier '{media}' non supporté par {globalDevice}. Remplacement par A4 sur la feuille {numSheet}.\n");
                                }

                                LayoutManager.Current.CurrentLayout = layout.LayoutName;

                                pagePlotInfo.OverrideSettings = plotSettings;
                                plotInfoValidator.Validate(pagePlotInfo);

                                if (numSheet == 1)
                                {
                                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, "Custom Plot Progress");
                                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                                    plotProcessDialog.LowerPlotProgressRange = 0;
                                    plotProcessDialog.UpperPlotProgressRange = 100;
                                    plotProcessDialog.PlotProgressPos = 0;

                                    var file = new FileInfo(outputFileName);
                                    if (file.Exists)
                                    {
                                        try
                                        {
                                            file.Delete();
                                        }
                                        catch (System.Exception)
                                        {
                                            System.Windows.MessageBox.Show("The output file is being used by another process. Cancelled.");
                                            ed.WriteMessage($"\nError opening output file for plotting. Cancelled.\n");
                                            printingError = true;
                                            break;
                                        }
                                    }

                                    plotProcessDialog.OnBeginPlot();
                                    plotProcessDialog.IsVisible = true;
                                    plotEngine.BeginPlot(plotProcessDialog, null);

                                    // L'initialisation du document est maintenant verrouillée sur "DWG To PDF.pc3"
                                    plotEngine.BeginDocument(pagePlotInfo, doc.Name, null, 1, true, outputFileName);
                                }

                                plotProcessDialog.StatusMsgString = "Plotting " +
                                                                    doc.Name.Substring(doc.Name.LastIndexOf("\\") + 1) +
                                                                    " - sheet " + numSheet.ToString() +
                                                                    " of " +
                                                                    layoutsToPlot.Count.ToString();

                                plotProcessDialog.OnBeginSheet();
                                plotProcessDialog.LowerSheetProgressRange = 0;
                                plotProcessDialog.UpperSheetProgressRange = 100;
                                plotProcessDialog.SheetProgressPos = 0;

                                PlotPageInfo plotPageInfo = new PlotPageInfo();
                                Debug.WriteLine($"\nTentative de tracé : {globalDevice} sur papier {media}");
                                plotEngine.BeginPage(plotPageInfo, pagePlotInfo, (numSheet == layoutsToPlot.Count), null);
                                plotEngine.BeginGenerateGraphics(null);
                                plotProcessDialog.SheetProgressPos = 50;
                                plotEngine.EndGenerateGraphics(null);
                                plotEngine.EndPage(null);
                                plotProcessDialog.SheetProgressPos = 100;
                                plotProcessDialog.OnEndSheet();
                                numSheet++;
                                plotProcessDialog.PlotProgressPos = (int)Math.Floor((double)numSheet * 100 / layoutsToPlot.Count);
                            }

                            if (!printingError)
                            {
                                plotEngine.EndDocument(null);
                                plotProcessDialog.PlotProgressPos = 100;
                                plotProcessDialog.OnEndPlot();
                                plotEngine.EndPlot(null);
                            }
                            ed.WriteMessage($"\nPlotting completed.\n");


                            tr.Commit();
                            bool AutoOpenFile = true;
                            if (AutoOpenFile && !printingError)
                                System.Diagnostics.Process.Start(outputFileName);
                        }
                    }
                }
            }
        }





        /// <summary>
        /// Récupère le nom du traceur PDF via le registre ou le premier layout
        /// </summary>
        private static string GetPdfDeviceName(IEnumerable<Layout> layouts)
        {
            string deviceName = string.Empty;
            bool isMultiPage = layouts.Count() > 1;

            if (isMultiPage)
            {
                string sProdKey = HostApplicationServices.Current.UserRegistryProductRootKey;
                string currentProfile = Application.GetSystemVariable("CPROFILE") as string;

                if (!string.IsNullOrEmpty(currentProfile))
                {
                    string regPath = $@"{sProdKey}\Profiles\{currentProfile}\Dialogs\AcPublishDlg";
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath))
                    {
                        object selectedPlotter = key?.GetValue("SelectedPdfPlotter");
                        if (selectedPlotter != null)
                        {
                            var configInfo = PlotConfigManager.Devices
                                .Cast<PlotConfigInfo>()
                                .FirstOrDefault(t => Path.GetFileNameWithoutExtension(t.DeviceName).Equals(selectedPlotter.ToString(), StringComparison.OrdinalIgnoreCase));

                            if (configInfo != null)
                                deviceName = configInfo.DeviceName;
                        }
                    }
                }
            }

            // Fallback si multipage échoue ou si single page
            if (string.IsNullOrEmpty(deviceName))
            {
                var firstLayout = layouts.FirstOrDefault();
                if (firstLayout != null && firstLayout.IsPlotDeviceAvailable())
                {
                    deviceName = firstLayout.PlotConfigurationName;
                }
            }

            return deviceName;
        }
    













        public static void PublishLayoutsNew(this Layout layout, string outputPdfPath)
        {
           // MultiSheetPlotterOld(outputPdfPath, new ObjectIdCollection() { layout.ObjectId });
           

        }

        public static void PublishLayoutsNew(this IEnumerable<Layout> layouts, string outputPdfPath)
        {
           // MultiSheetPlotterOld(outputPdfPath, layouts.GetObjectIds().ToObjectIdCollection());
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
