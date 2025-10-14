using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SioForgeCAD.Functions
{
    public static class FIELDEDITOR
    {

        public static void Test()
        {
            //https://www.keanw.com/2007/07/accessing-the-a.html
            //https://www.keanw.com/2007/06/embedding_field.html
            //https://www.cadforum.cz/en/qaID.asp?tip=6381
            if (!DetectModalRegistered)
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.EnterModal += DetectModal;
                DetectModalRegistered = true;
            }
            //%<\AcExpr (%<\AcObjProp Object(%<\_ObjId 2621431877296>%).Area>%+10) \f "%lu6">%
            Debug.WriteLine(EditField("%<\\AcVar Date \\f \"dd/MM/yyyy\">%"));

        }

        private static bool DetectModalRegistered = false;
        private static void DetectModal(object sender, EventArgs e)
        {
            Autodesk.AutoCAD.ApplicationServices.InplaceTextEditor x = InplaceTextEditor.Current;
            var i = x.Selection;
            if (i?.FieldObject != null)
            {
                Debug.WriteLine("DetectModal");
                //Replace by SioForgeCad field editor
            }
        }


        public static string EditField(string FieldValue)
        {
            //https://www.keanw.com/2007/06/using-a-modal-1.html
            var db = Generic.GetDatabase();
            var doc = Generic.GetDocument();
            var ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            using (var tempMtext = new MText())
            {
                tempMtext.TextHeight = .1;
                tempMtext.Location = ed.GetCurrentViewBound().Middle();
                Field field = new Field(FieldValue);
                field.Evaluate();
                var settings = new InplaceTextEditorSettings
                {
                    Type = InplaceTextEditorSettings.EntityType.Default,
                    Flags = InplaceTextEditorSettings.EditFlags.SelectAll,
                    SimpleMText = true
                };

                tempMtext.SetField(field);
                bool ModalOpenned = false;
                Task.Run(() =>
                {
                    while (!ModalOpenned)
                    {
                        Application.MainWindow.SetAsForeground();
                        SendKeys.SendWait("^h");
                    }
                });

                Application.EnterModal += EnterModal;
                Application.LeaveModal += LeaveModal;
                InplaceTextEditor.Invoke(tempMtext, settings);
                Application.EnterModal -= EnterModal;
                Application.LeaveModal -= LeaveModal;

                void EnterModal(object sender, EventArgs e)
                {
                    Debug.WriteLine("EnterModal");
                    ModalOpenned = true;
                }
                void LeaveModal(object sender, EventArgs e)
                {
                    Debug.WriteLine("LeaveModal");
                    var stopwatch = Stopwatch.StartNew();
                    Task.Run(() =>
                    {
                        //Task.Delay(50);
                        var u = InplaceTextEditor.Current;
                        while (InplaceTextEditor.Current.CanExitEditor)
                        {
                            //Task.Delay(5);
                            if (stopwatch.Elapsed.TotalSeconds > 3)
                            {
                                Debug.WriteLine("3s TimeOut");
                                break;
                            }
                            InplaceTextEditor.Current.Close(TextEditor.ExitStatus.ExitSave);

                        }
                        Debug.WriteLine("InplaceTextEditor Close");
                    });
                }
                var Value = field.GetFieldCode(FieldCodeFlags.AddMarkers);
                tr.Commit();
                return Value;
            }
        }
    }
}
