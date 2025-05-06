using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

            var SelRedraw = ed.GetSelectionRedraw();
            if (SelRedraw.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> LayerNames = new List<string>();
                foreach (var SelItem in SelRedraw.Value.GetObjectIds())
                {
                    var LayerName = SelItem.GetEntity().Layer;
                    if (!LayerNames.Contains(LayerName))
                    {
                        LayerNames.Add(LayerName);
                    }
                }

                List<TypedValue> typedValues = new List<TypedValue>
                {
                    new TypedValue((int)DxfCode.Operator, "<or")
                };
                foreach (var LayerName in LayerNames)
                {
                    Generic.WriteMessage($"Selection des entités sur le calque \"{LayerName}\"");
                    typedValues.Add(new TypedValue((int)DxfCode.LayerName, LayerName));
                }
                typedValues.Add(new TypedValue((int)DxfCode.Operator, "or>"));
                SelectionFilter sf = new SelectionFilter(typedValues.ToArray());
                PromptSelectionResult psr = ed.SelectAll(sf);
                ed.SetImpliedSelection(psr.Value);
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

            var SelRedraw = ed.GetSelectionRedraw();
            if (SelRedraw.Status != PromptStatus.OK) { return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> BlocksNames = new List<string>();
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
