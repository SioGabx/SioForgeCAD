using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace SioForgeCAD.Commun
{
    public static class SelectInXref
    {
       
        public static (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) Select(string Message, Point3d? NonInterractivePickedPoint = null)
        {
            Editor ed = Generic.GetEditor();

            PromptNestedEntityOptions nestedEntOpt = new PromptNestedEntityOptions(Message);
            if (NonInterractivePickedPoint != null)
            {
                nestedEntOpt.NonInteractivePickPoint = NonInterractivePickedPoint ?? Point3d.Origin;
                nestedEntOpt.UseNonInteractivePickPoint = true;
            }
            PromptNestedEntityResult nestedEntRes = ed.GetNestedEntity(nestedEntOpt);
            if (nestedEntRes.Status != PromptStatus.OK)
            {
                return (new ObjectId[0], ObjectId.Null, nestedEntRes.Status);
            }
            (ObjectId[] XrefObjectId, ObjectId SelectedObjectId) = nestedEntRes.GetEntityInChildXref();
            return (XrefObjectId, SelectedObjectId, nestedEntRes.Status);
        }

        public static (ObjectId[] XrefObjectId, ObjectId SelectedObjectId) GetEntityInChildXref(this PromptNestedEntityResult res)
        {
            return (res.GetContainers(), res.ObjectId);
        }

        public static string GetEntityPathInChildXref(this PromptNestedEntityResult res)
        {
            Database db = HostApplicationServices.WorkingDatabase;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                List<string> Path = new List<string>();
                foreach (ObjectId id in res.GetContainers().Reverse())
                {
                    BlockReference container = tr.GetObject(id, OpenMode.ForRead) as Autodesk.AutoCAD.DatabaseServices.BlockReference;

                    Path.Add(container.Name);
                }
                tr.Commit(); 
                return string.Join(">", Path);
            }
            
        }



        public static Points TransformPointInXrefsToCurrent(Point3d XrefPosition, IEnumerable<ObjectId> NestedXrefsContainer)
        {
            Point3d BlkPosition = XrefPosition;
            foreach (ObjectId objectId in NestedXrefsContainer)
            {
                BlkPosition = Points.From3DPoint(BlockReferenceExtensions.ProjectPointToCurrentSpace(objectId, BlkPosition)).SCG;
            }
            return BlkPosition.ToPoints();
        }













    }
}