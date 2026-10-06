using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Functions
{
    public static class CCFROMZBLK
    {
        public static void CreateCotationBlocFromBlockZ()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!ed.GetBlocks(out ObjectId[] ids, "Selectionnez un bloc", false, false) || ids.Length == 0)
                    {
                        return;
                    }

                    foreach (ObjectId id in ids)
                    {
                        if (id.GetEntity() is BlockReference blkRef)
                        {
                            Point3d location = blkRef.Position;
                            double altimetrie = location.Z;

                            string altimetrieStr = CotePoints.FormatAltitude(altimetrie);
                            var attributes = new Dictionary<string, string>() { { "ALTIMETRIE", altimetrieStr } };

                            BlockReferences.InsertFromNameImportIfNotExist(
                                Settings.BlkAltimetry,
                                nameof(Settings.BlkAltimetry),
                                location.ToPoints(),
                                ed.GetUSCRotation(AngleUnit.Radians),
                                attributes
                            );
                        }
                    }
                }
                finally
                {
                    tr.Commit();
                }
            }
        }
    }
}
