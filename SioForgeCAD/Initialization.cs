using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
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
            VerifApp();
        }

        public static void VerifApp()
        {
            DateTime buildDate = Assembly.GetExecutingAssembly().GetLinkerTime();

            // Si la date actuelle a dépassé (date de compilation + 2 ans)
            if (DateTime.Now > buildDate.AddYears(2))
            {
                Generic.WriteMessage($"=======================================================");
                Generic.WriteMessage($"[ALERTE DE SÉCURITÉ] SioForgeCAD");
                Generic.WriteMessage($"Cette version a été compilée le {buildDate:dd/MM/yyyy} (il y a plus de 2 ans).");
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
            Functions.WIPEOUTGRIP.EnableOverrule(true);

            //Event
            Functions.SAVEFILEATCLOSE.Event.Attach();
            Functions.ALLANAVDIALOGSDEFINECURRENTDRAWING.Event.Attach();

            //Override
            Functions.LAYERMANAGERNEWLAYERDEFAULTNAME.Override();
            Functions.LAYERMANAGERHANDLEBETTEREDITING.Override();
        }

        public void Terminate()
        {
            //Event
            Functions.SAVEFILEATCLOSE.Event.Detach();
            Functions.ALLANAVDIALOGSDEFINECURRENTDRAWING.Event.Detach();
        }
    }
}
