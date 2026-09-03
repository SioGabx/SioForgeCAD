using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using System;

namespace SioForgeCAD.Functions
{
    public static class CREATELABELSONCONTOURELEVATIONS
    {
        public static void Create()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            PromptDistanceOptions intervalOptions = new PromptDistanceOptions("Intervalle entre les textes : ")
            {
                AllowZero = false,
                AllowNegative = false,
                DefaultValue = 10.0,
                UseDefaultValue = true
            };
            PromptDoubleResult intervalResult = ed.GetDistance(intervalOptions);
            if (intervalResult.Status != PromptStatus.OK)
            {
                return;
            }
            double interval = intervalResult.Value;

            PromptDistanceOptions heightOptions = new PromptDistanceOptions("Hauteur du texte : ")
            {
                AllowZero = false,
                AllowNegative = false,
                DefaultValue = 0.20,
                UseDefaultValue = true
            };
            PromptDoubleResult heightResult = ed.GetDistance(heightOptions);
            if (heightResult.Status != PromptStatus.OK)
            {
                return;
            }
            double textHeight = heightResult.Value;

            PromptIntegerOptions decimalOptions = new PromptIntegerOptions("Nombre de décimales : ")
            {
                AllowNegative = false,
                AllowZero = true,
                DefaultValue = 2,
                UseDefaultValue = true
            };
            PromptIntegerResult decimalResult = ed.GetInteger(decimalOptions);
            if (decimalResult.Status != PromptStatus.OK)
            {
                return;
            }
            int decimals = decimalResult.Value;

            PromptDistanceOptions offsetOptions = new PromptDistanceOptions("\nDécalage du texte par rapport à la courbe : ")
            {
                AllowNegative = true,
                DefaultValue = 0.0,
                UseDefaultValue = true
            };
            PromptDoubleResult offsetResult = ed.GetDistance(offsetOptions);
            if (offsetResult.Status != PromptStatus.OK)
            {
                return;
            }
            double offset = offsetResult.Value;

            // Option pour le masque d'arrière-plan
            PromptKeywordOptions maskOptions = new PromptKeywordOptions("\nAjouter un masque d'arrière-plan ? ")
            {
                AllowNone = false
            };
            maskOptions.Keywords.Add("Oui");
            maskOptions.Keywords.Add("Non");
            maskOptions.Keywords.Default = "Non";

            PromptResult maskResult = ed.GetKeywords(maskOptions);
            if (maskResult.Status != PromptStatus.OK)
            {
                return;
            }
            bool useBackgroundMask = (maskResult.StringResult == "Oui");

            PromptSelectionOptions selectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = "\nSélectionnez les courbes de niveau : "
            };

            SelectionFilter filter = new SelectionFilter(new TypedValue[] {
                new TypedValue((int)DxfCode.Operator, "<OR"),
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
                new TypedValue((int)DxfCode.Start, "POLYLINE"),
                new TypedValue((int)DxfCode.Operator, "OR>")
            });

            PromptSelectionResult selectionResult = ed.GetSelection(selectionOptions, filter);

            if (selectionResult.Status != PromptStatus.OK)
            {
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = Generic.GetCurrentSpaceBlockTableRecord(tr, OpenMode.ForWrite);

                foreach (SelectedObject selectedObject in selectionResult.Value)
                {
                    if (selectedObject == null)
                    {
                        continue;
                    }

                    if (!(selectedObject.ObjectId.GetDBObject(OpenMode.ForRead) is Entity entity))
                    {
                        continue;
                    }

                    if (entity is Polyline polyline)
                    {
                        double elevation = polyline.Elevation;
                        CreateTextsOnPolyline(polyline, elevation, interval, textHeight, decimals, offset, useBackgroundMask, modelSpace, tr);
                    }
                }

                tr.Commit();
            }

            Generic.WriteMessage("LABELCONTOURELEVATIONS terminé.");
        }

        private static void CreateTextsOnPolyline(Polyline polyline, double elevation, double interval, double textHeight, int decimals, double offset, bool useBackgroundMask, BlockTableRecord modelSpace, Transaction tr)
        {
            double length = polyline.Length;
            double startDistance = interval;

            for (double distance = startDistance; distance <= length + 1e-8; distance += interval)
            {
                Point3d point;

                try
                {
                    point = polyline.GetPointAtDist(distance);
                }
                catch
                {
                    continue;
                }

                Vector3d tangent;

                try
                {
                    tangent = polyline.GetFirstDerivative(point);
                }
                catch
                {
                    continue;
                }

                if (tangent.Length < 1e-9)
                {
                    continue;
                }

                tangent = tangent.GetNormal();

                // Orientation du texte pour la lisibilité de gauche à droite
                double angle = Math.Atan2(tangent.Y, tangent.X);
                if (angle > Math.PI / 2.0 && angle < 3.0 * Math.PI / 2.0)
                {
                    tangent = tangent.Negate();
                    angle += Math.PI;
                }

                // Décalage du texte
                Vector3d normal = new Vector3d(-tangent.Y, tangent.X, 0.0);
                Point3d textPoint = point;

                if (Math.Abs(offset) > 1e-9)
                {
                    textPoint = point + (normal * offset);
                }

                textPoint = new Point3d(textPoint.X, textPoint.Y, elevation);
                string textValue = elevation.ToString("F" + decimals);

                Entity textEntity;

                if (useBackgroundMask)
                {
                    MText mText = new MText
                    {
                        Contents = textValue,
                        TextHeight = textHeight,
                        Attachment = AttachmentPoint.MiddleCenter,
                        Location = textPoint,
                        Direction = tangent, // Définit la direction X du texte (alignement exact avec la tangente)
                        BackgroundFill = true,
                        UseBackgroundColor = true,
                        BackgroundScaleFactor = 1.2
                    };
                    textEntity = mText;
                }
                else
                {
                    DBText text = new DBText
                    {
                        TextString = textValue,
                        Height = textHeight,
                        Position = textPoint,
                        HorizontalMode = TextHorizontalMode.TextCenter,
                        VerticalMode = TextVerticalMode.TextVerticalMid,
                        Rotation = angle,
                        Normal = Vector3d.ZAxis
                    };

                    modelSpace.AppendEntity(text);
                    tr.AddNewlyCreatedDBObject(text, true);

                    text.AlignmentPoint = textPoint;
                    text.AdjustAlignment(Generic.GetDatabase());

                    textEntity = text;
                }

                if (useBackgroundMask)
                {
                    modelSpace.AppendEntity(textEntity);
                    tr.AddNewlyCreatedDBObject(textEntity, true);
                }
            }
        }
    }
}