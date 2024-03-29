﻿using System;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class VegblocEditDialog : Form
    {
        public VegblocEditDialog()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }


        private bool HasError(Control Ctrl)
        {
            string value = null;
            if (Ctrl is TextBox)
            {
                value = ((TextBox)Ctrl).Text;
            }

            if (Ctrl is ComboBox)
            {
                value = ((ComboBox)Ctrl).Text;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Veuillez completer l'ensemble des champs");
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
