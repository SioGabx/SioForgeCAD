using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Windows.Data;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace SioForgeCAD.Commun
{
    public static class Layers
    {
        public static string GetCurrentLayerName()
        {
            return AcAp.GetSystemVariable("clayer").ToString();
        }

        public static DataItemCollection GetAllLayersInDrawing()
        {
            return AcAp.UIBindings.Collections.Layers;
        }
        public static bool IsLayerLocked(ObjectId entity)
        {
            ObjectId layerId = entity.GetEntity().LayerId;
            LayerTableRecord layerRecord = layerId.GetObject(OpenMode.ForRead) as LayerTableRecord;
            return (layerRecord != null && layerRecord.IsLocked);
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

        public static Autodesk.AutoCAD.Colors.Color GetLayerColor(ObjectId LayerTableRecordObjId)
        {
            LayerTableRecord layerTableRecord = LayerTableRecordObjId.GetDBObject() as LayerTableRecord;
            return layerTableRecord.Color;
        }


    }
}
