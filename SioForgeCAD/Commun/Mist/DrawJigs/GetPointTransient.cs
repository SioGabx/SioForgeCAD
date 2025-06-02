using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

namespace SioForgeCAD.Commun.Mist.DrawJigs
{
    public class GetPointTransient : DrawJig
    {
        private Point3d _currentPoint = Point3d.Origin;
        private Point3d _basePoint = Point3d.Origin;
        private bool _useBasePoint = false;
        private readonly DBObjectCollection _entities;
        private readonly Func<Points, Dictionary<string, string>> _updateFunction;
        private readonly Editor _ed;
        private Dictionary<string, string> _keywordMap;
        private string _message;

        public GetPointTransient(DBObjectCollection entities, Func<Points, Dictionary<string, string>> updateFunction)
        {
            _entities = entities;
            _updateFunction = updateFunction;
            _ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor;
        }

        public (Points Point, PromptResult PromptPointResult) Run(string message, Points originPoint, params string[] keywords)
        {
            _message = message;
            _currentPoint = originPoint != Points.Null ? originPoint.SCU : Point3d.Origin;

            if (originPoint != Points.Null)
            {
                _useBasePoint = true;
                _basePoint = originPoint.SCU;
            }

            PromptResult result = _ed.Drag(this);

            if (result.Status == PromptStatus.OK)
            {

                return (new Points(_currentPoint), result);
            }

            return (null, result);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            PromptPointOptions ppo = new PromptPointOptions("\n" + _message);
            if (_useBasePoint)
            {
                ppo.UseBasePoint = true;
                ppo.BasePoint = _basePoint;
            }

            if (_keywordMap != null)
            {
                foreach (var kv in _keywordMap)
                {
                    ppo.Keywords.Add(kv.Key);
                }

                ppo.AppendKeywordsToMessage = true;
                ppo.AllowArbitraryInput = true;
            }

            PromptPointResult res = prompts.AcquirePoint(ppo);
            if (res.Status != PromptStatus.OK)
                return SamplerStatus.Cancel;

            if (res.Value.IsEqualTo(_currentPoint))
                return SamplerStatus.NoChange;

            _currentPoint = res.Value;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            // Appelle la fonction de mise à jour si elle existe
            if (_updateFunction != null)
            {
                var values = _updateFunction(new Points(_currentPoint));
                // Tu peux t'en servir pour tracer ou manipuler tes entités
            }

            if (_entities != null)
            {
                foreach (Entity ent in _entities)
                {
                    Entity clone = ent.Clone() as Entity;
                    if (clone != null)
                    {
                        clone.TransformBy(Matrix3d.Displacement(_basePoint.GetVectorTo(_currentPoint)));
                        draw.Geometry.Draw(clone);
                        clone.Dispose();
                    }
                }
            }

            return true;
        }
    }
}
