using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class SPECIALSSELECTIONS
    {
        public static void AllOnCurrentLayer()
        {
            Editor ed = Generic.GetEditor();
            TypedValue[] tvs = new TypedValue[] {
                new TypedValue((int)DxfCode.LayerName,Layers.GetCurrentLayerName()),
            };
            SelectionFilter sf = new SelectionFilter(tvs);
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.SetImpliedSelection(psr.Value);
        }

        public static void AllOnSelectedEntitiesLayers()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            List<string> selectedLayersNames = new List<string>();


            var selResult = ed.GetSelectionRedraw(Options: new string[] { "Noms" });

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Si l'utilisateur a sélectionné des objets directement à l'écran
                if (selResult.Status == PromptStatus.OK)
                {
                    foreach (var objId in selResult.Value.GetObjectIds())
                    {
                        var layerName = objId.GetEntity().Layer;
                        if (!selectedLayersNames.Contains(layerName))
                        {
                            selectedLayersNames.Add(layerName);
                        }
                    }
                }
                else if (selResult.Status == PromptStatus.Keyword && selResult.Value.ToString() == "Noms")
                {
                    var allLayersNames = Layers.GetAllLayersInDrawings().OrderBy(t => t);
                    List<string> SelectedLayers = new List<string>();
                    if (!allLayersNames.Any())
                    {
                        Generic.WriteMessage("Aucun calque trouvé dans le dessin.");
                        return;
                    }
                    var dlg = new ComboboxDialog(allLayersNames.ToArray(), "Selectionnez les calques", false);
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedLayersNames.AddRangeUnique(dlg.GetSelectedItems());
                    }
                    else
                    {
                        return;
                    }
                }

                if (selectedLayersNames.Count == 0) { return; }

                List<TypedValue> typedValues = new List<TypedValue> { new TypedValue((int)DxfCode.Operator, "<or") };
                foreach (var layerName in selectedLayersNames)
                {
                    Generic.WriteMessage($"Sélection des entités sur le calque \"{layerName}\"");
                    typedValues.Add(new TypedValue((int)DxfCode.LayerName, layerName));
                }
                typedValues.Add(new TypedValue((int)DxfCode.Operator, "or>"));

                SelectionFilter sf = new SelectionFilter(typedValues.ToArray());
                PromptSelectionResult psr = ed.SelectAll(sf);
                if (psr.Status == PromptStatus.OK)
                {
                    ed.SetImpliedSelection(psr.Value);
                }

                tr.Commit();
            }
        }

        public static void AllWithSelectedEntitiesColors()
        {
            //    (entget (car (entsel)))
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var SelRedraw = ed.GetSelectionRedraw();
            if (SelRedraw.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Color> EntColors = new List<Color>();
                foreach (var SelItem in SelRedraw.Value.GetObjectIds())
                {
                    var EntColor = SelItem.GetEntity().Color;
                    if (!EntColors.Contains(EntColor))
                    {
                        EntColors.Add(EntColor);
                    }
                }

                PromptSelectionResult psr = ed.SelectAll();
                if (psr.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return;
                }
                HashSet<ObjectId> SameColorEntsObjId = new HashSet<ObjectId>();
                foreach (var SelItem in psr.Value.GetObjectIds())
                {
                    var EntColor = SelItem.GetEntity().Color;
                    if (EntColors.Contains(EntColor))
                    {
                        SameColorEntsObjId.Add(SelItem);
                    }
                }

                ed.SetImpliedSelection(SameColorEntsObjId.ToArray());
                tr.Commit();
            }
        }

        public static void AllWithSelectedEntitiesTypes()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var SelRedraw = ed.GetSelectionRedraw();
            if (SelRedraw.Status != PromptStatus.OK) { return; }


            Type GetTypeFromObjectId(ObjectId ObjId)
            {
                var SelItemEnt = ObjId.GetDBObject();
                if (AssocArray.IsAssociativeArray(ObjId))
                {
                    return typeof(AssocArray);
                }
                else if (SelItemEnt is Viewport)
                {
                    return typeof(Viewport);
                }
                else
                {
                    return SelItemEnt.GetType();
                }
            }


            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<Type> EntityTypes = new List<Type>();
                foreach (var SelItemObjId in SelRedraw.Value.GetObjectIds())
                {
                    var SelItemType = GetTypeFromObjectId(SelItemObjId);
                    if (!EntityTypes.Contains(SelItemType))
                    {
                        EntityTypes.Add(SelItemType);
                    }
                }

                //Check each object, we can't use SelectionFilter because ACAD_PROXY_ENTITY is ""
                PromptSelectionResult psr = ed.SelectAll();
                if (psr.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return;
                }
                HashSet<ObjectId> SameTypeEntsObjId = new HashSet<ObjectId>();

                foreach (var SelItemObjId in psr.Value.GetObjectIds())
                {
                    var SelItemType = GetTypeFromObjectId(SelItemObjId);
                    if (EntityTypes.Contains(SelItemType))
                    {
                        SameTypeEntsObjId.Add(SelItemObjId);
                    }
                }


                ed.SetImpliedSelection(SameTypeEntsObjId.ToArray());
                tr.Commit();
            }
        }

        public static void AllWithSelectedEntitiesTransparency()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var SelRedraw = ed.GetSelectionRedraw();
            if (SelRedraw.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> EntitiesTransparency = new List<string>();
                foreach (var SelItem in SelRedraw.Value.GetObjectIds())
                {
                    var SelItemEnt = SelItem.GetEntity();

                    var EntTransparency = SelItemEnt.Transparency.ToString().RemoveNonNumeric();
                    if (!EntitiesTransparency.Contains(EntTransparency))
                    {
                        EntitiesTransparency.Add(EntTransparency);
                    }
                }

                List<TypedValue> typedValues = new List<TypedValue>
                {
                    new TypedValue((int)DxfCode.Operator, "<or")
                };
                foreach (var EntTransparency in EntitiesTransparency)
                {
                    Generic.WriteMessage($"Selection des objets ayant \"{EntTransparency}\" de transparence");
                    typedValues.Add(new TypedValue((int)DxfCode.Alpha, int.Parse(EntTransparency)));
                }
                typedValues.Add(new TypedValue((int)DxfCode.Operator, "or>"));
                SelectionFilter sf = new SelectionFilter(typedValues.ToArray());
                PromptSelectionResult psr = ed.SelectAll(sf);
                ed.SetImpliedSelection(psr.Value);
                tr.Commit();
            }
        }

        public static void InsideCrossingPolyline()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Polyline Boundary = ed.GetPolyline(out ObjectId EntObjectId, "\nSélectionnez une polyligne qui delimite / croise les objects à selectionner", false))
            {
                if (Boundary is null)
                {
                    return;
                }
                Boundary.Cleanup();
                Point3dCollection collection = Boundary.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
                var SavedView = ed.GetCurrentView();
                Boundary?.GetExtents().ZoomExtents();
                var SelectCrossingPolygonResult = ed.SelectCrossingPolygon(collection);
                if (SelectCrossingPolygonResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Une erreur s'est produite lors de la selection");
                    ed.SetCurrentView(SavedView);
                    return;
                }
                var Objects = SelectCrossingPolygonResult?.Value?.GetObjectIds()?.ToList();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Objects.Remove(EntObjectId);
                    ed.SetImpliedSelection(Objects.ToArray());
                    ed.SetCurrentView(SavedView);
                    tr.Commit();
                }
            }
        }

        public static void InsideStrictPolyline()
        {
            //https://forums.autodesk.com/t5/net/cannot-get-the-entities-using-selectcrossingpolygon-and/td-p/6384137
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            using (Polyline Boundary = ed.GetPolyline("\nSélectionnez une polyligne qui delimite les objects à selectionner", false))
            {
                if (Boundary is null)
                {
                    return;
                }
                Boundary.Cleanup();
                Point3dCollection collection = Boundary.GetPoints().Distinct().ToPoint3dCollection().ConvertToUCS();
                var SavedView = ed.GetCurrentView();
                Boundary?.GetExtents().ZoomExtents();

                var SelectWindowPolygonResult = ed.SelectWindowPolygon(collection);
                if (SelectWindowPolygonResult.Status != PromptStatus.OK)
                {
                    Generic.WriteMessage("Une erreur s'est produite lors de la selection");
                    ed.SetCurrentView(SavedView); return;
                }
                var Objects = SelectWindowPolygonResult?.Value;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    ed.SetImpliedSelection(Objects);
                    ed.SetCurrentView(SavedView);
                    tr.Commit();
                }
            }
        }

        public static void AllBlockWithSelectedBlocksNames()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            List<string> BlocksNames = new List<string>();

            var SelRedraw = ed.GetSelectionRedraw(Options: new string[] { "Nom", "Liste" });


            // Si l'utilisateur a choisi l'option/mot-clé "Nom"
            if (SelRedraw.Status == PromptStatus.Keyword && SelRedraw.Value.ToString() == "Nom")
            {
                bool isNameValid = false; 
                string BlkName = string.Empty;
                List<string> AllBlksNames = BlockReferenceExtensions.GetAllBlockNames(db, false, true);
                while (!isNameValid)
                {
                    using (Forms.InputDialogBox dialogBox = new Forms.InputDialogBox())
                    {
                        dialogBox.SetPrompt($"Entrez le nom du bloc");
                        dialogBox.SetUserInputPlaceholder(BlkName);
                        dialogBox.SetCursorAtEnd();

                        if (dialogBox.ShowDialog() != DialogResult.OK)
                        {
                            return;
                        }

                        BlkName = dialogBox.GetUserInput();

                        if (string.IsNullOrWhiteSpace(BlkName))
                        {
                            Generic.WriteMessage("Impossible, le nom du block est vide.");
                            continue;
                        }

                        if (AllBlksNames.Contains(BlkName))
                        {
                            isNameValid = true;
                        }
                        else
                        {
                            var suggestions = AllBlksNames.LevenshteinDistance(BlkName);
                           
                            string Message = $"Le dessin ne contient pas de block nommé {BlkName}{(suggestions.Count > 0 ? $"\nVouliez-vous dire : {string.Join(", ", suggestions)} ?" : string.Empty)}";
                            Generic.WriteMessage(Message);

                            if (suggestions.Count == 1)
                            {
                                var result = MessageBox.Show(Message, Generic.GetExtensionDLLName(), MessageBoxButtons.YesNo);
                                if (result == DialogResult.Yes)
                                {
                                    BlkName = suggestions.FirstOrDefault();
                                    isNameValid = true;
                                }
                            }
                            else
                            {
                                MessageBox.Show(Message);
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(BlkName))
                {
                    Generic.WriteMessage("Impossible, le nom du block est vide.");
                    return;
                }

                BlocksNames.Add(BlkName);

            }
            else if (SelRedraw.Status == PromptStatus.Keyword && SelRedraw.Value.ToString() == "Liste")
            {
                List<string> AllBlksNames = BlockReferenceExtensions.GetAllBlockNames(db, false, true);
                List<string> SelectedBlkNames = new List<string>();
                if (AllBlksNames.Count == 0)
                {
                    Generic.WriteMessage("Aucun bloc trouvé dans le dessin.");
                    return;
                }
                var dlg = new ComboboxDialog(AllBlksNames.ToArray(), "Selectionnez les noms des blocks", false);
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    BlocksNames.AddRangeUnique(dlg.GetSelectedItems());
                }
                else
                {
                    return;
                }

            }
            else if (SelRedraw.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var SelItem in SelRedraw.Value.GetObjectIds())
                    {
                        if (SelItem.GetDBObject(OpenMode.ForRead) is BlockReference SelBlkRef)
                        {
                            var BlkName = SelBlkRef.GetBlockReferenceName();
                            if (!BlocksNames.Contains(BlkName))
                            {
                                BlocksNames.Add(BlkName);
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            else
            {
                return;
            }

            if (BlocksNames.Count == 0) { return; }

            // Recherche de toutes les références correspondant aux noms collectés
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                PromptSelectionResult psr = ed.SelectAll();
                if (psr.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return;
                }

                HashSet<ObjectId> BlocksWithSameName = new HashSet<ObjectId>();
                foreach (var SelItem in psr.Value.GetObjectIds())
                {
                    if (SelItem.GetDBObject(OpenMode.ForRead) is BlockReference SelBlkRef)
                    {
                        var BlkName = SelBlkRef.GetBlockReferenceName();
                        if (BlocksNames.Contains(BlkName))
                        {
                            BlocksWithSameName.Add(SelItem);
                        }
                    }
                }

                ed.SetImpliedSelection(BlocksWithSameName.ToArray());
                tr.Commit();
            }
        }
    }
}
