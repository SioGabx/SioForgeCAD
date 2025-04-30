using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SioForgeCAD.Functions
{
    public static class TLENSBLKATTR
    {
        private class AttrResults
        {
            public class AttrProperty
            {
                public string PropertyName;
                public double PropertyCumulatedValue;
                public DynamicBlockReferencePropertyUnitsType PropertyUnitsType;
            }

            public string BlockName = string.Empty;
            public int BlockCount = 0;
            public List<AttrProperty> Properties = new List<AttrProperty>();

            public override string ToString()
            {
                var result = $"{BlockName} (x{BlockCount})";
                if (Properties.Count > 0)
                {
                    int maxLength = Properties.Max(p => p.PropertyName.Length);
                    foreach (var prop in Properties)
                    {
                        result += $"\n- {prop.PropertyName.PadRight(maxLength)} : {Generic.FormatNumberForPrint(prop.PropertyCumulatedValue)}"; ;
                    }
                }
                return result.TrimEnd();
            }
        }

        public static void Compute()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            PromptSelectionResult res = ed.GetSelectionRedraw();
            if (res.Status != PromptStatus.OK) { return; }
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var AttrResults = new List<AttrResults>();
                foreach (SelectedObject selObj in res.Value)
                {
                    if (!(selObj?.ObjectId.GetDBObject() is BlockReference blockRef))
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

                foreach (var AttrResult in AttrResults)
                {
                    Generic.WriteMessage(AttrResult);
                }
                ed.SetImpliedSelection(res.Value);
                tr.Commit();
            }
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
                            PropertyName = DynamicPropertyName,
                            PropertyUnitsType = DynamicPropertyUnitsType,
                        };
                        AttrResult.Properties.Add(TlensBlkAttrResultProperty);
                    }
                    TlensBlkAttrResultProperty.PropertyCumulatedValue += DynamicPropertyDoubleValue;
                }
            }
        }

        private static void GetBlockReferenceStandardProperties(BlockReference blockRef, AttrResults AttrResult)
        {
            foreach (ObjectId attId in blockRef.AttributeCollection)
            {
                AttributeReference attRef = attId.GetDBObject() as AttributeReference;
                if (attRef == null || string.IsNullOrWhiteSpace(attRef.TextString)) continue;

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
                        PropertyCumulatedValue = value,
                        PropertyUnitsType = DynamicBlockReferencePropertyUnitsType.NoUnits
                    });
                }
                else
                {
                    prop.PropertyCumulatedValue += value;
                }
            }
        }









    }
}
