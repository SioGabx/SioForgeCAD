using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
namespace SioForgeCAD.Commun
{
    public static class Layers
    {
        public static string GetCurrentLayerId()
        {
            return (string)AcAp.GetSystemVariable("clayer");
        }

        public static DataItemCollection GetAllLayersInDrawing()
        {
            return AcAp.UIBindings.Collections.Layers;
        }

        public static ObjectId GetLayerIdByName(Database db, string layerName)
        {
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
    }
}
