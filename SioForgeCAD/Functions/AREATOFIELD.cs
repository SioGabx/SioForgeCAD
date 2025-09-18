using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SioForgeCAD.Commun.Mist;
using SioForgeCAD.Commun.Mist.DrawJigs;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

namespace SioForgeCAD.Functions
{
    public static class AREATOFIELD
    {
        public static void Compute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();

            if (!ed.GetImpliedSelection(out PromptSelectionResult AllSelectedObject))
            {
                AllSelectedObject = ed.GetSelection();
            }

            if (AllSelectedObject.Status != PromptStatus.OK)
            {
                return;
            }

            var AllSelectedObjectIds = AllSelectedObject.Value.GetObjectIds();
            using (var tr = doc.TransactionManager.StartTransaction())
            {

                short DisplayPrecision = (short)Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("LUPREC");


                List<double> AreaList = new List<double>();
                List<string> AcObjPropAreaList = new List<string>();
                string ValueFormating = " \\f \"%lu2%pr2\"";

                foreach (ObjectId ObjId in AllSelectedObjectIds)
                {
                    var DBObject = ObjId.GetDBObject();
                    if (DBObject is Entity Ent)
                    {
                        Ent.RegisterHighlight();
                        AreaList.Add(Ent.TryGetArea());
                        AcObjPropAreaList.Add($"%<\\AcObjProp Object(%<\\_ObjId {Ent.ObjectId.ToString().TrimStart('(').TrimEnd(')')}>%).Area{ValueFormating}>%");
                    }
                }

                var FieldValue = string.Empty;
                if (AcObjPropAreaList.Count == 1)
                {
                    FieldValue = AcObjPropAreaList.First();
                }
                else if (AcObjPropAreaList.Count > 1)
                {
                    FieldValue = $"%<\\AcExpr ({string.Join(" + ", AcObjPropAreaList)}){ValueFormating}>%";
                }


                using (var DynSumMtext = new MText())
                {
                    DynSumMtext.TextHeight = .1;
                    DynSumMtext.Location = Point3d.Origin;
                    Field field = new Field("Aire : " + FieldValue);
                    field.Evaluate();
                    DynSumMtext.SetField(field);




                    using (var GetPointJig = new GetPointJig()
                    {
                        Entities = new DBObjectCollection() { DynSumMtext },
                        StaticEntities = new DBObjectCollection(),
                        UpdateFunction = null
                    })
                    {
                        var GetPointTransientResult = GetPointJig.GetPoint($"Indiquez l'emplacement du texte\n\nFormule : \n{string.Join(" + ", AreaList.ConvertAll(dbl => dbl.RoundToNearestMultiple(1)))} = {AreaList.Sum().RoundToNearestMultiple(1)}");

                        if (GetPointTransientResult.Point != null && GetPointTransientResult.PromptPointResult.Status == PromptStatus.OK)
                        {
                            DynSumMtext.Location = GetPointTransientResult.Point.SCU;
                            DynSumMtext.AddToDrawing();
                        }
                    }



                }
                HightLighter.UnhighlightAll();
                tr.Commit();
            }
        }



    }
}
