using Microsoft.Win32;
using SioForgeCAD.Commun;
using SioForgeCAD.Commun.Mist.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SioForgeCAD
{
    public static class Settings
    {
        //Ordinateur\HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\R24.0\ACAD-4101:40C\Applications\SioForgeCAD\Settings
        private static readonly ConcurrentDictionary<string, object> _RegistryValuesCache = new ConcurrentDictionary<string, object>();

        public static string RegistryPath
        {
            get
            {
                string sProdKey = Autodesk.AutoCAD.DatabaseServices.HostApplicationServices.Current.UserRegistryProductRootKey;
                string sAppName = Generic.GetExtensionDLLName();
                return $@"{sProdKey}\Applications\{sAppName}\Settings";
            }
        }

        public static void CreateAllRegistryKeys()
        {
            var properties = typeof(Settings)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.CanWrite && p.SetMethod?.IsPublic == true);

            foreach (var prop in properties)
            {
                Debug.WriteLine(prop.Name);
                try
                {
                    object value = prop.GetValue(null);
                    prop.SetValue(null, value);
                    Debug.WriteLine($"Value set for : {prop.Name}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur {prop.Name} : {ex.Message}");
                }
            }
        }

        private static T GetValue<T>(string name, T defaultValue)
        {
            //On vérifie si la valeur est déjà en cache (ultra-rapide)
            if (false && _RegistryValuesCache.TryGetValue(name, out object cachedValue))
            {
                return (T)cachedValue;
            }

            // Lecture registre
            T regValue = Registries.GetValue(RegistryPath, name, defaultValue);

            // Si la valeur == valeur par défaut => on supprime l'entrée du registre
            if (Registries.KeyExist(RegistryPath, name) && EqualityComparer<T>.Default.Equals(regValue, defaultValue))
            {
                Registries.DeleteValue(RegistryPath, name);
            }

            _RegistryValuesCache[name] = regValue;
            return regValue;
        }

        private static void SetValue<T>(string name, T value)
        {
            Registries.SetValue(RegistryPath, name, value);
            _RegistryValuesCache[name] = value;
        }

        // --- 3. LES PROPRIÉTÉS ---

        public static int MultithreadingMaxNumberOfThread => 1;

        public static string CopyrightMessage => $"{Generic.GetExtensionDLLName()} - Copyright © HOFFMANN François / SioGabx - {DateTime.Now.Year}.";

        public static string PrefixCAD
        {
            get => GetValue(nameof(PrefixCAD), "_SIOFORGECAD_");
            set => SetValue(nameof(PrefixCAD), value);
        }


        public static string PrefixVegblocLayer
        {
            get => GetValue(nameof(PrefixVegblocLayer), PrefixCAD + "VEG_");
            set => SetValue(nameof(PrefixVegblocLayer), value);
        }

        public static string PrefixInfoLayer
        {
            get => GetValue(nameof(PrefixInfoLayer), PrefixCAD + "INFO_");
            set => SetValue(nameof(PrefixInfoLayer), value);
        }

        public static string BlkAltimetry
        {
            get => GetValue(nameof(BlkAltimetry), PrefixCAD + "COTATIONS_Altimetries");
            set => SetValue(nameof(BlkAltimetry), value);
        }

        public static string BlkSectionAltimetry
        {
            get => GetValue(nameof(BlkSectionAltimetry), PrefixCAD + "CP_Altimetries");
            set => SetValue(nameof(BlkSectionAltimetry), value);
        }

        public static string BlkSlopePercentage
        {
            get => GetValue(nameof(BlkSlopePercentage), PrefixCAD + "COTATIONS_Pentes");
            set => SetValue(nameof(BlkSlopePercentage), value);
        }

        public static int TransientPrimaryColorIndex
        {
            get => GetValue(nameof(TransientPrimaryColorIndex), 252);
            set => SetValue(nameof(TransientPrimaryColorIndex), value);
        }

        public static int TransientSecondaryColorIndex
        {
            get => GetValue(nameof(TransientSecondaryColorIndex), 255);
            set => SetValue(nameof(TransientSecondaryColorIndex), value);
        }

        public static string VegblocLayerHeightName
        {
            get => GetValue(nameof(VegblocLayerHeightName), PrefixInfoLayer + "Hauteurs_végétaux");
            set => SetValue(nameof(VegblocLayerHeightName), value);
        }

        public static bool VegblocCopyGripDeselectAfterCopy
        {
            get => GetValue(nameof(VegblocCopyGripDeselectAfterCopy), true);
            set => SetValue(nameof(VegblocCopyGripDeselectAfterCopy), value);
        }

        public static bool VegblocGeneratePeripheryCircles
        {
            get => GetValue(nameof(VegblocGeneratePeripheryCircles), false);
            set => SetValue(nameof(VegblocGeneratePeripheryCircles), value);
        }

        public static string NewLayerDefaultName
        {
            get => GetValue(nameof(NewLayerDefaultName), PrefixCAD + "SansNom");
            set => SetValue(nameof(NewLayerDefaultName), value);
        }

        public static string EmptyLayoutGabaritFile
        {
            get => GetValue(nameof(EmptyLayoutGabaritFile), @"%UserProfile%\AppData\Local\Autodesk\AutoCAD 2021\R24.0\fra\Template\HOFFMANN.dwt");
            set => SetValue(nameof(EmptyLayoutGabaritFile), value);
        }

        public static string EmptyLayoutGabaritPresentationName
        {
            get => GetValue(nameof(EmptyLayoutGabaritPresentationName), "");
            set => SetValue(nameof(EmptyLayoutGabaritPresentationName), value);
        }

        public static string GabaritFile
        {
            get => GetValue(nameof(GabaritFile), @"%UserProfile%\AppData\Local\Autodesk\AutoCAD 2021\R24.0\fra\Template\HOFFMANN.dwt");
            set => SetValue(nameof(GabaritFile), value);
        }

        public static string SaveFileAtCloseDirectory
        {
            get => GetValue(nameof(SaveFileAtCloseDirectory), Path.GetTempPath());
            set => SetValue(nameof(SaveFileAtCloseDirectory), value);
        }
    }
}