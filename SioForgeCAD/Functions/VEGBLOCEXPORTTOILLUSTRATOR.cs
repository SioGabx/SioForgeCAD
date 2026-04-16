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

            // Récupération de la matrice de transformation Système Général (WCS) -> Système Utilisateur (SCU)
            Matrix3d wcsToUcs = ed.CurrentUserCoordinateSystem.Inverse();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // 1. Périmètre et Origine par rapport au SCU
                Polyline perimeter = tr.GetObject(perimeterId, OpenMode.ForRead) as Polyline;
                if (perimeter == null) return;

                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;
                List<Point2d> perimeterUcsPoints = new List<Point2d>();

                // Calcul de l'encombrement du périmètre dans le référentiel SCU
                for (int i = 0; i < perimeter.NumberOfVertices; i++)
                {
                    Point3d wcsPt = perimeter.GetPoint3dAt(i);
                    Point3d ucsPt = wcsPt.TransformBy(wcsToUcs);

                    if (ucsPt.X < minX) minX = ucsPt.X;
                    if (ucsPt.Y < minY) minY = ucsPt.Y;
                    if (ucsPt.X > maxX) maxX = ucsPt.X;
                    if (ucsPt.Y > maxY) maxY = ucsPt.Y;

                    perimeterUcsPoints.Add(new Point2d(ucsPt.X, ucsPt.Y));
                }

                double originX = minX;
                double originY = maxY;

                double svgWidth = (maxX - minX) * multiplier;
                double svgHeight = (maxY - minY) * multiplier;

                string svgPerimeter = GeneratePerimeterSvg(perimeterUcsPoints, originX, originY, multiplier);

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
                    // Note: La définition interne du bloc n'a pas besoin de connaître le SCU, elle reste dans son espace local
                    if (!processedBlocks.ContainsKey(btrId))
                    {
                        SymbolData sData = ProcessBlockDefinition(tr, btrId, blockName, multiplier, svgDefs);
                        processedBlocks.Add(btrId, sData);
                    }

                    // Placement de l'instance dans le SCU
                    SymbolData symData = processedBlocks[btrId];
                    svgUses.AppendLine(GenerateUseSvg(blkRef, symData, originX, originY, multiplier, wcsToUcs));
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

        private static string GeneratePerimeterSvg(List<Point2d> ucsPoints, double originX, double originY, double multiplier)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("    <polygon points=\"");
            foreach (var pt in ucsPoints)
            {
                double sx = (pt.X - originX) * multiplier;
                double sy = (originY - pt.Y) * multiplier;
                sb.Append($"{F(sx)},{F(sy)} ");
            }
            sb.AppendLine("\" style=\"fill:none;stroke:#FF0000;stroke-width:1;\" />");
            return sb.ToString();
        }

        // --- MÉTHODE GenerateUseSvg MODIFIÉE ---
        private static string GenerateUseSvg(BlockReference blkRef, SymbolData symData, double originX, double originY, double multiplier, Matrix3d wcsToUcs)
        {
            // 1. Calcul de la position de l'insertion dans le SCU
            Point3d insPtWcs = blkRef.Position;
            Point3d insPtUcs = insPtWcs.TransformBy(wcsToUcs);

            // 2. Conversion en coordonnées SVG (Y inversé par rapport à l'origine du périmètre)
            double svgX = (insPtUcs.X - originX) * multiplier;
            double svgY = (originY - insPtUcs.Y) * multiplier;

            // 3. Ajustement : Le x,y d'un <use> cible le coin haut-gauche du viewBox du symbole.
            // On doit ajouter le décalage (MinX, MinY) du symbole pour que le point d'insertion reste correct.
            double finalX = svgX + symData.MinX;
            double finalY = svgY + symData.MinY;

            // On retourne le tag sans l'attribut transform
            return $"    <use xlink:href=\"#{symData.Id}\" x=\"{F(finalX)}\" y=\"{F(finalY)}\" width=\"{F(symData.Width)}\" height=\"{F(symData.Height)}\" />";
        }

        // --- MÉTHODE ProcessBlockDefinition MODIFIÉE ---
        private static SymbolData ProcessBlockDefinition(Transaction tr, ObjectId btrId, string blockName, double multiplier, StringBuilder svgDefs)
        {
            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
            StringBuilder innerGeometries = new StringBuilder();
            Extents3d blockExtents = new Extents3d();
            bool hasBlockExtents = false;

            // Calcul des extents pour définir le viewBox
            foreach (ObjectId entId in btr)
            {
                Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                try
                {
                    if (!hasBlockExtents) { blockExtents = ent.GeometricExtents; hasBlockExtents = true; }
                    else { blockExtents.AddExtents(ent.GeometricExtents); }
                }
                catch { }

                // Conversion avec inversion de l'axe Y intégrée
                string entitySvg = EntityToSvgInverted(ent, multiplier);
                if (!string.IsNullOrEmpty(entitySvg))
                    innerGeometries.AppendLine(entitySvg);
            }

            SymbolData sData = new SymbolData { Id = blockName, MinX = 0, MinY = 0, Width = 100, Height = 100 };
            if (hasBlockExtents)
            {
                // En SVG inversé, le MinY devient le point le plus haut (donc lié au Max Y AutoCAD)
                sData.MinX = blockExtents.MinPoint.X * multiplier;
                sData.MinY = -blockExtents.MaxPoint.Y * multiplier;
                sData.Width = (blockExtents.MaxPoint.X - blockExtents.MinPoint.X) * multiplier;
                sData.Height = (blockExtents.MaxPoint.Y - blockExtents.MinPoint.Y) * multiplier;
            }

            svgDefs.AppendLine($"    <symbol id=\"{sData.Id}\" viewBox=\"{F(sData.MinX)} {F(sData.MinY)} {F(sData.Width)} {F(sData.Height)}\" overflow=\"visible\">");
            svgDefs.Append(innerGeometries.ToString());
            svgDefs.AppendLine("    </symbol>");

            return sData;
        }

        // --- NOUVELLE MÉTHODE POUR LA GÉOMÉTRIE INVERSÉE ---
        private static string EntityToSvgInverted(Entity ent, double multiplier)
        {
            // Préparation du style de base
            string strokeColor = "#333333";
            string style = $"fill:none;stroke:{strokeColor};stroke-width:0.5;";

            // Gestion de la transparence (Alpha)
            byte alphaValue = ent.Transparency.IsByAlpha ? ent.Transparency.Alpha : (byte)255;
            if (alphaValue < 255)
            {
                style += $"opacity:{F(alphaValue / 255.0)};";
            }

            // 1. CERCLES
            if (ent is Circle circle)
            {
                return $"        <circle cx=\"{F(circle.Center.X * multiplier)}\" cy=\"{F(-circle.Center.Y * multiplier)}\" r=\"{F(circle.Radius * multiplier)}\" style=\"{style}\" />";
            }

            // 2. POLYLIGNES
            else if (ent is Polyline poly)
            {
                StringBuilder pts = new StringBuilder();
                for (int i = 0; i < poly.NumberOfVertices; i++)
                {
                    Point2d pt = poly.GetPoint2dAt(i);
                    // Inversion de l'axe Y : -pt.Y
                    pts.Append($"{F(pt.X * multiplier)},{F(-pt.Y * multiplier)} ");
                }
                string tag = poly.Closed ? "polygon" : "polyline";
                return $"        <{tag} points=\"{pts.ToString().Trim()}\" style=\"{style}\" />";
            }

            // 3. TEXTE SIMPLE (DBText)
            else if (ent is DBText dbText)
            {
                string text = SecurityElement.Escape(dbText.TextString);
                double posX = dbText.Position.X * multiplier;
                double posY = -dbText.Position.Y * multiplier; // Inversion Y
                double fontSize = dbText.Height * multiplier;

                // Note : On n'utilise pas de scale(1 -1) ici pour que le texte reste lisible.
                // L'ancrage est mis à 'middle' pour correspondre aux blocs de végétation centrés.
                return $"        <text x=\"{F(posX)}\" y=\"{F(posY)}\" font-family=\"Arial\" font-size=\"{F(fontSize)}\" fill=\"{strokeColor}\" text-anchor=\"middle\">{text}</text>";
            }

            // 4. TEXTE MULTI-LIGNE (MText)
            else if (ent is MText mText)
            {
                // Nettoyage des codes de formatage AutoCAD (\P, \f, etc.)
                string cleanText = Regex.Replace(mText.Contents, @"\\P|\\f[^;]*;|\\L|\\l|\\S[^;]*;|\\T[^;]*;|\\Q[^;]*;|\\W[^;]*;|\\A[^;]*;|\\H[^;]*;|\\C[^;]*;|[{}]", " ");
                string text = SecurityElement.Escape(cleanText.Trim());

                double posX = mText.Location.X * multiplier;
                double posY = -mText.Location.Y * multiplier; // Inversion Y
                double fontSize = mText.TextHeight * multiplier;

                return $"        <text x=\"{F(posX)}\" y=\"{F(posY)}\" font-family=\"Arial\" font-size=\"{F(fontSize)}\" fill=\"{strokeColor}\" text-anchor=\"middle\">{text}</text>";
            }

            // 5. HACHURES (Hatch)
            else if (ent is Hatch hatch)
            {
                StringBuilder pathData = new StringBuilder();
                for (int i = 0; i < hatch.NumberOfLoops; i++)
                {
                    HatchLoop loop = hatch.GetLoopAt(i);
                    if (loop.IsPolyline)
                    {
                        bool first = true;
                        foreach (BulgeVertex bv in loop.Polyline)
                        {
                            string cmd = first ? "M" : "L";
                            pathData.Append($"{cmd}{F(bv.Vertex.X * multiplier)},{F(-bv.Vertex.Y * multiplier)} ");
                            first = false;
                        }
                        pathData.Append("Z ");
                    }
                }

                if (pathData.Length > 0)
                {
                    // Style de remplissage par défaut pour les hachures
                    return $"        <path d=\"{pathData.ToString().Trim()}\" style=\"fill:#E7E7E7;stroke:none;opacity:0.6;\" />";
                }
            }

            return null; // Entité non supportée
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

        private static string F(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}