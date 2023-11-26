using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SioForgeCAD.Commun
{
    public static class SelectInXref
    {
        ///<summary>
        /// Get the child entity of the first xref in the nested selection.
        ///</summary>
        ///<returns>ObjectId of the top-level object from the outer xref.</returns>
        public static (ObjectId XrefObjectId, ObjectId SelectedObjectId) GetFirstXrefChild(this PromptNestedEntityResult res)
        {
            var retId = ObjectId.Null;
            var selId = res.ObjectId;
            var conts = res.GetContainers();
            var db = selId.Database;
            ObjectId XrefObjectId = ObjectId.Null;
            // Use an open-close transaction as we're in a utility function
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                // Work backwards through the containers, looking for an xref
                for (int i = conts.Length - 1; i >= 0; i--)
                {
                    var br = tr.GetObject(conts[i], OpenMode.ForRead) as BlockReference;
                    if (br != null)
                    {
                        XrefObjectId = conts[i];
                        var btr = (BlockTableRecord)tr.GetObject(br.BlockTableRecord, OpenMode.ForRead);
                        // If we have an xref, we'll return the next container or the
                        // selected entity in the case we're at the innermost container
                        if (btr.IsFromExternalReference)
                        {
                            if (i > 0)
                            {
                                retId = conts[i - 1];
                            }
                            else
                            {
                                retId = selId;
                            }
                            break;
                        }
                    }
                }
                tr.Commit();
            }
            return (XrefObjectId, retId);
        }
   
        public static (ObjectId XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) Select(string Message)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return (ObjectId.Null,ObjectId.Null, PromptStatus.Other);
            var ed = doc.Editor;
            // Select an entity within an xref
           
            var pner = ed.GetNestedEntity(Message);
            if (pner.Status != PromptStatus.OK)
            {
                return (ObjectId.Null,ObjectId.Null, PromptStatus.Cancel);
            }

            // Get the ID of the entity that we want to select in the xref
            // (this is the first entity contained by an xref)
            (ObjectId XrefObjectId, ObjectId SelectedObjectId) selId = pner.GetFirstXrefChild();
            return (selId.XrefObjectId, selId.SelectedObjectId, pner.Status);
        }
    }
}