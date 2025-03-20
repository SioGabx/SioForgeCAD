using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class WIPEOUT
    {
        private static WipeoutGripOverrule wipeoutGripOverrule = new WipeoutGripOverrule();

        public static void ModifyWipeout()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez un Wipeout : ");
            peo.SetRejectMessage("\nL'objet sélectionné n'est pas un Wipeout.");
            peo.AddAllowedClass(typeof(Wipeout), true);
            PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Wipeout wipeout = tr.GetObject(per.ObjectId, OpenMode.ForWrite) as Wipeout;
                if (wipeout != null)
                {
                    Overrule.AddOverrule(RXObject.GetClass(typeof(Wipeout)), wipeoutGripOverrule, true);
                    Application.UpdateScreen();
                    ed.WriteMessage("\nLes grips sont activés. Déplacez les sommets pour modifier la forme du Wipeout.");
                }
                tr.Commit();
            }
        }
    }

    public class WipeoutGripOverrule : GripOverrule
    {
        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
           
            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
            if (entity is Wipeout wipeout)
            {
                Point2dCollection points = wipeout.GetClipBoundary();
              
            }
        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            if (entity is Wipeout wipeout)
            {
                Point2dCollection points = wipeout.GetClipBoundary();


                wipeout.SetClipBoundary(ClipBoundaryType.Poly,points);
            }
            else
            {

                base.MoveGripPointsAt(entity, grips, offset, bitFlags);
            }
        }
    }
}
