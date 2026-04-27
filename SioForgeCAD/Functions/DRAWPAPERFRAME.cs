using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class DRAWPAPERFRAME
    {
        public static void Draw()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayoutManager layoutManager = LayoutManager.Current;
                Layout layout = layoutManager.GetCurrentLayout();
                if (layout.IsModel())
                {
                    Generic.WriteMessage("Impossible de dessiner le format papier sur l'espace object");
                    return;
                }

                using (var poly = layout.GetPaperFrame())
                {
                    if (poly != null)
                    {
                        poly.Color = Color.FromRgb(255, 255, 255);
                        poly.LineWeight = LineWeight.LineWeight000;
                        ed.SetImpliedSelection(new ObjectId[] { poly.AddToDrawing() });
                    }
                }
                tr.Commit();
            }

        }
    }
}
