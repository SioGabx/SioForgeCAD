using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using System;

namespace SioForgeCAD.Functions
{
    public static class VPGETSCALEINFO
    {
        public static void GetViewportScaleInfo()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (db.TileMode)
            {
                Generic.WriteMessage("Veuillez basculer sur un onglet de Présentation (Layout).");
                return;
            }
            if (ed.CurrentViewportObjectId != db.PaperSpaceVportId)
            {
                ed.SwitchToPaperSpace();
            }

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez le contour de la fenêtre (Viewport) : ");
            peo.SetRejectMessage("\nL'objet sélectionné n'est ni un Viewport ni un contour de délimitation.");
            peo.AddAllowedClass(typeof(Viewport), true);
            peo.AddAllowedClass(typeof(Curve), false);

            PromptEntityResult per = ed.GetEntity(peo);
            if (per.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId vpId = ObjectId.Null;
                Entity selectedEnt = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Entity;

                if (selectedEnt is Viewport)
                {
                    vpId = selectedEnt.ObjectId;
                }
                else if (selectedEnt is Curve)
                {
                    vpId = LayoutManager.Current.GetNonRectangularViewportIdFromClipId(selectedEnt.ObjectId);
                }

                if (vpId.IsNull)
                {
                    Generic.WriteMessage("Cet objet n'est pas un Viewport et n'est lié à aucune fenêtre.");
                    tr.Commit();
                    return;
                }

                Viewport vp = tr.GetObject(vpId, OpenMode.ForRead) as Viewport;

                if (vp != null)
                {
                    double customScale = vp.CustomScale;
                    double unitFactor = GetMillimetersPerDrawingUnit(db);
                    double denominator = unitFactor / customScale;

                    Generic.WriteMessage($"--- Informations d'échelle du Viewport ---");
                    Generic.WriteMessage($"Facteur brut (CustomScale) : {customScale:0.####}");
                    Generic.WriteMessage($"Unité du dessin (INSUNITS) : {db.Insunits} (Facteur : {unitFactor})");

                    if (denominator >= 1.0)
                    {
                        Generic.WriteMessage($"Échelle calculée : 1/{Math.Round(denominator, 2)}e");
                    }
                    else
                    {
                        Generic.WriteMessage($"Échelle calculée : {Math.Round(1.0 / denominator, 2)}/1");
                    }
                }
                tr.Commit();
            }
        }

        private static double GetMillimetersPerDrawingUnit(Database db)
        {
            switch (db.Insunits)
            {
                case UnitsValue.Millimeters:
                    return 1.0;
                case UnitsValue.Centimeters:
                    return 10.0;
                case UnitsValue.Decimeters:
                    return 100.0;
                case UnitsValue.Meters:
                    return 1000.0;
                case UnitsValue.Undefined:
                default:
                    // Par défaut en topo/architecture en France, un dessin sans unité est en mètres (1m = 1000mm)
                    return 1000.0;
            }
        }
    }
}
