using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SioForgeCAD.Functions
{
    public static class ROTATEONSINGLEAXIS
    {
        public static void Rotate()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            var per = ed.GetSelectionRedraw("Selectionnez des entités");
            if (per.Status != PromptStatus.OK) return;

            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEntrez l'angle de rotation (sens horaire) :")
            {
                AllowNegative = true,
                AllowZero = false,
                DefaultValue = 0,
                UseDefaultValue = true
            };

            if (double.TryParse(Clipboard.GetText(), out double clipboardValue))
            {
                pdo.DefaultValue = clipboardValue;
            }

            PromptDoubleResult PromptAngle = ed.GetDouble(pdo);
            if (PromptAngle.Status != PromptStatus.OK) return;

            var RotationAxis = GetRotateAxis(ed);
            if (RotationAxis is null) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId SelectedEntityObjId in per.Value.GetObjectIds())
                {
                    ApplyRotate(SelectedEntityObjId, PromptAngle.Value, RotationAxis);
                }

                tr.Commit();
            }
        }

        private static Vector3d? GetRotateAxis(Editor ed)
        {
            PromptKeywordOptions KeywordOptions = new PromptKeywordOptions("Sur quel axe de l'UCS souhaitez vous effectuer la rotation ?")
            {
                AllowArbitraryInput = false,
                AllowNone = false
            };
            KeywordOptions.Keywords.Add("XAxis");
            KeywordOptions.Keywords.Add("YAxis");
            KeywordOptions.Keywords.Add("ZAxis");
            var ax = ed.GetKeywords(KeywordOptions);
            if (ax.Status != PromptStatus.OK)
            {
                return null;
            }
            switch (ax.StringResult)
            {
                case "XAxis":
                    return Vector3d.XAxis;
                case "YAxis":
                    return Vector3d.YAxis;
                case "ZAxis":
                    return Vector3d.ZAxis;
            }
            return Vector3d.ZAxis;
        }

        private static void ApplyRotate(ObjectId SelectedEntityObjId, double DegreesAngle, Vector3d? Axis)
        {
            Entity entity = SelectedEntityObjId.GetEntity();
            if (entity == null)
            {
                return;
            }
            // Get the current bounding box
            Point3d GeometryCenter = entity.GetExtents().GetCenter();

            // Rotate the entity around the center of the bounding box
            Matrix3d rotationMatrix = Matrix3d.Rotation(
                DegreesAngle * (Math.PI / 180),
                Axis ?? Vector3d.ZAxis,
                GeometryCenter
            );

            entity.TransformBy(
                Matrix3d.Identity.PreMultiplyBy(rotationMatrix)
                );
        }
    }
}
