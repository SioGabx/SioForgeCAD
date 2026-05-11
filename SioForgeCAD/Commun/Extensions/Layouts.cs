using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsSystem;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Mist;
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



        /// <summary>
        /// Récupère le nom du traceur PDF via le registre ou le premier layout
        /// </summary>
        private static string GetPlotDeviceName(IEnumerable<Layout> layouts)
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
                            {
                                deviceName = configInfo.DeviceName;
                            }
                        }
                    }
                }
            }

            // Fallback si multipage échoue ou si single page
            if (string.IsNullOrEmpty(deviceName))
            {
                var firstLayout = layouts.FirstOrDefault();
                if (firstLayout?.IsPlotDeviceAvailable() == true)
                {
                    deviceName = firstLayout.PlotConfigurationName;
                }
            }

            return deviceName;
        }

        public static void PublishLayouts(this Layout layout, string outputFilePath)
        {
            new Layout[1] { layout }.PublishLayouts(outputFilePath);
        }
        public static void PublishLayouts(this IEnumerable<Layout> layouts, string outputFilePath)
        {
            layouts.ProcessPrintLayouts(outputFilePath);
        }

        private static void ProcessPrintLayouts(this IEnumerable<Layout> layouts, string outputFilePath, bool AutoOpenFile = false)
        {
            if (layouts?.Any() != true)
            {
                return;
            }

            string firstMedia = layouts.First().CanonicalMediaName ?? string.Empty;
            PlotRotation plotRotation = layouts.First().PlotRotation;
            bool HaveSamePaperFormat = layouts.All(l =>
            {
                return string.Equals(l.CanonicalMediaName ?? string.Empty, firstMedia, StringComparison.OrdinalIgnoreCase) &&
                plotRotation == l.PlotRotation;
            });
            bool PlotSuccess = false;

            if (HaveSamePaperFormat || true)
            {
                //PlotSuccess = true;
                //PublishLayoutsWithSamePaperFormatsOld(outputFilePath, layouts.Select(l=> l.ObjectId).ToObjectIdCollection());
                PlotSuccess = PublishLayoutsWithSamePaperFormats(layouts, outputFilePath);
            }

            if (!PlotSuccess) //if PublishLayoutsWithSamePaperFormats fails, we run your 
            {
                PlotSuccess = PublishLayoutsWithDifferentsPaperFormats(layouts, outputFilePath);
            }

            if (PlotSuccess && AutoOpenFile && File.Exists(outputFilePath))
            {
                Process.Start(outputFilePath); //System.Diagnostics.Process.Start
            }
        }


        //https://github.com/cadwiki/cadwiki-nuget/blob/a98c59f2715ab9a3dc640986c8599ac47ce07be1/cadwiki-nuget/cadwiki.AC/Plotters/PlotterMultiPage.cs#L12
        private static bool PublishLayoutsWithSamePaperFormats(this IEnumerable<Layout> layouts, string outputFilePath)
        {
            Document doc = Generic.GetDocument();
            string docName = doc.Name.Substring(doc.Name.LastIndexOf("\\") + 1);
            if (layouts?.Any() != true)
            {
                Generic.WriteMessage("The number of sheets to plot is zero. Skipped.");
                return false;
            }

            if (!Files.TryDeleteFile(outputFilePath))
            {
                Generic.WriteMessage("The output file is being used by another process. Cancelled.");
                return false;
            }

            Database db = doc.Database;


            PlotInfoValidator plotInfoValidator = new PlotInfoValidator
            {
                MediaMatchingPolicy = MatchingPolicy.MatchEnabled
            };
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                Generic.WriteMessage("Cancelled. Printer is busy (another plot job is in progress).");
                return false;
            }

            using (PlotEngine plotEngine = PlotFactory.CreatePublishEngine())
            using (PlotProgressDialog plotProcessDialog = new PlotProgressDialog(false, layouts.Count(), true)
            {
                LowerPlotProgressRange = 0,
                UpperPlotProgressRange = 100,
                PlotProgressPos = 0
            })
            using (Generic.GetLock())
            {
                try
                {
                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.DialogTitle, $"{Generic.GetExtensionDLLName()} - Plot Progress");
                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                    plotProcessDialog.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");


                    string globalDevice = GetPlotDeviceName(layouts);

                    foreach (var (layout, layoutIndex) in layouts.Select((val, i) => (val, i + 1)))
                    {
                        // 1. Créer un nouveau PlotInfo pour chaque page
                        PlotInfo pagePlotInfo = new PlotInfo
                        {
                            Layout = layout.ObjectId
                        };

                        // 2. Configurer les Overrides
                        using (PlotSettings plotSettings = new PlotSettings(layout.ModelType))
                        {
                            plotSettings.CopyFrom(layout);

                            PlotSettingsValidator plotSettingsValidator = PlotSettingsValidator.Current;
                            plotSettingsValidator.RefreshLists(plotSettings);
                            plotSettingsValidator.SetPlotType(plotSettings, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);

                            plotSettingsValidator.SetUseStandardScale(plotSettings, true);
                            plotSettingsValidator.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);
                            plotSettingsValidator.SetPlotRotation(plotSettings, layout.PlotRotation);

                            // APPLIQUER LE MÊME TRACEUR À TOUTES LES PAGES
                            string media = layout.CanonicalMediaName;
                            try
                            {
                                // On tente d'appliquer la taille de papier d'origine sur le traceur global
                                if (!string.IsNullOrEmpty(media))
                                {
                                    plotSettingsValidator.SetPlotConfigurationName(plotSettings, globalDevice, media);
                                }
                                else
                                {
                                    Generic.WriteMessage($"Attention : Format de papier n'est pas indiqué pour la présentation {layout.LayoutName}");
                                    return false;
                                }
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception)
                            {
                                Generic.WriteMessage($"Attention : Format de papier '{media}' non supporté par {globalDevice} pour la présentation {layout.LayoutName}.");
                                return false;
                            }

                            LayoutManager.Current.CurrentLayout = layout.LayoutName;
                            pagePlotInfo.OverrideSettings = plotSettings;
                            plotInfoValidator.Validate(pagePlotInfo);

                            if (layoutIndex == 1)
                            {
                                plotProcessDialog.OnBeginPlot();
                                plotProcessDialog.IsVisible = true;
                                plotEngine.BeginPlot(plotProcessDialog, null);

                                // L'initialisation du document est maintenant verrouillée sur "DWG To PDF.pc3"
                                plotEngine.BeginDocument(pagePlotInfo, doc.Name, null, 1, true, outputFilePath);
                            }

                            plotProcessDialog.StatusMsgString = $"Plotting {docName} - présentation {layoutIndex} / {layouts.Count()}";
                            plotProcessDialog.OnBeginSheet();
                            plotProcessDialog.LowerSheetProgressRange = 0;
                            plotProcessDialog.UpperSheetProgressRange = 100;
                            plotProcessDialog.SheetProgressPos = 0;


                            PlotPageInfo plotPageInfo = new PlotPageInfo();
                            Debug.WriteLine($"Tentative de tracé : {globalDevice} sur papier {media}, rotation : {layout.PlotRotation}");

                            plotEngine.BeginPage(plotPageInfo, pagePlotInfo, layoutIndex == layouts.Count(), null);
                          
                            plotEngine.BeginGenerateGraphics(null);
                            plotProcessDialog.SheetProgressPos = 50;
                            plotEngine.EndGenerateGraphics(null);

                            plotEngine.EndPage(null);
                            plotProcessDialog.SheetProgressPos = 100;
                            plotProcessDialog.OnEndSheet();

                            plotProcessDialog.PlotProgressPos = (int)Math.Floor((double)layoutIndex * 100 / layouts.Count());
                        }
                    }
                    Generic.WriteMessage("Plotting completed.");
                }
                catch (System.Exception ex)
                {
                    Generic.WriteMessage("Plotting failed.");
                    Debug.WriteLine(ex.ToString());
                    return false;
                }
                finally
                {
                    //end plot, even if we have erros
                    plotEngine.EndDocument(null);
                    plotProcessDialog.PlotProgressPos = 100;
                    plotProcessDialog.OnEndPlot();
                    plotEngine.EndPlot(null);
                }
            }


            return true;
        }

        private static bool PublishLayoutsWithDifferentsPaperFormats(this IEnumerable<Layout> layouts, string outputFilePath)
        {
            if (layouts?.Any() != true)
            {
                return false;
            }

            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            string drawingName = Path.GetFileNameWithoutExtension(doc.Name);
            string outFileName = Path.GetFileNameWithoutExtension(outputFilePath);

            // 1. GESTION DU FICHIER TEMPORAIRE
            string cleanName = string.Join("_", drawingName.Split(Path.GetInvalidFileNameChars()));
            string uniqueId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            string tempFolderPath = Path.Combine(Path.GetTempPath(), $"AcPublish_{uniqueId}");
            string tempDwgPath = Path.Combine(tempFolderPath, $"{cleanName}.dwg");

            try
            {
                Directory.CreateDirectory(tempFolderPath);
                db.SaveAs(tempDwgPath, DwgVersion.Current);
                // On utilise 'false' pour buildDefaultDrawing car on va lire un fichier existant
                using (Database dbSide = new Database(false, true))
                {
                    dbSide.ReadDwgFile(tempDwgPath, FileShare.ReadWrite, true, null);
                    string originalDwgDir = string.IsNullOrEmpty(db.Filename) ? string.Empty : Path.GetDirectoryName(db.Filename);
                    if (!string.IsNullOrEmpty(originalDwgDir))
                    {
                        dbSide.MakeXREFPathAbsolute(originalDwgDir);
                    }
                    dbSide.SaveAs(tempDwgPath, DwgVersion.Current);
                }
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage($"\nErreur lors de la préparation du clonage du dessin : {ex.Message}");
                return false;
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
                    dsdFileData.ProjectPath = Path.GetDirectoryName(outputFilePath);
                    dsdFileData.LogFilePath = Path.Combine(tempFolderPath, "publish.log");
                    dsdFileData.SheetType = isMultiPage ? SheetType.MultiPdf : SheetType.SinglePdf;
                    dsdFileData.DestinationName = outputFilePath;
                    dsdFileData.IsHomogeneous = false;
                    dsdFileData.PromptForDwfName = false;
                    dsdFileData.PlotStampOn = false;
                    dsdFileData.SetUnrecognizedData("PromptForDwfName", "FALSE");
                    dsdFileData.SetUnrecognizedData("IncludeLayer", "TRUE");
                    dsdFileData.SetUnrecognizedData("PromptForPwd", "FALSE");

                    if (!Files.TryDeleteFile(dsdFilePath))
                    {
                        return false;
                    }

                    // On écrit le fichier DSD, on le force en texte brut, puis on le relit.
                    dsdFileData.WriteDsd(dsdFilePath);

                    string dsdText = File.ReadAllText(dsdFilePath);

                    dsdText = dsdText.Replace("PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
                    //dsdText = dsdText.Replace("IncludeLayer=FALSE", "IncludeLayer=TRUE");
                    File.WriteAllText(dsdFilePath, dsdText);

                    // On recharge le DSD modifié
                    dsdFileData.ReadDsd(dsdFilePath);

                    PlotConfig plotConfig = PlotConfigManager.SetCurrentConfig(GetPlotDeviceName(layouts));

                    if (!Files.TryDeleteFile(outputFilePath))
                    {
                        Generic.WriteMessage("Impossible de remplacer le fichier PDF (il est peut-être ouvert)");
                        return false;
                    }
                    using (PlotProgressDialog plotProcessDialog = new PlotProgressDialog(false, layouts.Count(), true))
                    {
                        //We use PublishDsd instead of Application.Publisher.PublishExecute(dsdFileData, plotConfig); because PublishExecute seams to save the temp file into recent open file
                        Application.Publisher.PublishDsd(dsdFilePath, plotProcessDialog);
                    }

                }
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage(ex.Message);
            }
            finally
            {
                Generic.SetSystemVariable("BACKGROUNDPLOT", bgPlot, false);
                try { if (File.Exists(dsdFilePath)) { File.Delete(dsdFilePath); } } catch { }
                try { if (File.Exists(tempDwgPath)) { File.Delete(tempDwgPath); } } catch { }
            }
            return true;
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
