using SioForgeCAD.Commun;
using System;
using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public partial class InputDialogBox : Form
    {
        public InputDialogBox()
        {
            InitializeComponent();
            this.Text = Generic.GetExtensionDLLName();
        }

        public string GetUserInput()
        {
            return UserInputBox.Text;
        }

        public void SetUserInputPlaceholder(string UserInputBoxPlaceholder)
        {
            UserInputBox.Text = UserInputBoxPlaceholder;
        }

        public void SetPrompt(string Prompt)
        {
            PromptLabel.Text = Prompt;
        }

        private void PromptAcceptButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void PromptCancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void UserInputBox_Validated(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
