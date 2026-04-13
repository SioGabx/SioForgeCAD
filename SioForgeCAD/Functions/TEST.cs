

using SioForgeCAD.Commun.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace SioForgeCAD.Functions
{
    internal static class TEST
    {
        public static void Attach()
        {
            List<FrameworkElement> LayoutSwitchControlList = new List<FrameworkElement>();

            if (CUSTOMLAYOUTBAR.ObtenirStatusBarContainer() is DependencyObject root)
            {
                var EnfantsVisuels = root.TrouverEnfantsVisuels<FrameworkElement>();
                Debug.WriteLine($"{EnfantsVisuels.Count()} Enfants visuels trouvés");

                foreach (var LayoutSwitchControl in EnfantsVisuels.Where(
                    ct => ct.GetType().FullName == "Autodesk.AutoCAD.UserControls.LayoutSwitchControl"))
                {
                    // 1. On récupère le DataContext en tant qu'objet standard (pas de dynamic)


                    var LayoutSwitchControlEnfantsVisuels = LayoutSwitchControl.TrouverEnfantsVisuels<FrameworkElement>();
                    foreach (var LayoutSwitchControlEnfantsVisuelsChild in LayoutSwitchControlEnfantsVisuels)
                    {
                        if (LayoutSwitchControlEnfantsVisuelsChild is System.Windows.Controls.TabControl tb)
                        {
                            tb.DataContextChanged += Tb_DataContextChanged;
                            tb.SourceUpdated += Tb_SourceUpdated;
                            tb.SelectionChanged += Tb_SelectionChanged;
                        }
                       
                    }

                        object context = LayoutSwitchControl.DataContext;
                    LayoutSwitchControl.DataContextChanged += LayoutSwitchControl_DataContextChanged;
                    (root as FrameworkElement).DataContextChanged += root_DataContextChanged; ;
                    LayoutSwitchControl.SourceUpdated += LayoutSwitchControl_SourceUpdated;
                    LayoutSwitchControl.TargetUpdated += LayoutSwitchControl_TargetUpdated;
                    LayoutSwitchControl.LayoutUpdated += LayoutSwitchControl_LayoutUpdated;
                    LayoutSwitchControl.Loaded += LayoutSwitchControl_Loaded;

                    if (context != null)
                    {
                        // 1. Écouter le remplacement de la propriété TabsCollection
                        var descriptor = DependencyPropertyDescriptor.FromName("TabsCollection", context.GetType(), context.GetType());
                        if (descriptor != null)
                        {
                            descriptor.AddValueChanged(context, (s, e) =>
                            {
                                Debug.WriteLine("La collection TabsCollection a été remplacée !");

                                // 2. Extraire la nouvelle collection
                                PropertyInfo tabsCollectionProp = context.GetType().GetProperty("TabsCollection");
                                object newTabsCollectionObj = tabsCollectionProp.GetValue(context);

                                if (newTabsCollectionObj is INotifyCollectionChanged newObservable)
                                {
                                    // Se désabonner pour éviter les fuites de mémoire (idéalement tu gardes une référence sur l'ancienne collection pour le faire proprement)
                                    newObservable.CollectionChanged -= SurOngletsModifies;
                                    // S'abonner à la nouvelle collection
                                    newObservable.CollectionChanged += SurOngletsModifies;

                                    Debug.WriteLine("Réabonné à la nouvelle collection !");
                                }
                            });
                        }
                    }

                    if (context != null)
                    {
                        // 2. On utilise la réflexion pour chercher la propriété "TabsCollection"
                        PropertyInfo tabsCollectionProp = context.GetType().GetProperty("TabsCollection");

                        if (tabsCollectionProp != null)
                        {
                            // 3. On extrait la valeur (l'ObservableCollection) de notre contexte
                            object tabsCollectionObj = tabsCollectionProp.GetValue(context);


                            Type type = tabsCollectionObj.GetType();
                            if (type.IsGenericType
                               && type.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                            {
                                EventInfo collectionChanged = type.GetEvent("CollectionChanged");
                                collectionChanged.AddEventHandler(tabsCollectionObj,
    new NotifyCollectionChangedEventHandler((sender, e) =>
    {
        Debug.WriteLine($"CollectionChanged");
    }));
                            }



                            // 4. On cast de manière sécurisée vers INotifyCollectionChanged
                            if (tabsCollectionObj is INotifyCollectionChanged observableCollection)
                            {
                                // On nettoie l'ancien abonnement pour éviter les doublons
                                observableCollection.CollectionChanged -= SurOngletsModifies;

                                // On s'abonne au nouvel événement
                                observableCollection.CollectionChanged += SurOngletsModifies;

                                Debug.WriteLine("Écouteur attaché avec succès via la Réflexion !");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Propriété 'TabsCollection' introuvable sur cet objet via la Réflexion.");
                        }
                    }
                }
            }
        }

        private static void Tb_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"Tb_SelectionChanged");
        }

        private static void Tb_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            Debug.WriteLine($"Tb_SourceUpdated");
        }

        private static void Tb_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine($"Tb_DataContextChanged");
        }

        private static void root_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine($"root_DataContextChanged");
        }

        private static void LayoutSwitchControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"LayoutSwitchControl_Loaded");
        }

        private static void LayoutSwitchControl_LayoutUpdated(object sender, System.EventArgs e)
        {
            //Debug.WriteLine($"_LayoutUpdated");
        }

        private static void LayoutSwitchControl_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {Debug.WriteLine($"LayoutSwitchControl_TargetUpdated");
           
        }

        private static void LayoutSwitchControl_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
        {
            Debug.WriteLine($"LayoutSwitchControl_SourceUpdated");
        }

        private static void LayoutSwitchControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine($"LayoutSwitchControl_DataContextChanged");
        }

        // L'événement qui sera déclenché à chaque modification des onglets
        private static void SurOngletsModifies(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.WriteLine($"Nouvel onglet ajouté. Nombre d'éléments ajoutés : {e.NewItems.Count}");
                    // Tu peux parcourir e.NewItems pour voir l'objet LayoutData ajouté
                    /* foreach (dynamic newItem in e.NewItems) {
                           Debug.WriteLine($"Nom du nouvel onglet : {newItem.Name}");
                       } */
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.WriteLine($"Onglet supprimé. Nombre d'éléments supprimés : {e.OldItems.Count}");
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Debug.WriteLine("Un onglet a été remplacé.");
                    break;

                case NotifyCollectionChangedAction.Move:
                    Debug.WriteLine("Un onglet a été déplacé (réorganisation).");
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Debug.WriteLine("La collection d'onglets a été complètement réinitialisée.");
                    break;
            }
        }

    }
}