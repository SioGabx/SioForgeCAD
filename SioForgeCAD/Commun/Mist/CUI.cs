using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Customization;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

namespace SioForgeCAD.Commun.Mist
{
    public static class CUI
    {
        //https://github.com/HanDefu/SunacCoordination/blob/master/RemoveCuiDoubleClick/CUITools.cs
        public static CustomizationSection GetMainCustomizationSection(this Document _)
        {
            string mainCuiFile = Application.GetSystemVariable("MENUNAME") + ".CUIX";
            return new CustomizationSection(mainCuiFile);
        }
        public static CustomizationSection CreatePartialCui(this Document _, string menuGroupName, string cuiFilePath = null)
        {
            if (string.IsNullOrEmpty(cuiFilePath))
            {
                cuiFilePath = Path.Combine(System.IO.Path.GetDirectoryName(Generic.GetExtensionDLLLocation()), Generic.GetExtensionDLLName() + ".CUIX");
            }

            if (!File.Exists(cuiFilePath))
            {
                CustomizationSection cs = new CustomizationSection
                {
                    MenuGroupDisplayName = menuGroupName,
                    MenuGroupName = menuGroupName
                };
                cs.SaveAs(cuiFilePath);
                return cs;
            }
            else
            {
                return new CustomizationSection(cuiFilePath);
            }
        }

        public static void LoadCui(this CustomizationSection cs)
        {
            if (cs.IsModified)
            {
                cs.Save();
            }
            Document doc = Generic.GetDocument();
            CustomizationSection mainCs = doc.GetMainCustomizationSection();
            if (mainCs.PartialCuiFiles.Contains(cs.CUIFileName))
            {
                Application.UnloadPartialMenu(cs.CUIFileBaseName);
            }

            Application.LoadPartialMenu(cs.CUIFileName);
        }

        public static MenuMacro GetRubbanCommand(this CustomizationSection source, string ElementID)
        {
            foreach (MacroGroup macrog in source.MenuGroup.MacroGroups)
            {
                foreach (MenuMacro menug in macrog.MenuMacros)
                {
                    if (menug.ElementID == ElementID)
                    {
                        return menug;
                    }
                }
            }
            return null;
        }

        public static MenuAccelerator AddPermanentKeyboardShortcut(this CustomizationSection source, string AcceleratorShortcutKey, string Name, string Command, string Description, string NewElementID)
        {
            foreach (MenuAccelerator item in source.MenuGroup.Accelerators)
            {
                if (item is MenuAccelerator ExistMenuAccelerator)
                {
                    if (ExistMenuAccelerator.Name == Name || ExistMenuAccelerator.ElementID == NewElementID)
                    {
                        Debug.WriteLine("KeyboardShortcut non ajouté : l'élément existe déja");
                        return null;
                    }
                }
            }

            MacroGroup mg = GetMacroGroup(source, Name);
            MenuMacro Macro = new MenuMacro(mg, Name, Command, NewElementID, MacroType.Overrides);
            return new MenuAccelerator(Macro, source.MenuGroup)
            {
                AcceleratorShortcutKey = AcceleratorShortcutKey,
                ElementID = NewElementID
            };
        }

        public static TemporaryOverride AddTempKeyboardShortcut(this CustomizationSection source, string OverrideShortcutKey, string Name, string Command, string Description, string NewElementID)
        {
            // This command will install a temporary override key. Temporary override keys are keys that temporarily 
            // turn on or turn off one of the drawing aids that are set in the Drafting Settings dialog box 
            // (for example, Ortho mode, object snaps, or Polar mode).
            foreach (TemporaryOverride item in source.MenuGroup.TemporaryOverrides)
            {
                if (item is TemporaryOverride ExistTempOverride)
                {
                    if (ExistTempOverride.Name == Name || ExistTempOverride.ElementID == NewElementID)
                    {
                        Debug.WriteLine("TempKeyboardShortcut non ajouté : l'élément existe déja");
                        return null;
                    }
                }
            }
            MacroGroup mg = GetMacroGroup(source, Name);
            MenuMacro Macro = new MenuMacro(mg, Name, Command, NewElementID, MacroType.Overrides);
            return new TemporaryOverride(source.MenuGroup, Macro)
            {
                ElementID = NewElementID,
                OverrideShortcutKey = OverrideShortcutKey
            };
        }

        private static MacroGroup GetMacroGroup(this CustomizationSection source, string name)
        {
            MenuGroup menuGroup = source.MenuGroup;
            return menuGroup.FindMacroGroup(menuGroup.Name) ?? new MacroGroup(menuGroup.Name, menuGroup);
        }

        private static void UpdateMacroProperties(MenuMacro macro, string name, string command, string helpString, string imagePath, string CLICommand)
        {
            macro.macro.prepForUpdate();
            macro.macro.Command = command;
            macro.macro.CLICommand = CLICommand;
            macro.macro.Name = name;
            macro.macro.Tags = new StringCollection();
            macro.macro.SmallImage = imagePath;
            macro.macro.LargeImage = imagePath;
            macro.macro.HelpString = helpString;
            macro.macro.ToolTip.BasicContent = helpString;
            macro.macro.ToolTip.HasCommandName = true;
            macro.macro.ToolTip.CommandName = name;
          
        }

        private static MenuMacro TryGetUpdateExistingMacro(MenuMacroCollection macros, string name, string command, string elementID, string helpString, string imagePath, string CLICommand, bool UpdateIfExist)
        {
            foreach (MenuMacro macro in macros)
            {
                if (macro.ElementID == elementID)
                {
                    if (UpdateIfExist)
                    {
                        UpdateMacroProperties(macro, name, command, helpString, imagePath, CLICommand);
                    }
                    return macro;
                }
            }
            return null;
        }



        public static MenuMacro AddMacro(this CustomizationSection source, string name, string command, string elementID, string helpString, string imagePath, string CLICommand = "", bool UpdateIfExist = false)
        {
            MacroGroup macroGroup = GetMacroGroup(source, name);
            MenuMacro existingMacro = TryGetUpdateExistingMacro(macroGroup.MenuMacros, name, command, elementID, helpString, imagePath, CLICommand,UpdateIfExist);

            if (UpdateIfExist)
            {
                CustomizationSection MainSource = GetMainCustomizationSection(null);
                if (MainSource != source)
                {
                    foreach (MacroGroup mgo in MainSource.MenuGroup.MacroGroups)
                    {
                        if (TryGetUpdateExistingMacro(mgo.MenuMacros, name, command, elementID, helpString, imagePath, CLICommand, UpdateIfExist) != null)
                        {
                            MainSource.Save();
                        }
                    }
                }
            }
            if (existingMacro != null) { return existingMacro; }
            MenuMacro menuMacro = new MenuMacro(macroGroup, name, command, elementID);
            UpdateMacroProperties(menuMacro, name, command, helpString, imagePath, CLICommand);

            return menuMacro;
        }

        public static PopMenu AddPopMenu(this MenuGroup menuGroup, string name, StringCollection aliasList, string tag)
        {
            PopMenu pm = null;
            if (menuGroup.PopMenus.IsNameFree(name))
            {
                pm = new PopMenu(name, aliasList, tag, menuGroup);
            }
            return pm;
        }
        public static PopMenuItem AddMenuItem(this PopMenu parentMenu, int index, string name, string macroId)
        {
            PopMenuItem newPmi = null;
            foreach (PopMenuItem pmi in parentMenu.PopMenuItems)
            {
                if (pmi.Name == name && pmi.Name != null)
                {
                    return newPmi;
                }
            }

            newPmi = new PopMenuItem(parentMenu, index);
            if (name != null)
            {
                newPmi.Name = name;
            }

            newPmi.MacroID = macroId;
            return newPmi;
        }
        public static PopMenu AddSubMenu(this PopMenu parentMenu, int index, string name, string tag)
        {
            PopMenu pm = null;
            if (parentMenu.CustomizationSection.MenuGroup.PopMenus.IsNameFree(name))
            {
                pm = new PopMenu(name, null, tag, parentMenu.CustomizationSection.MenuGroup);
                _ = new PopMenuRef(pm, parentMenu, index);
            }
            return pm;
        }
        public static PopMenuItem AddSeparator(this PopMenu parentMenu, int index)
        {
            return new PopMenuItem(parentMenu, index);
        }

        public static Toolbar AddToolbar(this MenuGroup menuGroup, string name)
        {
            Toolbar tb = null;
            if (menuGroup.Toolbars.IsNameFree(name))
            {
                tb = new Toolbar(name, menuGroup)
                {
                    ToolbarOrient = ToolbarOrient.floating,
                    ToolbarVisible = ToolbarVisible.show
                };
            }
            return tb;
        }
        public static ToolbarButton AddToolbarButton(this Toolbar parent, int index, string name, string macroId)
        {
            return new ToolbarButton(macroId, name, parent, index);
        }

        public static void AttachToolbarToFlyout(this Toolbar parent, int index, Toolbar toolbarRef)
        {
            ToolbarFlyout flyout = new ToolbarFlyout(parent, index);
            flyout.ToolbarReference = toolbarRef.Name;
            toolbarRef.ToolbarVisible = ToolbarVisible.hide;
        }

    }
}
