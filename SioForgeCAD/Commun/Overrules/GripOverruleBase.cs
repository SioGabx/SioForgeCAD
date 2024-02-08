using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SioForgeCAD.Commun.Overrules.SetOverruledEntityHelper;

namespace SioForgeCAD.Commun.Overrules
{
    public class GripOverruleBase : GripOverrule
    {
        private readonly MyOverruleTypes _overruleType;
        private readonly RXClass[] _overruledClasses;
        private readonly Func<Entity, bool> _customFilter;
        private bool _enabled = false;
        private bool _originalOverruling = false;

        public GripOverruleBase(
            MyOverruleTypes overruleType,
            RXClass[] overruledClasses,
            Func<Entity, bool> customFilter = null) : base()
        {
            _overruleType = overruleType;
            _overruledClasses = overruledClasses;
            _customFilter = customFilter;
        }

        public void EnableOverrule(bool enable)
        {
            if (enable)
            {
                if (_enabled) return;
                _originalOverruling = Overrule.Overruling;
                if (_overruledClasses != null)
                {
                    foreach (var cls in _overruledClasses)
                    {
                        AddOverrule(cls, this, false);
                    }
                }
                else
                {
                    throw new InvalidOperationException("No overrule target is set!");
                }

                if (_customFilter != null)
                {
                    SetCustomFilter();
                }
                else
                {
                    this.SetExtensionDictionaryEntryFilter(_overruleType.ToString());
                }
                Overrule.Overruling = true;
                _enabled = true;
            }
            else
            {
                if (!_enabled) return;
                foreach (var cls in _overruledClasses)
                {
                    RemoveOverrule(cls, this);
                }
                Overrule.Overruling = _originalOverruling;
                _enabled = false;
            }
        }
        public override bool IsApplicable(RXObject overruledSubject)
        {
            var ent = overruledSubject as Entity;
            if (ent != null)
            {
                return _customFilter(ent);
            }
            else
            {
                return false;
            }
        }
    }
}
