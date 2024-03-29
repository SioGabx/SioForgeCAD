﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public class SCALEBY
    {
        public static void ScaleBy()
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            PromptSelectionResult selResult = ed.GetSelection();
            if (selResult.Status == PromptStatus.OK)
            {
                PromptDoubleOptions promptDoubleOptions = new PromptDoubleOptions("Indiquez une echelle d'agrandissement ou de réduction")
                {
                    AllowArbitraryInput = true,
                    AllowNegative = false,
                    AllowZero = false,
                    DefaultValue = 1,
                };
                var AskRatioResult = ed.GetDouble(promptDoubleOptions);
                if (AskRatioResult.Status == PromptStatus.OK)
                {
                    double Ratio = AskRatioResult.Value;
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        SelectionSet selSet = selResult.Value;
                        foreach (SelectedObject selObj in selSet)
                        {
                            if (selObj != null)
                            {
                                if (selObj.ObjectId.GetDBObject(OpenMode.ForWrite) is Entity ent)
                                {
                                    var TransformCenter = ent.GetExtents().GetCenter();
                                    if (ent is BlockReference blkRef)
                                    {
                                        TransformCenter = blkRef.Position;
                                    }
                                    Matrix3d scaleMatrix = Matrix3d.Scaling(Ratio, TransformCenter);
                                    ent.TransformBy(scaleMatrix);
                                }
                            }
                        }
                        tr.Commit();
                    }
                }

            }


        }
    }
}
