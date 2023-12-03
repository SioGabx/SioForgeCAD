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

namespace SioForgeCAD.Functions
{
    public static class DRAWCPTERRAIN
    {
        public class TerrainPoint
        {
            public Points StartPoint { get; set; }
            public Points EndPoint { get; set; }
            public BlockReference Block { get; set; }
            public double DistanceFromTerrainStart { get; set; }
            public double Altitude { get; set; }
            public double Length { get; set; }
            public Vector3d vector3D { get; set; }
        }

        public static Polyline GetBaseTerrain()
        {
            while (true)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                Editor ed = doc.Editor;

                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
                {
                    MessageForAdding = "Sélectionnez une polyligne comme base de terrain",
                    SingleOnly = true,
                };
                PromptSelectionResult polyResult = ed.GetSelection(promptSelectionOptions);
                if (polyResult.Status != PromptStatus.OK)
                {
                    return null;
                }
                Entity SelectedEntity;

                using (Transaction GlobalTrans = db.TransactionManager.StartTransaction())
                {
                    SelectedEntity = polyResult.Value[0].ObjectId.GetEntity();
                }
                if (SelectedEntity is Line ProjectionTargetLine)
                {
                    SelectedEntity = ProjectionTargetLine.ToPolyline();
                }
                if (!(SelectedEntity is Polyline ProjectionTarget))
                {
                    ed.WriteMessage("L'objet sélectionné n'est pas une polyligne.");
                    continue;
                }
                return ProjectionTarget;
            }
        }

        public static Points GetProjectedPointOnBaseTerrain(Points BasePoint, Polyline Terrain)
        {
            var ListOfPerpendicularLines = PerpendicularPoint.GetListOfPerpendicularLinesFromPoint(BasePoint, Terrain, true);
            if (ListOfPerpendicularLines.Count > 0)
            {
                Line NearestPointPerpendicularLine = ListOfPerpendicularLines.FirstOrDefault();
                return NearestPointPerpendicularLine.EndPoint.ToPoints();
            }
            return null;
        }

        public static void DrawTerrainFromSelectedPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Polyline TerrainBasePolyline = GetBaseTerrain();

            TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
            SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
            PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = $"Selectionnez des côtes à projeter",
                RejectObjectsOnLockedLayers = false,
            };
            var BlockRefSelection = ed.GetSelection(PromptBlocSelectionOptions, SelectionEntitiesFilter);
            if (BlockRefSelection.Status != PromptStatus.OK)
            {
                return;
            }
            List<TerrainPoint> terrainPoints = new List<TerrainPoint>();

            List<ObjectId> SelectedCotes = BlockRefSelection.Value.GetObjectIds().ToList();
            //Dictionary<BlockReference, Points> BaseTerrainIntersectionPoint = new Dictionary<BlockReference, Points>();
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in SelectedCotes)
                {
                    var BlkRef = id.GetEntity() as BlockReference;
                    Points BlkRefPosition = BlkRef.Position.ToPoints();
                    var BlkRefOnTerrainPosition = GetProjectedPointOnBaseTerrain(BlkRefPosition, TerrainBasePolyline);
                    if (BlkRefOnTerrainPosition == null)
                    {
                        continue;
                    }
                    var TerrainPoint = new TerrainPoint()
                    {
                        StartPoint = BlkRefOnTerrainPosition,
                        Block = BlkRef,
                        Altitude = CotePoints.GetAltitudeFromBloc(BlkRef.ObjectId) ?? 0,
                        DistanceFromTerrainStart = new Line(TerrainBasePolyline.StartPoint.Flatten(), BlkRefOnTerrainPosition.SCG.Flatten()).Length
                    };
                    Lines.Draw(new Line(TerrainBasePolyline.StartPoint, BlkRefOnTerrainPosition.SCG), 8);
                    terrainPoints.Add(TerrainPoint);
                }
                terrainPoints = terrainPoints.OrderBy(TerrainPoint => TerrainPoint.DistanceFromTerrainStart).ToList();

                TerrainPoint StartTerPoint = new TerrainPoint()
                {
                    StartPoint = TerrainBasePolyline.StartPoint.ToPoints(),
                    Block = terrainPoints.First().Block,
                    Altitude = terrainPoints.First().Altitude,
                    DistanceFromTerrainStart = 0
                };
                TerrainPoint EndTerPoint = new TerrainPoint()
                {
                    StartPoint = TerrainBasePolyline.EndPoint.ToPoints(),
                    Block = terrainPoints.Last().Block,
                    Altitude = terrainPoints.Last().Altitude,
                    DistanceFromTerrainStart = 999999//TerrainBasePolyline.Length
                };
                terrainPoints.Insert(0, StartTerPoint);
                terrainPoints.Insert(terrainPoints.Count, EndTerPoint);

                Vector3d TerrainBaseLineVector = TerrainBasePolyline.EndPoint - TerrainBasePolyline.StartPoint;
                Vector3d PerpendicularTerrainBaseLineVector = new Vector3d(-TerrainBaseLineVector.Y, TerrainBaseLineVector.X, 0).GetNormal();

                double CoteMinimal = double.MaxValue;

                foreach (TerrainPoint terrainPoint in terrainPoints)
                {
                    CoteMinimal = Math.Min(CoteMinimal, terrainPoint.Altitude - 1);
                }
                double CoteMinimalMaximumMultipleOfFive = CoteMinimal.RoundToNearestMultiple(5);

                List<Points> TerrainPointsToConnect = new List<Points>();
                for (int i = 0; i < terrainPoints.Count; i++)
                {
                    TerrainPoint terrainPoint = terrainPoints[i];
                    
                    terrainPoint.Length = terrainPoint.Altitude - CoteMinimalMaximumMultipleOfFive;
                    terrainPoint.vector3D = PerpendicularTerrainBaseLineVector.MultiplyBy(terrainPoint.Length);
                    terrainPoint.EndPoint = GetEndPoint(terrainPoint.StartPoint, terrainPoint.vector3D);
                    TerrainPointsToConnect.Add(terrainPoint.EndPoint);
                    Lines.Draw(terrainPoint.StartPoint, terrainPoint.EndPoint);
                }
                Polylines.Draw(TerrainPointsToConnect);
                trans.Commit();
            }
        }

        private static Points GetEndPoint(Points startPoint, Vector3d vector3d)
        {
            return startPoint.SCG.Add(vector3d).ToPoints();
        }

    }
}
