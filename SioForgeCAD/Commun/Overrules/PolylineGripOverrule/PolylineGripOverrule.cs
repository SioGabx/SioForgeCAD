using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using SioForgeCAD.Commun.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace SioForgeCAD.Commun.Overrules.PolyGripOverrule
{
    public class PolyGripOverrule : GripOverrule
    {
        private bool _enabled = false;
        public bool IsEnabled => _enabled;
        private bool _originalOverruling = false;

        private readonly Type _targetType;
        private readonly bool _hideOriginals;
        private readonly Func<Entity, bool> _filterFunction;
        private readonly Action<ObjectId, Point3d> _onHotGripAction;

        public PolyGripOverrule(Type TargetType, Func<Entity, bool> FilterFunction, Action<ObjectId, Point3d> OnHotGripAction, bool HideOriginals = true)
        {
            this._targetType = TargetType;
            this._filterFunction = FilterFunction;
            this._hideOriginals = HideOriginals;
            this._onHotGripAction = OnHotGripAction;


            var overruled = new OverruledBlock();
            Overrule.GetClass(typeof(Wipeout)).AddX(GetClass(typeof(OverruledBlock)), overruled);
        }

        private class OverruledBlock : MultiModesGripPE
        {
            public override GripMode CurrentMode(Entity entity, GripData gripData)
            {
                var grip = gripData as PolyGrip;
                if (grip == null) return null;
                var index = (int)grip.CurrentModeId - (int)GripMode.ModeIdentifier.CustomStart;
                return grip.GripModes[index];
            }

            public override uint CurrentModeId(Entity entity, GripData gripData)
            {
                var grip = gripData as PolyGrip;
                if (grip != null) return (uint)grip.CurrentModeId;
                return 0;
            }

            public override bool GetGripModes(
                Entity entity, GripData gripData, GripModeCollection modes, ref uint curMode)
            {
                if (!(gripData is PolyGrip)) return false;
                return ((PolyGrip)gripData).GetGripModes(ref modes, ref curMode);
            }

            public override GripType GetGripType(Entity entity, GripData gripData)
            {
                return (gripData is PolyGrip) ? GripType.Secondary : GripType.Primary;
            }

            public override bool SetCurrentMode(Entity entity, GripData gripData, uint curMode)
            {
                if (!(gripData is PolyGrip)) return false;
                ((PolyGrip)gripData).CurrentModeId = (GripMode.ModeIdentifier)curMode;
                return true;
            }

            public override void Reset(Entity entity)
            {
                base.Reset(entity);
            }
        }

        public void EnableOverrule(bool enable)
        {
            if (enable)
            {
                if (_enabled) return;
                _originalOverruling = Overrule.Overruling;
                AddOverrule(RXClass.GetClass(_targetType), this, false);
                SetCustomFilter();
                Overrule.Overruling = true;
                _enabled = true;
            }
            else
            {
                if (!_enabled) return;
                RemoveOverrule(RXClass.GetClass(_targetType), this);
                Overrule.Overruling = _originalOverruling;
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

                foreach (var DefaultGrip in DefaultGrips)
                {
                    var grip = new PolyGrip()
                    {
                        GripPoint = DefaultGrip.GripPoint,
                        EntityId = entity.ObjectId,
                        OnHotGripAction = _onHotGripAction
                    };
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

        public override void MoveGripPointsAt(Entity entity, GripDataCollection grips, Vector3d offset, MoveGripPointsFlags bitFlags)
        {
            if (grips.Count > 1) {
                Generic.WriteMessage("Impossible de déplacer un point superposé");
                return;
            }
            base.MoveGripPointsAt(entity, grips, offset, bitFlags);
        }
    }


}
