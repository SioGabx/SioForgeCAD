using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Mist.DrawJigs
{
    public class GetEntityJig : IDisposable
    {
        private readonly Editor _editor;
        private bool _disposed;

        private Point3d _currentPoint = Point3d.Origin;

        // Entités originales
        public DBObjectCollection Entities { get; set; }
        public DBObjectCollection StaticEntities { get; set; }

        // Clones transients
        private readonly List<Entity> _dynamicTransients = new List<Entity>();
        private readonly List<Entity> _staticTransients = new List<Entity>();

        // Dernière matrice appliquée
        private Matrix3d _lastTransform = Matrix3d.Identity;

        public Func<Point3d, InputPointContext, GetEntityJig, bool> UpdateFunction;

        public Points BasePoint = Points.Null;

        public GetEntityJig()
        {
            _editor = Generic.GetEditor();
        }

        public (Entity Entity, PromptEntityResult Result) GetEntity(
            string message,
            Type[] allowedClassNames,
            bool allowObjectOnLockedLayer,
            params string[] keywords)
        {
            PromptEntityOptions peo = new PromptEntityOptions("\n" + message);
            peo.SetRejectMessage("\n" + message);
            if (allowedClassNames != null)
            {
                foreach (var c in allowedClassNames)
                {
                    peo.AddAllowedClass(c, true);
                }
            }

            if (keywords != null)
            {
                foreach (string k in keywords)
                {
                    peo.Keywords.Add(k);
                }

                peo.AppendKeywordsToMessage = true;
            }

            peo.AllowObjectOnLockedLayer = allowObjectOnLockedLayer;

            CreateTransients();

            _editor.PointMonitor += Editor_PointMonitor;

            try
            {
                PromptEntityResult res = _editor.GetEntity(peo);

                if (res.Status != PromptStatus.OK)
                {
                    return (null, res);
                }

                using (Transaction tr = _editor.Document.Database.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(res.ObjectId, OpenMode.ForRead);

                    tr.Commit();

                    return (ent, res);
                }
            }
            finally
            {
                _editor.PointMonitor -= Editor_PointMonitor;
                EraseTransients();
            }
        }

        public void RegenTransients()
        {
            EraseTransients();
            CreateTransients();
        }

        private void CreateTransients()
        {
            if (Entities != null)
            {
                foreach (Entity ent in Entities)
                {
                    Entity clone = (Entity)ent.Clone();

                    TransientManager.CurrentTransientManager.AddTransient(
                        clone,
                        TransientDrawingMode.DirectShortTerm,
                        128,
                        TransientManager.CurrentTransientManager.GetViewPortsNumbers());

                    _dynamicTransients.Add(clone);
                }
            }

            if (StaticEntities != null)
            {
                foreach (Entity ent in StaticEntities)
                {
                    Entity clone = (Entity)ent.Clone();

                    TransientManager.CurrentTransientManager.AddTransient(
                        clone,
                        TransientDrawingMode.DirectShortTerm,
                        127,
                        TransientManager.CurrentTransientManager.GetViewPortsNumbers());

                    _staticTransients.Add(clone);
                }
            }
        }

        private void Editor_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            _currentPoint = e.Context.RawPoint;

            UpdateFunction?.Invoke(_currentPoint, e.Context, this);

            Point3d basePt = BasePoint?.SCU ?? Point3d.Origin;

            Matrix3d newTransform =                Matrix3d.Displacement(basePt.GetVectorTo(_currentPoint));

            // Delta entre ancienne position et nouvelle
            Matrix3d delta =_lastTransform.Inverse() * newTransform;

            foreach (Entity ent in _dynamicTransients)
            {
                ent.TransformBy(delta);
                TransientManager.CurrentTransientManager.UpdateTransient(ent,TransientManager.CurrentTransientManager.GetViewPortsNumbers());
            }

            foreach (Entity ent2 in _staticTransients)
            {
                TransientManager.CurrentTransientManager.UpdateTransient(ent2,TransientManager.CurrentTransientManager.GetViewPortsNumbers());
            }

            _lastTransform = newTransform;
        }

        private void EraseTransients()
        {
            foreach (Entity ent in _dynamicTransients)
            {
                TransientManager.CurrentTransientManager.EraseTransient(
                    ent,
                    TransientManager.CurrentTransientManager.GetViewPortsNumbers());

                ent.Dispose();
            }

            foreach (Entity ent in _staticTransients)
            {
                TransientManager.CurrentTransientManager.EraseTransient(
                    ent,
                    TransientManager.CurrentTransientManager.GetViewPortsNumbers());

                ent.Dispose();
            }

            _dynamicTransients.Clear();
            _staticTransients.Clear();

            _lastTransform = Matrix3d.Identity;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _editor.PointMonitor -= Editor_PointMonitor;

                EraseTransients();

                Entities?.DeepDispose();
                StaticEntities?.DeepDispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}