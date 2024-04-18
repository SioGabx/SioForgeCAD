using Autodesk.AutoCAD.DatabaseServices;

namespace SioForgeCAD.Commun.Drawing
{
    public static class Groups
    {
        public static ObjectId Create(string Name, string Description, ObjectIdCollection EntitiesObjectIdCollection)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Group grp = new Group(Description, true);
                DBDictionary gd = db.GroupDictionaryId.GetDBObject(OpenMode.ForWrite) as DBDictionary;
                int DuplicateNameIndex = 0;

                string GroupName = SymbolUtilityServices.RepairSymbolName(Name, false);

                while (gd.Contains(GroupName))
                {
                    DuplicateNameIndex++;
                    GroupName = $"{Name}_{DuplicateNameIndex}";
                }

                ObjectId grpId = gd.SetAt(GroupName, grp);
                tr.AddNewlyCreatedDBObject(grp, true);
                grp.InsertAt(0, EntitiesObjectIdCollection);
                tr.Commit();
                return grpId;
            }
        }
    }
}
