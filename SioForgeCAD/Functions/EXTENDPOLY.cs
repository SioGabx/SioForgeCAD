using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Functions
{
    public static class EXTENDPOLY
    {
        public static class ExtendMode
        {
            public const string START = "Début";
            public const string END = "Fin";
            public const string BOTH = "Début & Fin";
        }
        public static double LastExtendDist = 1;
        public static void Execute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            Database db = doc.Database;

            var selRes = ed.GetSelectionRedraw("Sélectionnez des polylignes :", true, false, ed.GetCurvesFilter());

            if (selRes.Status != PromptStatus.OK)
            {
                Generic.WriteMessage("Aucune polyligne sélectionnée.");
                return;
            }

            double extensionValue = GetValueInteractively(ed, LastExtendDist);

            if (double.IsNaN(extensionValue))
            {
                return;
            }
            string currentMode = ExtendMode.BOTH; // Modes possibles : "START", "END", "BOTH"

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    while (true)
                    {
                        DBObjectCollection PreviewStaticEntities = new DBObjectCollection();

                        // Génération du preview
                        foreach (var selObj in selRes.Value.GetSelectionSet())
                        {
                            // On ignore si ce n'est pas une polyligne
                            if (!(tr.GetObject(selObj, OpenMode.ForRead) is Polyline poly))
                            {
                                continue;
                            }

                            // On clone pour le preview (pour ne pas modifier l'original tout de suite)
                            Polyline clonedPoly = (Polyline)poly.Clone();
                            PreviewStaticEntities.Add(clonedPoly);

                            foreach (var Seg in GetExtensionSegments(clonedPoly, currentMode, extensionValue))
                            {
                                clonedPoly.CopyPropertiesTo(Seg);
                                var HSVColor = Colors.ColorToHSV(Colors.GetRealColor(clonedPoly));
                                //HSVColor.hue = (HSVColor.hue + 180) % 360; //Negative color
                                if (HSVColor.value > 0.5)
                                {
                                    // Si la couleur est claire, on l'assombrit (sans descendre sous 0)
                                    HSVColor.value = Math.Max(0.0, HSVColor.value - 0.4);
                                }
                                else
                                {
                                    // Si la couleur est sombre, on l'éclaircit (sans dépasser 1)
                                    HSVColor.value = Math.Min(1.0, HSVColor.value + 0.4);
                                }
                                Seg.Color = Colors.FromHSV(HSVColor.hue, HSVColor.saturation, HSVColor.value);
                                PreviewStaticEntities.Add(Seg);
                            }


                        }

                        // Utilisation de ton Transient
                        using (GetPointTransient transient = new GetPointTransientNoColorChange(new DBObjectCollection(), null)
                        {
                            SetStaticEntities = PreviewStaticEntities
                        })
                        {
                            var input = transient.GetPoint("Cliquez pour valider", Points.Null, false, "Mode", "Distance");
                            var status = input.PromptPointResult.Status;

                            if (status == PromptStatus.OK)
                            {
                                // Si validé, on modifie LES ENTITÉS ORIGINALES
                                foreach (var selObj in selRes.Value.GetSelectionSet())
                                {
                                    if (tr.GetObject(selObj, OpenMode.ForWrite) is Polyline poly)
                                    {
                                        ExtendPolyline(poly, currentMode, extensionValue);
                                    }
                                }
                                //Save validate value
                                LastExtendDist = extensionValue;
                                break;
                            }
                            else if (status == PromptStatus.Keyword)
                            {
                                if (input.PromptPointResult.StringResult == "Mode")
                                {
                                    if (currentMode == ExtendMode.BOTH)
                                    {
                                        currentMode = ExtendMode.START;
                                    }
                                    else if (currentMode == ExtendMode.START)
                                    {
                                        currentMode = ExtendMode.END;
                                    }
                                    else if (currentMode == ExtendMode.END)
                                    {
                                        currentMode = ExtendMode.BOTH;
                                    }

                                    Generic.WriteMessage($"Changement de mode actuel : {currentMode}");
                                }
                                else if (input.PromptPointResult.StringResult == "Distance")
                                {
                                    // Redemander la valeur d'extension
                                    double newVal = GetValueInteractively(ed, extensionValue);
                                    if (!double.IsNaN(newVal))
                                    {
                                        extensionValue = newVal;
                                    }
                                }
                            }
                            else
                            {
                                Generic.WriteMessage("Opération annulée.");
                                return;
                            }
                        }
                    }
                }
                finally
                {
                    ed.SetImpliedSelection(selRes.GetObjectIds());
                    tr.Commit();
                }

            }
        }
        private static (Point2d? newStart, Point2d? newEnd) CalculateExtensionPoints(Polyline poly, string mode, double dist)
        {
            Point2d? newStart = null;
            Point2d? newEnd = null;

            // On n'étend pas une polyligne fermée ou qui a moins de 2 sommets
            if (poly.Closed || poly.NumberOfVertices < 2) return (newStart, newEnd);

            if (mode == ExtendMode.START || mode == ExtendMode.BOTH)
            {
                Point2d p0 = poly.GetPoint2dAt(0);
                Point2d p1 = poly.GetPoint2dAt(1);

                // Vecteur directeur du 1er segment vers l'extérieur
                Vector2d vStart = (p0 - p1).GetNormal();
                newStart = p0 + (vStart * dist);
            }

            if (mode == ExtendMode.END || mode == ExtendMode.BOTH)
            {
                int last = poly.NumberOfVertices - 1;
                Point2d pLast = poly.GetPoint2dAt(last);
                Point2d pPrev = poly.GetPoint2dAt(last - 1);

                // Vecteur directeur du dernier segment vers l'extérieur
                Vector2d vEnd = (pLast - pPrev).GetNormal();
                newEnd = pLast + (vEnd * dist);
            }

            return (newStart, newEnd);
        }

        private static void ExtendPolyline(Polyline poly, string mode, double dist)
        {
            var (newStart, newEnd) = CalculateExtensionPoints(poly, mode, dist);

            if (newStart.HasValue)
            {
                poly.SetPointAt(0, newStart.Value);
            }

            if (newEnd.HasValue)
            {
                int last = poly.NumberOfVertices - 1;
                poly.SetPointAt(last, newEnd.Value);
            }
        }

        private static System.Collections.Generic.List<Polyline> GetExtensionSegments(Polyline poly, string mode, double dist)
        {
            var previewSegments = new System.Collections.Generic.List<Polyline>();

            // On récupère les points calculés
            var (newStart, newEnd) = CalculateExtensionPoints(poly, mode, dist);

            if (newStart.HasValue)
            {
                Polyline startSeg = new Polyline();
                startSeg.AddVertexAt(0, poly.GetPoint2dAt(0), 0, 0, 0);
                startSeg.AddVertexAt(1, newStart.Value, 0, 0, 0);
                startSeg.ColorIndex = 1; // Rouge

                previewSegments.Add(startSeg);
            }

            if (newEnd.HasValue)
            {
                Polyline endSeg = new Polyline();
                int last = poly.NumberOfVertices - 1;
                endSeg.AddVertexAt(0, poly.GetPoint2dAt(last), 0, 0, 0);
                endSeg.AddVertexAt(1, newEnd.Value, 0, 0, 0);
                endSeg.ColorIndex = 1; // Rouge

                previewSegments.Add(endSeg);
            }

            return previewSegments;
        }


        private static double GetValueInteractively(Editor ed, double defaultValue)
        {
            PromptStringOptions strOpts = new PromptStringOptions("\nEntrez la distance d'extension : ")
            {
                AllowSpaces = false,
                UseDefaultValue = true,
                DefaultValue = defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };

            PromptResult strRes = ed.GetString(strOpts);
            if (strRes.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(strRes.StringResult))
            {
                Generic.WriteMessage("Entrée invalide.");
                return double.NaN;
            }

            // Remplacement sécurisé de la virgule par un point pour le parsing
            string stringVal = strRes.StringResult.Replace(',', '.');
            if (double.TryParse(stringVal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            Generic.WriteMessage("Erreur dans la saisie de la distance.");
            return double.NaN;
        }

        public class GetPointTransientNoColorChange : GetPointTransient
        {
            public GetPointTransientNoColorChange(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
            {
            }

            public override Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
            {
                return Drawable.Color;
            }

            public override Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
            {
                return Drawable.Transparency;
            }
        }
    }
}