using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace SioForgeCAD.Functions
{
    public static class SMARTFLATTEN
    {
        public static void FlattenAll()
        {
            var db = Generic.GetDatabase();
            List<ObjectId> ids = new List<ObjectId>();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                var BlkTable = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                foreach (ObjectId btrId in BlkTable)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    if (btr.IsLayout)
                    {
                        continue;
                    }

                    Debug.WriteLine($"Inspecting block: {btr.Name}");

                    foreach (ObjectId objId in btr)
                    {
                        ids.Add(objId);
                    }
                }

                BlockTableRecord modelSpace = tr.GetObject(BlkTable[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
                foreach (ObjectId objId in modelSpace)
                {
                    ids.Add(objId);
                }

                FlatSCU();

                // On passe false ici car FlattenAll a déjà parcouru toutes les définitions de blocs ci-dessus
                FlatObjects(ids.ToArray(), false);
                tr.Commit();
            }
        }

        public static void Flatten()
        {
            var ed = Generic.GetEditor();
            var EntitiesSelection = ed.GetSelectionRedraw(Options: new string[] { "SCU" });
            if (EntitiesSelection.Status == PromptStatus.Keyword) { FlatSCU(); return; }
            if (EntitiesSelection.Status != PromptStatus.OK) { return; }

            // Demande à l'utilisateur s'il veut traiter l'intérieur des blocs
            PromptKeywordOptions pko = new PromptKeywordOptions("\nVoulez-vous traiter l'intérieur des blocs ? [Oui/Non] : ");
            pko.Keywords.Add("Oui");
            pko.Keywords.Add("Non");
            pko.Keywords.Default = "Non";
            pko.AllowNone = true;

            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK && pkr.Status != PromptStatus.None) { return; }

            bool processBlocks = pkr.StringResult == "Oui";

            FlatObjects(EntitiesSelection.Value.GetObjectIds(), processBlocks);
        }

        /// <summary>
        /// Récupère tous les ObjectId (Entités et Attributs) à l'intérieur d'une référence de bloc.
        /// </summary>
        public static IEnumerable<ObjectId> GetEntityInBlock(ObjectId blockRefId, Transaction tr, bool recursive = true)
        {
            HashSet<ObjectId> collectedIds = new HashSet<ObjectId>();
            Queue<ObjectId> queue = new Queue<ObjectId>();
            queue.Enqueue(blockRefId);

            while (queue.Count > 0)
            {
                ObjectId currentId = queue.Dequeue();
                var dbObj = tr.GetObject(currentId, OpenMode.ForRead);

                if (dbObj is BlockReference blockRef)
                {
                    foreach (ObjectId attId in blockRef.AttributeCollection)
                    {
                        collectedIds.Add(attId);
                    }

                    // 2. Récupérer le contenu de la définition du bloc
                    var btr = tr.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    if (btr?.IsLayout == false)
                    {
                        foreach (ObjectId innerEntId in btr)
                        {
                            // Si l'entité n'a pas encore été traitée
                            if (collectedIds.Add(innerEntId))
                            {
                                // Si on veut de la récursivité et que c'est un sous-bloc, on l'ajoute à la file d'attente
                                if (recursive)
                                {
                                    var innerObj = tr.GetObject(innerEntId, OpenMode.ForRead);
                                    if (innerObj is BlockReference)
                                    {
                                        queue.Enqueue(innerEntId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return collectedIds;
        }

        private static void FlatSCU()
        {
            Database db = Generic.GetDatabase();
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                UcsTable ucsTable = (UcsTable)db.UcsTableId.GetDBObject();
                foreach (ObjectId ucsId in ucsTable)
                {
                    UcsTableRecord ucs = (UcsTableRecord)ucsId.GetDBObject(OpenMode.ForWrite);
                    if (ucs.Origin.Z != 0)
                    {
                        Generic.WriteMessage($"Le SCU \"{ucs.Name}\" à été aplati. {ucs.Origin.Z} -> 0");
                    }
                    ucs.Origin = ucs.Origin.Flatten();
                }
                tr.Commit();
            }
        }

        private static void FlatObjects(ObjectId[] objectIds, bool processInsideBlocks)
        {
            Database db = Generic.GetDatabase();
            using (LongOperationProcess LongOperation = new LongOperationProcess("Flattening..."))
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // HashSet permet d'éviter les doublons si l'utilisateur a sélectionné plusieurs 
                // blocs qui partagent la même définition (BlockTableRecord).
                HashSet<ObjectId> finalIdsToProcess = new HashSet<ObjectId>(objectIds);

                // Si l'utilisateur a répondu "Oui", on injecte le contenu des blocs dans la liste finale
                if (processInsideBlocks)
                {
                    foreach (ObjectId id in objectIds)
                    {
                        var dbObj = tr.GetObject(id, OpenMode.ForRead);
                        if (dbObj is BlockReference bk)
                        {
                            foreach (var innerId in GetEntityInBlock(id, tr, recursive: true))
                            {
                                finalIdsToProcess.Add(innerId);
                            }
                        }
                    }
                }

                LongOperation.SetTotalOperations(finalIdsToProcess.Count);

                // Traitement de l'aplatissement
                HashSet<ObjectId> updatedBlockDefs = new HashSet<ObjectId>();
                foreach (ObjectId entityObjectId in finalIdsToProcess)
                {
                    if (LongOperation.IsCanceled) { return; }
                    LongOperation.UpdateProgress();

                    var DbObjEnt = tr.GetObject(entityObjectId, OpenMode.ForWrite, true, true);
                    if (!(DbObjEnt is Entity entity))
                    {
                        DbObjEnt.DowngradeOpen();
                        continue;
                    }

                    if (!entity.Flatten())
                    {
                        Debug.WriteLine($"Entité non traitée : \"{entity.GetType()}\"");
                    }

                    if (entity is BlockReference bk && !updatedBlockDefs.Contains(bk.BlockTableRecord))
                    {
                        bk.RegenAllBlkDefinition();
                        updatedBlockDefs.Add(bk.BlockTableRecord);
                    }
                }
                tr.Commit();
            }
        }
    }
}