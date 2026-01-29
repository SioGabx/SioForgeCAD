using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class OFFSETMULTIPLE
    {
        public static void Execute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            Database db = doc.Database;

            var selRes = ed.GetSelectionRedraw("Sélectionnez des curves :", true, false, ed.GetCurvesFilter());

            if (selRes.Status != PromptStatus.OK)
            {
                Generic.WriteMessage("Aucune polyligne sélectionnée.");
                return;
            }

            double[] offsets = GetOffsetsInteractively(ed, Generic.GetSystemVariable("OFFSETDIST").ToString());
            if (offsets == null)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                while (true)
                {
                    DBObjectCollection PreviewStaticEntities = new DBObjectCollection();

                    foreach (var selObj in selRes.Value.GetSelectionSet())
                    {
                        if (!(tr.GetObject(selObj, OpenMode.ForRead) is Curve curve))
                        {
                            continue;
                        }

                        bool isClockwise = curve is Polyline poly && ((curve.Closed || poly.NumberOfVertices >= 2) ? poly.IsClockwise() : poly.IsAtRightSide(Point3d.Origin));
                        foreach (double offsetDist in offsets)
                        {
                            double effectiveOffset = offsetDist;
                            if (isClockwise)
                            {
                                effectiveOffset = -offsetDist;
                            }

                            foreach (Entity ent in curve.GetOffsetCurves(effectiveOffset))
                            {
                                PreviewStaticEntities.Add(ent);
                            }
                        }
                    }

                    using (GetPointTransient transient = new GetPointTransient(new DBObjectCollection(), null)
                    {
                        SetStaticEntities = PreviewStaticEntities
                    })
                    {
                        var input = transient.GetPoint("Cliquez pour valider", Points.Null, false, "Inverser", "Redéfinir");

                        var status = input.PromptPointResult.Status;

                        if (status == PromptStatus.OK)
                        {
                            PreviewStaticEntities.AddToDrawing(Clone: true);
                            break;
                        }
                        else if (status == PromptStatus.Keyword)
                        {
                            if (input.PromptPointResult.StringResult == "Inverser")
                            {
                                offsets = offsets.Select(o => -o).ToArray();
                            }
                            else if (input.PromptPointResult.StringResult == "Redéfinir")
                            {
                                offsets = GetOffsetsInteractively(ed, string.Join(";", offsets));
                                if (offsets == null)
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            Generic.WriteMessage("Opération annulée.");
                            tr.Commit();
                            return;
                        }
                    }

                }

                tr.Commit();
            }
        }

        private static double[] GetOffsetsInteractively(Editor ed, string OldValue)
        {
            PromptStringOptions strOpts = new PromptStringOptions("\nEntrez les distances d'offset séparées par ';' : ")
            {
                AllowSpaces = false,
                UseDefaultValue = true,
                DefaultValue = OldValue
            };
            
            PromptResult strRes = ed.GetString(strOpts);
            if (strRes.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(strRes.StringResult))
            {
                ed.WriteMessage("\nEntrée invalide.");
                return null;
            }


            try
            {

                return strRes.StringResult
                    .SplitUserInputByDelimiters(";", ",")
                    .Select(s => Convert.ToDouble(s.Trim()))
                    .ToArray();
            }
            catch
            {
                ed.WriteMessage("\nErreur dans la saisie des distances.");
                return null;
            }
        }
    }
}