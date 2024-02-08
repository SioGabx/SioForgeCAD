using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;

[assembly: CommandClass(typeof(SioForgeCAD.Commands))]

namespace SioForgeCAD
{
    public class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Attach();
        }

        public void Terminate()
        {
            Functions.CIRCLETOPOLYLIGNE.ContextMenu.Detach();
        }

        [CommandMethod("SIOFORGECAD")]
        public static void SIOFORGECAD()
        {
            var ed = Generic.GetEditor();
            PromptKeywordOptions promptKeywordOptions = new PromptKeywordOptions("Veuillez selectionner une option")
            {
                AppendKeywordsToMessage = true
            };
            promptKeywordOptions.Keywords.Add("Settings");
            promptKeywordOptions.Keywords.Add("Register");
            promptKeywordOptions.Keywords.Add("Unregister");
            var result = ed.GetKeywords(promptKeywordOptions);
            switch (result.StringResult)
            {
                case "Settings":
                    break;
                case "Register":
                    Commun.PluginRegister.Register();
                    break;
                case "Unregister":
                    Commun.PluginRegister.Unregister();
                    break;
            }
        }


        [CommandMethod("CCI")]
        public void CCI()
        {
            new Functions.CCI().Compute();
        }

        [CommandMethod("CCP")]
        public void CCP()
        {
            new Functions.CCP().Compute();
        }
        [CommandMethod("CCD")]
        public void CCD()
        {
            new Functions.CCD().Compute();
        }

        [CommandMethod("CCA")]
        public void CCA()
        {
            Functions.CCA.Compute();
        }

        [CommandMethod("CCXREF", CommandFlags.Redraw)]
        public void CCXREF()
        {
            Functions.CCXREF.MoveCotationFromXrefToCurrentDrawing();
        }

        [CommandMethod("RENBLK", CommandFlags.Redraw)]
        public void RENBLK()
        {
            Functions.RENBLK.RenameBloc();
        }

        [CommandMethod("BLKMAKEUNIQUE", CommandFlags.Redraw)]
        public void MAKEUNIQUBLK()
        {
            new Functions.BLKMAKEUNIQUE(true).MakeUniqueBlockReferences();
        }

        [CommandMethod("BLKMAKEUNIQUEEACH", CommandFlags.Redraw)]
        public void BLKMAKEUNIQUEEACH()
        {
            new Functions.BLKMAKEUNIQUE(false).MakeUniqueBlockReferences();
        }

        [CommandMethod("DRAWPERPENDICULARLINEFROMPOINT", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public void DRAWPERPENDICULARLINEFROMPOINT()
        {
            Functions.DRAWPERPENDICULARLINEFROMPOINT.DrawPerpendicularLineFromPoint();
        }

        [CommandMethod("CIRCLETOPOLYLIGNE", CommandFlags.UsePickSet)]
        public static void CIRCLETOPOLYLIGNE()
        {
            Functions.CIRCLETOPOLYLIGNE.ConvertCirclesToPolylines();
        }

        [CommandMethod("DRAWCPTERRAIN", CommandFlags.UsePickSet | CommandFlags.Redraw)]
        public static void DRAWCPTERRAIN()
        {
            new Functions.DRAWCPTERRAIN().DrawTerrainFromSelectedPoints();
        }

        [CommandMethod("DROPCPOBJECTTOTERRAIN")]
        public static void DROPCPOBJECTTOTERRAIN()
        {
            Functions.DROPCPOBJECTTOTERRAIN.Project();
        }

        [CommandMethod("FORCELAYERCOLORTOENTITY", CommandFlags.UsePickSet)]
        public static void FORCELAYERCOLORTOENTITY()
        {
            Functions.FORCELAYERCOLORTOENTITY.Convert();
        }

        [CommandMethod("SSCL", CommandFlags.Transparent)]
        public static void SSCL()
        {
            Functions.SSCL.Select();
        }

        [CommandMethod("RRR", CommandFlags.UsePickSet)]
        public static void RRR()
        {
            Functions.RRR.Rotate();
        }

        [CommandMethod("BLKINSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        [CommandMethod("INSEDIT", CommandFlags.UsePickSet | CommandFlags.Modal)]
        public void BLKINSEDIT()
        {
            Functions.BLKINSEDIT.MoveBasePoint();
        }

        [CommandMethod("RP2")]
        public void RP2()
        {
            Functions.RP2.RotateUCS();
        }

        [CommandMethod("TAREA")]
        public void TAREA()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("TLEN")]
        public void TLEN()
        {
            throw new NotImplementedException();
        }

        [CommandMethod("VEGBLOC", CommandFlags.Modal)]
        public void VEGBLOC()
        {
            Functions.VEGBLOC.Create();
        }

        [CommandMethod("BLKTOSTATICBLOCK", CommandFlags.UsePickSet)]
        public static void BLKTOSTATICBLOCK()
        {
            Functions.BLKTOSTATICBLOCK.Convert();
        }

        [CommandMethod("BATTLEMENTS")]
        public static void BATTLEMENTS()
        {
            Functions.BATTLEMENTS.Draw();
        }


















        [CommandMethod("DEBUG", "COPYOVERRULE", CommandFlags.UsePickSet)]
        public void COPYOVERRULE()
        {
            Commun.Overrules.CopyGripOverrule.CopyGripOverrule.Instance.HideOriginals = true;
            Commun.Overrules.CopyGripOverrule.CopyGripOverrule.Instance.EnableOverrule(true);
            Generic.WriteMessage("COPYOVERRULE is on");
        }
        

        [CommandMethod("DEBUG", "TRIANGLECC", CommandFlags.UsePickSet)]
        public void TRIANGLECC()
        {
            Commun.Triangulate.TriangulateCommand();
        }


#if DEBUG
        [CommandMethod("DEBUG","RANDOM_POINTS", CommandFlags.Transparent)]
        public static void DEBUG_RANDOM_POINTS()
        {
            Random _random = new Random();

            // Generates a random number within a range.
            int RandomNumber(int min, int max)
            {
                return _random.Next(min, max);
            }
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < 150; i++)
                {
                    double x = RandomNumber(-200, 200);
                    double y = RandomNumber(-50, 50);
                    double alti = RandomNumber(100, 120) + RandomNumber(0, 99) * 0.01;
                    Point3d point = new Point3d(x, y, alti);
                    Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist("_APUd_COTATIONS_Altimetries", new Points(point), Generic.GetUSCRotation(Generic.AngleUnit.Radians), new Dictionary<string, string>() { { "ALTIMETRIE", alti.ToString("#.00") } });
                }
                tr.Commit();
            }
            ed.Command("_PLAN", "");
        }

#endif
    }
}
