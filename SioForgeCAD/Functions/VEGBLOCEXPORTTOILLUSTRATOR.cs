using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace SioForgeCAD.Functions
{
    internal class VEGBLOCEXPORTTOILLUSTRATOR
    {
        private struct SymbolData
        {
            public string Id;
            public double MinX;
            public double MinY;
            public double Width;
            public double Height;
        }

        // --- MÉTHODE PRINCIPALE ---
        public static void ExportToSvg()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            if (!GetUserInput(ed, out PromptSelectionResult selResult, out ObjectId perimeterId, out double scaleFactor))
                return;

            double multiplier = (1000.0 / scaleFactor) * (72.0 / 25.4);

            StringBuilder svgDefs = new StringBuilder();
            StringBuilder svgUses = new StringBuilder();
            Dictionary<ObjectId, SymbolData> processedBlocks = new Dictionary<ObjectId, SymbolData>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 1. Périmètre et Origine
                Polyline perimeter = tr.GetObject(perimeterId, OpenMode.ForRead) as Polyline;
                if (perimeter == null) return;

                Extents3d perimeterExtents = perimeter.GeometricExtents;
                double originX = perimeterExtents.MinPoint.X;
                double originY = perimeterExtents.MaxPoint.Y;

                double svgWidth = (perimeterExtents.MaxPoint.X - perimeterExtents.MinPoint.X) * multiplier;
                double svgHeight = (perimeterExtents.MaxPoint.Y - perimeterExtents.MinPoint.Y) * multiplier;

                string svgPerimeter = GeneratePerimeterSvg(perimeter, originX, originY, multiplier);

                // 2. Traitement des blocs
                foreach (SelectedObject selObj in selResult.Value)
                {
                    if (selObj?.ObjectId.IsDerivedFrom(typeof(BlockReference)) != true) continue;

                    BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                    if (blkRef.IsXref()) continue;

                    ObjectId btrId = blkRef.DynamicBlockTableRecord;
                    string rawName = blkRef.IsDynamicBlock ? ((BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead)).Name : blkRef.Name;
                    string blockName = "VEG_" + Regex.Replace(rawName, "[^a-zA-Z0-9_\\-]", "_");

                    // Définition du symbole (Mise en cache)
                    if (!processedBlocks.ContainsKey(btrId))
                    {
                        SymbolData sData = ProcessBlockDefinition(tr, btrId, blockName, multiplier, svgDefs);
                        processedBlocks.Add(btrId, sData);
                    }

                    // Placement de l'instance
                    SymbolData symData = processedBlocks[btrId];
                    svgUses.AppendLine(GenerateUseSvg(blkRef, symData, originX, originY, multiplier));
                }
                tr.Commit();

                if (processedBlocks.Count == 0)
                {
                    ed.WriteMessage("\nAucun bloc n'a été traité.");
                    return;
                }

                // 3. Assemblage et Sauvegarde
                string finalSvg = BuildSvgDocument(svgWidth, svgHeight, svgDefs.ToString(), svgPerimeter, svgUses.ToString());
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportPlanVeg.svg");
                File.WriteAllText(path, finalSvg, Encoding.UTF8);

                ed.WriteMessage($"\nExport SVG réussi. Fichier : {path}");
            }
        }

        // --- SOUS-MÉTHODES ---

        private static bool GetUserInput(Editor ed, out PromptSelectionResult selResult, out ObjectId perimeterId, out double scaleFactor)
        {
            perimeterId = ObjectId.Null;
            scaleFactor = 200.0;

            PromptSelectionOptions pso = new PromptSelectionOptions { MessageForAdding = "\nSélectionnez les blocs VEGBLOC à exporter : " };
            selResult = ed.GetSelection(pso);
            if (selResult.Status != PromptStatus.OK) return false;

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez la polyligne de périmètre : ");
            peo.SetRejectMessage("\nVeuillez sélectionner une Polyligne.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult perResult = ed.GetEntity(peo);
            if (perResult.Status != PromptStatus.OK) return false;
            perimeterId = perResult.ObjectId;

            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'échelle du périmètre (ex: 200 pour 1/200e) : ")
            {
                AllowZero = false,
                AllowNegative = false,
                DefaultValue = 200.0
            };
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return false;
            scaleFactor = pdr.Value;

            return true;
        }

        private static string GeneratePerimeterSvg(Polyline perimeter, double originX, double originY, double multiplier)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("    <polygon points=\"");
            for (int i = 0; i < perimeter.NumberOfVertices; i++)
            {
                Point2d pt = perimeter.GetPoint2dAt(i);
                double sx = (pt.X - originX) * multiplier;
                double sy = (originY - pt.Y) * multiplier;
                sb.Append($"{F(sx)},{F(sy)} ");
            }
            sb.AppendLine("\" style=\"fill:none;stroke:#FF0000;stroke-width:1;\" />");
            return sb.ToString();
        }

        private static SymbolData ProcessBlockDefinition(Transaction tr, ObjectId btrId, string blockName, double multiplier, StringBuilder svgDefs)
        {
            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            StringBuilder innerGeometries = new StringBuilder();
            Extents3d blockExtents = new Extents3d();
            bool hasBlockExtents = false;

            foreach (ObjectId entId in btr)
            {
                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                try
                {
                    if (!hasBlockExtents) { blockExtents = ent.GeometricExtents; hasBlockExtents = true; }
                    else { blockExtents.AddExtents(ent.GeometricExtents); }
                }
                catch { /* Ignore les entités non géométriques */ }

                string entitySvg = EntityToSvg(ent, multiplier);
                if (!string.IsNullOrEmpty(entitySvg))
                {
                    innerGeometries.AppendLine(entitySvg);
                }
            }

            SymbolData sData = new SymbolData { Id = blockName, MinX = 0, MinY = 0, Width = 100, Height = 100 };
            if (hasBlockExtents)
            {
                sData.MinX = blockExtents.MinPoint.X * multiplier;
                sData.MinY = -blockExtents.MaxPoint.Y * multiplier;
                sData.Width = (blockExtents.MaxPoint.X - blockExtents.MinPoint.X) * multiplier;
                sData.Height = (blockExtents.MaxPoint.Y - blockExtents.MinPoint.Y) * multiplier;
            }

            svgDefs.AppendLine($"    <symbol id=\"{sData.Id}\" viewBox=\"{F(sData.MinX)} {F(sData.MinY)} {F(sData.Width)} {F(sData.Height)}\" style=\"overflow:visible;\">");
            svgDefs.Append(innerGeometries.ToString());
            svgDefs.AppendLine("    </symbol>");

            return sData;
        }

        private static string EntityToSvg(Entity ent, double multiplier)
        {
            string style = "fill:none;stroke:#333333;stroke-width:0.5;";
            byte alphaValue = ent.Transparency.IsByAlpha ? ent.Transparency.Alpha : (byte)255;

            if (alphaValue < 255)
            {
                style += $"opacity:{F(alphaValue / 255.0)};";
            }

            if (ent is Circle circle)
            {
                return $"        <circle cx=\"{F(circle.Center.X * multiplier)}\" cy=\"{F(circle.Center.Y * multiplier)}\" r=\"{F(circle.Radius * multiplier)}\" style=\"{style}\" />";
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
                return $"        <{tag} points=\"{pts.ToString().Trim()}\" style=\"{style}\" />";
            }
            else if (ent is DBText dbText)
            {
                string text = SecurityElement.Escape(dbText.TextString);
                return $"        <text transform=\"translate({F(dbText.Position.X * multiplier)} {F(dbText.Position.Y * multiplier)}) scale(1 -1)\" font-family=\"Arial\" font-size=\"{F(dbText.Height * multiplier)}\" fill=\"#333333\">{text}</text>";
            }
            else if (ent is MText mText)
            {
                string text = SecurityElement.Escape(mText.Text);
                return $"        <text transform=\"translate({F(mText.Location.X * multiplier)} {F(mText.Location.Y * multiplier)}) scale(1 -1)\" font-family=\"Arial\" font-size=\"{F(mText.TextHeight * multiplier)}\" fill=\"#333333\">{text}</text>";
            }
            else if (ent is Hatch hatch && hatch.Associative)
            {
                StringBuilder pathData = new StringBuilder();
                for (int i = 0; i < hatch.NumberOfLoops; i++)
                {
                    HatchLoop loop = hatch.GetLoopAt(i);
                    if (loop.IsPolyline)
                    {
                        foreach (BulgeVertex bv in loop.Polyline)
                        {
                            string cmd = pathData.Length == 0 ? "M" : "L";
                            pathData.Append($"{cmd}{F(bv.Vertex.X * multiplier)},{F(bv.Vertex.Y * multiplier)} ");
                        }
                        pathData.Append("Z ");
                    }
                }
                if (pathData.Length > 0)
                {
                    return $"        <path d=\"{pathData.ToString().Trim()}\" style=\"fill:#E7E7E7;stroke:none;\" />";
                }
            }
            return null; // Entité non supportée
        }

        private static string GenerateUseSvg(BlockReference blkRef, SymbolData symData, double originX, double originY, double multiplier)
        {
            double sX = blkRef.ScaleFactors.X;
            double sY = blkRef.ScaleFactors.Y;
            double rot = blkRef.Rotation;

            double a = sX * Math.Cos(rot);
            double b = -sX * Math.Sin(rot);
            double c = -sY * Math.Sin(rot);
            double d = -sY * Math.Cos(rot);
            double e = (blkRef.Position.X - originX) * multiplier;
            double f = (originY - blkRef.Position.Y) * multiplier;

            return $"    <use xlink:href=\"#{symData.Id}\" x=\"{F(symData.MinX)}\" y=\"{F(symData.MinY)}\" width=\"{F(symData.Width)}\" height=\"{F(symData.Height)}\" transform=\"matrix({F(a)} {F(b)} {F(c)} {F(d)} {F(e)} {F(f)})\" style=\"overflow:visible;\" />";
        }

        private static string BuildSvgDocument(double width, double height, string defs, string perimeter, string uses)
        {
            StringBuilder svg = new StringBuilder();
            svg.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            svg.AppendLine($"<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" x=\"0px\" y=\"0px\" width=\"{F(width)}px\" height=\"{F(height)}px\" viewBox=\"0 0 {F(width)} {F(height)}\" style=\"enable-background:new 0 0 {F(width)} {F(height)};\" xml:space=\"preserve\">");
            svg.AppendLine("<defs>");
            svg.Append(defs);
            svg.AppendLine("</defs>");
            svg.Append(perimeter);
            svg.Append(uses);
            svg.AppendLine("</svg>");
            return svg.ToString();
        }

        // --- UTILITAIRES ---

        /// <summary>
        /// Formate un double avec un point décimal et un maximum de 4 décimales.
        /// </summary>
        private static string F(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}