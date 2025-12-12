using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;

namespace SioForgeCAD.Functions
{
    public static class TBLK
    {
        // --- Modèle interne pour un bloc et ses propriétés ---
        private class BlkInstance
        {
            public string BlockName;
            public int Count;
            public List<BlkProperty> Properties = new List<BlkProperty>();

            public BlkInstance(string blockName)
            {
                BlockName = blockName;
                Count = 0;
            }

            public class BlkProperty
            {
                public string PropertyName;
                public List<object> PropertyValues = new List<object>();
                public DynamicBlockReferencePropertyUnitsType UnitsType;

                public BlkProperty(string name, DynamicBlockReferencePropertyUnitsType units = DynamicBlockReferencePropertyUnitsType.NoUnits)
                {
                    PropertyName = name;
                    UnitsType = units;
                }
            }
        }



        // --- Compute simple: nombre de blocs sélectionnés ---
        public static void Compute()
        {
            var ed = Generic.GetEditor();
            var blkList = AcquireSelectedBlocks(out var selectionSet);
            if (blkList == null) return;

            StringBuilder ClipBoardValue = new StringBuilder();
            StringBuilder ConsoleValue = new StringBuilder();
            int BlkNameMaxLength = blkList.Max(p => p.BlockName.Length);
            foreach (var blk in blkList.OrderBy(b => b.BlockName))
            {
                ClipBoardValue.AppendLine($"\"{blk.BlockName}\"\t{blk.Count}");
                ConsoleValue.AppendLine($"{blk.BlockName.PadRight(BlkNameMaxLength)} : {blk.Count}");
            }

            Generic.WriteMessage($"Les métrés des blocs sélectionnés ont été copiés dans le presse-papiers.\nNombre de blocks : {blkList.Count(b => b.Count > 0)}");
            Generic.WriteMessage(ConsoleValue);
            
            
            Clipboard.SetText(ClipBoardValue.ToString());
            ed.SetImpliedSelection(selectionSet.ToArray());
        }


        public static void ComputeCumulativeAttributes()
        {
            var ed = Generic.GetEditor();
            var blkList = AcquireSelectedBlocks(out var selectionSet);
            if (blkList == null) return;

            StringBuilder sb = new StringBuilder();

            foreach (var blk in blkList.OrderBy(b => b.BlockName))
            {
                sb.AppendLine($"{blk.BlockName} (x{blk.Count})");

                int PropertyNamesMaxLength = blk.Properties.Max(p => p.PropertyName.Length);
                foreach (var prop in blk.Properties.OrderBy(p => p.PropertyName))
                {
                    double sum = prop.PropertyValues.HasTypeOf(typeof(double)) ? prop.PropertyValues.SumNumeric() : 0;
                    sb.AppendLine($"- {prop.PropertyName.PadRight(PropertyNamesMaxLength)} : {Generic.FormatNumberForPrint(sum)}");
                }
            }

            string finalText = sb.ToString();
            Generic.WriteMessage(finalText);
            Clipboard.SetText(finalText);
            ed.SetImpliedSelection(selectionSet.ToArray());
        }


        // --- Compute détaillé: propriétés + attributs ---
        public static void ComputeDetailed()
        {
            var ed = Generic.GetEditor();
            var blkList = AcquireSelectedBlocks(out var selectionSet);
            if (blkList == null) return;

            StringBuilder sb = new StringBuilder();

            foreach (var blk in blkList.OrderBy(b => b.BlockName))
            {
                sb.AppendLine($"\n{blk.BlockName} (x{blk.Count})");
                int PropertyNamesMaxLength = blk.Properties.Max(p => p.PropertyName.Length);
                foreach (var prop in blk.Properties)
                {
                    string numericSum = prop.PropertyValues.HasTypeOf(typeof(double)) ? Generic.FormatNumberForPrint(prop.PropertyValues.SumNumeric()).ToString() : "";
                    sb.AppendLine($"  - {prop.PropertyName.PadRight(PropertyNamesMaxLength)} : {numericSum}");

                    var GroupedPropertyValues = prop.PropertyValues.GroupBy(v => v);
                    int groupKeyMaxLength = GroupedPropertyValues.Max(p => p.Key.ToString().Length);
                    foreach (var group in GroupedPropertyValues)
                    {
                        sb.AppendLine($"    - {group.Key.ToString().PadRight(groupKeyMaxLength)} : {group.Count()} fois");
                    }
                }
            }

            string finalText = sb.ToString().Trim();
            Generic.WriteMessage(finalText);
            Clipboard.SetText(finalText);
            ed.SetImpliedSelection(selectionSet.ToArray());
        }











        // --- Méthode commune pour récupérer les blocs sélectionnés ---
        private static List<BlkInstance> AcquireSelectedBlocks(out IEnumerable<ObjectId> selectionSet)
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();
            selectionSet = null;

            SelectionFilter blkFilter = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "INSERT") });
            var selRes = ed.GetSelectionRedraw(null, false, false, blkFilter);
            if (selRes.Status != PromptStatus.OK) return null;

            selectionSet = selRes.Value.GetSelectionSet();
            List<BlkInstance> blkList = new List<BlkInstance>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var objId in selRes.Value.GetObjectIds())
                {
                    if (!objId.IsDerivedFrom(typeof(BlockReference))) continue;

                    var blkRef = (BlockReference)tr.GetObject(objId, OpenMode.ForRead);
                    if (blkRef?.IsXref() == true) continue;

                    var blk = blkList.FirstOrDefault(b => b.BlockName == blkRef.GetBlockReferenceName());
                    if (blk == null)
                    {
                        blk = new BlkInstance(blkRef.GetBlockReferenceName());
                        blkList.Add(blk);
                    }

                    blk.Count++;

                    // --- Propriétés dynamiques ---
                    foreach (DynamicBlockReferenceProperty dynProp in blkRef.DynamicBlockReferencePropertyCollection)
                    {
                        if (dynProp.ReadOnly || !dynProp.VisibleInCurrentVisibilityState) continue;
                        AddPropertyValue(blk, dynProp.PropertyName, dynProp.Value, dynProp.UnitsType);
                    }

                    // --- Attributs classiques ---
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        var attRef = attId.GetDBObject() as AttributeReference;
                        if (attRef == null || string.IsNullOrWhiteSpace(attRef.TextString)) continue;
                        AddPropertyValue(blk, attRef.Tag, attRef.TextString, DynamicBlockReferencePropertyUnitsType.NoUnits);
                    }
                }
                tr.Commit();
            }

            return blkList;
        }

        // --- Ajoute une valeur à une propriété du bloc ---
        private static void AddPropertyValue(BlkInstance blk, string propName, object value, DynamicBlockReferencePropertyUnitsType units)
        {
            var prop = blk.Properties.FirstOrDefault(p => p.PropertyName == propName && p.UnitsType == units);
            if (prop == null)
            {
                prop = new BlkInstance.BlkProperty(propName, units);
                blk.Properties.Add(prop);
            }
            prop.PropertyValues.Add(value);
        }
    }
}
