using SioForgeCAD.Commun.Extensions;
using SioForgeCAD.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SioForgeCAD.Functions
{
    public static class CUSTOMLAYOUTBAR
    {
        private static LayoutBar InjectedLayoutBar;
        public static FrameworkElement GetStatusBarContainer()
        {
            foreach (PresentationSource source in PresentationSource.CurrentSources)
            {
                if (source.RootVisual?.GetType().FullName == "Autodesk.AutoCAD.StatusBar.StatusBarContainer")
                {
                    return source.RootVisual as FrameworkElement;
                }
            }
            return null;
        }

        public static IEnumerable<FrameworkElement> GetLayoutSwitchControl()
        {
            if (GetStatusBarContainer() is DependencyObject root)
            {
                var EnfantsVisuels = root.TrouverEnfantsVisuels<FrameworkElement>();

                foreach (var LayoutSwitchControl in EnfantsVisuels.Where(
                    ct => ct.GetType().FullName == "Autodesk.AutoCAD.UserControls.LayoutSwitchControl"))
                {
                    yield return LayoutSwitchControl;
                }
            }
        }

        public static void Override()
        {
            Debug.WriteLine("Overrided Layout Bar");
            foreach (var LayoutSwitchControl in GetLayoutSwitchControl())
            {
                if (InjectedLayoutBar is null)
                {
                    InjectedLayoutBar = new LayoutBar();

                    // 2. Placement UI (on prend exactement la place de la barre native)
                    Grid.SetRow(InjectedLayoutBar, Grid.GetRow(LayoutSwitchControl));
                    Grid.SetColumn(InjectedLayoutBar, Grid.GetColumn(LayoutSwitchControl));
                    Grid.SetRowSpan(InjectedLayoutBar, Grid.GetRowSpan(LayoutSwitchControl));
                    Grid.SetColumnSpan(InjectedLayoutBar, Grid.GetColumnSpan(LayoutSwitchControl));

                    if (LayoutSwitchControl.Parent is Grid LayoutSwitchControlParent)
                    {
                        LayoutSwitchControlParent.Children.Insert(0, InjectedLayoutBar);
                        LayoutSwitchControlParent.UpdateLayout();
                    }

                    // 3. On affiche la tienne et on cache celle d'AutoCAD
                    InjectedLayoutBar.Visibility = Visibility.Visible;
                    LayoutSwitchControl.Visibility = Visibility.Collapsed;

                }
            }
        }

        public static void Toggle()
        {
            Debug.WriteLine("Toggle Visibility Layout Bar");
            foreach (var LayoutSwitchControl in GetLayoutSwitchControl())
            {
                if (InjectedLayoutBar is null)
                {
                    Override();
                    return;
                }
                else if (InjectedLayoutBar.Visibility != Visibility.Visible)
                {
                    InjectedLayoutBar.Visibility = Visibility.Visible;
                    LayoutSwitchControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    InjectedLayoutBar.Visibility = Visibility.Collapsed;
                    LayoutSwitchControl.Visibility = Visibility.Visible;
                }
                return;
            }
        }
    }
}