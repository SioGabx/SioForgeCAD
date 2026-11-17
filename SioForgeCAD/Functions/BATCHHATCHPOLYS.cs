using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class BATCHHATCHPOLYS
    {
        public static void HatchClosedPolylines()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions opts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSélectionnez les polylignes à hachurer : "
            };

            var selRes = ed.GetSelectionRedraw(opts, filter);
            if (selRes.Status != PromptStatus.OK)
            {
                return;
            }

            ObjectId[] selectedIds = selRes.Value.GetObjectIds();
            int hatchedCount = 0;

            using (LongOperationProcess longOperation = new LongOperationProcess("Hachurage des polylignes fermées..."))
            {
                longOperation.SetTotalOperations(selectedIds.Length);

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTableRecord currentSpace = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                        for (int i = 0; i < selectedIds.Length; i++)
                        {
                            longOperation.UpdateProgress();

                            if (longOperation.IsCanceled)
                            {
                                Generic.WriteMessage("Opération annulée par l'utilisateur.");
                                break;
                            }

                            Polyline poly = tr.GetObject(selectedIds[i], OpenMode.ForRead) as Polyline;

                            // Vérification : la polyligne doit être fermée et avoir une aire non nulle
                            if (poly != null && poly.Closed && poly.Area > 0)
                            {
                                Hatch hatch = new Hatch();
                                currentSpace.AppendEntity(hatch);
                                tr.AddNewlyCreatedDBObject(hatch, true);
                                hatch.SetDatabaseDefaults();
                                hatch.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256); // Couleur DuCalque
                                hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                hatch.Associative = true;

                                ObjectIdCollection loopIds = new ObjectIdCollection { poly.ObjectId };
                                hatch.AppendLoop(HatchLoopTypes.Outermost, loopIds);
                                hatch.EvaluateHatch(true);

                                hatchedCount++;
                            }
                        }

                        tr.Commit();
                        ed.UpdateScreen();
                        Generic.WriteMessage($"Terminé : {hatchedCount} polyligne(s) fermée(s) hachurée(s).");
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Generic.WriteMessage($"Erreur lors du hachurage : {ex.Message}");
                        tr.Abort();
                    }
                }
            }
        }
    }
}
