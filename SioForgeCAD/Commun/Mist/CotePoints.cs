using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SioForgeCAD.Commun
{
    public class CotePoints
    {
        public static readonly CotePoints Null = null;
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

        public static string FormatAltitude(double? Altitude, int NumberOfDecimal = 2)
        {
            if (Altitude == null)
            {
                Altitude = 0;
            }
            return Altitude?.ToString($"#.{new string('0', NumberOfDecimal)}");
        }

        private static void SaveSelectionPointsType(SelectionPointsType SelectionPointsType)
        {
            Properties.Settings.Default.SelectionPointsType = SelectionPointsType.ToString();
            Properties.Settings.Default.Save();
        }

        public static bool NullPointExit(CotePoints cotePoints)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
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
            var db = Generic.GetDatabase();
            var ed = Generic.GetEditor();

            //Write a point where the altitude is asked
            DBPoint PointDrawingEntity = new DBPoint(Origin.SCG);
            ObjectId PointDrawingEntityObjectId;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable acBlkTbl = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord acBlkTblRec = Generic.GetCurrentSpaceBlockTableRecord(tr);
                acBlkTblRec.AppendEntity(PointDrawingEntity);
                tr.AddNewlyCreatedDBObject(PointDrawingEntity, true);
                PointDrawingEntityObjectId = PointDrawingEntity.ObjectId;
                tr.Commit();
            }
            PromptDoubleOptions PromptDoubleAltitudeOptions = new PromptDoubleOptions("Saississez la cote\n")
            {
                AllowNegative = false,
                AllowNone = false
            };
            PromptDoubleResult PromptDoubleAltitudeResult = ed.GetDouble(PromptDoubleAltitudeOptions);

            //remove the point where the altitude is asked
            if (!PointDrawingEntityObjectId.IsErased)
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

        public static double? GetAltitudeFromBloc(ObjectId BlocObjectId)
        {
            var db = Generic.GetDatabase();
            TransactionManager tr = db.TransactionManager;
            DBObject BlocObject = tr.GetObject(BlocObjectId, OpenMode.ForRead);
            if (BlocObject is BlockReference blkRef)
            {
                return GetAltitudeFromBloc(blkRef);
            }
            return null;
        }

        public static double? GetAltitudeFromBloc(BlockReference blkRef)
        {
            var db = Generic.GetDatabase();
            TransactionManager tr = db.TransactionManager;

            foreach (ObjectId AttributeObjectId in blkRef.AttributeCollection)
            {
                AttributeReference Attribute = (AttributeReference)tr.GetObject(AttributeObjectId, OpenMode.ForRead);
                if (Attribute.TextString.Contains("."))
                {
                    bool IsDouble = double.TryParse(Attribute.TextString.Trim(), out double Altimetrie);
                    if (IsDouble)
                    {
                        Generic.WriteMessage($"Cote sélectionnée : {FormatAltitude(Altimetrie)}");
                        blkRef.RegisterHighlight();
                        return Altimetrie;
                    }
                    else
                    {
                        double? ExtractedAltitude = ExtractDoubleInStringFromPoint(Attribute.TextString.Trim());
                        if (ExtractedAltitude.HasValue)
                        {
                            blkRef.RegisterHighlight();
                            return ExtractedAltitude;
                        }
                        else
                            continue;//If no value, continue search
                    }
                }
            }
            return null;
        }

        public static double? ExtractDoubleInStringFromPoint(string OriginalString)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            if (OriginalString.Contains("%"))
            {
                Generic.WriteMessage("Par mesure de sécurité, les textes contenant des % ne peuvent être convertis en cote.");
                return null;
            }

            int[] StringPointPosition = OriginalString.AllIndexesOf(".").ToArray();
            string NumberValueBeforePoint = "";
            string NumberValueAfterPoint = "";

            foreach (int index in StringPointPosition)
            {
                int n = index;
                while (n > 0 && char.IsDigit(OriginalString[n - 1]))
                {
                    NumberValueBeforePoint = OriginalString[n - 1].ToString() + NumberValueBeforePoint;
                    n--;
                }

                n = index;
                while (OriginalString.Length > n + 1 && char.IsDigit(OriginalString[n + 1]))
                {
                    NumberValueAfterPoint += OriginalString[n + 1].ToString();
                    n++;
                }

                if (string.IsNullOrWhiteSpace(NumberValueBeforePoint) || string.IsNullOrWhiteSpace(NumberValueAfterPoint))
                {
                    //Not sure if this is a cote
                    return null;
                }

                string FinalNumberString = $"{NumberValueBeforePoint}.{NumberValueAfterPoint}";
                bool IsValidNumber = double.TryParse(FinalNumberString, out double FinalNumberDouble);
                if (IsValidNumber)
                {
                    ed.WriteMessage($"Côte détéctée : {FinalNumberString}\n");
                    return FinalNumberDouble;
                }
                else
                {
                    //No number found
                    return null;
                }
            }
            //Foreach return 0 element
            return null;
        }

        public static CotePoints GetBlockInXref(string Message, Point3d? NonInterractivePickedPoint, out PromptStatus PromptStatus)
        {
            var ed = Generic.GetEditor();
            BlockReference blkRef = null;
            List<ObjectId> XrefObjectId;

            (ObjectId[] XrefObjectId, ObjectId SelectedObjectId, PromptStatus PromptStatus) XrefSelection = SelectInXref.Select(Message, NonInterractivePickedPoint);
            XrefObjectId = XrefSelection.XrefObjectId.ToList();
            PromptStatus = XrefSelection.PromptStatus;
            if (XrefSelection.PromptStatus != PromptStatus.OK)
            {
                return Null;
            }
            if (XrefSelection.SelectedObjectId == ObjectId.Null)
            {
                return Null;
            }

            HightLighter.UnhighlightAll();
            XrefSelection.SelectedObjectId.RegisterHighlight();
            DBObject XrefObject = XrefSelection.SelectedObjectId.GetDBObject();

            if (XrefObject is AttributeReference blkChildAttribute)
            {
                var DbObj = blkChildAttribute.OwnerId.GetDBObject();
                blkRef = DbObj as BlockReference;
            }
            else if (XrefObject is BlockReference XrefObjectBlkRef)
            {
                blkRef = XrefObjectBlkRef;
            }
            else
            {
                foreach (ObjectId objId in XrefSelection.XrefObjectId)
                {
                    XrefObjectId.Remove(objId);
                    XrefObject = objId.GetDBObject();

                    if (XrefObject is BlockReference ParentBlkRef && !ParentBlkRef.IsXref())
                    {
                        blkRef = XrefObject as BlockReference;
                        break;
                    }
                }
            }

            if (blkRef is null)
            {
                return Null;
            }

            double? Altimetrie = GetAltitudeFromBloc(blkRef);
            Points BlockPosition = SelectInXref.TransformPointInXrefsToCurrent(blkRef.Position, XrefObjectId.ToArray());

            if (Altimetrie == null)
            {
                Altimetrie = blkRef.Position.Z;
                if (Altimetrie == 0)
                {
                    return new CotePoints(BlockPosition, 0);
                }
                PromptKeywordOptions options = new PromptKeywordOptions($"Aucune cote n'a été trouvée pour ce bloc, cependant une altitude Z a été définie à {FormatAltitude(Altimetrie)}. Voulez-vous utiliser cette valeur ?\n");
                options.Keywords.Add("OUI");
                options.Keywords.Add("NON");
                options.AllowNone = true;
                PromptResult result = ed.GetKeywords(options);
                if (result.Status == PromptStatus.OK && result.StringResult != "OUI")
                {
                    return new CotePoints(BlockPosition, 0);
                }
            }

            return new CotePoints(BlockPosition, Altimetrie ?? 0);
        }

        private static CotePoints GetBloc(string Message)
        {
            var db = Generic.GetDatabase();
            var ed = Generic.GetEditor();

            while (true)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    //list of available SelectionFilter : https://help.autodesk.com/view/ACD/2018/ENU/?guid=GUID-7D07C886-FD1D-4A0C-A7AB-B4D21F18E484
                    TypedValue[] EntitiesGroupCodesList = new TypedValue[1] { new TypedValue((int)DxfCode.Start, "INSERT") };
                    SelectionFilter SelectionEntitiesFilter = new SelectionFilter(EntitiesGroupCodesList);
                    string PromptSelectionKeyWordString = nameof(SelectionPointsType.Points).CapitalizeFirstLetters(2);
                    PromptEntityOptions PromptBlocSelectionOptions = new PromptEntityOptions($"{Message} [{PromptSelectionKeyWordString}]")
                    {
                        AllowNone = false,
                        AllowObjectOnLockedLayer = true,
                    };
                    PromptBlocSelectionOptions.Keywords.Add(PromptSelectionKeyWordString);
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
                        CotePoints CotePoint = GetBlockInXref(string.Empty, PromptBlocSelectionResult.PickedPoint, out PromptStatus _);
                        bool IsCotePointNotNull = CotePoint != Null;
                        bool IsAltimetrieDefined = (CotePoint?.Altitude ?? 0) != 0;
                        if (IsCotePointNotNull && IsAltimetrieDefined)
                        {
                            var AskKeepXREFCoteValuesOptions = new PromptKeywordOptions($"La cote {FormatAltitude(CotePoint.Altitude)} a été trouvée dans une XREF. Voulez-vous utiliser cette valeur ?\n");
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
                        Generic.WriteMessage("Aucune côte détéctée");
                        tr.Commit();
                        continue;
                    }

                    tr.Commit();
                    return new CotePoints(CoteLocation, Altitude ?? 0);
                }
            }
        }

        public static CotePoints GetCotePoints(string Message, Points Origin)
        {
            var ed = Generic.GetEditor();
            PromptPointOptions PromptPointOptions = new PromptPointOptions($"{Message} [{SelectionPointsType.Bloc}]\n", nameof(SelectionPointsType.Bloc));

            if (Origin != null)
            {
                PromptPointOptions.UseBasePoint = true;
                PromptPointOptions.BasePoint = Origin.SCG.Flatten();
                PromptPointOptions.UseDashedLine = true;
                PromptPointOptions.AppendKeywordsToMessage = true;
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
                        Autodesk.AutoCAD.Runtime.Exception AutEx = ex;
                        if (AutEx.ErrorStatus == Autodesk.AutoCAD.Runtime.ErrorStatus.OK && ex.Message.IgnoreCaseEquals(nameof(SelectionPointsType.Points)))
                        {
                            SaveSelectionPointsType(SelectionPointsType.Points);
                            IsLooping = true;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            } while (IsLooping);

            return Null;
        }
    }
}
