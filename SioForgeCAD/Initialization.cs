using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Reflection;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD
{
    public class Initialization : IExtensionApplication
    {
        public void Initialize()
        {
            AcAp.Idle += OnIdle;
        }

        private void OnIdle(object sender, EventArgs e)
        {
            var doc = Generic.GetDocument();
            if (doc != null)
            {
                AcAp.Idle -= OnIdle;
                doc.Editor.WriteMessage($"\n{Settings.CopyrightMessage}\n");
            }
            InitPlugin();
            SetDefaultSysVariables();
            VerifApp();
        }

        public static void VerifApp()
        {
            DateTime buildDate = Assembly.GetExecutingAssembly().GetLinkerTime();
            if (buildDate.IsOlderThan(DateTime.Now, 2)) // Si la date actuelle a dépassé (date de compilation + 2 ans)
            {
                Generic.WriteMessage($"=======================================================");
                Generic.WriteMessage($"[ALERTE DE SÉCURITÉ] {Generic.GetExtensionDLLName()}");
                Generic.WriteMessage($"{Generic.GetExtensionDLLName()} a été compilée le {buildDate:dd/MM/yyyy} (il y a plus de {buildDate.GetDifferenceInYears(DateTime.Now)} ans).");
                Generic.WriteMessage($"Pour des raisons de sécurité et de stabilité, une mise à jour est requise.");
                Generic.WriteMessage($"=======================================================\n");

                PluginRegister.Unregister();
            }
        }


        public static void InitPlugin()
        {
            //Entity ContextMenu
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Attach();
            Functions.ELLIPSETOPOLYLIGNE.ContextMenu.Attach();
            Functions.LINETOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE2DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.POLYLINE3DTOPOLYLIGNE.ContextMenu.Attach();
            Functions.CONVERTIMAGETOOLE.ContextMenu.Attach();
            Functions.DIMDISASSOCIATE.ContextMenu.Attach();

            //Controls ContextMenu
            Functions.LAYERMANAGERCTXNEWLAYERFROMSELECTED.ContextMenu.Attach();

            //Tray
            Functions.PICKSTYLETRAY.AddTray();

            //Overrule
            try
            {
                Functions.WIPEOUTGRIP.EnableOverrule(true);
            }
            catch (System.Exception ex)
            {
                Generic.WriteMessage($"[ERROR] Wipeout Grip Overrule: {ex.Message}");
            }

            //Event
            Functions.SAVEFILEATCLOSE.Event.Attach();
            Functions.ALLANAVDIALOGSDEFINECURRENTDRAWING.Event.Attach();

            //Override
            Functions.LAYERMANAGERNEWLAYERDEFAULTNAME.Override();
            Functions.LAYERMANAGERHANDLEBETTEREDITING.Override();
            Functions.CUSTOMLAYOUTBAR.Override();

        }


        public static void SetDefaultSysVariables()
        {
            Generic.SetSystemVariable("HPNAME", "SOLID", false);
        }

        public void Terminate()
        {
            //Event
            Functions.SAVEFILEATCLOSE.Event.Detach();
            Functions.ALLANAVDIALOGSDEFINECURRENTDRAWING.Event.Detach();
        }
    }
}
