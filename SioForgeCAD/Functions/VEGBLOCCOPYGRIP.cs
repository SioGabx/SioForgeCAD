using Autodesk.AutoCAD.DatabaseServices;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Overrules.CopyGripOverrule;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCCOPYGRIP
    {
        private static CopyGripOverrule _instance = null;
        public static CopyGripOverrule Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CopyGripOverrule(typeof(BlockReference), FilterFunction, OnHotGripAction, false);
                }
                return _instance;
            }
        }


        public static void AddGrip()
        {            
            if (!Instance.IsEnabled)
            {
                Instance.EnableOverrule(true);
                Generic.WriteMessage("VEGBLOCCOPYGRIP est activé.");
            }
            else
            {
                Instance.EnableOverrule(false);
                Generic.WriteMessage("VEGBLOCCOPYGRIP est désactivé.");
            }
        }

        public static bool FilterFunction(Entity Entity)
        {
            if (Entity is BlockReference BlkRef)
            {
                if (BlkRef.GetBlockReferenceName().StartsWith(Settings.VegblocLayerPrefix))
                {
                    return true;
                }
            }
            return false;
        }

        public static void OnHotGripAction(ObjectId objectid)
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                if (objectid.GetDBObject() is BlockReference blockReference)
                {
                    string BlkName = blockReference.GetBlockReferenceName();
                    tr.Commit();

                    bool IsInsertSuccess = true;
                    while (IsInsertSuccess)
                    {
                        IsInsertSuccess = Functions.VEGBLOC.AskInsertVegBloc(BlkName, blockReference.Layer);
                    }
                }
            }
        }
    }
}
