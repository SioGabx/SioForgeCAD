using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Windows.Data;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class InsertionTransientPoints
    {
        private Func<Points, Dictionary<string, string>> UpdateFunction;
        private DBObjectCollection Entities;
        private List<Drawable> Drawable = new List<Drawable>();
        public InsertionTransientPoints(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction)
        {
            this.UpdateFunction = UpdateFunction;
            this.Entities = Entities;
        }

        public (Points Point, PromptPointResult PromptPointResult) GetInsertionPoint(string Message, params string[] KeyWords)
        {
            Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Point3d basePt = Points.Empty.SCG;
            Point3d curPt = basePt;
            CreateTransGraphics();

            PointMonitorEventHandler handler = delegate (object sender, PointMonitorEventArgs e)
            {
                Point3d pt = e.Context.ComputedPoint;
                UpdateTransGraphics(curPt, pt);
                curPt = pt;
            };
            ed.PointMonitor += handler;
            PromptPointOptions pointOptions = new PromptPointOptions(Message);
            foreach (string KeyWord in KeyWords)
            {
                pointOptions.Keywords.Add(KeyWord);
                pointOptions.AppendKeywordsToMessage = true;
            }
            PromptPointResult InsertionPromptPointResult = ed.GetPoint(pointOptions);
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


        private void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
        {
            Matrix3d mat = Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
            Dictionary<string, string> Values = UpdateFunction(new Points(moveToPt));
            for (int i = 0; i < Drawable.Count; i++)
            {

                Entity e = Drawable[i] as Entity;
                //if ((e is AttributeDefinition AttributeElement))
                //{
                //    string AttributeDefinitionName = AttributeElement.Prompt.ToUpperInvariant();
                //    if (Values != null && Values.ContainsKey(AttributeDefinitionName))
                //    {
                //        if (Values.TryGetValue(AttributeDefinitionName, out string AttributeDefinitionTargetValue))
                //        {
                //            AttributeElement.TextString = AttributeDefinitionTargetValue;
                //            AttributeElement.Tag = AttributeDefinitionTargetValue;
                //        }
                //    }
                //    e = AttributeElement;
                //}



                Autodesk.AutoCAD.ApplicationServices.Document doc = AcAp.DocumentManager.MdiActiveDocument;
                var ed = doc.Editor;
                var db = doc.Database;

                if (e is BlockReference blockReference)
                {
                    // Open the block reference for write
                    using (Autodesk.AutoCAD.DatabaseServices.TransactionManager tr = db.TransactionManager)
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
                                if (Values != null && Values.ContainsKey(AttributeDefinitionName))
                                {
                                    if (Values.TryGetValue(AttributeDefinitionName, out string AttributeDefinitionTargetValue))
                                    {
                                        AttributeElement.TextString = AttributeDefinitionTargetValue;
                                    }
                                }
                            }

                        }

                    }
                }



















                e.TransformBy(mat);
                TransientManager.CurrentTransientManager.UpdateTransient(Drawable[i], new IntegerCollection());
            }
        }

        private void ClearTransGraphics()
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
            Drawable.Clear();
            Entities.Clear();
        }

        private void CreateTransGraphics()
        {

            foreach (Entity drawable in this.Entities)
            {
                Entity drawableClone = drawable.Clone() as Entity;

                if (drawableClone is AttributeDefinition AttributeDefinition)
                {
                    AttributeDefinition.Prompt = AttributeDefinition.Tag.ToUpperInvariant();
                }
                drawableClone.ColorIndex = 252;
                Drawable.Add(drawableClone);
            }

            // Draw each one initially
            foreach (Drawable d in Drawable)
            {
                TransientManager.CurrentTransientManager.AddTransient(d, TransientDrawingMode.DirectShortTerm, 128, new IntegerCollection());
            }
        }

    }
}
