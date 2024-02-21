using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
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




        public static Autodesk.AutoCAD.Colors.Color GetLayerColor(string LayerName)
        {
            ObjectId LayerTableRecordObjId = Layers.GetLayerIdByName(LayerName);
            return GetLayerColor(LayerTableRecordObjId);
        }

        public static Autodesk.AutoCAD.Colors.Color GetLayerColor(ObjectId LayerTableRecordObjId)
        {
            LayerTableRecord layerTableRecord = LayerTableRecordObjId.GetDBObject() as LayerTableRecord;
            return layerTableRecord.Color;
        }


    }
}
