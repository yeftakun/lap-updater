using System.Drawing;
using System.Windows.Forms;

namespace LapUpdater;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "About";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var label = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Text = "Lorem ipsum skibidi toilet...",
            Padding = new Padding(12)
        };

        var closeButton = new Button
        {
            Text = "Close",
            AutoSize = true
        };
        closeButton.Click += (_, _) => Close();
        CancelButton = closeButton;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(12)
        };
        buttonPanel.Controls.Add(closeButton);

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        contentPanel.Controls.Add(label);

        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
    }
}
