using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCLAYOUT
    {
        public static void Create()
        {
            Editor ed = Generic.GetEditor();
            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string layoutName = PromptForLayoutName();
                if (string.IsNullOrEmpty(layoutName))
                {
                    return;
                }

                Generic.WriteMessage($"Selected \"{layoutName}\"");

                var layout = ed.GetLayoutFromName(layoutName);
                if (layout == null)
                {
                    return;
                }

                var viewport = GetViewport(layout);
                var boundary = viewport?.GetBoundary();
                boundary?.PaperToModel(viewport);

                if (boundary == null)
                {
                    Generic.WriteMessage($"Aucun viewport trouvé dans le layout {layoutName}.");
                    tr.Commit();
                    return;
                }

                var vectors = new List<Vector3d>();
                bool Continue = true;
                while (Continue)
                {
                    Continue = false;
                    CollectVectors(boundary, vectors);
                    if (vectors.Count > 0)
                    {
                        var confirm = ed.GetOptions("Voulez-vous terminer et générer les présentations ?", false, "Générer", "Continuer", "Annuler");
                        if (confirm.Status == PromptStatus.OK)
                        {
                            if (confirm.StringResult == "Générer")
                            {
                                GenerateLayoutFromVectors(layout, vectors);
                            }
                            else if (confirm.StringResult == "Continuer")
                            {
                                Continue = true;
                            }
                        }
                    }
                }
                boundary?.Dispose();
                tr.Commit();
            }
        }

        private static List<Vector3d> CollectVectors(Polyline boundary, List<Vector3d> vectors)
        {
            Editor ed = Generic.GetEditor();
            var centroid = boundary.GetCentroid();
            var toOrigin = centroid.GetVectorTo(Point3d.Origin);
            boundary.TransformBy(Matrix3d.Displacement(toOrigin));

            bool AlwaysTrue(Points _, GetPointJig __) => true;

            using (var jig = new GetPointJig
            {
                Entities = new DBObjectCollection { boundary.Clone() as Entity },
                StaticEntities = new DBObjectCollection(),
                UpdateFunction = AlwaysTrue,
            })
            {
                int currentIndex = vectors.Count > 0 ? vectors.Count - 1 : -1;

                // Reconstruit toutes les entités statiques et la hachure depuis la liste des vecteurs
                void UpdateStaticEntities()
                {
                    jig.StaticEntities.DeepDispose(false);
                    jig.StaticEntities.Clear();

                    if (vectors.Count == 0)
                    {
                        currentIndex = -1;
                        return;
                    }

                    // S'assurer que l'index reste valide
                    if (currentIndex < 0) currentIndex = 0;
                    if (currentIndex >= vectors.Count) currentIndex = vectors.Count - 1;

                    Polyline selectedPolyline = null;

                    // 2. Génération des polylignes pour chaque vecteur
                    for (int i = 0; i < vectors.Count; i++)
                    {
                        var clone = boundary.Clone() as Polyline;

                        // Le vecteur part du centroid initial, mais 'boundary' est à l'Origine (0,0,0).
                        // On convertit donc le vecteur pour repartir de l'Origine dans le Jig :
                        Vector3d displacementFromOrigin = Point3d.Origin.GetVectorTo(centroid + vectors[i]);

                        clone.TransformBy(Matrix3d.Displacement(displacementFromOrigin));
                        jig.StaticEntities.Add(clone);

                        if (i == currentIndex)
                        {
                            selectedPolyline = clone;
                        }
                    }

                    // 3. Génération de la hachure de surbrillance sur le cadre sélectionné
                    if (selectedPolyline != null)
                    {
                        var highlightHatch = new Hatch();
                        highlightHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                        highlightHatch.Color = Color.FromColorIndex(ColorMethod.ByAci, 1); // Rouge
                        highlightHatch.Transparency = new Transparency(128); // 50% transparence

                        Point2dCollection pts = new Point2dCollection();
                        DoubleCollection bulges = new DoubleCollection();

                        for (int i = 0; i < selectedPolyline.NumberOfVertices; i++)
                        {
                            pts.Add(selectedPolyline.GetPoint2dAt(i));
                            bulges.Add(selectedPolyline.GetBulgeAt(i));
                        }

                        highlightHatch.AppendLoop(HatchLoopTypes.Polyline, pts, bulges);
                        highlightHatch.EvaluateHatch(true);

                        jig.StaticEntities.Add(highlightHatch);
                    }
                }

                UpdateStaticEntities();

                while (true)
                {
                    string promptMsg = vectors.Count > 0
                        ? $"\nClic = Ajouter cadre | Cadre sélectionné : [{currentIndex + 1}/{vectors.Count}]"
                        : "\nIndiquez les emplacements des présentations";

                    var res = jig.GetPoint(promptMsg, "Terminer", "Suivant", "Precedent", "Deplacer", "sUpprimer");

                    if (res.PromptPointResult.Status == PromptStatus.OK)
                    {
                        var target = res.Point.SCG;
                        vectors.Add(centroid.GetVectorTo(target));
                        currentIndex = vectors.Count - 1;
                        UpdateStaticEntities();
                    }
                    else if (res.PromptPointResult.Status == PromptStatus.Keyword)
                    {
                        string kw = res.PromptPointResult.StringResult;

                        if (kw == "Terminer")
                        {
                            break;
                        }

                        if (vectors.Count == 0)
                        {
                            continue;
                        }

                        switch (kw)
                        {
                            case "Suivant":
                                currentIndex = (currentIndex + 1) % vectors.Count;
                                UpdateStaticEntities();
                                break;

                            case "Precedent":
                                currentIndex = (currentIndex - 1 + vectors.Count) % vectors.Count;
                                UpdateStaticEntities();
                                break;

                            case "Deplacer":
                                // Masque temporairement le cadre en cours de déplacement pour la Jig dynamique
                                var tempVector = vectors[currentIndex];
                                vectors.RemoveAt(currentIndex);
                                UpdateStaticEntities();

                                string moveMsg = $"\nIndiquez le nouvel emplacement du cadre";
                                var moveRes = jig.GetPoint(moveMsg);

                                if (moveRes.PromptPointResult.Status == PromptStatus.OK)
                                {
                                    var newTarget = moveRes.Point.SCG;
                                    vectors.Insert(currentIndex, centroid.GetVectorTo(newTarget));
                                }
                                else
                                {
                                    // Restaure le vecteur d'origine en cas d'annulation
                                    vectors.Insert(currentIndex, tempVector);
                                }

                                UpdateStaticEntities();
                                break;

                            case "sUpprimer":
                                vectors.RemoveAt(currentIndex);
                                if (currentIndex >= vectors.Count && vectors.Count > 0)
                                {
                                    currentIndex = vectors.Count - 1;
                                }
                                UpdateStaticEntities();
                                break;
                        }
                    }
                    else
                    {
                        // Annulation (Échap / Clic droit)
                        break;
                    }
                }

                jig.StaticEntities.DeepDispose(false);
                jig.StaticEntities.Clear();
            }

            return vectors;
        }

        private static void GenerateLayoutFromVectors(Layout BaseLayout, List<Vector3d> Vectors)
        {
            Editor ed = Generic.GetEditor();

            foreach (var vector in Vectors)
            {
                var CloneName = GetUniqueLayoutName(BaseLayout.LayoutName);
                BaseLayout.CloneLayout(CloneName);
                var Layout = ed.GetLayoutFromName(CloneName);
                var ViewPort = GetViewport(Layout);

                if (!ViewPort.IsWriteEnabled) { ViewPort.UpgradeOpen(); }

                ViewPort.Locked = false;

                // Matrix transformation WCS -> DCS
                Matrix3d matWcsToDcs = Matrix3d.PlaneToWorld(ViewPort.ViewDirection);
                matWcsToDcs = Matrix3d.Displacement(ViewPort.ViewTarget - Point3d.Origin) * matWcsToDcs;
                matWcsToDcs = Matrix3d.Rotation(-ViewPort.TwistAngle, ViewPort.ViewDirection, ViewPort.ViewTarget) * matWcsToDcs;
                matWcsToDcs = matWcsToDcs.Inverse();

                Vector3d dcsVector = vector.TransformBy(matWcsToDcs);
                ViewPort.ViewCenter = ViewPort.ViewCenter.Add(new Vector2d(dcsVector.X, dcsVector.Y));
                ViewPort.Locked = true;
            }
        }

        public static string GetUniqueLayoutName(string baseName)
        {
            Editor ed = Generic.GetEditor();
            var ListOfLayoutNames = ed.GetAllLayout().ConvertAll(ele => ele.LayoutName);

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string newName = baseName;
                for (int index = 1; ListOfLayoutNames.Contains(newName); index++)
                {
                    newName = string.Format("{0}{1}", baseName, index.ToString("D2"));
                }
                return SymbolUtilityServices.RepairSymbolName(newName, false);
            }
        }

        private static string PromptForLayoutName()
        {
            Editor ed = Generic.GetEditor();
            List<Layout> AllLayouts = ed.GetAllLayout().OrderBy(ele => ele.TabOrder).ToList();
            List<string> LayoutsNames = AllLayouts.ConvertAll(ele => $"{ele.LayoutName}");
            if (LayoutsNames.Count == 0)
            {
                Generic.WriteMessage("Aucun layout disponible.");
                return null;
            }

            var res = ed.GetOptions("Sélectionnez le layout cible :", true, LayoutsNames.ToArray());
            string SelectedLayoutName = res.StringResult ?? string.Empty;

            return res.Status == PromptStatus.OK ? SelectedLayoutName : null;
        }

        private static Viewport GetViewport(Layout layout)
        {
            Editor ed = Generic.GetEditor();
            var btr = layout.BlockTableRecordId.GetDBObject(OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId VpObjId in ed.GetAllViewportsInPaperSpace(btr))
            {
                if (VpObjId.GetDBObject(OpenMode.ForRead) is Viewport viewport)
                {
                    return viewport;
                }
            }
            return null;
        }
    }
}