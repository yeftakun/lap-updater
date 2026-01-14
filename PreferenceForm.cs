using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace LapUpdater;

internal sealed class PreferenceForm : Form
{
    private readonly string _repoRootPath;

    private readonly TextBox _driverName;
    private readonly TextBox _driverGear;
    private readonly TextBox _featuredLinkLabel;
    private readonly TextBox _featuredLinkUrl;

    private readonly CheckBox _featuredLapShow;
    private readonly TextBox _featuredLapTrack;
    private readonly TextBox _featuredLapCar;
    private readonly TextBox _featuredLapNote;

    private readonly TextBox _metaTitle;
    private readonly TextBox _metaDescription;
    private readonly TextBox _metaSiteUrl;
    private readonly TextBox _metaBase;
    private readonly TextBox _metaImage;

    private readonly Button _saveButton;

    private WebsiteConfig? _config;

    public PreferenceForm(string repoRootPath)
    {
        _repoRootPath = repoRootPath;

        Text = "Preference";
        Width = 720;
        Height = 640;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12),
            ColumnCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Driver profile
        AddHeader(layout, "Driver Profile");
        _driverName = AddRow(layout, "Name", singleLine: true);
        _driverGear = AddRow(layout, "Gear", singleLine: true);
        _featuredLinkLabel = AddRow(layout, "Featured link label", singleLine: true);
        _featuredLinkUrl = AddRow(layout, "Featured link url", singleLine: true);

        // Featured lap
        AddHeader(layout, "Featured Lap");
        _featuredLapShow = new CheckBox { Text = "Show", AutoSize = true };
        AddControlRow(layout, "Show", _featuredLapShow);
        _featuredLapTrack = AddRow(layout, "Track", singleLine: true);
        _featuredLapCar = AddRow(layout, "Car", singleLine: true);
        _featuredLapNote = AddRow(layout, "Note", singleLine: false, height: 80);

        // Meta
        AddHeader(layout, "Meta");
        _metaTitle = AddRow(layout, "Title", singleLine: true);
        _metaDescription = AddRow(layout, "Description", singleLine: false, height: 60);
        _metaSiteUrl = AddRow(layout, "Site URL", singleLine: true);
        _metaBase = AddRow(layout, "Base", singleLine: true);
        _metaImage = AddRow(layout, "Image", singleLine: true);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };

        _saveButton = new Button { Text = "Save", AutoSize = true };
        _saveButton.Click += OnSave;

        var cancelButton = new Button { Text = "Close", AutoSize = true };
        cancelButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(_saveButton);
        buttonPanel.Controls.Add(cancelButton);

        layout.Controls.Add(buttonPanel, 0, layout.RowCount);
        layout.SetColumnSpan(buttonPanel, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowCount++;

        Controls.Add(layout);

        LoadConfigIntoUi();
    }

    private static void AddHeader(TableLayoutPanel layout, string title)
    {
        var label = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Padding = new Padding(0, 12, 0, 6)
        };

        layout.Controls.Add(label, 0, layout.RowCount);
        layout.SetColumnSpan(label, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowCount++;
    }

    private static TextBox AddRow(TableLayoutPanel layout, string label, bool singleLine, int height = 0)
    {
        var tb = new TextBox
        {
            Dock = DockStyle.Top,
            Multiline = !singleLine,
            ScrollBars = singleLine ? ScrollBars.None : ScrollBars.Vertical
        };

        if (!singleLine && height > 0)
        {
            tb.Height = height;
        }

        AddControlRow(layout, label, tb);
        return tb;
    }

    private static void AddControlRow(TableLayoutPanel layout, string label, Control control)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = true,
            Padding = new Padding(0, 6, 8, 0)
        };

        layout.Controls.Add(lbl, 0, layout.RowCount);
        layout.Controls.Add(control, 1, layout.RowCount);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowCount++;
    }

    private string GetWebsiteConfigPath()
    {
        return Path.Combine(_repoRootPath, "src", "data", "config.json");
    }

    private void LoadConfigIntoUi()
    {
        var path = GetWebsiteConfigPath();
        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"File not found: {path}", "Preference", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _saveButton.Enabled = false;
            return;
        }

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            _config = JsonSerializer.Deserialize<WebsiteConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _config ??= new WebsiteConfig();

            _driverName.Text = _config.DriverProfile.Name ?? string.Empty;
            _driverGear.Text = _config.DriverProfile.Gear ?? string.Empty;
            _featuredLinkLabel.Text = _config.DriverProfile.FeaturedLink.Label ?? string.Empty;
            _featuredLinkUrl.Text = _config.DriverProfile.FeaturedLink.Url ?? string.Empty;

            _featuredLapShow.Checked = _config.FeaturedLap.Show;
            _featuredLapTrack.Text = _config.FeaturedLap.Track ?? string.Empty;
            _featuredLapCar.Text = _config.FeaturedLap.Car ?? string.Empty;
            _featuredLapNote.Text = _config.FeaturedLap.Note ?? string.Empty;

            _metaTitle.Text = _config.Meta.Title ?? string.Empty;
            _metaDescription.Text = _config.Meta.Description ?? string.Empty;
            _metaSiteUrl.Text = _config.Meta.SiteUrl ?? string.Empty;
            _metaBase.Text = _config.Meta.Base ?? string.Empty;
            _metaImage.Text = _config.Meta.Image ?? string.Empty;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to load config.json: {ex.Message}", "Preference", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _saveButton.Enabled = false;
        }
    }

    private void OnSave(object? sender, EventArgs e)
    {
        try
        {
            _config ??= new WebsiteConfig();

            _config.DriverProfile.Name = _driverName.Text.Trim();
            _config.DriverProfile.Gear = _driverGear.Text.Trim();
            _config.DriverProfile.FeaturedLink.Label = _featuredLinkLabel.Text.Trim();
            _config.DriverProfile.FeaturedLink.Url = _featuredLinkUrl.Text.Trim();

            _config.FeaturedLap.Show = _featuredLapShow.Checked;
            _config.FeaturedLap.Track = _featuredLapTrack.Text.Trim();
            _config.FeaturedLap.Car = _featuredLapCar.Text.Trim();
            _config.FeaturedLap.Note = _featuredLapNote.Text.Trim();

            _config.Meta.Title = _metaTitle.Text.Trim();
            _config.Meta.Description = _metaDescription.Text.Trim();
            _config.Meta.SiteUrl = _metaSiteUrl.Text.Trim();
            _config.Meta.Base = _metaBase.Text.Trim();
            _config.Meta.Image = _metaImage.Text.Trim();

            var path = GetWebsiteConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(path, json, Encoding.UTF8);
            MessageBox.Show(this, "Saved.", "Preference", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to save config.json: {ex.Message}", "Preference", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed record WebsiteConfig
{
    [JsonPropertyName("driverProfile")]
    public DriverProfile DriverProfile { get; set; } = new();

    [JsonPropertyName("featuredLap")]
    public FeaturedLap FeaturedLap { get; set; } = new();

    [JsonPropertyName("meta")]
    public Meta Meta { get; set; } = new();
}

internal sealed record DriverProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("gear")]
    public string? Gear { get; set; }

    [JsonPropertyName("featuredLink")]
    public FeaturedLink FeaturedLink { get; set; } = new();
}

internal sealed record FeaturedLink
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

internal sealed record FeaturedLap
{
    [JsonPropertyName("show")]
    public bool Show { get; set; }

    [JsonPropertyName("track")]
    public string? Track { get; set; }

    [JsonPropertyName("car")]
    public string? Car { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

internal sealed record Meta
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("siteUrl")]
    public string? SiteUrl { get; set; }

    [JsonPropertyName("base")]
    public string? Base { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }
}
