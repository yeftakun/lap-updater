using System.Drawing;
using System.Windows.Forms;

namespace LapUpdater;

internal sealed class SideImageForm : Form
{
    private const int SideImageWidthMin = 120;
    private const int SideImageWidthMax = 520;
    private const int DefaultSideImageWidth = 300;

    private readonly ComboBox _imageSelect;
    private readonly CheckBox _basedOnPictureCheckBox;
    private readonly NumericUpDown _widthUpDown;
    private readonly Button _saveButton;

    private readonly AppSettings _settings;
    private readonly int _sideImageAreaHeight;
    private bool _isInitializing;

    public bool WasSaved { get; private set; }

    public event EventHandler<SideImageSavedEventArgs>? Saved;

    public SideImageForm(AppSettings settings, int sideImageAreaHeight)
    {
        _settings = settings;
        _sideImageAreaHeight = Math.Max(1, sideImageAreaHeight);

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
        _imageSelect.SelectedIndexChanged += (_, _) => OnSelectionChanged();

        _basedOnPictureCheckBox = new CheckBox
        {
            Text = "Based on picture",
            AutoSize = true
        };
        _basedOnPictureCheckBox.CheckedChanged += (_, _) => OnBasedOnPictureChanged();

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

        layout.Controls.Add(new Label { Text = "Mode", AutoSize = true, Padding = new Padding(0, 6, 8, 0) }, 0, 2);
        layout.Controls.Add(_basedOnPictureCheckBox, 1, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(closeButton);

        layout.Controls.Add(buttons, 0, 3);
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

    private static string GetSideImagePath(string fileName)
    {
        return Path.Combine(GetSideImageDirectory(), fileName);
    }

    private void OnBasedOnPictureChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateWidthUiState();

        if (_basedOnPictureCheckBox.Checked)
        {
            TryUpdateWidthFromSelectedImage();
        }
    }

    private void OnSelectionChanged()
    {
        if (_isInitializing)
        {
            return;
        }

        if (_basedOnPictureCheckBox.Checked)
        {
            TryUpdateWidthFromSelectedImage();
        }
    }

    private void UpdateWidthUiState()
    {
        // When based-on-picture is enabled, width is derived from image aspect ratio.
        _widthUpDown.Enabled = !_basedOnPictureCheckBox.Checked;
    }

    private bool TryComputeWidthFromSelectedImage(out int width)
    {
        width = ClampSideImageWidth((int)_widthUpDown.Value);

        var selected = _imageSelect.SelectedItem?.ToString();
        var fileName = string.Equals(selected, "(none)", StringComparison.OrdinalIgnoreCase) ? null : selected;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var path = GetSideImagePath(fileName);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var img = Image.FromFile(path);
            if (img.Width <= 0 || img.Height <= 0)
            {
                return false;
            }

            var ratio = img.Width / (double)img.Height;
            var computed = (int)Math.Round(_sideImageAreaHeight * ratio);
            width = ClampSideImageWidth(computed);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TryUpdateWidthFromSelectedImage()
    {
        if (TryComputeWidthFromSelectedImage(out var width))
        {
            _isInitializing = true;
            try
            {
                _widthUpDown.Value = width;
            }
            finally
            {
                _isInitializing = false;
            }
        }
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

            _basedOnPictureCheckBox.Checked = _settings.SideImageBasedOnPicture;
            UpdateWidthUiState();

            if (_basedOnPictureCheckBox.Checked)
            {
                TryUpdateWidthFromSelectedImage();
            }
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
        var fileName = string.Equals(selected, "(none)", StringComparison.OrdinalIgnoreCase) ? null : selected;
        var basedOnPicture = _basedOnPictureCheckBox.Checked;

        var width = ClampSideImageWidth((int)_widthUpDown.Value);
        if (basedOnPicture && TryComputeWidthFromSelectedImage(out var computedWidth))
        {
            width = computedWidth;
            _isInitializing = true;
            try
            {
                _widthUpDown.Value = width;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        _settings.SideImageFileName = fileName;
        _settings.SideImageWidth = width;
        _settings.SideImageBasedOnPicture = basedOnPicture;

        SettingsStore.Save(_settings);
        WasSaved = true;

        Saved?.Invoke(this, new SideImageSavedEventArgs(fileName, width, basedOnPicture));
    }
}

internal sealed record SideImageSavedEventArgs(string? FileName, int Width, bool BasedOnPicture);
