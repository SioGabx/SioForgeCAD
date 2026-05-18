using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Microsoft.Win32;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SioForgeCAD.Commun.Mist.Helpers
{
    public static class Plotting
    {
        public static void PublishLayouts(this Layout layout, string outputFilePath)
        {
            new Layout[1] { layout }.PublishLayouts(outputFilePath);
        }
        public static void PublishLayouts(this IEnumerable<Layout> layouts, string outputFilePath)
        {
            layouts.ProcessPrintLayouts(outputFilePath);
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

            if (HaveSamePaperFormat)
            {
                PlotSuccess = PublishLayoutsWithSamePaperFormats(layouts, outputFilePath);
            }

            if (!PlotSuccess) //if PublishLayoutsWithSamePaperFormats fails, we run your PublishLayoutsWithDifferentsPaperFormats
            {
                PlotSuccess = PublishLayoutsWithDifferentsPaperFormats(layouts, outputFilePath);
            }

            if (PlotSuccess && AutoOpenFile && File.Exists(outputFilePath))
            {
                Process.Start(outputFilePath); //System.Diagnostics.Process.Start
            }
        }

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

            string tempFolderPath = Files.GetATempFolder("AcPublish");
            string outFileName = Path.GetFileNameWithoutExtension(outputFilePath);
            string tempOutputPdfPath = Path.Combine(tempFolderPath, $"{outFileName}.pdf");

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
                            if (!plotProcessDialog.IsVisible)
                            {
                                plotProcessDialog.IsVisible = true;
                            }

                            LayoutManager.Current.CurrentLayout = layout.LayoutName;
                            pagePlotInfo.OverrideSettings = plotSettings;
                            plotInfoValidator.Validate(pagePlotInfo);

                            if (layoutIndex == 1)
                            {
                                plotProcessDialog.OnBeginPlot();
                                plotEngine.BeginPlot(plotProcessDialog, null);
                                plotEngine.BeginDocument(pagePlotInfo, doc.Name, null, 1, true, tempOutputPdfPath);
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
                    //end plot, even if we have errors
                    try { plotEngine.EndDocument(null); } catch { }
                    plotProcessDialog.PlotProgressPos = 100;
                    plotProcessDialog.OnEndPlot();
                    try { plotEngine.EndPlot(null); } catch { }
                }
            }

            // Déplacer le fichier temporaire vers la destination finale en toute sécurité
            if (File.Exists(tempOutputPdfPath))
            {
                try
                {
                    File.Copy(tempOutputPdfPath, outputFilePath, true);
                    return true;
                }
                catch (System.Exception ex)
                {
                    Generic.WriteMessage($"Erreur lors de la copie du fichier final : {ex.Message}");
                    return false;
                }
                finally
                {
#if !DEBUG
                    Files.TryDeleteDirectory(tempFolderPath);
#endif
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

            // Fail-fast : Vérification préalable sur la destination
            if (!Files.TryDeleteFile(outputFilePath))
            {
                Generic.WriteMessage("Impossible de remplacer le fichier PDF final (il est peut-être ouvert ou verrouillé par un autre processus).");
                return false;
            }

            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            string drawingName = Path.GetFileNameWithoutExtension(doc.Name);
            string outFileName = Path.GetFileNameWithoutExtension(outputFilePath);

            // 1. GESTION DU DOSSIER ET FICHIERS TEMPORAIRES
            string cleanName = string.Join("_", drawingName.Split(Path.GetInvalidFileNameChars()));
            string tempFolderPath = Files.GetATempFolder("AcPublish");
            string tempDwgPath = Path.Combine(tempFolderPath, $"{cleanName}.dwg");
            string tempOutputPdfPath = Path.Combine(tempFolderPath, $"{outFileName}.pdf"); // Fichier PDF temporaire
            string originalDwgDir = string.IsNullOrEmpty(db.Filename) ? string.Empty : Path.GetDirectoryName(db.Filename);

            try
            {
                db.SaveAs(tempDwgPath, DwgVersion.Current);
                // On utilise 'false' pour buildDefaultDrawing car on va lire un fichier existant
                using (Database dbSide = new Database(false, true))
                {
                    dbSide.ReadDwgFile(tempDwgPath, FileShare.ReadWrite, true, null);
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

            // 2. FORCER LE TRACÉ AU PREMIER PLAN (Indispensable pour gérer les fichiers après)
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
                    dsdFileData.ProjectPath = tempFolderPath;
                    dsdFileData.LogFilePath = Path.Combine(tempFolderPath, "publish.log");
                    dsdFileData.SheetType = isMultiPage ? SheetType.MultiPdf : SheetType.SinglePdf;
                    dsdFileData.DestinationName = tempOutputPdfPath;

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

                    dsdFileData.WriteDsd(dsdFilePath);

                    //string dsdText = File.ReadAllText(dsdFilePath);
                    //dsdText = dsdText.Replace("PromptForDwfName=TRUE", "PromptForDwfName=FALSE");
                    //File.WriteAllText(dsdFilePath, dsdText);

                    //// On recharge le DSD modifié
                    //dsdFileData.ReadDsd(dsdFilePath);

                    PlotConfig plotConfig = PlotConfigManager.SetCurrentConfig(GetPlotDeviceName(layouts));

                    using (PlotProgressDialog plotProcessDialog = new PlotProgressDialog(false, layouts.Count(), true))
                    {
                        Autodesk.AutoCAD.ApplicationServices.Core.Application.Publisher.PublishDsd(dsdFilePath, plotProcessDialog);
                    }
                }

                // 3. Déplacer le fichier PDF temporaire vers son emplacement final
                if (File.Exists(tempOutputPdfPath))
                {
                    File.Copy(tempOutputPdfPath, outputFilePath, true);
                }
                else
                {
                    Generic.WriteMessage("Le tracé a échoué. Fichier PDF temporaire introuvable.");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage(ex.Message);
                return false;
            }
            finally
            {
                Generic.SetSystemVariable("BACKGROUNDPLOT", bgPlot, false);
#if !DEBUG
                    Files.TryDeleteDirectory(tempFolderPath);
#endif
            }
            return true;
        }

    }
}
