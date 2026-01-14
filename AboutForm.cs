using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace LapUpdater;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        var versionText = GetAppVersionText();

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
            Text =
                "Lap Time Updater " + versionText + 
                Environment.NewLine + Environment.NewLine +
                "A small utility to quickly update your lap times to your website repo." + Environment.NewLine +
                Environment.NewLine +
                "Features:" + Environment.NewLine +
                "- Copy personalbest.ini into the repo data folder" + Environment.NewLine +
                "- Check changes and push via git" + Environment.NewLine +
                "- Edit website config (src/data/config.json) via Preference" + Environment.NewLine +
                "- Configure Side Image (image, width, based on picture)" + Environment.NewLine +
                Environment.NewLine +
                "App config location:" + Environment.NewLine +
                "%APPDATA%\\LapUpdater\\config.json" + Environment.NewLine +
                Environment.NewLine +
                "Note: make sure git is available and the repo path is correct before pushing.",
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

    private static string GetAppVersionText()
    {
        try
        {
            var assembly = typeof(AboutForm).Assembly;

            var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                // .NET SDK may append build metadata like "+<git-sha>" for traceability.
                // For display purposes, hide the metadata and show only the semver core.
                var plusIndex = info.IndexOf('+');
                if (plusIndex >= 0)
                {
                    info = info[..plusIndex];
                }

                return $"v{info}";
            }

            var version = assembly.GetName().Version?.ToString();
            return string.IsNullOrWhiteSpace(version) ? "" : $"v{version}";
        }
        catch
        {
            return string.Empty;
        }
    }
}
