using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class NotesPalette : UserControl
    {
        private TabControl tabs;
        private TextBox historyBox;
        private TextBox newNoteBox;
        private Button saveButton;
        private ListBox pinnedListBox;
        private Button pinButton;
        private Button unpinButton;

        public Action<string> OnSaveNote;
        public Action<string> OnPinNote;
        public Action<string> OnUnpinNote;

        public NotesPalette()
        {
            InitializeComponent(); // Historique
            historyBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill
            };
            tabHistory.Controls.Add(historyBox);

            // Nouveau
            newNoteBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Top,
                Height = 100
            };
            saveButton = new Button
            {
                Text = "Enregistrer la note",
                Dock = DockStyle.Top
            };
            saveButton.Click += (s, e) => OnSaveNote?.Invoke(newNoteBox.Text);
            tabNew.Controls.Add(saveButton);
            tabNew.Controls.Add(newNoteBox);

            // Epinglé
            pinnedListBox = new ListBox { Dock = DockStyle.Fill };
            pinButton = new Button
            {
                Text = "Épingler la note courante",
                Dock = DockStyle.Top
            };
            pinButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(newNoteBox.Text))
                    OnPinNote?.Invoke(newNoteBox.Text);
            };
            unpinButton = new Button
            {
                Text = "Désépingler la sélection",
                Dock = DockStyle.Bottom
            };
            unpinButton.Click += (s, e) =>
            {
                if (pinnedListBox.SelectedItem is string selected)
                {
                    OnUnpinNote?.Invoke(selected);
                    pinnedListBox.Items.Remove(selected);
                }
            };
            tabPinned.Controls.Add(pinnedListBox);
            tabPinned.Controls.Add(pinButton);
            tabPinned.Controls.Add(unpinButton);

        }

        public void AddHistoryItem(string item)
        {
            historyBox.AppendText(item + Environment.NewLine + Environment.NewLine);
        }

        public void AddPinnedItem(string item)
        {
            if (!pinnedListBox.Items.Contains(item))
                pinnedListBox.Items.Add(item);
        }
    }
}
