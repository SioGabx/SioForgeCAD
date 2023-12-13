using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using System;
using System.Collections.Generic;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class InsertionTransientPoints
    {
        private Func<Points, Dictionary<string, string>> UpdateFunction { get; }
        private DBObjectCollection Entities { get; set; }
        public List<Drawable> Drawable { get; }

        public DBObjectCollection SetEntities
        {
            set
            {
                ClearTransGraphics();
                Entities = value;
                CreateTransGraphics();
            }
        }

        public DBObjectCollection GetEntities
        {
            get
            {
                return Entities ?? new DBObjectCollection();
            }
        }

        public InsertionTransientPoints(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction)
        {
            this.UpdateFunction = UpdateFunction;
            this.Entities = Entities;
            this.Drawable = new List<Drawable>();
        }

        public (Points Point, PromptPointResult PromptPointResult) GetInsertionPoint(string Message, Points OriginPoint, params string[] KeyWords)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Point3d basePt = Points.Empty.SCG;
            Point3d curPt = basePt;
            CreateTransGraphics();

            void handler(object sender, PointMonitorEventArgs e)
            {
                Point3d pt = e.Context.ComputedPoint;
                UpdateTransGraphics(curPt, pt);
                curPt = pt;
            }

            ed.PointMonitor += handler;
            PromptPointOptions pointOptions = new PromptPointOptions(Message);
            foreach (string KeyWord in KeyWords)
            {
                if (string.IsNullOrWhiteSpace(KeyWord)) { continue; }
                pointOptions.Keywords.Add(KeyWord);
                pointOptions.AppendKeywordsToMessage = true;
                pointOptions.AllowArbitraryInput = true;
            }

            if (OriginPoint != Points.Null)
            {
                pointOptions.UseBasePoint = true;
                pointOptions.BasePoint = OriginPoint.SCU;
            }
            PromptPointResult InsertionPromptPointResult = ed.GetPoint(pointOptions);
            ed.PointMonitor -= handler;
            var InsertionPointResult = Points.GetFromPromptPointResult(InsertionPromptPointResult);
            ClearTransGraphics();
            if (InsertionPromptPointResult.Status == PromptStatus.OK)
            {
                return (InsertionPointResult, InsertionPromptPointResult);
            }
            else
            {
                return (null, InsertionPromptPointResult);
            }
        }


        public virtual void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Dictionary<string, string> Values = UpdateFunction(new Points(moveToPt));
            for (int i = 0; i < Drawable.Count; i++)
            {
                Entity e = Drawable[i] as Entity;
                if (e is BlockReference blockReference)
                {
                    // Open the block reference for write
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        if (!blockReference.IsWriteEnabled)
                        {
                            blockReference.UpgradeOpen();
                        }
                        // Loop through the attributes of the block reference

                        foreach (var attId in blockReference.AttributeCollection)
                        {
                            if (attId is AttributeReference AttributeElement)
                            {
                                string AttributeDefinitionName = AttributeElement.Tag.ToUpperInvariant();
                                AttributeElement.ColorIndex = GetTransGraphicsColor(AttributeElement);
                                if (Values != null && Values.ContainsKey(AttributeDefinitionName))
                                {
                                    if (Values.TryGetValue(AttributeDefinitionName, out string AttributeDefinitionTargetValue))
                                    {
                                        AttributeElement.TextString = AttributeDefinitionTargetValue;
                                    }
                                }
                            }
                        }
                        tr.Commit();
                    }
                }

                Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
                TransformEntities(e, mat);
                RedrawTransEntities(Drawable[i]);
            }
        }

        public virtual void TransformEntities(Entity entity, Matrix3d mat)
        {
            entity.TransformBy(mat);
        }

        public void RedrawTransEntities(Drawable entity)
        {
            TransientManager.CurrentTransientManager.UpdateTransient(entity, new IntegerCollection());
        }


        public virtual void ClearTransGraphics()
        {
            // Clear the transient graphics for our drawables
            TransientManager.CurrentTransientManager.EraseTransients(
              TransientDrawingMode.DirectShortTerm,
              128, new IntegerCollection()
            );
            // Dispose of them and clear the list
            foreach (Drawable Entity in Drawable)
            {
                Entity.Dispose();
            }
            Drawable?.Clear();
            Entities?.Clear();
        }

        private void CreateTransGraphics()
        {
            if (Entities == null)
            {
                return;
            }
            foreach (Entity drawable in Entities)
            {
                CreateTransGraphicsEntity(drawable);
            }
        }

        public virtual int GetTransGraphicsColor(Entity Drawable)
        {
            return Settings.TransientPrimaryColorIndex;
        }

        public Entity CreateTransGraphicsEntity(Entity EntityToMakeDrawable)
        {
            Entity drawableClone = EntityToMakeDrawable.Clone() as Entity;
            drawableClone.ColorIndex = GetTransGraphicsColor(drawableClone);
            TransientManager.CurrentTransientManager.AddTransient(drawableClone, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
            Drawable.Add(drawableClone);
            return drawableClone;
        }
    }
}
