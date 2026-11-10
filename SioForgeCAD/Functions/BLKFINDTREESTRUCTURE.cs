using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    public static class BLKFINDTREESTRUCTURE
    {
        public static void Find()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            List<string> blocksNames = new List<string>();

            var selRedraw = ed.GetSelectionRedraw(Options: new string[] { "Nom", "Liste" });

            if (selRedraw.Status == PromptStatus.Keyword && selRedraw.Value.ToString() == "Nom")
            {
                bool isNameValid = false;
                string blkName = string.Empty;
                List<string> allBlksNames = BlockReferenceExtensions.GetAllBlockNames(db, false, true);

                while (!isNameValid)
                {
                    using (Forms.InputDialogBox dialogBox = new Forms.InputDialogBox())
                    {
                        dialogBox.SetPrompt("Entrez le nom du bloc :");
                        dialogBox.SetUserInputPlaceholder(blkName);
                        dialogBox.SetCursorAtEnd();

                        if (dialogBox.ShowDialog() != DialogResult.OK)
                        {
                            return;
                        }

                        blkName = dialogBox.GetUserInput();

                        if (string.IsNullOrWhiteSpace(blkName))
                        {
                            Generic.WriteMessage("Impossible, le nom du bloc est vide.");
                            continue;
                        }

                        if (allBlksNames.Contains(blkName))
                        {
                            isNameValid = true;
                        }
                        else
                        {
                            var suggestions = allBlksNames.LevenshteinDistance(blkName);
                            string message = $"Le dessin ne contient pas de bloc nommé {blkName}{(suggestions.Count > 0 ? $"\nVouliez-vous dire : {string.Join(", ", suggestions)} ?" : string.Empty)}";
                            Generic.WriteMessage(message);

                            if (suggestions.Count == 1)
                            {
                                var result = MessageBox.Show(message, Generic.GetExtensionDLLName(), MessageBoxButtons.YesNo);
                                if (result == DialogResult.Yes)
                                {
                                    blkName = suggestions.FirstOrDefault();
                                    isNameValid = true;
                                }
                            }
                            else
                            {
                                MessageBox.Show(message);
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(blkName))
                {
                    Generic.WriteMessage("Impossible, le nom du bloc est vide.");
                    return;
                }

                blocksNames.Add(blkName);
            }
            else if (selRedraw.Status == PromptStatus.Keyword && selRedraw.Value.ToString() == "Liste")
            {
                List<string> allBlksNames = BlockReferenceExtensions.GetAllBlockNames(db, false, true);
                if (allBlksNames.Count == 0)
                {
                    Generic.WriteMessage("Aucun bloc trouvé dans le dessin.");
                    return;
                }

                var dlg = new ComboboxDialog(allBlksNames.ToArray(), "Sélectionnez les noms des blocs", false);
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    blocksNames.AddRangeUnique(dlg.GetSelectedItems());
                }
                else
                {
                    return;
                }
            }
            else if (selRedraw.Status == PromptStatus.OK)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var selItem in selRedraw.Value.GetObjectIds())
                    {
                        if (tr.GetObject(selItem, OpenMode.ForRead) is BlockReference selBlkRef)
                        {
                            var blkName = selBlkRef.GetBlockReferenceName();
                            if (!blocksNames.Contains(blkName))
                            {
                                blocksNames.Add(blkName);
                            }
                        }
                    }
                    tr.Commit();
                }
            }
            else
            {
                return;
            }

            if (blocksNames.Count == 0) return;

            // 2. Recherche récursive de l'arborescence
            StringBuilder sbArborescence = new StringBuilder();
            HashSet<string> targetNamesSet = new HashSet<string>(blocksNames, StringComparer.OrdinalIgnoreCase);
            int totalFound = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                ObjectId modelSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(db);
                BlockTableRecord modelSpaceBtr = (BlockTableRecord)tr.GetObject(modelSpaceId, OpenMode.ForRead);
                SearchBlockHierarchy(tr, modelSpaceBtr, "ModelSpace", targetNamesSet, sbArborescence, 0, ref totalFound);

                foreach (ObjectId btrId in bt)
                {
                    // On ignore le ModelSpace puisqu'on vient de le faire
                    if (btrId == modelSpaceId) continue;

                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                    if (btr.IsLayout && !btr.LayoutId.IsNull)
                    {
                        Layout layout = (Layout)tr.GetObject(btr.LayoutId, OpenMode.ForRead);
                        if (!layout.ModelType)
                        {
                            string layoutName = $"PaperSpace ({layout.LayoutName})";
                            SearchBlockHierarchy(tr, btr, layoutName, targetNamesSet, sbArborescence, 0, ref totalFound);
                        }
                    }
                }

                tr.Commit();
            }

            string resultText = sbArborescence.ToString();
            if (!string.IsNullOrEmpty(resultText))
            {
                System.Windows.Forms.Clipboard.SetText(resultText);
                Generic.WriteMessage($"Arborescence copiée dans le presse-papier ({totalFound} occurrence(s) trouvée(s)).");
            }
            else
            {
                Generic.WriteMessage("Aucune occurrence des blocs recherchés n'a été trouvée.");
            }
        }

        /// <summary>
        /// Exploration récursive de l'arborescence des blocs.
        /// </summary>
        private static void SearchBlockHierarchy(Transaction tr, BlockTableRecord btr, string parentPath, HashSet<string> targetNames, StringBuilder sb, int level, ref int totalFound)
        {
            foreach (ObjectId id in btr)
            {
                if (id.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(BlockReference))))
                {
                    BlockReference blkRef = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (blkRef == null) continue;

                    string blkName = blkRef.GetBlockReferenceName();
                    string currentPath = $"{parentPath} > {blkName}";
                    string indent = new string(' ', level * 2);

                    if (targetNames.Contains(blkName))
                    {
                        sb.AppendLine($"{indent}├─ {currentPath} (Handle: {blkRef.Handle})");
                        totalFound++;
                    }

                    // Inspection récursive
                    ObjectId subBtrId = blkRef.BlockTableRecord;
                    if (!subBtrId.IsNull)
                    {
                        BlockTableRecord subBtr = tr.GetObject(subBtrId, OpenMode.ForRead) as BlockTableRecord;
                        if (subBtr != null && !subBtr.IsLayout)
                        {
                            SearchBlockHierarchy(tr, subBtr, currentPath, targetNames, sb, level + 1, ref totalFound);
                        }
                    }
                }
            }
        }
    }
}
