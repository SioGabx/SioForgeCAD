using SioForgeCAD.Commun;
using System;
using System.Windows.Forms;

namespace SioForgeCAD.Functions
{
    internal static class LAYERMANAGERHANDLEBETTEREDITING
    {
        public static void Override()
        {
            var Palettes = Layers.GetLayerPaletteControls();
            var LayerGrid = Palettes.LayerGrid;

            LayerGrid.EditingControlShowing += LayerGrid_EditingControlShowing;


        }

        private static void LayerGrid_EditingControlShowing(object sender, System.Windows.Forms.DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox tb)
            {
                // Positionner le curseur à la fin 
                tb.SelectionStart = tb.Text.Length;
                tb.SelectionLength = 0;

                // Éviter les abonnements multiples
                tb.KeyDown -= TextBox_KeyDown;
                tb.KeyDown += TextBox_KeyDown;
            }
        }

        private static void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb && e.Control)
            {
                char[] separators = new char[] { ' ', '_', '.', '-' };

                if (e.KeyCode == Keys.Delete)
                {
                    int start = tb.SelectionStart;
                    int end = start;

                    while (end < tb.Text.Length && Array.Exists(separators, s => s == tb.Text[end]))
                        end++;

                    while (end < tb.Text.Length && !Array.Exists(separators, s => s == tb.Text[end]))
                        end++;

                    if (end > start)
                    {
                        tb.SelectionStart = start;
                        tb.SelectionLength = end - start;
                        tb.SelectedText = ""; // supprime et notifie
                    }
                }
                // Ctrl + Backspace
                else if (e.KeyCode == Keys.Back)
                {
                    int start = tb.SelectionStart;
                    if (start == 0) return;

                    int end = start - 1;

                    while (end >= 0 && Array.Exists(separators, s => s == tb.Text[end]))
                        end--;

                    while (end >= 0 && !Array.Exists(separators, s => s == tb.Text[end]))
                        end--;

                    int removeStart = end + 1;
                    int length = start - removeStart;

                    if (length > 0)
                    {
                        tb.SelectionStart = removeStart;
                        tb.SelectionLength = length;
                        tb.SelectedText = ""; // supprime le texte et notifie AutoCAD
                    }
                    e.SuppressKeyPress = true;
                    tb.Modified = true; // to update after leaving
                }
            }
        }
    }
}
