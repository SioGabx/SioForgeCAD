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

            Polyline TerrainBasePolyline = LinesExtentions.AskForSelection("Sélectionnez une polyligne comme base de terrain");
            if (TerrainBasePolyline == null)
            {
                return;
            }
            TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
            SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
            PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = $"Selectionnez des côtes à projeter\n",
                RejectObjectsOnLockedLayers = false,
            };
            var BlockRefSelection = ed.GetSelection(PromptBlocSelectionOptions, SelectionEntitiesFilter);
            if (BlockRefSelection.Status != PromptStatus.OK)
            {
                return;
            }

            var AskInversedOptions = new PromptKeywordOptions("Voullez-vous inverser la coupe ?");
            AskInversedOptions.Keywords.Add("OUI");
            AskInversedOptions.Keywords.Add("NON");
            var AskInversedResult = ed.GetKeywords(AskInversedOptions);
            bool IsInversed = AskInversedResult.StringResult == "OUI";

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
                    DistanceFromTerrainStart = TerrainBasePolyline.Length
                };
                if (terrainPoints.First().DistanceFromTerrainStart != StartTerPoint.DistanceFromTerrainStart)
                {
                    terrainPoints.Insert(0, StartTerPoint);
                }
                if (terrainPoints.Last().DistanceFromTerrainStart != EndTerPoint.DistanceFromTerrainStart)
                {
                    terrainPoints.Insert(terrainPoints.Count, EndTerPoint);
                }

                Vector3d TerrainBaseLineVector = TerrainBasePolyline.EndPoint - TerrainBasePolyline.StartPoint;
                if (IsInversed)
                {
                    TerrainBaseLineVector *= -1;
                }
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

                    double USCRotation = GetRotation(TerrainBaseLineVector, Vector3d.ZAxis);
                    string AltimetrieStr = CotePoints.FormatAltitude(terrainPoint.Altitude);
                    Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };
                    CotationElements.InsertBlocFromBlocName(Settings.BlocNameAltimetrieCoupes, terrainPoint.EndPoint, USCRotation, AltimetrieValue);
                }
                Polylines.Draw(TerrainPointsToConnect);
                trans.Commit();
            }
        }

        public static double GetRotation(Vector3d vector, Vector3d normal)
        {
            var plane = new Plane(Point3d.Origin, normal);
            var ocsXAxis = Vector3d.XAxis.TransformBy(Matrix3d.PlaneToWorld(plane));
            return ocsXAxis.GetAngleTo(vector.ProjectTo(normal, normal), normal);
        }

        private static Points GetEndPoint(Points startPoint, Vector3d vector3d)
        {
            return startPoint.SCG.Add(vector3d).ToPoints();
        }

    }
}
