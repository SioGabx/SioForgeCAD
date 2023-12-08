using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Drawing;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class DROPCPOBJECTTOTERRAIN
    {
        public static async void Project()
        {
            await Task.Run(() => { 
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Polyline TerrainBasePolyline = LinesExtentions.AskForSelection("Sélectionnez une polyligne comme base de terrain");
            if (TerrainBasePolyline == null)
            {
                return;
            }
            var PromptSelectEntitiesOptions = new PromptSelectionOptions()
            {
                MessageForAdding = "Selectionnez les entités à projeter sur le terrain via le UCS courrant"
            };

            var AllSelectedObjectIds = ed.GetSelection(PromptSelectEntitiesOptions).Value.GetObjectIds();

            using (Transaction acTrans = db.TransactionManager.StartTransaction())
            {
                Matrix3d ucsMatrix = ed.CurrentUserCoordinateSystem;
                Vector3d PerpendicularVector = ucsMatrix.CoordinateSystem3d.Yaxis.MultiplyBy(-TerrainBasePolyline.Length); 

                foreach (ObjectId SelectedObjectId in AllSelectedObjectIds)
                {
                    if (!(SelectedObjectId.GetEntity(OpenMode.ForWrite) is BlockReference blkRef))
                    {
                        continue;
                    }

                    for (int PolylineSegmentIndex = 0; PolylineSegmentIndex < Polylines.getVerticesMaximum(TerrainBasePolyline); PolylineSegmentIndex++)
                    {
                        var PolylineSegment = Polylines.GetSegmentPoint(TerrainBasePolyline, PolylineSegmentIndex);
                        Line SegmentLine = new Line(PolylineSegment.PolylineSegmentStart, PolylineSegment.PolylineSegmentEnd);
                        Line PerpendicularLine = new Line(blkRef.Position, blkRef.Position.Add(PerpendicularVector.MultiplyBy(new Line(SegmentLine.EndPoint, blkRef.Position).Length)));
                        if (!Lines.AreLinesCutting(SegmentLine, PerpendicularLine, out Point3dCollection IntersectionPointsFounds))
                        {
                            continue;
                        }
                        Vector3d translationVector = IntersectionPointsFounds[0] - blkRef.Position;
                        blkRef.TransformBy(Matrix3d.Displacement(translationVector));
                    }
                }
                acTrans.Commit();
            }
            });
        }
    }
}
