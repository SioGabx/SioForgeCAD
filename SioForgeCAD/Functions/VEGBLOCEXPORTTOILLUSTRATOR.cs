using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCEXPORTTOILLUSTRATOR
    {
        private struct SymbolData
        {
            public string Id;
            public double MinX;
            public double MinY;
            public double Width;
            public double Height;
        }

        public static void ExportToSvg()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            if (!GetUserInput(ed, out PromptSelectionResult selResult, out ObjectId perimeterId, out double scaleFactor))
                return;

            // Facteur de conversion (72 DPI)
            double multiplier = (1000.0 / scaleFactor) * (72.0 / 25.4);

            StringBuilder svgDefs = new StringBuilder();
            StringBuilder svgContent = new StringBuilder();
            Dictionary<ObjectId, SymbolData> processedBlocks = new Dictionary<ObjectId, SymbolData>();

            Matrix3d wcsToUcs = ed.CurrentUserCoordinateSystem.Inverse();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Polyline perimeter = tr.GetObject(perimeterId, OpenMode.ForRead) as Polyline;
                if (perimeter == null) return;

                // Calcul de l'emprise (Bounding Box)
                Extents3d ucsExtents = GetUcsExtents(perimeter, wcsToUcs);
                double originX = ucsExtents.MinPoint.X;
                double originY = ucsExtents.MinPoint.Y; // On prend le bas pour le système Y-up
                double svgWidth = (ucsExtents.MaxPoint.X - ucsExtents.MinPoint.X) * multiplier;
                double svgHeight = (ucsExtents.MaxPoint.Y - ucsExtents.MinPoint.Y) * multiplier;

                // 1. Périmètre (en coordonnées AutoCAD pures)
                svgContent.AppendLine(GeneratePerimeterSvg(perimeter, originX, originY, multiplier, wcsToUcs));

                // 2. Traitement des blocs
                foreach (SelectedObject selObj in selResult.Value)
                {
                    if (selObj?.ObjectId.IsDerivedFrom(typeof(BlockReference)) != true) continue;
                    BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;

                    ObjectId btrId = blkRef.DynamicBlockTableRecord;
                    string rawName = blkRef.IsDynamicBlock ? ((BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead)).Name : blkRef.Name;
                    string blockName = "VEG_" + Regex.Replace(rawName, "[^a-zA-Z0-9_\\-]", "_");

                    if (!processedBlocks.TryGetValue(btrId, out SymbolData value))
                    {
                        value = ProcessBlockDefinition(tr, btrId, blockName, multiplier, svgDefs);
                        processedBlocks.Add(btrId, value);
                    }

                    svgContent.AppendLine(GenerateUseSvg(blkRef, value, originX, originY, multiplier, wcsToUcs));
                }
                tr.Commit();

                // 3. Assemblage Final avec le FLIP GLOBAL
                string finalSvg = BuildSvgDocument(svgWidth, svgHeight, svgDefs.ToString(), svgContent.ToString());
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportPlanVeg.svg");
                File.WriteAllText(path, finalSvg, Encoding.UTF8);

                Generic.WriteMessage($"Export réussi : {path}");
            }
        }

        private static string GeneratePerimeterSvg(Polyline poly, double originX, double originY, double multiplier, Matrix3d wcsToUcs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<polygon points=\"");
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point3d ucsPt = poly.GetPoint3dAt(i).TransformBy(wcsToUcs);
                double x = (ucsPt.X - originX) * multiplier;
                double y = (ucsPt.Y - originY) * multiplier; // Coordonnées AutoCAD pures
                sb.Append($"{F(x)},{F(y)} ");
            }
            sb.Append("\" style=\"fill:none;stroke:#FF0000;stroke-width:1;\" />");
            return sb.ToString();
        }

        private static SymbolData ProcessBlockDefinition(Transaction tr, ObjectId btrId, string blockName, double multiplier, StringBuilder svgDefs)
        {
            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            StringBuilder innerGeometries = new StringBuilder();
            Extents3d ext = new Extents3d();
            bool hasExt = false;

            var entIds = btr.ToList<ObjectId>();
            //Order first by Hatch, then Text, then MText, then others to ensure proper layering in SVG
            entIds.Sort((a, b) =>
            {
                int GetPriority(ObjectId EntId)
                {
                    if (EntId.IsDerivedFrom(typeof(Hatch))) return 1;
                    return int.MaxValue;
                }
                return GetPriority(a).CompareTo(GetPriority(b));
            });

            HashSet<ObjectId> processedEnts = new HashSet<ObjectId>();
            Dictionary<string, Layers.LayerStatus> LayersProps = Layers.GetAllLayersPropertiesInDrawing();

            foreach (ObjectId entId in entIds)
            {
                if (processedEnts.Contains(entId)) continue; // Skip already processed entities (in case of duplicates)
                processedEnts.Add(entId);
                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                if (!LayersProps[ent.Layer].WillPlot) { continue; }

                if (ent is Hatch hatch && hatch.Associative && hatch.GetAssociatedBoundary(out var Boundary, false) > 0)
                {
                    processedEnts.Add(Boundary.ObjectId);
                }
                try
                {
                    if (!hasExt) { ext = ent.GeometricExtents; hasExt = true; }
                    else { ext.AddExtents(ent.GeometricExtents); }
                }
                catch { }

                innerGeometries.AppendLine(EntityToSvgNormal(ent, multiplier));
            }

            SymbolData sData = new SymbolData
            {
                Id = blockName,
                MinX = ext.MinPoint.X * multiplier,
                MinY = ext.MinPoint.Y * multiplier,
                Width = (ext.MaxPoint.X - ext.MinPoint.X) * multiplier,
                Height = (ext.MaxPoint.Y - ext.MinPoint.Y) * multiplier
            };

            // On définit le symbole normalement (Y vers le haut)
            svgDefs.AppendLine($"<symbol id=\"{sData.Id}\" viewBox=\"{F(sData.MinX)} {F(sData.MinY)} {F(sData.Width)} {F(sData.Height)}\" overflow=\"visible\">");
            svgDefs.Append(innerGeometries.ToString());
            svgDefs.AppendLine("</symbol>");

            return sData;
        }

        private static string EntityToSvgNormal(Entity ent, double multiplier, string strokeColor = "#333333", double strokeWidth = 0.5, string fillColor = "none")
        {
            // Préparation du style de base
            string style = $"fill:{fillColor};stroke:{strokeColor};stroke-width:{strokeWidth};";

            // Gestion de la transparence (Alpha)
            byte alphaValue = ent.Transparency.IsByAlpha ? ent.Transparency.Alpha : (byte)255;
            if (alphaValue < 255)
            {
                style += $"opacity:{F(alphaValue / 255.0)};";
            }

            if (ent is Circle circle)
            {
                return $"<circle cx=\"{F(circle.Center.X * multiplier)}\" cy=\"{F(circle.Center.Y * multiplier)}\" r=\"{F(circle.Radius * multiplier)}\" style=\"{style}\" />";
            }

            else if (ent is Polyline poly)
            {
                StringBuilder pts = new StringBuilder();
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    Point2d pt = poly.GetPoint2dAt(i);
                    pts.Append($"{F(pt.X * multiplier)},{F(pt.Y * multiplier)} ");
                }
                string tag = poly.Closed ? "polygon" : "polyline";
                return $"<{tag} points=\"{pts.ToString().Trim()}\" style=\"{style}\" />";
            }
            else if (ent is DBText dbText)
            {
                string text = SecurityElement.Escape(dbText.TextString);
                return GenerateWrappedText(dbText.TextString, dbText.Position.X, dbText.Position.Y, dbText.Height, strokeColor);
            }
            else if (ent is AttributeDefinition dbAttDef)
            {
                string text = SecurityElement.Escape(dbAttDef.TextString);
                return GenerateWrappedText(dbAttDef.TextString, dbAttDef.Position.X, dbAttDef.Position.Y, dbAttDef.Height, strokeColor);
            }
            else if (ent is MText mText)
            {
                // Nettoyage des codes de formatage AutoCAD (\P, \f, etc.)
                string cleanText = Regex.Replace(mText.Contents, @"\\P|\\f[^;]*;|\\L|\\l|\\S[^;]*;|\\T[^;]*;|\\Q[^;]*;|\\W[^;]*;|\\A[^;]*;|\\H[^;]*;|\\C[^;]*;|[{}]", " ");
                return GenerateWrappedText(cleanText, mText.Location.X, mText.Location.Y, mText.TextHeight, strokeColor);
            }
            else if (ent is Hatch hatch)
            {
                StringBuilder pathData = new StringBuilder();
                if (hatch.GetHatchPolyline(out List<Curve> ExternalCurves, out List<(Curve curve, HatchLoopTypes looptype)> OtherCurves))
                {
                    var StrokeColor = "none";
                    if (hatch.Associative && hatch.GetAssociatedBoundary(out Curve AssociatedBoundary, false) > 0)
                    {
                        StrokeColor = GetEntColorHex(AssociatedBoundary);
                    }

                    foreach (var item in ExternalCurves.JoinMerge())
                    {
                        pathData.Append(EntityToSvgNormal(item, multiplier, StrokeColor, 0, GetEntColorHex(hatch)));
                    }
                    ExternalCurves.DeepDispose();
                    OtherCurves.ConvertAll(t => t.curve).DeepDispose();
                }
                return pathData.ToString();
            }

            return string.Empty;

            string GenerateWrappedText(string rawText, double x, double y, double height, string color)
            {
                string textContent = SecurityElement.Escape(rawText.Trim());
                // Découpage du texte en mots (vous pouvez ajuster la logique de découpage ici)
                string[] words = textContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                double tx = x * multiplier;
                double ty = y * multiplier;
                double fontSize = height * multiplier;
                double lineHeight = fontSize * 1.2; // Espacement de 120% entre les lignes

                StringBuilder sb = new StringBuilder();
                sb.Append($"<text transform=\"translate({F(tx)} {F(ty)}) scale(1 -1)\" font-family=\"Arial\" font-size=\"{F(fontSize)}\" fill=\"{color}\" text-anchor=\"middle\">");

                for (int i = 0; i < words.Length; i++)
                {
                    // Calcul du dy : 
                    // Pour la première ligne, on remonte un peu pour centrer le bloc (-0.3em environ)
                    // Pour les suivantes, on descend d'une hauteur de ligne
                    string dy = (i == 0) ? $"-{F(lineHeight * 0.4)}" : F(lineHeight);

                    sb.Append($"<tspan x=\"0\" dy=\"{dy}\">{words[i]}</tspan>");
                }

                sb.Append("</text>");
                return sb.ToString();
            }
        }

        private static string GetEntColorHex(Entity ent)
        {
            if (ent.Color.IsByBlock)
            {
                return "inherit";
            }
            return ent.GetRealColor().ColorToHex();
        }

        private static string GenerateUseSvg(BlockReference blkRef, SymbolData symData, double originX, double originY, double multiplier, Matrix3d wcsToUcs)
        {
            Point3d ucsPt = blkRef.Position.TransformBy(wcsToUcs);

            // Coordonnées AutoCAD pures relatives à l'origine bas-gauche
            double x = ((ucsPt.X - originX) * multiplier) + symData.MinX;
            double y = ((ucsPt.Y - originY) * multiplier) + symData.MinY;

            return $"<use xlink:href=\"#{symData.Id}\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(symData.Width)}\" height=\"{F(symData.Height)}\"  fill=\"{GetEntColorHex(blkRef)}\"/>";
        }

        private static string BuildSvgDocument(double width, double height, string defs, string content)
        {
            StringBuilder svg = new StringBuilder();
            svg.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            svg.AppendLine($"<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"{F(width)}\" height=\"{F(height)}\" viewBox=\"0 0 {F(width)} {F(height)}\">");
            svg.AppendLine("<defs>" + defs + "</defs>");

            // --- LE SECRET EST ICI ---
            // On crée un groupe qui flippe TOUT le dessin. 
            // 1. On translate de la hauteur du SVG
            // 2. On scale Y par -1
            // Ainsi, l'origine (0,0) est en bas à gauche et Y monte, exactement comme dans AutoCAD.
            svg.AppendLine($"<g transform=\"translate(0, {F(height)}) scale(1, -1)\">");
            svg.Append(content);
            svg.AppendLine("</g>");

            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        private static Extents3d GetUcsExtents(Polyline p, Matrix3d wcsToUcs)
        {
            Extents3d ex = new Extents3d();
            for (int i = 0; i < p.NumberOfVertices; i++) ex.AddPoint(p.GetPoint3dAt(i).TransformBy(wcsToUcs));
            return ex;
        }

        private static string F(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
        private static bool GetUserInput(Editor ed, out PromptSelectionResult sel, out ObjectId id, out double s)
        {
            id = ObjectId.Null; s = 200;
            sel = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSélectionnez les blocs : " });
            if (sel.Status != PromptStatus.OK) return false;
            var per = ed.GetEntity("\nPérimètre : ");
            if (per.Status != PromptStatus.OK) return false;
            id = per.ObjectId;
            var sc = ed.GetDouble("\nÉchelle (ex: 200) : ");
            if (sc.Status == PromptStatus.OK) s = sc.Value;
            return true;
        }
    }
}