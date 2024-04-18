using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SioForgeCAD.Commun
{
    public class TransientBase : IDisposable
    {
        private Func<Points, Dictionary<string, string>> UpdateFunction { get; }
        private DBObjectCollection Entities { get; set; }
        private DBObjectCollection StaticEntities { get; set; }
        public List<Drawable> Drawable { get; }
        public List<Drawable> StaticDrawable { get; }

        public DBObjectCollection SetEntities
        {
            set
            {
                EraseTransients();
                DisposeDrawable();
                Entities = value;
                CreateTransGraphics();
            }
        }
        public DBObjectCollection SetStaticEntities
        {
            set
            {
                EraseTransients();
                DisposeStaticDrawable();
                StaticEntities = value;
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

        public DBObjectCollection GetStaticEntities
        {
            get
            {
                return StaticEntities ?? new DBObjectCollection();
            }
        }

        public TransientBase(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction)
        {
            this.UpdateFunction = UpdateFunction;
            this.Entities = Entities;
            this.Drawable = new List<Drawable>();
            this.StaticDrawable = new List<Drawable>();
        }

        public virtual void UpdateTransGraphics(Point3d curPt, Point3d moveToPt)
        {
            Document doc = AcAp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            Dictionary<string, string> Values;
            if (UpdateFunction != null)
            {
                Values = UpdateFunction(new Points(moveToPt));
            }
            else
            {
                Values = new Dictionary<string, string>();
            }
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
                                AttributeElement.Color = GetTransGraphicsColor(AttributeElement, false);
                                if (Values?.ContainsKey(AttributeDefinitionName) == true)
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

                TransformEntities(e, curPt, moveToPt);
                RedrawTransEntities(Drawable[i]);
            }
        }

        public virtual void TransformEntities(Entity entity, Point3d currentPoint, Point3d destinationPoint)
        {
            return;
        }

        public static void RedrawTransEntities(Drawable entity)
        {
            TransientManager.CurrentTransientManager.UpdateTransient(entity, new IntegerCollection());
        }

        public virtual void EraseTransients()
        {
            TransientManager.CurrentTransientManager.EraseTransients(
             TransientDrawingMode.DirectShortTerm,
             128, new IntegerCollection()
           );
        }

        public void DisposeDrawable()
        {
            if (Drawable != null)
            {
                foreach (Drawable Entity in Drawable)
                {
                    Entity.Dispose();
                }
                Drawable?.Clear();
            }
        }

        public void DisposeStaticDrawable()
        {
            if (StaticDrawable != null)
            {
                foreach (Drawable Entity in StaticDrawable)
                {
                    Entity.Dispose();
                }
                StaticDrawable?.Clear();
            }
        }

        public void DisposeEntities()
        {
            if (Entities != null)
            {
                foreach (DBObject item in Entities)
                {
                    item.Dispose();
                }
                Entities?.Clear();
            }
        }

        public void DisposeStaticEntities()
        {
            if (StaticEntities != null)
            {
                foreach (DBObject item in StaticEntities)
                {
                    item.Dispose();
                }
                StaticEntities?.Clear();
            }
        }

        public virtual void ClearTransGraphics()
        {
            // Clear the transient graphics for our drawables
            EraseTransients();

            // Dispose of them and clear the list
            DisposeDrawable();
            DisposeStaticDrawable();
        }

        public void CreateTransGraphics()
        {
            if (Entities != null)
            {
                foreach (Entity drawable in Entities)
                {
                    var DrawableClone = CreateTransGraphicsEntity(drawable, 0, false);
                    Drawable.Add(DrawableClone);
                }
            }
            if (StaticEntities != null)
            {
                foreach (Entity drawable in StaticEntities)
                {
                    var DrawableClone = CreateTransGraphicsEntity(drawable, 1, true);
                    StaticDrawable.Add(DrawableClone);
                }
            }
        }

        public virtual Autodesk.AutoCAD.Colors.Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
        {
            return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, (short)Settings.TransientPrimaryColorIndex);
        }

        public virtual Autodesk.AutoCAD.Colors.Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
        {
            if (IsStaticDrawable)
            {
                const byte Alpha = (255 * (100 - 50) / 100);
                Drawable.Transparency = new Autodesk.AutoCAD.Colors.Transparency(Alpha);
            }
            return Drawable.Transparency;
        }

        public Entity CreateTransGraphicsEntity(Entity EntityToMakeDrawable, int index, bool IsStaticDrawable)
        {
            Entity drawableClone = EntityToMakeDrawable.Clone() as Entity;
            drawableClone.Color = GetTransGraphicsColor(drawableClone, IsStaticDrawable);
            drawableClone.Transparency = GetTransGraphicsTransparency(drawableClone, IsStaticDrawable);
            TransientManager.CurrentTransientManager.AddTransient(drawableClone, TransientDrawingMode.DirectShortTerm, 128 - index, TransientManager.CurrentTransientManager.GetViewPortsNumbers());
            return drawableClone;
        }

        public void Dispose()
        {
            DisposeEntities();
            DisposeStaticEntities();

            DisposeDrawable();
            DisposeStaticDrawable();

            ClearTransGraphics();

            GC.SuppressFinalize(this);
        }
    }

    public class GetPointTransient : TransientBase
    {
        public GetPointTransient(DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
        {
        }

        public (Points Point, PromptPointResult PromptPointResult) GetPoint(object Message, Points OriginPoint, params string[] KeyWords)
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

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
            PromptPointOptions pointOptions = new PromptPointOptions("\n" + Message);
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
            bool IsNotValid = true;
            PromptPointResult InsertionPromptPointResult = null;
            while (IsNotValid)
            {
                InsertionPromptPointResult = ed.GetPoint(SetPromptPointOptions(pointOptions));
                if (InsertionPromptPointResult.Status == PromptStatus.OK)
                {
                    IsNotValid = !IsValidPoint(InsertionPromptPointResult);
                }
                else
                {
                    break;
                }
            }

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

        public virtual PromptPointOptions SetPromptPointOptions(PromptPointOptions PromptPointOptions)
        {
            return PromptPointOptions;
        }

        public virtual bool IsValidPoint(PromptPointResult pointResult)
        {
            return true;
        }

        public override void TransformEntities(Entity entity, Point3d currentPoint, Point3d destinationPoint)
        {
            Matrix3d mat = Matrix3d.Displacement(currentPoint.GetVectorTo(destinationPoint));
            entity.TransformBy(mat);
        }
    }

    public class GetAngleTransient : TransientBase
    {
        private readonly Points OriginPoint;
        private readonly Points SecondPoint;
        public GetAngleTransient(Points OriginPoint, Points SecondPoint, DBObjectCollection Entities, Func<Points, Dictionary<string, string>> UpdateFunction) : base(Entities, UpdateFunction)
        {
            this.OriginPoint = OriginPoint;
            this.SecondPoint = SecondPoint;
        }

        public override Autodesk.AutoCAD.Colors.Color GetTransGraphicsColor(Entity Drawable, bool IsStaticDrawable)
        {
            if (IsStaticDrawable)
            {
                return base.GetTransGraphicsColor(Drawable, IsStaticDrawable);
            }
            else
            {
                return Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByColor, (short)Drawable.ColorIndex);
            }
        }

        public override Autodesk.AutoCAD.Colors.Transparency GetTransGraphicsTransparency(Entity Drawable, bool IsStaticDrawable)
        {
            if (Drawable is Hatch || IsStaticDrawable)
            {
                //50% alpha
                const byte AlphaPourcentage = 50;
                const byte Alpha = (255 * (100 - AlphaPourcentage) / 100);
                Drawable.Transparency = new Autodesk.AutoCAD.Colors.Transparency(Alpha);
            }
            return Drawable.Transparency;
        }

        public (double Angle, PromptDoubleResult PromptAngleResult) GetAngle(string Message, params string[] KeyWords)
        {
            var ed = Generic.GetEditor();
            var db = Generic.GetDatabase();

            Point3d basePt = OriginPoint.SCG;
            Point3d curPt = SecondPoint.SCG;
            CreateTransGraphics();

            void handler(object sender, PointMonitorEventArgs e)
            {
                Point3d pt = e.Context.ComputedPoint;
                UpdateTransGraphics(curPt, pt);
                curPt = pt;
            }

            ed.PointMonitor += handler;
            PromptAngleOptions angleOptions = new PromptAngleOptions(Message);
            foreach (string KeyWord in KeyWords)
            {
                if (string.IsNullOrWhiteSpace(KeyWord)) { continue; }
                angleOptions.Keywords.Add(KeyWord);
                angleOptions.AppendKeywordsToMessage = true;
                angleOptions.AllowArbitraryInput = true;
            }

            if (OriginPoint != Points.Null)
            {
                angleOptions.UseBasePoint = true;
                angleOptions.BasePoint = OriginPoint.SCU;
            }
            PromptDoubleResult AnglePromptPointResult = ed.GetAngle(angleOptions);
            ed.PointMonitor -= handler;
            ClearTransGraphics();
            if (AnglePromptPointResult.Status == PromptStatus.OK)
            {
                return (AnglePromptPointResult.Value, AnglePromptPointResult);
            }
            else
            {
                return (0, AnglePromptPointResult);
            }
        }

        public override void TransformEntities(Entity entity, Point3d currentPoint, Point3d destinationPoint)
        {
            double currentRotation = OriginPoint.SCG.GetAngleWith(currentPoint);

            // Nouvel angle avec le point de base
            double newRotation = OriginPoint.SCG.GetAngleWith(destinationPoint);

            // Différence d'angle entre l'angle actuel et le nouvel angle
            double rotationAngle = newRotation - currentRotation;

            Debug.WriteLine($"Current Rotation: {currentRotation}");
            Debug.WriteLine($"New Rotation: {newRotation}");

            // Appliquer la rotation autour du point de base
            Matrix3d rotationMatrix = Matrix3d.Rotation(rotationAngle, Vector3d.ZAxis, OriginPoint.SCG);
            entity.TransformBy(rotationMatrix);
        }
    }
}
