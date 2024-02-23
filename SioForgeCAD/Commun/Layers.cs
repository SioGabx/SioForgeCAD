using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows.Data;
using System.Diagnostics;
using System.Windows.Controls;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace SioForgeCAD.Commun
{
    public static class Layers
    {
        public static string GetCurrentLayerName()
        {
            return AcAp.GetSystemVariable("clayer").ToString();
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
            LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return (layerRecord != null && layerRecord.IsLocked);
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
                bool IsLocked = layerRecord != null && layerRecord.IsLocked;
                return IsLocked;
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

        public static bool CheckIfLayerExist(string layername)
        {
            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            using (Transaction acTrans = doc.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl;
                acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                acTrans.Commit();
                if (acLyrTbl.Has(layername))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }



        public static void CreateLayer(string Name, Color Color, LineWeight LineWeight, Transparency Transparence, bool IsPlottable)
        {
            Document doc = Generic.GetDocument();
            Database db = Generic.GetDatabase();
            using (Transaction acTrans = doc.TransactionManager.StartTransaction())
            {
                LayerTable acLyrTbl = acTrans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (acLyrTbl.Has(Name) == false)
                {
                    using (LayerTableRecord acLyrTblRec = new LayerTableRecord())
                    {
                        acLyrTblRec.Name = Name;
                        acLyrTblRec.Color = Color;
                        acLyrTblRec.IsPlottable = IsPlottable;
                        acLyrTblRec.LineWeight = LineWeight;
                        acLyrTbl.UpgradeOpen();
                        acLyrTbl.Add(acLyrTblRec);
                        acTrans.AddNewlyCreatedDBObject(acLyrTblRec, true);
                        acLyrTblRec.Transparency = Transparence;
                    }
                }
                acTrans.Commit();
            }
        }

        public static void Rename(string OldName, string NewName)
        {
            Database db = Generic.GetDatabase();

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                string layerName = OldName;
                string newLayerName = NewName;

                // Renommer le calque
                LayerTable lt = (LayerTable)trans.GetObject(db.LayerTableId, OpenMode.ForWrite);
                if (lt.Has(layerName))
                {
                    LayerTableRecord ltr = (LayerTableRecord)trans.GetObject(lt[layerName], OpenMode.ForWrite);
                    ltr.Name = newLayerName;
                }
                trans.Commit();
            }
        }

        public static void SetLayerColor(string LayerName, Autodesk.AutoCAD.Colors.Color color)
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
                trans.Commit();
            }
        }


        public static Autodesk.AutoCAD.Colors.Color GetLayerColor(string LayerName)
        {
            ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(LayerName);
            return GetLayerColor(LayerTableRecordObjId);
        }

        public static Autodesk.AutoCAD.Colors.Color GetLayerColor(ObjectId LayerTableRecordObjId)
        {
            Database db = Generic.GetDatabase();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTableRecord layerTableRecord = LayerTableRecordObjId.GetDBObject() as LayerTableRecord;
                trans.Commit();
                return layerTableRecord.Color;
            }
        }


    }
}
