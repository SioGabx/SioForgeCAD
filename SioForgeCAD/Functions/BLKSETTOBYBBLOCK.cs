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

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId blockRefId in ObjectIds)
                {
                    if (!(blockRefId.GetDBObject(OpenMode.ForWrite) is BlockReference blockRef))
                    {
                        return;
                    }

                    BlockTableRecord blockDef = blockRef.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                    foreach (ObjectId EntityInBlockDef in blockDef)
                    {
                        if (EntityInBlockDef.GetDBObject(OpenMode.ForWrite) is Entity ent)
                        {
                            if (ent is Hatch hatchEnt)
                            {
                                switch (IgnoreHatch)
                                {
                                    case HatchSupport.Ignore:
                                        continue;
                                    case HatchSupport.Include:
                                        SetEntityToByBloc(hatchEnt);
                                        hatchEnt.BackgroundColor = Color.FromColorIndex(ColorMethod.ByBlock, 0);
                                        break;
                                    case HatchSupport.SetToWhite:
                                        SetEntityToByBloc(hatchEnt);
                                        hatchEnt.Color = Color.FromRgb(255, 255, 255);
                                        hatchEnt.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                                        break;
                                }
                            }
                            else
                            {
                                SetEntityToByBloc(ent);
                            }
                        }
                    }
                    foreach (ObjectId BlkRefInDrawing in blockDef.GetBlockReferenceIds(true, false))
                    {
                        (BlkRefInDrawing.GetDBObject(OpenMode.ForWrite) as BlockReference).RecordGraphicsModified(true);
                    }
                }
                tr.Commit();
            }
        }

        private static void SetEntityToByBloc(Entity entity)
        {
            entity.ColorIndex = 0; //ByBlock 
            entity.Transparency = new Autodesk.AutoCAD.Colors.Transparency(TransparencyMethod.ByBlock);
            entity.Linetype = "BYBLOCK";
            entity.LineWeight = LineWeight.ByBlock;
            entity.Layer = "0";
        }
    }
}
