using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class MERGEHATCH
    {
        public static void Merge()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (!ed.GetHatch(out Hatch FirstHachure, "Veuillez selectionner une première hachure"))
                {
                    return;
                }

                bool Reselect = true;
                Hatch SecondHachure = null;
                while (Reselect)
                {
                    Reselect = false;
                    if (!ed.GetHatch(out SecondHachure, "Veuillez selectionner une deuxième hachure"))
                    {
                        return;
                    }
                    if (SecondHachure == FirstHachure)
                    {
                        Reselect = true;
                    }
                }

                Entity ExistingBoundaryStyle = FirstHachure;
                if (FirstHachure.Associative)
                {
                    FirstHachure.GetAssociatedBoundary(out Curve AssociatedBoundary);
                    ExistingBoundaryStyle = AssociatedBoundary;
                }

                if (!FirstHachure.GetPolyHole(out var FirstHachurePolyHole) || !SecondHachure.GetPolyHole(out var SecondHachurePolyHole))
                {
                    return;
                }


                //bool IsValidMerge = false;
                //If SecondHachurePolyHole intersect FirstHachurePolyHole
                //If SecondHachurePolyHole intersect with a hole of FirstHachurePolyHole
                //expand SecondHachurePolyHole
                //expand FirstHachurePolyHole Boundary and shrink holes
                //Redo all previous check


                if (PolygonOperation.Union(new List<PolyHole>() { FirstHachurePolyHole, SecondHachurePolyHole }, out var unionResult, true))
                {
                    foreach (PolyHole item in unionResult)
                    {
                        //Apply the hatch inside
                        ExistingBoundaryStyle.CopyPropertiesTo(item.Boundary);
                        var Hatch = Hatchs.ApplyHatchV2(item.Boundary, item.Holes.Cast<Curve>(), FirstHachure);
                        if (Hatch != null)
                        {
                            Hatch.Origin = FirstHachure.Origin;
                        }

                    }
                    //Delete old
                    DeleteOldHatch(FirstHachure);
                    DeleteOldHatch(SecondHachure);
                }
                else
                {
                    Generic.WriteMessage("Impossible de merger ces hachures. Vérifiez qu'elles se chevauchent");
                }

                FirstHachurePolyHole.Dispose();
                SecondHachurePolyHole.Dispose();
                tr.Commit();
            }
        }


        private static void DeleteOldHatch(Hatch Hachure)
        {
            foreach (ObjectId item in Hachure.GetAssociatedObjectIds())
            {
                item.EraseObject();
            }
            Hachure.ObjectId.EraseObject();
        }
    }



}
