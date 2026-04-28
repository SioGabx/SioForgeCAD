using System;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class VegblocEditDialog : Form
    {
        public Autodesk.AutoCAD.Colors.Color SelectedColor { get; private set; }

        public VegblocEditDialog()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        public void SetColor(Autodesk.AutoCAD.Colors.Color color)
        {
            SelectedColor = color;
            ColorPreviewPanel.BackColor = color.ColorValue;
        }

        private void ColorSelectButton_Click(object sender, EventArgs e)
        {
            var colorDialog = new Autodesk.AutoCAD.Windows.ColorDialog();
                if (SelectedColor != null)
                {
                    colorDialog.Color = SelectedColor;
                }

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    SetColor(colorDialog.Color);
                }
        }

        private static bool HasError(Control Ctrl)
        {
            string value = null;
            if (Ctrl is TextBox textbox)
            {
                value = textbox.Text;
            }
            else if (Ctrl is ComboBox combobox)
            {
                value = combobox.Text;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog("Veuillez completer l'ensemble des champs");
                Ctrl.Focus();
                return true;
            }
            return false;
        }

        private void PromptAcceptButton_Click(object sender, EventArgs e)
        {
            if (HasError(NameInput) || HasError(HeightInput) || HasError(WidthInput) || HasError(TypeInput))
            {
                return;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void PromptCancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}