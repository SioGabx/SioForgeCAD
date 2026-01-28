using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;

namespace SioForgeCAD.Functions
{
    public static class BLKSETTOBYBBLOCK
    {
        public enum HatchSupport { Ignore, Include, SetToWhite }
        public static void ByBlock(HatchSupport IgnoreHatch)
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();
            if (!ed.GetBlocks(out ObjectId[] ObjectIds, null, true, true))
            {
                return;
            }

            bool? ChangeEntitySelectedNotInsideBlock = null;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId blockRefId in ObjectIds)
                {
                    var SelEnt = blockRefId.GetDBObject(OpenMode.ForWrite) as Entity;
                    if (SelEnt is BlockReference blockRef)
                    {
                        BlockTableRecord blockDef = blockRef.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                        foreach (ObjectId EntityInBlockDef in blockDef)
                        {
                            if (EntityInBlockDef.GetDBObject(OpenMode.ForWrite) is Entity ent)
                            {
                                SetEntityToByBloc(ent, IgnoreHatch);
                            }
                        }
                        foreach (ObjectId BlkRefInDrawing in blockDef.GetBlockReferenceIds(true, false))
                        {
                            (BlkRefInDrawing.GetDBObject(OpenMode.ForWrite) as BlockReference).RecordGraphicsModified(true);
                        }

                    }
                    else
                    {
                        if (ChangeEntitySelectedNotInsideBlock is null)
                        {
                            var askResult = ed.GetOptions("Des entités en dehors d'un bloc ont été sélectionnées. Voulez-vous effectuer l'opération sur ces entités également ?", false, "Oui", "Non");
                            ChangeEntitySelectedNotInsideBlock = askResult.Status == PromptStatus.OK && askResult.StringResult == "Oui" ? true : false;
                        }
                        if (ChangeEntitySelectedNotInsideBlock == true)
                        {
                            SetEntityToByBloc(SelEnt, IgnoreHatch);
                        }

                    }
                }
                tr.Commit();
            }
        }


        private static void SetEntityToByBloc(Entity entity, HatchSupport hatchSupport)
        {
            if (entity is Hatch hatchEnt)
            {
                switch (hatchSupport)
                {
                    case HatchSupport.Ignore:
                        return;
                    case HatchSupport.Include:
                        SetEntityToByBloc(hatchEnt);
                        hatchEnt.BackgroundColor = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                        hatchEnt.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                        return;
                    case HatchSupport.SetToWhite:
                        SetEntityToByBloc(hatchEnt);
                        hatchEnt.Color = Color.FromRgb(255, 255, 255);
                        hatchEnt.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                        return;
                }
            }
            else
            {
                SetEntityToByBloc(entity);
            }

        }

        private static void SetEntityToByBloc(Entity entity)
        {
            entity.ColorIndex = 0; //ByBlock 
            entity.Transparency = new Autodesk.AutoCAD.Colors.Transparency(TransparencyMethod.ByBlock);
            entity.Linetype = "BYBLOCK";
            entity.LineWeight = LineWeight.ByBlock;
            entity.Layer = "0";
            //entity.Visible = false;
        }
    }
}
