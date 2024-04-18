using Autodesk.AutoCAD.ApplicationServices;
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
                //Check the layer name, because if we check a dynamic block real name, it slow down autocad
                if (BlkRef.Layer.StartsWith(Settings.VegblocLayerPrefix))
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
                    Points Origin = blockReference.Position.ToPoints();
                    tr.Commit();

                    bool IsInsertSuccess = true;
                    while (IsInsertSuccess)
                    {
                        IsInsertSuccess = Functions.VEGBLOC.AskInsertVegBloc(BlkName, blockReference.Layer, Origin) != ObjectId.Null;
                    }

                    if (Settings.VegblocCopyGripDeselectAfterCopy)
                    {
                        //Send ESCAPE to disable the current selection
                        Document doc = Generic.GetDocument();
                        doc.SendStringToExecute($"{(char)27}", false, false, false);
                    }
                }
            }
        }
    }
}
