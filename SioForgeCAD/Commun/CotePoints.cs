using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class CotePoints
    {
        public Points Points { get; }
        public double Altitude { get; }

        public CotePoints(Points Points, double Altitude)
        {
            this.Points = Points;
            this.Altitude = Altitude;
        }
        public CotePoints(Points Points, string Altitude)
        {
            this.Points = Points;
            if (double.TryParse(Altitude, out double AltitudeDbl))
            {
                this.Altitude = AltitudeDbl;
            }
            else
            {
                this.Altitude = 0;
            }
        }

        public enum SelectionPointsType
        {
            Points = 0,
            Bloc = 1,
        }

        private static SelectionPointsType GetSelectionPointsType()
        {
            if (Enum.TryParse(Properties.Settings.Default.SelectionPointsType, out SelectionPointsType SelectionPointsType))
            {
                return SelectionPointsType;
            }
            else
            {
                return SelectionPointsType.Points;
            }
        }

        private static void SaveSelectionPointsType(SelectionPointsType SelectionPointsType)
        {
            Properties.Settings.Default.SelectionPointsType = SelectionPointsType.ToString();
            Properties.Settings.Default.Save();
        }


        public static bool NullPointExit(CotePoints cotePoints)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            if (cotePoints is null)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    HightLighter.UnhighlightAll();
                    tr.Commit();
                    return true;
                }
            }
            return false;
        }


        private static double GetCote(Points Origin)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            //Write a point where the altitude is asked
            DBPoint PointDrawingEntity = new DBPoint(Origin.SCG);
            Autodesk.AutoCAD.DatabaseServices.ObjectId PointDrawingEntityObjectId;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                acBlkTblRec.AppendEntity(PointDrawingEntity);
                tr.AddNewlyCreatedDBObject(PointDrawingEntity, true);
                PointDrawingEntityObjectId = PointDrawingEntity.ObjectId;
                tr.Commit();
            }
            PromptDoubleOptions PromptDoubleAltitudeOptions = new PromptDoubleOptions("\n" + "Saississez la côte")
            {
                AllowNegative = false,
                AllowNone = false
            };
            PromptDoubleResult PromptDoubleAltitudeResult = ed.GetDouble(PromptDoubleAltitudeOptions);

            //remove the point where the altitude is asked
            if (PointDrawingEntityObjectId.IsErased == false)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBObject obj = PointDrawingEntityObjectId.GetObject(OpenMode.ForWrite);
                    obj.Erase(true);
                    tr.Commit();
                }
            }
            return PromptDoubleAltitudeResult.Value;
        }

        private static double? GetAltitudeFromBloc(Autodesk.AutoCAD.DatabaseServices.ObjectId BlocObjectId)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            TransactionManager tr = db.TransactionManager;
            BlockReference blkRef = (BlockReference)tr.GetObject(BlocObjectId, OpenMode.ForRead);

            foreach (Autodesk.AutoCAD.DatabaseServices.ObjectId AttributeObjectId in blkRef.AttributeCollection)
            {
                AttributeReference Attribute = (AttributeReference)tr.GetObject(AttributeObjectId, OpenMode.ForRead);
                if (Attribute.TextString.Contains("."))
                {
                    bool IsDouble = double.TryParse(Attribute.TextString.Trim(), out double Altimetrie);
                    if (IsDouble)
                    {
                        ed.WriteMessage($"Côte séléctionnée : {Altimetrie}\n");
                        blkRef.RegisterHighlight();
                        return Altimetrie;
                    }
                    else
                    {
                        double? ExtractedAltitude = Attribute.TextString.Trim().ExtractDoubleInStringFromPoint();
                        if (ExtractedAltitude.HasValue) { blkRef.RegisterHighlight(); }
                        return ExtractedAltitude;
                    }
                }
            }
            return null;
        }


        private static CotePoints GetBloc(string Message)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            do
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    //list of available SelectionFilter : https://help.autodesk.com/view/ACD/2018/ENU/?guid=GUID-7D07C886-FD1D-4A0C-A7AB-B4D21F18E484
                    TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                    SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
                    string PromptSelectionKeyWordString = SelectionPointsType.Points.ToString().CapitalizeFirstLetters(2);
                    PromptSelectionOptions PromptBlocSelectionOptions = new PromptSelectionOptions
                    {
                        MessageForAdding = $"{Message} [{PromptSelectionKeyWordString}]",
                        RejectObjectsOnLockedLayers = false,
                        SingleOnly = true
                    };
                    PromptBlocSelectionOptions.Keywords.Add(PromptSelectionKeyWordString);
                    PromptBlocSelectionOptions.KeywordInput += delegate (object sender, SelectionTextInputEventArgs e)
                    {
                        //Need to throw a exception here to exit the selection promt. Its catched to change the selection method to point and loop back
                        throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.OK, e.Input);
                    };

                    SelectionSet ObjectSelectionSet;
                    do
                    {
                        PromptSelectionResult PromptBlocSelectionResult = ed.GetSelection(PromptBlocSelectionOptions, SelectionEntitiesFilter);
                        if (PromptBlocSelectionResult.Status == PromptStatus.Cancel)
                        {
                            tr.Commit();
                            return null;
                        }
                        ObjectSelectionSet = PromptBlocSelectionResult.Value;

                    } while (ObjectSelectionSet == null || ObjectSelectionSet.Count != 1);
                    ObjectId SelectedBlocObjectId = ObjectSelectionSet.GetObjectIds().FirstOrDefault();
                    double? Altitude = GetAltitudeFromBloc(SelectedBlocObjectId);
                    if (Altitude == null)
                    {
                        ed.WriteMessage("Aucune côte détéctée\n"); 
                        tr.Commit();
                        continue;
                    }
                    Points CoteLocation = new Points((SelectedBlocObjectId.GetEntity() as BlockReference).Position);
                    tr.Commit();
                    return new CotePoints(CoteLocation, Altitude ?? 0);
                }
            } while (true);
        }

        public static CotePoints GetCotePoints(string Message, Points Origin)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            PromptPointOptions PromptPointOptions = new PromptPointOptions($"{Message} [{SelectionPointsType.Bloc}]", SelectionPointsType.Bloc.ToString());
            if (Origin != null)
            {
                PromptPointOptions.UseBasePoint = true;
                PromptPointOptions.BasePoint = new Point3d(Origin.SCG.X, Origin.SCG.Y, 0);
            }

            bool IsLooping;
            do
            {
                IsLooping = false;
                SelectionPointsType SelectionPointsType = GetSelectionPointsType();

                if (SelectionPointsType == SelectionPointsType.Points)
                {
                    PromptPointResult PromptPointResult = ed.GetPoint(PromptPointOptions);
                    if (PromptPointResult.Status == PromptStatus.Keyword)
                    {
                        SaveSelectionPointsType(SelectionPointsType.Bloc);
                        IsLooping = true;
                        continue;
                    }
                    if (PromptPointResult.Status == PromptStatus.OK)
                    {
                        Points CotePoint = Points.GetFromPromptPointResult(PromptPointResult);
                        double Altitude = GetCote(CotePoint);
                        return new CotePoints(CotePoint, Altitude);
                    }
                }
                if (SelectionPointsType == SelectionPointsType.Bloc)
                {
                    try
                    {
                        return GetBloc(Message);
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        Autodesk.AutoCAD.Runtime.Exception AutEx = ex as Autodesk.AutoCAD.Runtime.Exception;
                        if (AutEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK && ex.Message.IgnoreCaseEquals(SelectionPointsType.Points.ToString()))
                        {
                            SaveSelectionPointsType(SelectionPointsType.Points);
                            IsLooping = true;
                            continue;
                        }
                        else
                        {
                            throw ex;
                        }
                    }

                }
            } while (IsLooping);

            return null;
        }


    }
}
