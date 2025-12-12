using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SioForgeCAD.Functions
{
    public static class TLENSBLKATTR
    {
        private class AttrResults
        {
            public class AttrProperty
            {
                public string PropertyName;
                public List<double> PropertyValues;
                public DynamicBlockReferencePropertyUnitsType PropertyUnitsType;
            }

            public string BlockName = string.Empty;
            public int BlockCount = 0;
            public List<AttrProperty> Properties = new List<AttrProperty>();

            public override string ToString()
            {
                return GetCumulativeReport();
            }

            public string GetCumulativeReport()
            {
                var result = $"{BlockName} (x{BlockCount})";
                if (Properties.Count > 0)
                {
                    int maxLength = Properties.Max(p => p.PropertyName.Length);
                    foreach (var prop in Properties)
                    {
                        result += $"\n- {prop.PropertyName.PadRight(maxLength)} : {Generic.FormatNumberForPrint(prop.PropertyValues.Sum())}";
                    }
                }
                return result.TrimEnd();
            }

            public string GetDetailedReport()
            {
                var result = $"{BlockName} (x{BlockCount})";

                if (Properties.Count > 0)
                {
                    int PropertyNameMaxLength = Properties.Max(p => p.PropertyName.Length);

                    foreach (var prop in Properties)
                    {
                        result += $"\n- {prop.PropertyName.PadRight(PropertyNameMaxLength)} : {Generic.FormatNumberForPrint(prop.PropertyValues.Sum())}";

                        // Regroupe par valeur et compte les occurrences
                        var groups = prop.PropertyValues.GroupBy(v => v).OrderBy(g => g.Key);

                        int PropertyValuesMaxLength = groups.Max(p => Generic.FormatNumberForPrint(p.Key).ToString().Length);

                        foreach (var g in groups)
                        {
                            string formattedValue = Generic.FormatNumberForPrint(g.Key).ToString();
                            result += $"\n    - {formattedValue.PadRight(PropertyValuesMaxLength)} : {g.Count()} fois";
                        }
                    }
                }
                return result.TrimEnd();
            }
        }

        private static List<AttrResults> AquireBlkAttrResults(out object SelectionSet)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            SelectionSet = null;
            var res = ed.GetSelectionRedraw();
            if (res.Status != PromptStatus.OK) { return null; }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var AttrResults = new List<AttrResults>();
                foreach (var selObj in res.Value.GetSelectionSet())
                {
                    if (!(selObj.GetDBObject() is BlockReference blockRef))
                    {
                        continue;
                    }

                    string blockName = blockRef.GetBlockReferenceName();
                    AttrResults AttrResult = AttrResults.FirstOrDefault(t => t.BlockName == blockName);

                    if (AttrResult == null)
                    {
                        AttrResult = new AttrResults()
                        {
                            BlockName = blockName,
                        };
                        AttrResults.Add(AttrResult);
                    }
                    AttrResult.BlockCount++;

                    GetBlockReferenceDynamicProperties(blockRef, AttrResult);
                    GetBlockReferenceStandardProperties(blockRef, AttrResult);
                }
                tr.Commit();
                return AttrResults;
            }
        }

        public static void ComputeCumulative()
        {
            Editor ed = Generic.GetEditor();

            var AttrResults = AquireBlkAttrResults(out object res);
            if (AttrResults == null) { return; }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var AttrResult in AttrResults)
            {
                string CumulativeReport = AttrResult.GetCumulativeReport();
                Generic.WriteMessage(CumulativeReport);
                stringBuilder.AppendLine($"{CumulativeReport}\n");
            }
            System.Windows.Clipboard.SetText(stringBuilder.ToString());
            ed.SetImpliedSelection(res.GetSelectionSet().ToArray());
        }

        public static void ComputeDetailed()
        {
            Editor ed = Generic.GetEditor();

            var AttrResults = AquireBlkAttrResults(out object res);
            if (AttrResults == null) { return; }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var AttrResult in AttrResults)
            {
                string DetailedReport = AttrResult.GetDetailedReport();
                Generic.WriteMessage(DetailedReport);
                stringBuilder.AppendLine($"{DetailedReport}\n");
            }
            System.Windows.Clipboard.SetText(stringBuilder.ToString());
            ed.SetImpliedSelection(res.GetSelectionSet().ToArray());
        }

        private static void GetBlockReferenceDynamicProperties(BlockReference blockRef, AttrResults AttrResult)
        {
            foreach (DynamicBlockReferenceProperty DynamicProperty in blockRef.DynamicBlockReferencePropertyCollection)
            {
                string DynamicPropertyName = DynamicProperty.PropertyName;
                DynamicBlockReferencePropertyUnitsType DynamicPropertyUnitsType = DynamicProperty.UnitsType;
                if (DynamicPropertyUnitsType == DynamicBlockReferencePropertyUnitsType.NoUnits || DynamicPropertyUnitsType == DynamicBlockReferencePropertyUnitsType.Angular)
                {
                    continue;
                }

                object DynamicPropertyValue = DynamicProperty.Value;

                if (DynamicPropertyValue.TryGetDoubleValue(out double DynamicPropertyDoubleValue))
                {
                    var TlensBlkAttrResultProperty = AttrResult.Properties.FirstOrDefault(t => t.PropertyName == DynamicPropertyName && t.PropertyUnitsType == DynamicPropertyUnitsType);

                    if (TlensBlkAttrResultProperty == null)
                    {
                        TlensBlkAttrResultProperty = new AttrResults.AttrProperty()
                        {
                            PropertyValues = new List<double>(),
                            PropertyName = DynamicPropertyName,
                            PropertyUnitsType = DynamicPropertyUnitsType,
                        };
                        AttrResult.Properties.Add(TlensBlkAttrResultProperty);
                    }
                    TlensBlkAttrResultProperty.PropertyValues.Add(DynamicPropertyDoubleValue);
                }
            }
        }

        private static void GetBlockReferenceStandardProperties(BlockReference blockRef, AttrResults AttrResult)
        {
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                AttributeReference attRef = attId.GetDBObject() as AttributeReference;
                if (attRef == null || string.IsNullOrWhiteSpace(attRef.TextString))
                {
                    continue;
                }

                string tag = attRef.Tag;

                if (!double.TryParse(attRef.TextString.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
                {
                    continue; // Ignore les valeurs non-numériques
                }

                var prop = AttrResult.Properties.FirstOrDefault(p => p.PropertyName == tag);
                if (prop == null)
                {
                    AttrResult.Properties.Add(new AttrResults.AttrProperty
                    {
                        PropertyName = tag,
                        PropertyValues = new List<double>() { value },
                        PropertyUnitsType = DynamicBlockReferencePropertyUnitsType.NoUnits
                    });
                }
                else
                {
                    prop.PropertyValues.Add(value);
                }
            }
        }
    }
}
