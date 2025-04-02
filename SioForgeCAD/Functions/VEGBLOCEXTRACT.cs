using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace SioForgeCAD.Functions
{
    public static class VEGBLOCEXTRACT
    {
        private class VegInstance
        {
            public int Count;
            public string CompleteName;
            public string Type;

            public VegInstance(string completeName, string type)
            {
                Count = 1;
                CompleteName = completeName;
                Type = type;
            }
        }
        public static void Extract()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            List<VegInstance> VegInstanceList = new List<VegInstance>();

            PromptSelectionResult selResult = ed.GetSelectionRedraw(null, false, false);
            List<ObjectId> ExtractedBloc = new List<ObjectId>();
            if (selResult.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (SelectedObject selObj in selResult.Value)
                    {
                        var EntObjId = selObj.ObjectId;
                        if (selObj != null && EntObjId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                        {
                            BlockReference blkRef = tr.GetObject(EntObjId, OpenMode.ForRead) as BlockReference;
                            if (blkRef?.IsXref() == false)
                            {
                                string blockName = blkRef.GetBlockReferenceName();
                                var Infos = VEGBLOC.GetDataStore(blkRef);
                                if (Infos is null) { continue; }
                                ExtractedBloc.Add(EntObjId);
                                string Name = Infos[VEGBLOC.DataStore.CompleteName];
                                string Type = Infos[VEGBLOC.DataStore.Type]?.ToUpper();

                                VegInstance Instance = VegInstanceList.Where(inst => (inst.CompleteName == Name && inst.Type == Type))?.FirstOrDefault();
                                if (Instance is null)
                                {
                                    VegInstanceList.Add(new VegInstance(Name, Type));
                                }
                                else
                                {
                                    Instance.Count++;
                                }
                            }
                        }
                    }
                    StringBuilder ClipboardString = new StringBuilder();
                    foreach (var Extract in VegInstanceList.OrderByDescending(v => v.Type).ThenByDescending(v => v.CompleteName).Reverse())
                    {
                        var Name = Extract.CompleteName;
                        var Type = Extract.Type;
                        var Count = Extract.Count;

                        ClipboardString.AppendLine($"\"{Type}\"\t\"{Name}\"\t{Count}");
                    }
                    Clipboard.SetText(ClipboardString.ToString());
                    ed.SetImpliedSelection(ExtractedBloc.ToArray());

                    tr.Commit();
                }
            }
        }
    }
}
