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


        private string[] _keywords;
        private string _message;


        public DBObjectCollection Entities { get; set; }
        public DBObjectCollection StaticEntities { get; set; }

        public Func<Points, DrawJig, Dictionary<string, string>> UpdateFunction;
        public Points BasePoint = Points.Null;


        public (Points Point, PromptResult PromptPointResult) GetPoint(string message, params string[] keywords)
        {
            _message = message;
            _keywords = keywords;

            PromptResult result = Generic.GetEditor().Drag(this);

            if (result.Status == PromptStatus.OK)
            {

                return (new Points(_currentPoint), result);
            }

            return (Points.Null, result);
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions ppo = new JigPromptPointOptions("\n" + _message);
            if (BasePoint != Points.Null)
            {
                ppo.UseBasePoint = true;
                ppo.BasePoint = BasePoint.SCU;
            }

            ppo.UserInputControls =
                UserInputControls.GovernedByOrthoMode |
                UserInputControls.NullResponseAccepted |
                UserInputControls.GovernedByUCSDetect |
                UserInputControls.AcceptMouseUpAsPoint;


            if (_keywords != null)
            {
                foreach (var kv in _keywords)
                {
                    ppo.Keywords.Add(kv);
                }

                ppo.AppendKeywordsToMessage = true;
                ppo.UserInputControls = ppo.UserInputControls |
                UserInputControls.AcceptOtherInputString |
                UserInputControls.NullResponseAccepted;
            }


            PromptPointResult res = prompts.AcquirePoint(ppo);
            if (res.Status != PromptStatus.OK)
            {
                return SamplerStatus.Cancel;
            }

            if (res.Value.IsEqualTo(_currentPoint))
            {
                return SamplerStatus.NoChange;
            }

            _currentPoint = res.Value;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (UpdateFunction != null)
            {
                UpdateFunction(new Points(_currentPoint), this);
            }

            if (Entities != null)
            {
                foreach (Entity ent in Entities)
                {
                    Entity clone = ent.Clone() as Entity;
                    if (clone != null)
                    {
                        clone.TransformBy(Matrix3d.Displacement(BasePoint.SCU.GetVectorTo(_currentPoint)));
                        draw.Geometry.Draw(clone);
                        clone.Dispose();
                    }
                }
            }

            if (StaticEntities != null)
            {
                foreach (Entity ent in StaticEntities)
                {
                    Entity clone = ent.Clone() as Entity;
                    if (clone != null)
                    {
                        draw.Geometry.Draw(clone);
                        clone.Dispose();
                    }
                }
            }

            return true;
        }
    }
}
