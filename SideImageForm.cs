using System.Drawing;
using System.Windows.Forms;

namespace LapUpdater;

internal sealed class SideImageForm : Form
{
    private const int SideImageWidthMin = 120;
    private const int SideImageWidthMax = 520;
    private const int DefaultSideImageWidth = 300;

    private readonly ComboBox _imageSelect;
    private readonly NumericUpDown _widthUpDown;
    private readonly Button _saveButton;

    private readonly AppSettings _settings;
    private bool _isInitializing;

    public bool WasSaved { get; private set; }

    public SideImageForm(AppSettings settings)
    {
        _settings = settings;

        Text = "Side Image";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        _imageSelect = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 360
        };

        _widthUpDown = new NumericUpDown
        {
            Minimum = SideImageWidthMin,
            Maximum = SideImageWidthMax,
            Increment = 10,
            Width = 90
        };

        _saveButton = new Button
        {
            Text = "Save",
            AutoSize = true
        };
        _saveButton.Click += OnSave;

        var closeButton = new Button
        {
            Text = "Close",
            AutoSize = true
        };
        closeButton.Click += (_, _) => Close();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "Image", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 0);
        layout.Controls.Add(_imageSelect, 1, 0);

        layout.Controls.Add(new Label { Text = "Width", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 1);
        layout.Controls.Add(_widthUpDown, 1, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(closeButton);

        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);

        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        LoadOptionsFromDisk();
    }

    private static int ClampSideImageWidth(int value)
    {
        return Math.Clamp(value, SideImageWidthMin, SideImageWidthMax);
    }

    private static string GetSideImageDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "img");
    }

    private void LoadOptionsFromDisk()
    {
        _isInitializing = true;
        try
        {
            _imageSelect.Items.Clear();

            var imgDir = GetSideImageDirectory();
            if (!Directory.Exists(imgDir))
            {
                _imageSelect.Enabled = false;
                _imageSelect.Items.Add("(no img folder)");
                _imageSelect.SelectedIndex = 0;
                _saveButton.Enabled = false;
                return;
            }

            var files = Directory
                .EnumerateFiles(imgDir, "*.bmp", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OfType<string>()
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _imageSelect.Items.Add("(none)");
            foreach (var file in files)
            {
                _imageSelect.Items.Add(file);
            }

            _imageSelect.Enabled = files.Count > 0;

            var savedFile = _settings.SideImageFileName;
            if (!string.IsNullOrWhiteSpace(savedFile) && files.Contains(savedFile, StringComparer.OrdinalIgnoreCase))
            {
                _imageSelect.SelectedItem = files.First(f => string.Equals(f, savedFile, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                _imageSelect.SelectedIndex = files.Count > 0 ? 1 : 0;
            }

            var width = ClampSideImageWidth(_settings.SideImageWidth ?? DefaultSideImageWidth);
            _widthUpDown.Value = width;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        var selected = _imageSelect.SelectedItem?.ToString();
        _settings.SideImageFileName = string.Equals(selected, "(none)", StringComparison.OrdinalIgnoreCase) ? null : selected;
        _settings.SideImageWidth = ClampSideImageWidth((int)_widthUpDown.Value);

        SettingsStore.Save(_settings);
        WasSaved = true;
        Close();
    }
}
