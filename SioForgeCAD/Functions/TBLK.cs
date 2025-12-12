using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class TBLK
    {
        private class BlkInstance
        {
            public int Count;
            public string BlockName;
            public List<BlkInstanceProperty> Properties = new List<BlkInstanceProperty>();

            public BlkInstance(string blockName)
            {
                Count = 0;
                BlockName = blockName;
            }

            public class BlkInstanceProperty
            {
                public string PropertyName;
                public List<object> PropertyValues = new List<object>();

                public BlkInstanceProperty(string propertyName)
                {
                    PropertyName = propertyName;
                }
            }
        }

        public static void Compute()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            SelectionFilter BlkSelectionFilter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            var SelectionBlkPSR = ed.GetSelectionRedraw(null, false, false, BlkSelectionFilter);


            if (SelectionBlkPSR.Status != PromptStatus.OK) { return; }
            Generic.WriteMessage("Extraction en cours... Veuillez patienter...");
            System.Windows.Forms.Application.DoEvents();

            List<BlkInstance> BlkInstanceList = new List<BlkInstance>();
            HashSet<ObjectId> ExtractedBlocObjIds = new HashSet<ObjectId>();

            ObjectId[] SelectedBlocObjIds = SelectionBlkPSR.Value.GetObjectIds();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var allBlocObjid in SelectedBlocObjIds.GetSelectionSet())
                {
                    if (!allBlocObjid.IsDerivedFrom(typeof(BlockReference)))
                    {
                        continue;
                    }

                    BlockReference allBlocBlkRef = (BlockReference)tr.GetObject(allBlocObjid, OpenMode.ForRead);
                    if (allBlocBlkRef?.IsXref() == true) { continue; }
                    string blockName = allBlocBlkRef.GetBlockReferenceName();

                    var instance = BlkInstanceList.FirstOrDefault(inst => inst.BlockName == blockName);
                    if (instance == null)
                    {
                        instance = new BlkInstance(blockName);
                        BlkInstanceList.Add(instance);
                    }
                    ExtractedBlocObjIds.Add(allBlocObjid);
                    instance.Count++;
                }

                var clipboardText = string.Join("\n", BlkInstanceList
                    .OrderBy(v => v.BlockName)
                    .Select(v => $"\"{v.BlockName}\"\t{v.Count}")
                );

                Generic.WriteMessage($"Les métrés des blocs sélectionnés ont été copiés dans le presse-papiers.\nNombre de blocks : {BlkInstanceList.Count(inst => inst.Count > 0)}");
                Clipboard.SetText(clipboardText);
                ed.SetImpliedSelection(ExtractedBlocObjIds.ToArray());
                tr.Commit();
            }
        }


        public static void ComputeDetailed()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            SelectionFilter blkFilter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            var selectionResult = ed.GetSelectionRedraw(null, false, false, blkFilter);

            if (selectionResult.Status != PromptStatus.OK) return;

            List<BlkInstance> blkList = new List<BlkInstance>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (var objId in selectionResult.Value.GetObjectIds())
                {
                    if (!objId.IsDerivedFrom(typeof(BlockReference))) continue;

                    BlockReference blkRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                    if (blkRef?.IsXref() == true) continue;

                    string blockName = blkRef.GetBlockReferenceName();
                    var instance = blkList.FirstOrDefault(b => b.BlockName == blockName);
                    if (instance == null)
                    {
                        instance = new BlkInstance(blockName);
                        blkList.Add(instance);
                    }

                    instance.Count++;

                    // --- Propriétés dynamiques ---
                    foreach (DynamicBlockReferenceProperty dynProp in blkRef.DynamicBlockReferencePropertyCollection)
                    {
                        if (dynProp.ReadOnly || !dynProp.VisibleInCurrentVisibilityState) continue;

                        var prop = instance.Properties.FirstOrDefault(p => p.PropertyName == dynProp.PropertyName);
                        if (prop == null)
                        {
                            prop = new BlkInstance.BlkInstanceProperty(dynProp.PropertyName);
                            instance.Properties.Add(prop);
                        }
                        prop.PropertyValues.Add(dynProp.Value);
                    }

                    // --- Attributs classiques ---
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        AttributeReference attRef = attId.GetDBObject() as AttributeReference;
                        if (attRef == null || string.IsNullOrWhiteSpace(attRef.TextString)) continue;

                        var prop = instance.Properties.FirstOrDefault(p => p.PropertyName == attRef.Tag);
                        if (prop == null)
                        {
                            prop = new BlkInstance.BlkInstanceProperty(attRef.Tag);
                            instance.Properties.Add(prop);
                        }
                        prop.PropertyValues.Add(attRef.TextString);
                    }
                }

                // --- Formatage du texte final ---
                List<string> output = new List<string>();
                foreach (var blk in blkList.OrderBy(b => b.BlockName))
                {
                    output.Add($"\n - {blk.BlockName} (x{blk.Count})");
                    foreach (var prop in blk.Properties)
                    {
                        output.Add($"  - {prop.PropertyName.PadRight(10)} : {(prop.PropertyValues.HasTypeOf(typeof(double)) ? prop.PropertyValues.SumNumeric() : "")}");
                        foreach (var valGroup in prop.PropertyValues.GroupBy(v => v))
                        {
                            output.Add($"    - {valGroup.Key} : {valGroup.Count()} fois");
                        }
                    }
                }

                string finalText = string.Join("\n", output);
                Generic.WriteMessage(finalText);
                Clipboard.SetText(finalText);

                tr.Commit();
            }
        }



































    }
}
