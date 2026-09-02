using System.Windows.Forms;

namespace SioForgeCAD.Forms
{
    public class ProgressDialog : Form
    {
        private Label label;
        private ProgressBar progress;

        public ProgressDialog(string text)
        {
            Width = 400;
            Height = 120;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            ControlBox = false;
            Text = "Conversion";

            label = new Label
            {
                Text = text,
                AutoSize = true,
                Left = 20,
                Top = 15
            };

            progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Left = 20,
                Top = 45,
                Width = 340
            };

            Controls.Add(label);
            Controls.Add(progress);
        }
    }
}
