/// NB: this code requires a reference to AcLayer.dll
using Autodesk.AutoCAD.LayerManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Functions
{
    public static class LAYERMANAGERNEWLAYERDEFAULTNAME
    {
        public static void Override()
        {
            Type typeLocalResources = typeof(LocalResources);
            FieldInfo champRm = typeLocalResources.GetField("rm_", BindingFlags.NonPublic | BindingFlags.Static);

            if (champRm == null)
            {
                return;
            }

            // By calling the method once, we ensure that the if (rm_ == null) in the original code actually creates the instance.
            LocalResources.GetString(string.Empty);
            ResourceManager originalRm = (ResourceManager)champRm.GetValue(null);
            InjectedResourceManager fauxRm = new InjectedResourceManager(originalRm);
            champRm.SetValue(null, fauxRm);

            Debug.WriteLine("Injection de InjectedResourceManager");
        }

        public class InjectedResourceManager : ResourceManager
        {
            private ResourceManager _originalRm;

            public InjectedResourceManager(ResourceManager originalRm)
            {
                _originalRm = originalRm;
            }

            public override string GetString(string name)
            {
                if (name == "Layer")
                {
                    return Settings.NewLayerDefaultName;
                }

                if (_originalRm != null)
                {
                    return _originalRm.GetString(name);
                }

                return base.GetString(name);
            }
        }
    }
}
