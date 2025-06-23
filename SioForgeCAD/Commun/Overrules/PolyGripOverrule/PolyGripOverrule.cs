using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Linq;

namespace SioForgeCAD.Commun.Overrules.PolyGripOverrule
{
    public class PolyGripOverrule : GripOverrule
    {
        public enum ModeIdAction
        {
            Default = 100,
            Stretch = 101,
            Add = 102,
            Remove = 103
        }

        private bool _enabled = false;
        public bool IsEnabled => _enabled;
        private bool _originalOverruling = false;

        public readonly Type _targetType;
        private readonly bool _hideOriginals;
        private readonly Func<Entity, bool> _filterFunction;
        private readonly Action<ObjectId, GripData> _CornerOnHotGripAction;
        private readonly Action<ObjectId, GripData> _MiddleOnHotGripAction;

        public PolyGripOverrule(Type TargetType, Func<Entity, bool> FilterFunction, Action<ObjectId, GripData> CornerOnHotGripAction, Action<ObjectId, GripData> MiddleOnHotGripAction, bool HideOriginals = true)
        {
            this._targetType = TargetType;
            this._filterFunction = FilterFunction;
            this._hideOriginals = HideOriginals;
            this._CornerOnHotGripAction = CornerOnHotGripAction;
            this._MiddleOnHotGripAction = MiddleOnHotGripAction;
        }

        public void EnableOverrule(bool enable)
        {
            if (enable)
            {
                if (_enabled)
                {
                    return;
                }

                _originalOverruling = Overruling;
                AddOverrule(GetClass(_targetType), this, false);
                SetCustomFilter();
                var overruled = new PolyGripMenu();
                GetClass(_targetType).AddX(GetClass(typeof(PolyGripMenu)), overruled);
                Overruling = true;
                _enabled = true;
            }
            else
            {
                if (!_enabled)
                {
                    return;
                }

                RemoveOverrule(GetClass(_targetType), this);
                Overruling = _originalOverruling;
                _enabled = false;
            }
        }

        public override bool IsApplicable(RXObject overruledSubject)
        {
            var ent = overruledSubject as Entity;
            return IsApplicable(ent);
        }

        public bool IsApplicable(Entity ent)
        {
            if (ent != null)
            {
                return _filterFunction(ent);
            }
            return false;
        }

        public override void GetGripPoints(Entity entity, GripDataCollection grips, double curViewUnitSize, int gripSize, Vector3d curViewDir, GetGripPointsFlags bitFlags)
        {
            if (IsApplicable(entity))
            {
                //  Dont use transaction here, this cause AutoCAD to crash when changing properties : An item with the same key has already been added
                GripDataCollection DefaultGrips = new GripDataCollection();
                base.GetGripPoints(entity, DefaultGrips, curViewUnitSize, gripSize, curViewDir, bitFlags);
                GripData[] DefaultGripsArray = DefaultGrips.ToArray();

                Point3dCollection AlreadyAddedPoints = new Point3dCollection();
                //Corner grip
                int index = 0;
                foreach (GripData DefaultGrip in DefaultGripsArray)
                {
                    if (!AlreadyAddedPoints.ContainsTolerance(DefaultGrip.GripPoint, Generic.MediumTolerance))
                    {
                        var grip = new PolyCornerGrip()
                        {
                            Index = index,
                            GripPoint = DefaultGrip.GripPoint,
                            EntityId = entity.ObjectId,
                            OnHotGripAction = _CornerOnHotGripAction
                        };
                        AlreadyAddedPoints.Add(DefaultGrip.GripPoint);
                        if (!grips.Contains(grip))
                        {
                            grips.Add(grip);
                        }
                        index++;
                    }
                }

                //Middle grip
                for (int i = 0; i < DefaultGripsArray.Length; i++)
                {
                    GripData DefaultGrip = DefaultGripsArray[i];
                    GripData DefaultGripN = DefaultGripsArray[i < (DefaultGripsArray.Length - 1) ? i + 1 : 0];
                    Point3d MiddlePoint = DefaultGrip.GripPoint.GetMiddlePoint(DefaultGripN.GripPoint);

                    if (!AlreadyAddedPoints.ContainsTolerance(MiddlePoint, Generic.MediumTolerance))
                    {
                        var grip = new PolyMiddleGrip()
                        {
                            Index = i,
                            GripPoint = MiddlePoint,
                            EntityId = entity.ObjectId,
                            DrawVector = DefaultGrip.GripPoint.GetVectorTo(DefaultGripN.GripPoint),
                            PreviousPoint = DefaultGrip.GripPoint,
                            NextPoint = DefaultGripN.GripPoint,
                            OnHotGripAction = _MiddleOnHotGripAction,
                        };
                        AlreadyAddedPoints.Add(MiddlePoint);
                        if (!grips.Contains(grip))
                        {
                            grips.Add(grip);
                        }
                    }
                }
                if (!_hideOriginals)
                {
                    base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
                }
                return;
            }

            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
        }

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            if (grips.Count > 1)
            {
                Generic.WriteMessage("Impossible de déplacer un point superposé");
                return;
            }
            //base.MoveGripPointsAt(entity, grips, offset, bitFlags);
        }
    }
}
