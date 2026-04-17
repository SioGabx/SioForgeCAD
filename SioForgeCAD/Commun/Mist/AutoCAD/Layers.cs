using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.LayerManager;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace SioForgeCAD.Commun
{
    public static class Layers
    {
        public static (DataGridView LayerGrid, LayerManagerControl LayerManager) GetLayerPaletteControls()
        {
            FieldInfo field = typeof(PaletteHost).GetField("layerManager_", BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                object rslt = field.GetValue(null);
                if (rslt is LayerManagerControl lmc)
                {
                    if (lmc.FindControlByTypeName("LayerGrid") is DataGridView LayerGridControl)//Autodesk.AutoCAD.LayerManager.LayerGrid;
                    {
                        return (LayerGridControl, lmc);
                    }
                }
            }

            return (null, null);
        }

        public static string GetCurrentLayerName()
        {
            return Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("clayer").ToString();
        }
        public static void SetCurrentLayerName(string LayerName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable ltb = (LayerTable)db.LayerTableId.GetDBObject(OpenMode.ForRead);
                db.Clayer = ltb[LayerName];
                tr.Commit();
            }
        }

        public static DataItemCollection GetAllLayersInDrawing()
        {
            return AcAp.UIBindings.Collections.Layers;
        }

        public static bool IsEntityOnLockedLayer(ObjectId entity)
        {
            return IsEntityOnLockedLayer(entity.GetEntity());
        }

        public static bool IsEntityOnLockedLayer(this Entity entity)
        {
            ObjectId layerId = entity.LayerId;
            if (layerId.GetNoTransactionDBObject(OpenMode.ForRead) is LayerTableRecord layerRecord)
            {
                return layerRecord?.IsLocked == true;
            }
            return true;
        }

        public static bool IsLayerLocked(string Name)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId layerId = layerTable[Name];
                LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
                trans.Commit();
                return layerRecord?.IsLocked == true;
            }
        }

        public static Transparency GetTransparency(string Name)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId layerId = layerTable[Name];
                LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
                trans.Commit();
                return layerRecord.Transparency;
            }
        }

        public static void SetTransparency(string Name, Transparency transparency)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId layerId = layerTable[Name];
                LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
                layerRecord.Transparency = transparency;
                trans.Commit();
            }
        }

        public static void SetLock(string Name, bool Lock)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                ObjectId layerId = layerTable[Name];
                LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForWrite) as LayerTableRecord;
                layerRecord.IsLocked = Lock;
                trans.Commit();
            }
        }

        public static ObjectId GetLayerIdByName(string layerName, Database db = null)
        {
            if (db == null)
            {
                db = Generic.GetDatabase();
            }
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable layerTable = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (layerTable.Has(layerName))
                {
                    ObjectId layerId = layerTable[layerName];
                    trans.Commit();
                    return layerId;
                }

                return ObjectId.Null;
            }
        }

        public static LayerTableRecord GetLayerTableRecordByName(string layerName, Database db = null)
        {
            if (db == null)
            {
                db = Generic.GetDatabase();
            }
            var LayId = GetLayerIdByName(layerName, db);
            if (LayId == ObjectId.Null)
            {
                return null;
            }
            return LayId.GetDBObject() as LayerTableRecord;
        }

        public static bool CheckIfLayerExist(string layername)
        {
            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            using (Transaction acTrans = doc.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                acTrans.Commit();
                return acLyrTbl.Has(layername);
            }
        }

        public static ObjectId CreateLayer(string Name, Color Color, LineWeight LineWeight, Transparency Transparence, bool IsPlottable)
        {
            Database db = Generic.GetDatabase();
            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                ObjectId LayerId = ObjectId.Null;
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!acLyrTbl.Has(Name))
                {
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        acLyrTblRec.Name = Name;
                        acLyrTblRec.Color = Color;
                        acLyrTblRec.IsPlottable = IsPlottable;
                        acLyrTblRec.LineWeight = LineWeight;
                        acLyrTbl.UpgradeOpen();
                        LayerId = acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                        acLyrTblRec.Transparency = Transparence;
                    }
                }
                else
                {
                    LayerTableRecord ltr = (LayerTableRecord)acTrans.GetObject(acLyrTbl[Name], OpenMode.ForWrite);
                    ltr.Name = Name;

                    LayerId = ltr.ObjectId;
                }
                acTrans.Commit();
                return LayerId;
            }
        }

        public static bool Rename(string OldName, string NewName)
        {
            Database db = Generic.GetDatabase();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    string layerName = OldName;
                    string newLayerName = NewName;

                    // Renommer le calque
                    LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
                    if (lt.Has(layerName))
                    {
                        LayerTableRecord ltr = (LayerTableRecord)trans.GetObject(lt[layerName], OpenMode.ForWrite);
                        ltr.Name = newLayerName;
                        return true;
                    }
                    return false;
                }
                finally
                {
                    trans.Commit();
                }
            }
        }

        public static void SetLayerColor(string LayerName, Color color)
        {
            Database db = Generic.GetDatabase();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                // Renommer le calque
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
                if (lt.Has(LayerName))
                {
                    LayerTableRecord ltr = (LayerTableRecord)trans.GetObject(lt[LayerName], OpenMode.ForWrite);
                    ltr.Color = color;
                }
                trans.Commit();
            }
        }

        public static void Merge(string sourceLayerName, string targetLayerName)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
                //Has is IgnoreCase, so we need to check that we are not merging the same layer
                if (!string.Equals(sourceLayerName, targetLayerName, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    if (lt.Has(sourceLayerName) && lt.Has(targetLayerName))
                    {
                        // Iterate through all entities in the drawing
                        foreach (ObjectId objId in btr)
                        {
                            Entity ent = objId.GetEntity(OpenMode.ForRead);
                            if (ent.Layer == sourceLayerName)
                            {
                                ent.UpgradeOpen();
                                ent.Layer = targetLayerName;
                            }
                        }
                    }
                    try
                    {
                        ObjectId sourceLayerId = lt[sourceLayerName];
                        LayerTableRecord sourceLayer = (LayerTableRecord)trans.GetObject(sourceLayerId, OpenMode.ForWrite);
                        if (sourceLayerName != targetLayerName)
                        {
                            if (GetCurrentLayerName() == sourceLayerName)
                            {
                                SetCurrentLayerName(targetLayerName);
                            }
                            sourceLayer.Erase();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
                trans.Commit();
            }
        }

        public static void Merge(Transaction tr, Database db, ObjectId sourceLayerId, ObjectId targetLayerId)
        {
            LayerTableRecord sourceLayer = tr.GetObject(sourceLayerId, OpenMode.ForRead) as LayerTableRecord;
            LayerTableRecord targetLayer = tr.GetObject(targetLayerId, OpenMode.ForRead) as LayerTableRecord;

            if (sourceLayer == null || targetLayer == null)
            {
                return;
            }

            string sourceLayerName = sourceLayer.Name;

            // Move every entities
            foreach (ObjectId blockId in tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable)
            {
                foreach (ObjectId entId in tr.GetObject(blockId, OpenMode.ForWrite) as BlockTableRecord)
                {
                    Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                    if (ent != null && ent.LayerId == sourceLayerId)
                    {
                        if (ent.IsEntityOnLockedLayer())
                        {
                            SetLock(ent.Layer, false);
                        }
                        ent.UpgradeOpen();
                        ent.LayerId = targetLayerId;
                    }
                }
            }

            try
            {
                if (!sourceLayer.IsErased && !sourceLayer.IsDependent)
                {
                    sourceLayer.UpgradeOpen();
                    sourceLayer.Erase(true);
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                Generic.WriteMessage($"Impossible de supprimer le calque {sourceLayerName}: {ex.Message}");
            }
        }

        public static Dictionary<string, int> GetLayerCountsFromCollection(ObjectIdCollection ids)
        {
            var layerStats = new Dictionary<string, int>();

            if (ids == null || ids.Count == 0)
            {
                return layerStats;
            }

            Database db = Generic.GetDatabase();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (id.IsValid && id.ObjectClass.IsDerivedFrom(RXClass.GetClass(typeof(Entity))))
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;

                        if (ent != null)
                        {
                            string layerName = ent.Layer;

                            if (layerStats.TryGetValue(layerName, out int value))
                            {
                                layerStats[layerName] = ++value;
                            }
                            else
                            {
                                layerStats[layerName] = 1;
                            }
                        }
                    }
                }
                tr.Commit();
            }

            return layerStats.OrderByDescending(x => x.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public static Color GetLayerColor(string LayerName)
        {
            ObjectId LayerTableRecordObjId = GetLayerIdByName(LayerName);
            return GetLayerColor(LayerTableRecordObjId);
        }

        public static Color GetLayerColor(ObjectId LayerTableRecordObjId)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTableRecord layerTableRecord = LayerTableRecordObjId.GetDBObject() as LayerTableRecord;
                trans.Commit();
                return layerTableRecord?.Color ?? Color.FromRgb(0, 0, 0);
            }
        }
    }
}
