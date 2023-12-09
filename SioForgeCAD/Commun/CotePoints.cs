using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class CotePoints
    {
        public static CotePoints Null = null;
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

        public static string FormatAltitude(double? Altitude)
        {
            if (Altitude == null)
            {
                Altitude = 0;
            }
            return Altitude?.ToString("#.00");
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
            PromptDoubleOptions PromptDoubleAltitudeOptions = new PromptDoubleOptions("\n" + "Saississez la cote")
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

        public static double? GetAltitudeFromBloc(Autodesk.AutoCAD.DatabaseServices.ObjectId BlocObjectId)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            TransactionManager tr = db.TransactionManager;
            DBObject BlocObject = tr.GetObject(BlocObjectId, OpenMode.ForRead);
            return GetAltitudeFromBloc(BlocObject);
        }

        public static double? GetAltitudeFromBloc(DBObject BlocObject)
        {
            var doc = AcAp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            TransactionManager tr = db.TransactionManager;
            if (!(BlocObject is BlockReference blkRef))
            {
                return null;
            }

            foreach (ObjectId AttributeObjectId in blkRef.AttributeCollection)
            {
                AttributeReference Attribute = (AttributeReference)tr.GetObject(AttributeObjectId, OpenMode.ForRead);
                if (Attribute.TextString.Contains("."))
                {
                    bool IsDouble = double.TryParse(Attribute.TextString.Trim(), out double Altimetrie);
                    if (IsDouble)
                    {
                        ed.WriteMessage($"Cote sélectionnée : {FormatAltitude(Altimetrie)}\n");
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

        public static CotePoints GetBlockInXref(string Message, Point3d? NonInterractivePickedPoint)
        {
            var ed = Generic.GetEditor();
            (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = Commun.SelectInXref.Select(Message, NonInterractivePickedPoint);
            List<ObjectId> XrefObjectId = XrefSelection.XrefObjectId.ToList();
            if (XrefSelection.PromptStatus != PromptStatus.OK)
            {
                return CotePoints.Null;
            }
            if (XrefSelection.SelectedObjectId == ObjectId.Null)
            {
                return CotePoints.Null;
            }

            HightLighter.UnhighlightAll();
            XrefSelection.SelectedObjectId.RegisterHighlight();
            DBObject XrefObject = XrefSelection.SelectedObjectId.GetDBObject();
            BlockReference blkRef = null;

            if (XrefObject is AttributeReference blkChildAttribute)
            {
                var DbObj = blkChildAttribute.OwnerId.GetDBObject();
                blkRef = DbObj as BlockReference;
            }
            else if (XrefObject is BlockReference)
            {
                blkRef = XrefObject as BlockReference;
            }
            else
            {
                foreach (ObjectId objId in XrefSelection.XrefObjectId)
                {
                    XrefObjectId.Remove(objId);
                    XrefObject = objId.GetDBObject();

                    if (XrefObject is BlockReference ParentBlkRef)
                    {
                        if (!ParentBlkRef.IsXref())
                        {
                            blkRef = XrefObject as BlockReference;
                            break;
                        }
                    }
                }
            }
            if (blkRef is null)
            {
                return CotePoints.Null;
            }
            double? Altimetrie = CotePoints.GetAltitudeFromBloc(blkRef);
            if (Altimetrie == null)
            {
                Altimetrie = blkRef.Position.Z;
                if (Altimetrie == 0)
                {
                    return CotePoints.Null;
                }
                PromptKeywordOptions options = new PromptKeywordOptions($"Aucune cote n'a été trouvée pour ce bloc, cependant une altitude Z a été définie à {CotePoints.FormatAltitude(Altimetrie)}. Voulez-vous utiliser cette valeur ?");
                options.Keywords.Add("OUI");
                options.Keywords.Add("NON");
                options.AllowNone = true;
                PromptResult result = ed.GetKeywords(options);
                if (result.Status == PromptStatus.OK && result.StringResult != "OUI")
                {
                    return CotePoints.Null;
                }
            }

            Points BlockPosition = SelectInXref.TransformPointInXrefsToCurrent(blkRef.Position, XrefObjectId.ToArray());
            return new CotePoints(BlockPosition, Altimetrie ?? 0);
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
                    PromptEntityOptions PromptBlocSelectionOptions = new PromptEntityOptions($"{Message} [{PromptSelectionKeyWordString}]")
                    {
                        AllowNone = false
                    };
                    PromptBlocSelectionOptions.Keywords.Add(PromptSelectionKeyWordString);
                    //PromptBlocSelectionOptions.KeywordInput += delegate (object sender, SelectionTextInputEventArgs e)
                    //{
                    //    //Need to throw a exception here to exit the selection promt. Its catched to change the selection method to point and loop back
                    //    throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.OK, e.Input);
                    //};


                    Entity SelectedObject;
                    PromptEntityResult PromptBlocSelectionResult;
                    do
                    {
                        PromptBlocSelectionResult = ed.GetEntity(PromptBlocSelectionOptions);

                        if (PromptBlocSelectionResult.Status == PromptStatus.Cancel)
                        {
                            tr.Commit();
                            return null;
                        }
                        if (PromptBlocSelectionResult.Status == PromptStatus.Keyword)
                        {
                            tr.Commit();
                            throw new Autodesk.AutoCAD.Runtime.Exception(Autodesk.AutoCAD.Runtime.ErrorStatus.OK, PromptBlocSelectionResult.StringResult);
                        }
                        SelectedObject = PromptBlocSelectionResult.ObjectId.GetEntity();

                    } while (!(SelectedObject is BlockReference));
                    BlockReference blockReference = SelectedObject as BlockReference;
                    ObjectId SelectedBlocObjectId = blockReference.ObjectId;
                    double? Altitude = GetAltitudeFromBloc(SelectedBlocObjectId);
                    Points CoteLocation = new Points(blockReference.Position);

                    if (blockReference.IsXref())
                    {
                        CotePoints CotePoint = GetBlockInXref(string.Empty, PromptBlocSelectionResult.PickedPoint);
                        if (CotePoint != CotePoints.Null)
                        {
                            var AskKeepXREFCoteValuesOptions = new PromptKeywordOptions($"La cote {FormatAltitude(CotePoint.Altitude)} a été trouvée dans une XREF. Voulez-vous utiliser cette valeur ?");
                            AskKeepXREFCoteValuesOptions.Keywords.Add("Oui");
                            AskKeepXREFCoteValuesOptions.Keywords.Add("Non");
                            AskKeepXREFCoteValuesOptions.Keywords.Default = "Oui";
                            AskKeepXREFCoteValuesOptions.AllowNone = true;
                            var AskKeepXREFCoteValues = ed.GetKeywords(AskKeepXREFCoteValuesOptions);
                            if ((AskKeepXREFCoteValues.Status == PromptStatus.OK && AskKeepXREFCoteValues.StringResult == "Oui") || AskKeepXREFCoteValues.Status == PromptStatus.None)
                            {
                                Altitude = CotePoint.Altitude;
                                CoteLocation = CotePoint.Points;
                            }
                        }
                    }

                    if (Altitude == null)
                    {
                        ed.WriteMessage("Aucune côte détéctée\n");
                        tr.Commit();
                        continue;
                    }
                    
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
                PromptPointOptions.BasePoint = Origin.SCG.Flatten();
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
