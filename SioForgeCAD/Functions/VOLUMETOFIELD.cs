using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Commun.Mist.DrawJigs;
using System.Collections.Generic;
using System.Linq;


namespace SioForgeCAD.Functions
{
    public static class VOLUMETOFIELD
    {
        /*
        Function: Compute the volume between two closed polylines at different elevations.
        This is useful for civil engineering, topography, and construction projects.
        It uses the truncated prism formula: V = h * (A1 + A2 + sqrt(A1 * A2)) / 3,
        where A1 and A2 are the areas of the top and bottom polylines and h is the vertical distance.
        The function creates a dynamic AutoCAD field that updates automatically if the polylines change.
        Supports offset polylines or irregular closed contours
        */

        static double PreviousDepth = 0.3;
        public static void Compute()
        {
            Document doc = Generic.GetDocument();
            Editor ed = Generic.GetEditor();
            using (var tr = doc.TransactionManager.StartTransaction())
            {
                try
                {
                    if (!AskGetAreaFromAPolyligne("Sélectionnez une polyligne qui defini le bord HAUT de la noue", out ObjectId PolyBordHautObjId, out double PolyBordHautSurface)) { return; }
                    if (!AskGetAreaFromAPolyligne("Sélectionnez une polyligne qui defini le bord BAS de la noue", out ObjectId PolyBordBasObjId, out double PolyBordBasSurface)) { return; }
                    if (!AskGetDepth(out double Depth)) { return; }

                    string valueFormatting = " \\f \"%lu2%pr2\"";

                    string A1 = $"%<\\AcObjProp Object(%<\\_ObjId {FormatObjIdForFields(PolyBordHautObjId)}>%).Area{valueFormatting}>%";
                    string A2 = $"%<\\AcObjProp Object(%<\\_ObjId {FormatObjIdForFields(PolyBordBasObjId)}>%).Area{valueFormatting}>%";
                    string volumeField = $"%<\\AcExpr ({Depth} * ({A1} + {A2} + ({A1} * {A2})^(1/2)) / 3){valueFormatting}>%";

                    PlaceTxt($"Aire : {A1} m²\nProfondeur : {Depth} m\nVolume : {volumeField} m³");

                }
                finally
                {
                    tr.Commit();
                }
            }

        }

        private static void PlaceTxt(string Value)
        {
            using (var DynSumMtext = new MText())
            {
                DynSumMtext.TextHeight = .1;
                DynSumMtext.Location = Point3d.Origin;
                DynSumMtext.Rotation = 0;
                Field field = new Field(Value);
                field.Evaluate();

                DynSumMtext.SetField(field);

                Generic.WriteMessage(field.Value);

                using (var GetPointJig = new GetPointJig()
                {
                    Entities = new DBObjectCollection() { DynSumMtext },
                    StaticEntities = new DBObjectCollection(),
                    UpdateFunction = null
                })
                {
                    var GetPointTransientResult = GetPointJig.GetPoint("Indiquez l'emplacement du texte");

                    if (GetPointTransientResult.Point != null && GetPointTransientResult.PromptPointResult.Status == PromptStatus.OK)
                    {
                        DynSumMtext.Location = GetPointTransientResult.Point.SCG;
                        DynSumMtext.AddToDrawing();
                    }
                }

            }
        }

        private static string FormatObjIdForFields(ObjectId ObjId)
        {
            return ObjId.ToString().TrimStart('(').TrimEnd(')');
        }
        private static bool AskGetDepth(out double Depth)
        {
            Editor ed = Generic.GetEditor();
            Depth = PreviousDepth;
            var getDoubleOption = new PromptDoubleOptions("Entrez la profondeur de la noue/bassin")
            {
                DefaultValue = Depth,
                UseDefaultValue = true,
                AllowArbitraryInput = true,
                AllowNegative = false,
                AllowNone = false,
            };
            var GetLargeurValue = ed.GetDouble(getDoubleOption);
            if (GetLargeurValue.Status != PromptStatus.OK)
            {
                return false;
            }
            Depth = GetLargeurValue.Value;
            PreviousDepth = Depth;
            return true;
        }
        private static bool AskGetAreaFromAPolyligne(string Message, out ObjectId ObjId, out double Area)
        {
            Area = 0;
            ObjId = ObjectId.Null;

            Editor ed = Generic.GetEditor();
            using (Polyline PolyBordHaut = ed.GetPolyline("\n" + Message, false, false))
            {
                if (PolyBordHaut == null)
                {
                    return false;
                }
                Area = PolyBordHaut.TryGetArea();
                if (Area == 0)
                {
                    return false;
                }
                ObjId = PolyBordHaut.ObjectId;
            }
            return true;
        }



    }
}
