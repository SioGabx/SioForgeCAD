using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    internal class EXECUTEONEACHOBJECTSINSELECTION
    {
        //_QDIM {SELECTIONSET} {} _C {P}
        private static List<string> _lastCommandInput = new List<string>();
        public static async Task RunCommandOnSelectionAsync()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var selRes = ed.GetSelectionRedraw();
            if (selRes.Status != PromptStatus.OK)
            {
                return;
            }

            Generic.WriteMessage("--- TOKENS DISPONIBLES ---");
            Generic.WriteMessage("  {ENT}          : Entité");
            Generic.WriteMessage("  {SELECTIONSET} : Entité (SelectionSet)");
            Generic.WriteMessage("  {HANDLE}       : Handle de l'entité");
            Generic.WriteMessage("  {LAYER}        : Nom du calque");
            Generic.WriteMessage("  {COLOR}        : Couleur de l'entité");
            Generic.WriteMessage("  {TYPE}         : Type de l'objet");
            Generic.WriteMessage("  {P}            : Coordonnées \"X,Y,Z\"");
            Generic.WriteMessage("  {PAUSE}        : Pause utilisateur (Editor.PauseToken)");
            Generic.WriteMessage("  {}             : Validation");
            Generic.WriteMessage("  Séparateur     : ';' pour chaîner PLUSIEURS commandes");
            Generic.WriteMessage("--------------------------");

            if (_lastCommandInput.Count > 0)
            {
                Generic.WriteMessage($"[Dernières commandes ({_lastCommandInput.Count}) : {string.Join(" ; ", _lastCommandInput)}]");
            }

            PromptStringOptions pso = new PromptStringOptions("\nEntrez la/les commande(s) séparées par ';'")
            {
                AllowSpaces = true,
                DefaultValue = string.Join("; ",_lastCommandInput)
            };

            PromptResult pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK)
            {
                return;
            }

            string rawInput = pr.StringResult.Trim();
            List<string> commandsToRun = new List<string>();

            // Gestion du keyword LAST
            if (rawInput.Equals("LAST", StringComparison.OrdinalIgnoreCase))
            {
                if (_lastCommandInput.Count == 0)
                {
                    Generic.WriteMessage("Aucune liste de commandes enregistrée dans LAST.");
                }
                commandsToRun = new List<string>(_lastCommandInput);
                Generic.WriteMessage($"Réutilisation de {commandsToRun.Count} commande(s).");
            }
            else
            {
                // Découpage par le séparateur ';' pour extraire chaque commande
                string[] splitCmds = rawInput.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string cmd in splitCmds)
                {
                    string trimmed = cmd.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        commandsToRun.Add(trimmed);
                    }
                }

                if (commandsToRun.Count > 0)
                {
                    _lastCommandInput = new List<string>(commandsToRun);
                }
                else
                {
                    return;
                }
            }

            // 4. Exécution : Pour chaque entité, on déroule TOUTES les commandes de la liste
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId[] objectIds = Array.Empty<ObjectId>();

                if (selRes.Value is SelectionSet selection && selection.Count > 0)
                {
                    objectIds = selection.GetObjectIds();
                }

                foreach (ObjectId so in objectIds)
                {
                    if (so == null)
                    {
                        continue;
                    }

                    Entity ent = tr.GetObject(so, OpenMode.ForRead) as Entity;
                    if (ent == null)
                    {
                        continue;
                    }

                    // Exécution séquentielle de la liste de commandes sur l'entité courante
                    foreach (string singleCommandTemplate in commandsToRun)
                    {
                        List<object> args = BuildCommandArgs(ent, singleCommandTemplate);

                        if (args.Count > 0)
                        {
                            try
                            {
                                Generic.WriteMessage(string.Join(" ", args));
                                await Generic.CommandAsync(args.ToArray());
                            }
                            catch (System.Exception ex)
                            {
                                Generic.WriteMessage($"Erreur lors de l'exécution sur l'entité {ent.Handle}: {ex.Message}");
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }

        private static List<object> BuildCommandArgs(Entity ent, string rawInput)
        {
            List<object> commandTokens = new List<object>();
            string[] rawTokens = rawInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Autodesk.AutoCAD.Geometry.Point3d refPt = GetEntityRefPoint(ent);

            foreach (string token in rawTokens)
            {
                switch (token) {
                    case "\\":
                        commandTokens.Add(Editor.PauseToken); 
                        break;
                    case "{PAUSE}":
                        commandTokens.Add(Editor.PauseToken); 
                        break;
                    case "{}":
                        commandTokens.Add(string.Empty); 
                        break;
                    case "{ENT}":
                        commandTokens.Add(ent.ObjectId); 
                        break;
                    case "{LAYER}":
                        commandTokens.Add(ent.Layer); 
                        break;
                    case "{COLOR}":
                        commandTokens.Add(ent.Color); 
                        break;
                    case "{TYPE}":
                        commandTokens.Add(ent.GetRXClass().Name); 
                        break;
                    case "{PX}":
                        commandTokens.Add(refPt.X); 
                        break;
                    case "{PY}":
                        commandTokens.Add(refPt.Y); 
                        break;
                    case "{P}":
                        commandTokens.Add(refPt); 
                        break;
                    case "{SELECTIONSET}":
                        // Pour que ed.Command accepte l'entité comme une sélection valide :
                        commandTokens.Add(SelectionSet.FromObjectIds(new[] { ent.ObjectId }));
                        break;
                    default:
                        commandTokens.Add(token);
                        break;

                }
            }

            return commandTokens;
        }

        private static Autodesk.AutoCAD.Geometry.Point3d GetEntityRefPoint(Entity ent)
        {
            if (ent is BlockReference blk)
            {
                return blk.Position;
            }

            if (ent is DBText txt)
            {
                return txt.Position;
            }

            if (ent is MText mtxt)
            {
                return mtxt.Location;
            }

            if (ent is Curve crv)
            {
                return crv.StartPoint;
            }

            return ent.GeometricExtents.MinPoint;
        }

    }
}
