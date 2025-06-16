using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Extensions
{
    public static class LayoutsExtensions
    {
        public static string GetUniqueLayoutName(this Editor _, string oldName)
        {
            var ListOfLayoutNames = _.GetAllLayout().ConvertAll(ele => ele.LayoutName);

            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                string newName = oldName;
                for (int index = 1; ListOfLayoutNames.Contains(newName); index++)
                {
                    newName = $"{oldName}_Copy{(index > 1 ? $" ({index})" : "")}";
                }
                return SymbolUtilityServices.RepairSymbolName(newName, false);
            }
        }


        //public static void CloneLayout(this Layout sourceLayout, string newLayoutName)
        //{
        //    Document doc = Application.DocumentManager.MdiActiveDocument;
        //    Database db = doc.Database;
        //    Editor ed = doc.Editor;

        //    using (Transaction tr = db.TransactionManager.StartTransaction())
        //    {

        //        BlockTableRecord sourceBtr = (BlockTableRecord)tr.GetObject(sourceLayout.BlockTableRecordId, OpenMode.ForRead);

        //        // Clone du BlockTableRecord (contenu de la layout)
        //        BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
        //        string newBtrName = "*Paper_Space" + DateTime.Now.Ticks;  // Nom provisoire automatique
        //        BlockTableRecord newBtr = new BlockTableRecord
        //        {
        //            Name = newBtrName,
        //            Origin = sourceBtr.Origin,
        //            LayoutId = ObjectId.Null // Lié ensuite
        //        };
        //        ObjectId newBtrId = bt.Add(newBtr);
        //        tr.AddNewlyCreatedDBObject(newBtr, true);

        //        // Clone des entités de la layout
        //        foreach (ObjectId id in sourceBtr)
        //        {
        //            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        //            if (ent != null)
        //            {
        //                Entity clone = ent.Clone() as Entity;
        //                newBtr.AppendEntity(clone);
        //                tr.AddNewlyCreatedDBObject(clone, true);
        //            }
        //        }

        //        // Création de la nouvelle layout
        //        LayoutManager lm = LayoutManager.Current;
        //        ObjectId newLayoutId = lm.CreateLayout(newLayoutName);
        //        Layout newLayout = (Layout)tr.GetObject(newLayoutId, OpenMode.ForWrite);
        //        newLayout.BlockTableRecordId
        //        newLayout.CopyFrom(sourceLayout); // Copie propriétés (format, marges, etc.)

        //        newLayout.BlockTableRecordId = newBtrId;
        //        newBtr.LayoutId = newLayoutId;

        //        tr.Commit();
        //        ed.WriteMessage($"\nLayout '{newLayoutName}' créée avec succès.");
        //    }
        //}
        public static void CloneLayout(this Layout sourceLayout, string newLayoutName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord sourceBtr = (BlockTableRecord)tr.GetObject(sourceLayout.BlockTableRecordId, OpenMode.ForRead);
                LayoutManager lm = LayoutManager.Current;

                // 1. Crée la nouvelle layout  ➜ AutoCAD génère son BTR "*Paper_Space#"
                ObjectId newLayoutId = lm.CreateLayout(newLayoutName);
                Layout newLayout = (Layout)tr.GetObject(newLayoutId, OpenMode.ForWrite);
                BlockTableRecord newBtr = (BlockTableRecord)tr.GetObject(newLayout.BlockTableRecordId, OpenMode.ForWrite);

                // 2. Duplique les objets depuis la layout source
                IdMapping map = new IdMapping();
                foreach (ObjectId id in sourceBtr)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent != null)
                    {
                        Entity clone = ent.Clone() as Entity;
                        newBtr.AppendEntity(clone);
                        tr.AddNewlyCreatedDBObject(clone, true);
                    }
                }

                // 3. Copie les réglages de tracé
                newLayout.CopyFrom(sourceLayout);
                tr.Commit();
            }
        }

    }
}
