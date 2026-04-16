using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;

using SioForgeCAD.Commun.Extensions;

// using SioForgeCAD.Commun;            // À décommenter si besoin
// using SioForgeCAD.Commun.Extensions; // À décommenter si besoin
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SioForgeCAD.Functions
{
    internal class VEGBLOCEXPORTTOILLUSTRATOR
    {
        // Structure pour stocker les dimensions exactes de chaque symbole pour Illustrator
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

            PromptSelectionOptions pso = new PromptSelectionOptions();
            pso.MessageForAdding = "\nSélectionnez les blocs VEGBLOC à exporter : ";
            PromptSelectionResult selResult = ed.GetSelection(pso);
            if (selResult.Status != PromptStatus.OK) return;

            PromptEntityOptions peo = new PromptEntityOptions("\nSélectionnez la polyligne de périmètre : ");
            peo.SetRejectMessage("\nVeuillez sélectionner une Polyligne.");
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult perResult = ed.GetEntity(peo);
            if (perResult.Status != PromptStatus.OK) return;

            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'échelle du périmètre (ex: 200 pour 1/200e) : ");
            pdo.AllowZero = false;
            pdo.AllowNegative = false;
            pdo.DefaultValue = 200.0;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK) return;

            double scaleFactor = pdr.Value;
            // 1m CAD = 1000mm. On divise par l'échelle pour avoir les mm sur papier.
            // Ensuite, on convertit ces mm en Points (Pixels Illustrator) avec 72 / 25.4
            double mmToPt = 72.0 / 25.4;
            double multiplier = (1000.0 / scaleFactor) * mmToPt;

            StringBuilder svgDefs = new StringBuilder();
            StringBuilder svgUses = new StringBuilder();
            StringBuilder svgPerimeter = new StringBuilder();

            Dictionary<ObjectId, SymbolData> processedBlocks = new Dictionary<ObjectId, SymbolData>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // --- 1. LE PÉRIMÈTRE ET L'ORIGINE ---
                Polyline perimeter = tr.GetObject(perResult.ObjectId, OpenMode.ForRead) as Polyline;
                if (perimeter == null) return;

                Extents3d perimeterExtents = perimeter.GeometricExtents;
                double originX = perimeterExtents.MinPoint.X;
                double originY = perimeterExtents.MaxPoint.Y; // Le haut du rectangle (car Y AutoCAD monte)

                // Calcul de la taille de la zone pour le ViewBox
                double svgWidth = (perimeterExtents.MaxPoint.X - perimeterExtents.MinPoint.X) * multiplier;
                double svgHeight = (perimeterExtents.MaxPoint.Y - perimeterExtents.MinPoint.Y) * multiplier;

                svgPerimeter.Append("    <polygon points=\"");
                for (int i = 0; i < perimeter.NumberOfVertices; i++)
                {
                    Point2d pt = perimeter.GetPoint2dAt(i);
                    double sx = (pt.X - originX) * multiplier;
                    double sy = (originY - pt.Y) * multiplier;
                    svgPerimeter.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1} ", sx, sy);
                }
                svgPerimeter.AppendLine("\" style=\"fill:none;stroke:#FF0000;stroke-width:1;\" />");

                // --- 2. TRAITEMENT DES BLOCS ---
                foreach (SelectedObject selObj in selResult.Value)
                {
                    if (selObj?.ObjectId.IsDerivedFrom(typeof(BlockReference)) == true)
                    {
                        BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blkRef.IsXref()) continue;

                        ObjectId btrId = blkRef.DynamicBlockTableRecord;

                        string rawName = blkRef.IsDynamicBlock ? ((BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead)).Name : blkRef.Name;
                        string blockName = "VEG_" + Regex.Replace(rawName, "[^a-zA-Z0-9_\\-]", "_");

                        // --- 3. DÉFINITION DU SYMBOLE (Avec calcul du ViewBox local) ---
                        if (!processedBlocks.ContainsKey(btrId))
                        {
                            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                            StringBuilder innerGeometries = new StringBuilder();
                            Extents3d blockExtents = new Extents3d();
                            bool hasBlockExtents = false;

                            foreach (ObjectId entId in btr)
                            {
                                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                                // Calcul de l'encombrement local
                                try
                                {
                                    if (!hasBlockExtents) { blockExtents = ent.GeometricExtents; hasBlockExtents = true; }
                                    else { blockExtents.AddExtents(ent.GeometricExtents); }
                                }
                                catch { /* Ignore les entités non géométriques */ }

                                double strokeWidth = Math.Max(0.1, 0.5 / multiplier);
                                string style = $"fill:none;stroke:#333333;stroke-width:{strokeWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)};";

                                byte alphaValue = 255;
                                if (ent.Transparency.IsByAlpha) alphaValue = ent.Transparency.Alpha;
                                if (alphaValue < 255)
                                {
                                    double opacity = alphaValue / 255.0;
                                    style += $"opacity:{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)};";
                                }

                                if (ent is Circle circle)
                                {
                                    innerGeometries.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "        <circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\" style=\"{3}\" />",
                                        circle.Center.X, circle.Center.Y, circle.Radius, style));
                                }
                                else if (ent is Polyline poly)
                                {
                                    StringBuilder pts = new StringBuilder();
                                    for (int i = 0; i < poly.NumberOfVertices; i++)
                                    {
                                        Point2d pt = poly.GetPoint2dAt(i);
                                        pts.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1} ", pt.X, pt.Y);
                                    }
                                    innerGeometries.AppendLine($"        <{(poly.Closed ? "polygon" : "polyline")} points=\"{pts.ToString().Trim()}\" style=\"{style}\" />");
                                }
                                else if (ent is DBText dbText)
                                {
                                    innerGeometries.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "        <text transform=\"translate({0} {1}) scale(1 -1)\" font-family=\"Arial\" font-size=\"{2}\" fill=\"#333333\">{3}</text>",
                                        dbText.Position.X, dbText.Position.Y, dbText.Height, System.Security.SecurityElement.Escape(dbText.TextString)));
                                }
                                else if (ent is MText mText)
                                {
                                    innerGeometries.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "        <text transform=\"translate({0} {1}) scale(1 -1)\" font-family=\"Arial\" font-size=\"{2}\" fill=\"#333333\">{3}</text>",
                                        mText.Location.X, mText.Location.Y, mText.TextHeight, System.Security.SecurityElement.Escape(mText.Text)));
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
                                                pathData.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}{1},{2} ",
                                                    (pathData.Length == 0 ? "M" : "L"), bv.Vertex.X, bv.Vertex.Y);
                                            }
                                            pathData.Append("Z ");
                                        }
                                    }
                                    if (pathData.Length > 0)
                                    {
                                        innerGeometries.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                            "        <path d=\"{0}\" style=\"fill:#E7E7E7;stroke:none;\" />", pathData.ToString().Trim()));
                                    }
                                }
                            }

                            // Définition des dimensions locales pour Illustrator
                            SymbolData sData = new SymbolData { Id = blockName, MinX = 0, MinY = 0, Width = 100, Height = 100 };
                            if (hasBlockExtents)
                            {
                                sData.MinX = blockExtents.MinPoint.X;
                                sData.MinY = -blockExtents.MaxPoint.Y; // Inversion du haut CAD vers SVG
                                sData.Width = blockExtents.MaxPoint.X - blockExtents.MinPoint.X;
                                sData.Height = blockExtents.MaxPoint.Y - blockExtents.MinPoint.Y;
                            }
                            processedBlocks.Add(btrId, sData);

                            // Création du <symbol> avec le viewBox exigé par Illustrator
                            svgDefs.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "    <symbol id=\"{0}\" viewBox=\"{1} {2} {3} {4}\" style=\"overflow:visible;\">",
                                sData.Id, sData.MinX, sData.MinY, sData.Width, sData.Height));

                            svgDefs.Append(innerGeometries.ToString());
                            svgDefs.AppendLine("    </symbol>");
                        }

                        // --- 4. PLACEMENT DE L'INSTANCE (<use>) AVEC WIDTH/HEIGHT STRICTS ---
                        SymbolData symData = processedBlocks[btrId];

                        double sX = blkRef.ScaleFactors.X;
                        double sY = blkRef.ScaleFactors.Y;
                        double rot = blkRef.Rotation;

                        double a = multiplier * sX * Math.Cos(rot);
                        double b = -multiplier * sX * Math.Sin(rot);
                        double c = -multiplier * sY * Math.Sin(rot);
                        double d = -multiplier * sY * Math.Cos(rot);

                        double e = (blkRef.Position.X - originX) * multiplier;
                        double f = (originY - blkRef.Position.Y) * multiplier;

                        // On injecte x, y, width et height calqués sur le ViewBox pour satisfaire Illustrator
                        svgUses.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "    <use xlink:href=\"#{0}\" x=\"{1}\" y=\"{2}\" width=\"{3}\" height=\"{4}\" transform=\"matrix({5} {6} {7} {8} {9} {10})\" style=\"overflow:visible;\" />",
                            symData.Id, symData.MinX, symData.MinY, symData.Width, symData.Height, a, b, c, d, e, f));
                    }
                }
                tr.Commit();

                if (processedBlocks.Count == 0)
                {
                    ed.WriteMessage("\nAucun bloc n'a été traité.");
                    return;
                }

                // --- 5. ASSEMBLAGE ---
                StringBuilder finalSvg = new StringBuilder();
                finalSvg.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

                // On ajoute x="0px" y="0px" et on formate la width et height avec "px"
                finalSvg.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
                    "x=\"0px\" y=\"0px\" width=\"{0}px\" height=\"{1}px\" viewBox=\"0 0 {0} {1}\" style=\"enable-background:new 0 0 {0} {1};\" xml:space=\"preserve\">",
                    svgWidth, svgHeight));

                finalSvg.AppendLine("<defs>");
                finalSvg.Append(svgDefs.ToString());
                finalSvg.AppendLine("</defs>");

                finalSvg.Append(svgPerimeter.ToString());
                finalSvg.Append(svgUses.ToString());

                finalSvg.AppendLine("</svg>");

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ExportPlanVeg.svg");
                File.WriteAllText(path, finalSvg.ToString(), Encoding.UTF8);

                ed.WriteMessage($"\nExport SVG réussi (Dimensions et ViewBox intégrés pour Illustrator). Fichier : {path}");
            }
        }
    }
}