using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SioForgeCAD.Commun.Extensions
{
    public static class ControlExtensions
    {
        /// <summary>
        /// Recherche récursivement un contrôle enfant d'un type spécifique.
        /// </summary>
        public static T FindControlByType<T>(this Control parent) where T : Control
        {
            if (parent == null) return null;

            foreach (Control child in parent.Controls)
            {
                // Vérifie si l'enfant correspond au type recherché
                if (child is T typedChild)
                {
                    return typedChild;
                }

                // Recherche récursive dans les enfants de cet enfant
                T result = child.FindControlByType<T>();
                if (result != null)
                {
                    return result;
                }
            }

            // Aucun contrôle de ce type n'a été trouvé
            return null;
        }

        /// <summary>
        /// Recherche récursivement un contrôle enfant en utilisant le nom de son type (pratique pour les classes internal/private).
        /// </summary>
        public static Control FindControlByTypeName(this Control parent, string typeName)
        {
            if (parent == null) return null;

            foreach (Control child in parent.Controls)
            {
                Debug.WriteLine(child.GetType().Name);
                if (child.GetType().Name == typeName)
                {
                    return child;
                }

                // Recherche récursive
                Control result = child.FindControlByTypeName(typeName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
