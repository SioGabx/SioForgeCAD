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
    public class DRAWCPTERRAIN
    {
        private List<TerrainPoint> TerrainPoints = new List<TerrainPoint>();
        private Polyline TerrainBasePolyline;
        private double TerrainBaseAltimetrie = 0;
        public class TerrainPoint
        {
            public Points StartPoint { get; set; }
            public Points EndPoint { get; set; }
            public BlockReference Block { get; set; }
            public double DistanceFromTerrainStart { get; set; }
            public double Altitude { get; set; }
            public double Length { get; set; }
            public Vector3d Vector3D { get; set; }
        }


        private Points GetProjectedPointOnBaseTerrain(Points BasePoint, Polyline Terrain)
        {
            var ListOfPerpendicularLines = PerpendicularPoint.GetListOfPerpendicularLinesFromPoint(BasePoint, Terrain, true);
            if (ListOfPerpendicularLines.Count > 0)
            {
                using (Line NearestPointPerpendicularLine = ListOfPerpendicularLines.FirstOrDefault())
                {
                    return NearestPointPerpendicularLine.EndPoint.ToPoints();
                }
            }
            return null;
        }

        public void DrawTerrainFromSelectedPoints()
        {
            Database db = Generic.GetDatabase();
            Editor ed = Generic.GetEditor();

            Polyline TerrainBasePolyline = LinesExtentions.AskForSelection("\nSélectionnez une polyligne comme base de terrain");
            if (TerrainBasePolyline == null)
            {
                return;
            }
            TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
            SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
            PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
            {
                MessageForAdding = $"\nSelectionnez des côtes à projeter",
                RejectObjectsOnLockedLayers = false
            };
            var BlockRefSelection = ed.GetSelection(PromptBlocSelectionOptions, SelectionEntitiesFilter);
            if (BlockRefSelection.Status != PromptStatus.OK) { return; }

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                ObjectId[] SelectedCoteBloc = BlockRefSelection.Value.GetObjectIds();
                this.TerrainBasePolyline = TerrainBasePolyline;
                this.TerrainPoints = GetTerrainPoints(SelectedCoteBloc);
                this.TerrainBaseAltimetrie = GetMinimalAltimetrie();

                DrawCPTerrainInsertionTransientPoints insertionTransientPoints = new DrawCPTerrainInsertionTransientPoints(GetTerrain, TerrainBasePolyline);
                var InsertionTransientPointsValues = insertionTransientPoints.GetPoint("Specifiez un point sur le coté pour definir l'orientation de la coupe", Points.Null);
                if (InsertionTransientPointsValues.PromptPointResult.Status == PromptStatus.OK)
                {
                    List<Entity> Terrain = GetTerrain(InsertionTransientPointsValues.Point);
                    ObjectIdCollection EntitiesObjectIdCollection = new ObjectIdCollection();
                    foreach (Entity ent in Terrain)
                    {
                        EntitiesObjectIdCollection.Add(ent.AddToDrawing());
                        ent.Dispose();
                    }
                    Commun.Drawing.Groups.Create("CPTERRAIN", $"Terrain généré à partir de {Generic.GetExtensionDLLName()}.", EntitiesObjectIdCollection);

                }
                HightLighter.UnhighlightAll(SelectedCoteBloc);
                trans.Commit();
            }
        }

        public static bool CheckIfIsInversed(Polyline BasePolyline, Point3d TargetPoint)
        {
            return BasePolyline.IsAtLeftSide(TargetPoint);
        }

        private List<TerrainPoint> GetTerrainPoints(ObjectId[] SelectedCotes)
        {
            List<TerrainPoint> terrainPoints = new List<TerrainPoint>();
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
                    DistanceFromTerrainStart = Lines.GetLength(TerrainBasePolyline.StartPoint.Flatten(), BlkRefOnTerrainPosition.SCG.Flatten())
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
            return terrainPoints;
        }

        public double GetMinimalAltimetrie()
        {
            var ed = Generic.GetEditor();

            double CoteMinimal = double.MaxValue;
            foreach (TerrainPoint terrainPoint in TerrainPoints)
            {
                CoteMinimal = Math.Min(CoteMinimal, terrainPoint.Altitude - 1);
            }
            double CoteMinimalMaximumMultipleOfFive = CoteMinimal.RoundToNearestMultiple(5);

            PromptDoubleOptions pDoubleOpts = new PromptDoubleOptions($"\nVeuillez entrer l'altimétrie de la ligne de base.\nCote minimale trouvée : {CoteMinimal}")
            {
                DefaultValue = CoteMinimalMaximumMultipleOfFive
            };

            PromptDoubleResult pDoubleRes = ed.GetDouble(pDoubleOpts);
            if (pDoubleRes.Status == PromptStatus.OK)
            {
                return pDoubleRes.Value;
            }
            else
            {
                return CoteMinimalMaximumMultipleOfFive;
            }
        }


        public List<Entity> GetTerrain(Points points)
        {
            var TerrainEntity = new List<Entity>();

            Vector3d TerrainBaseLineVector = TerrainBasePolyline.EndPoint - TerrainBasePolyline.StartPoint;
            if (CheckIfIsInversed(TerrainBasePolyline, points.SCG))
            {
                TerrainBaseLineVector *= -1;
            }
            Vector3d PerpendicularTerrainBaseLineVector = new Vector3d(-TerrainBaseLineVector.Y, TerrainBaseLineVector.X, 0).GetNormal();

            List<Points> TerrainPointsToConnect = new List<Points>();
            for (int i = 0; i < TerrainPoints.Count; i++)
            {
                TerrainPoint terrainPoint = TerrainPoints[i];
                terrainPoint.Length = terrainPoint.Altitude - TerrainBaseAltimetrie;
                terrainPoint.Vector3D = PerpendicularTerrainBaseLineVector.MultiplyBy(terrainPoint.Length);
                terrainPoint.EndPoint = GetEndPoint(terrainPoint.StartPoint, terrainPoint.Vector3D);
                TerrainPointsToConnect.Add(terrainPoint.EndPoint);
                TerrainEntity.Add(Lines.GetFromPoints(terrainPoint.StartPoint, terrainPoint.EndPoint));

                double USCRotation = GetRotation(TerrainBaseLineVector, Vector3d.ZAxis);
                string AltimetrieStr = CotePoints.FormatAltitude(terrainPoint.Altitude);
                Dictionary<string, string> AltimetrieValue = new Dictionary<string, string>() { { "ALTIMETRIE", AltimetrieStr } };

                var CotationBlockRefObjectId = Commun.Drawing.BlockReferences.InsertFromNameImportIfNotExist(Settings.BlocNameAltimetrieCoupes, terrainPoint.EndPoint, USCRotation, AltimetrieValue);
                TerrainEntity.Add(CotationBlockRefObjectId.GetEntity().Clone() as Entity);
                CotationBlockRefObjectId.EraseObject();
            }
            TerrainEntity.Add(Polylines.GetPolylineFromPoints(TerrainPointsToConnect));
            return TerrainEntity;
        }

        public static double GetRotation(Vector3d vector, Vector3d normal)
        {
            using (Plane plane = new Plane(Point3d.Origin, normal))
            {
                var ocsXAxis = Vector3d.XAxis.TransformBy(Matrix3d.PlaneToWorld(plane));
                return ocsXAxis.GetAngleTo(vector.ProjectTo(normal, normal), normal);
            }
        }

        private static Points GetEndPoint(Points startPoint, Vector3d vector3d)
        {
            return startPoint.SCG.Add(vector3d).ToPoints();
        }

    }

    internal class DrawCPTerrainInsertionTransientPoints : GetPointTransient
    {
        private readonly Func<Points, List<Entity>> UpdateFunction;
        private readonly Polyline TerrainBasePolyline;
        public DrawCPTerrainInsertionTransientPoints(Func<Points, List<Entity>> UpdateFunction, Polyline TerrainBasePolyline) : base(null, null)
        {
            this.UpdateFunction = UpdateFunction;
            this.TerrainBasePolyline = TerrainBasePolyline;
        }

        private bool CheckIfRedrawIsNeeded(Point3d LastPoint, Point3d NewPoint)
        {
            if (LastPoint == NewPoint)
            {
                //first draw
                return true;
            }
            //Redraw only if Inversed changed;
            bool IsLastPointInverted = DRAWCPTERRAIN.CheckIfIsInversed(TerrainBasePolyline, LastPoint);
            bool IsNewPointInverted = DRAWCPTERRAIN.CheckIfIsInversed(TerrainBasePolyline, NewPoint);
            return IsLastPointInverted != IsNewPointInverted;
        }


        public override void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
        {
            if (CheckIfRedrawIsNeeded(moveToPt, curPt))
            {
                SetEntities = UpdateFunction(moveToPt.ToPoints()).ToDBObjectCollection();
                foreach (Autodesk.AutoCAD.GraphicsInterface.Drawable entity in Drawable)
                {
                    RedrawTransEntities(entity);
                }
            }
        }

        public override void ClearTransGraphics()
        {
            foreach (Entity entity in GetEntities)
            {
                entity.Dispose();
            }
            base.ClearTransGraphics();
        }

    }
}
