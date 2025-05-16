using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Extensions;
using System;

namespace SioForgeCAD.Commun.Overrules.CopyGripOverrule
{
    public class CopyGripOverrule : GripOverrule
    {
        private bool _enabled = false;
        public bool IsEnabled => _enabled;
        private bool _originalOverruling = false;

        private readonly Type _targetType;
        private readonly bool _hideOriginals;
        private readonly Func<Entity, bool> _filterFunction;
        private readonly Action<ObjectId> _onHotGripAction;

        public CopyGripOverrule(Type TargetType, Func<Entity, bool> FilterFunction, Action<ObjectId> OnHotGripAction, bool HideOriginals = true)
        {
            this._targetType = TargetType;
            this._filterFunction = FilterFunction;
            this._hideOriginals = HideOriginals;
            this._onHotGripAction = OnHotGripAction;
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
                Matrix3d ucs = Generic.GetEditor().CurrentUserCoordinateSystem;
                Vector3d yAxis = -ucs.CoordinateSystem3d.Yaxis;
                var Extends = entity.GetExtents();
                var entityMiddleCenter = Extends.GetCenter();
                var entityHeight = Extends.Size().Height;

                var GripPoint = entityMiddleCenter.TransformBy(Matrix3d.Displacement(yAxis.SetLength((entityHeight / 2) * 0.35)));

                var grip = new CopyGrip()
                {
                    GripPoint = GripPoint,
                    EntityId = entity.ObjectId,
                    OnHotGripAction = _onHotGripAction
                };
                if (!grips.Contains(grip))
                {
                    grips.Add(grip);
                }

                if (!_hideOriginals)
                {
                    base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
                }
                return;
            }

            base.GetGripPoints(entity, grips, curViewUnitSize, gripSize, curViewDir, bitFlags);
        }
    }
}
