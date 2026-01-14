using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace LapUpdater;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

    private readonly TextBox _sourcePathBox;
    private readonly TextBox _repoRootBox;
    private readonly Button _checkChangesButton;
    private readonly Button _updateButton;
    private readonly Button _preferencesButton;
    private readonly Button _clearConsoleButton;
    private readonly RichTextBox _outputBox;
    private readonly Label _statusLabel;

    private readonly PictureBox _sideImageBox;
    private readonly TableLayoutPanel _shell;
    private readonly ColumnStyle _sideImageColumnStyle;
    private readonly Control _rightContent;

    private readonly ToolStripMenuItem _openConfigMenuItem;
    private readonly ToolStripMenuItem _sideImageMenuItem;
    private readonly ToolStripMenuItem _aboutMenuItem;

    private AppSettings _settings;
    private bool _isBusy;

    private const int SideImageWidthMin = 120;
    private const int SideImageWidthMax = 520;
    private const int DefaultSideImageWidth = 300;
    private const int RightContentMinWidth = 860;
    private const int WindowWidthMargin = 40;

    public MainForm()
    {
        Text = "Lap Time Updater";
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        Width = 1020;
        Height = 520;
        StartPosition = FormStartPosition.CenterScreen;
        _settings = SettingsStore.Load();

        var menuStrip = new MenuStrip { Dock = DockStyle.Top };
        var menuRoot = new ToolStripMenuItem("Menu");

        var githubMenuItem = new ToolStripMenuItem("GitHub");
        githubMenuItem.Click += OnGithub;

        _sideImageMenuItem = new ToolStripMenuItem("Side Image");
        _sideImageMenuItem.Click += OnSideImageMenu;

        _openConfigMenuItem = new ToolStripMenuItem("config.json") { Enabled = false };
        _openConfigMenuItem.Click += OnOpenConfig;

        _aboutMenuItem = new ToolStripMenuItem("About");
        _aboutMenuItem.Click += OnAbout;

        menuRoot.DropDownItems.Add(_openConfigMenuItem);
        menuRoot.DropDownItems.Add(_sideImageMenuItem);
        menuRoot.DropDownItems.Add(githubMenuItem);
        menuRoot.DropDownItems.Add(new ToolStripSeparator());
        menuRoot.DropDownItems.Add(_aboutMenuItem);
        menuStrip.Items.Add(menuRoot);
        MainMenuStrip = menuStrip;

        var instructions = new Label
        {
            AutoSize = true,
            Text = "This app is only for quickly updating your lap times. Assume you've completed the initial setup.",
            Padding = new Padding(0, 0, 0, 8)
        };

        _sourcePathBox = new TextBox
        {
            ReadOnly = true,
            TabStop = false,
            Cursor = Cursors.Arrow,
            Width = 520
        };

        _repoRootBox = new TextBox
        {
            ReadOnly = true,
            TabStop = false,
            Cursor = Cursors.Arrow,
            Width = 520
        };

        var browseFileButton = new Button
        {
            Text = "Browse file",
            AutoSize = true
        };
        browseFileButton.Click += OnBrowseFile;

        _sourcePathBox.Enter += (_, _) => browseFileButton.Focus();

        var browseFolderButton = new Button
        {
            Text = "Browse folder",
            AutoSize = true
        };
        browseFolderButton.Click += OnBrowseFolder;

        _repoRootBox.Enter += (_, _) => browseFolderButton.Focus();

        _checkChangesButton = new Button
        {
            Text = "Check changes",
            AutoSize = true,
            Enabled = false
        };
        _checkChangesButton.Click += OnCheckChanges;

        _updateButton = new Button
        {
            Text = "Update Laptime",
            AutoSize = true,
            Enabled = false
        };
        _updateButton.Click += OnUpdate;

        _preferencesButton = new Button
        {
            Text = "Preference",
            AutoSize = true,
            Enabled = false
        };
        _preferencesButton.Click += OnOpenPreference;

        _clearConsoleButton = new Button
        {
            Text = "Clear console",
            AutoSize = true
        };
        _clearConsoleButton.Click += OnClearConsole;

        _outputBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            DetectUrls = false,
            Font = CreateConsoleFont(10F)
        };

        _statusLabel = new Label
        {
            AutoSize = true,
            Text = "No changes",
            ForeColor = SystemColors.ControlText,
            Padding = new Padding(8, 6, 0, 0)
        };

        _sideImageBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.ControlLight
        };

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        mainLayout.Controls.Add(instructions, 0, 0);
        mainLayout.Controls.Add(CreatePathRow("Source personalbest.ini", _sourcePathBox, browseFileButton), 0, 1);
        mainLayout.Controls.Add(CreatePathRow("Website repo root", _repoRootBox, browseFolderButton), 0, 2);
        mainLayout.Controls.Add(CreateActionsRow(), 0, 3);
        mainLayout.Controls.Add(CreateOutputRow(), 0, 4);

        _shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        var initialWidth = GetInitialSideImageWidth(_settings);

        _sideImageColumnStyle = new ColumnStyle(SizeType.Absolute, initialWidth);
        _shell.ColumnStyles.Add(_sideImageColumnStyle);
        _shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _shell.Controls.Add(_sideImageBox, 0, 0);
        _shell.Controls.Add(mainLayout, 1, 0);

        _rightContent = mainLayout;

        Controls.Add(_shell);
        Controls.Add(menuStrip);

        LoadSettingsIntoUi();
        ApplySideImageFromSettings();
        UpdateButtonStates();
        ApplyStoredStatus();

        EnsureWindowFitsContent(initialWidth, mainLayout, allowShrink: string.IsNullOrWhiteSpace(_settings.SideImageFileName));
    }

    private static Font CreateConsoleFont(float size)
    {
        // Prefer modern monospace if available; fall back to Consolas.
        try
        {
            using var probe = new Font("Cascadia Mono", size);
            if (string.Equals(probe.Name, "Cascadia Mono", StringComparison.OrdinalIgnoreCase))
            {
                return new Font("Cascadia Mono", size);
            }
        }
        catch
        {
            // ignore
        }

        return new Font("Consolas", size);
    }

    private static int ClampSideImageWidth(int value)
    {
        return Math.Clamp(value, SideImageWidthMin, SideImageWidthMax);
    }

    private static int GetInitialSideImageWidth(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SideImageFileName))
        {
            return 0;
        }

        return ClampSideImageWidth(settings.SideImageWidth ?? DefaultSideImageWidth);
    }

    private void EnsureWindowFitsContent(int sideWidth, Control rightContent, bool allowShrink)
    {
        // TableLayoutPanel with Dock=Fill doesn't always report a useful PreferredSize,
        // so we combine measurement with a safe minimum.
        var measuredRight = rightContent.GetPreferredSize(new Size(int.MaxValue, int.MaxValue)).Width;
        var rightWidth = Math.Max(RightContentMinWidth, measuredRight);

        var desiredClientWidth = sideWidth + rightWidth;
        var desiredWindowWidth = SizeFromClientSize(new Size(desiredClientWidth, ClientSize.Height)).Width;
        Width = allowShrink
            ? ClampWindowWidthToScreen(desiredWindowWidth)
            : ClampWindowWidthToScreen(Math.Max(Width, desiredWindowWidth));
    }

    private int ClampWindowWidthToScreen(int requestedWidth)
    {
        var area = Screen.FromControl(this).WorkingArea;
        var max = Math.Max(300, area.Width - WindowWidthMargin);
        return Math.Min(requestedWidth, max);
    }

    private Control CreatePathRow(string label, Control textBox, Control button)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        row.Controls.Add(new Label { Text = label, AutoSize = true, Width = 150 });
        row.Controls.Add(textBox);
        row.Controls.Add(button);
        return row;
    }

    private Control CreateLabeledRow(string label, Control control)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        row.Controls.Add(new Label { Text = label, AutoSize = true, Width = 150 });
        row.Controls.Add(control);
        return row;
    }

    private Control CreateActionsRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 8)
        };

        row.Controls.Add(_checkChangesButton);
        row.Controls.Add(_updateButton);
        row.Controls.Add(_preferencesButton);
        row.Controls.Add(_clearConsoleButton);
        row.Controls.Add(_statusLabel);
        return row;
    }

    private Control CreateOutputRow()
    {
        var group = new GroupBox
        {
            Text = "Console output",
            Dock = DockStyle.Fill
        };
        group.Controls.Add(_outputBox);
        return group;
    }

    private void LoadSettingsIntoUi()
    {
        if (!string.IsNullOrWhiteSpace(_settings.SourceIniPath))
        {
            _sourcePathBox.Text = _settings.SourceIniPath;
        }

        if (!string.IsNullOrWhiteSpace(_settings.RepoRootPath))
        {
            _repoRootBox.Text = _settings.RepoRootPath;
        }
    }

    private static string GetSideImageDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "img");
    }

    private void ApplySideImageFromSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.SideImageFileName))
        {
            CollapseSideImageArea();
            SetSideImage(null);
            return;
        }

        ExpandSideImageArea();
        ApplySideImageWidth(_settings.SideImageWidth ?? DefaultSideImageWidth);

        var path = Path.Combine(GetSideImageDirectory(), _settings.SideImageFileName);
        SetSideImage(path);
    }

    private void CollapseSideImageArea()
    {
        var oldWidth = (int)_sideImageColumnStyle.Width;

        _shell.SuspendLayout();
        try
        {
            _sideImageBox.Visible = false;
            _sideImageColumnStyle.SizeType = SizeType.Absolute;
            _sideImageColumnStyle.Width = 0;
        }
        finally
        {
            _shell.ResumeLayout(true);
        }

        // Keep overall window width stable: if we collapsed a previously-visible side area,
        // shrink the window by the same amount (but never below content minimum).
        if (oldWidth > 0)
        {
            var measuredRight = _rightContent.GetPreferredSize(new Size(int.MaxValue, int.MaxValue)).Width;
            var rightWidth = Math.Max(RightContentMinWidth, measuredRight);

            var minWindowWidth = SizeFromClientSize(new Size(rightWidth, ClientSize.Height)).Width;
            Width = ClampWindowWidthToScreen(Math.Max(minWindowWidth, Width - oldWidth));
        }
    }

    private void ExpandSideImageArea()
    {
        if (_sideImageBox.Visible)
        {
            return;
        }

        _shell.SuspendLayout();
        try
        {
            _sideImageBox.Visible = true;
        }
        finally
        {
            _shell.ResumeLayout(true);
        }
    }

    private void ApplySideImageWidth(int width)
    {
        width = ClampSideImageWidth(width);

        var oldWidth = (int)_sideImageColumnStyle.Width;
        if (oldWidth == width)
        {
            return;
        }

        // Keep the right content area from shrinking by widening the window
        // by the same delta as the side image width.
        var delta = width - oldWidth;
        Width = ClampWindowWidthToScreen(Width + delta);

        _shell.SuspendLayout();
        try
        {
            _sideImageColumnStyle.SizeType = SizeType.Absolute;
            _sideImageColumnStyle.Width = width;
        }
        finally
        {
            _shell.ResumeLayout(true);
        }
    }

    private void SetSideImage(string? imagePath)
    {
        try
        {
            var old = _sideImageBox.Image;

            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                _sideImageBox.Image = null;
                old?.Dispose();
                return;
            }

            using var temp = new Bitmap(imagePath);
            _sideImageBox.Image = new Bitmap(temp);
            old?.Dispose();
        }
        catch
        {
            // Ignore image errors; keep UI responsive.
        }
    }

    private void OnSideImageMenu(object? sender, EventArgs e)
    {
        var sideAreaHeight = Math.Max(1, _sideImageBox.ClientSize.Height);
        using var form = new SideImageForm(_settings, sideAreaHeight);
        form.Saved += (_, args) =>
        {
            _settings.SideImageFileName = args.FileName;
            _settings.SideImageWidth = args.Width;
            _settings.SideImageBasedOnPicture = args.BasedOnPicture;

            ApplySideImageFromSettings();
            SetStatus("Side image saved!", Color.Green);
        };

        form.ShowDialog(this);

        // If user saved at least once, ensure main UI reflects the last saved state.
        if (form.WasSaved)
        {
            ApplySideImageFromSettings();
        }
    }

    private void OnBrowseFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select personalbest.ini",
            Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
            FileName = "personalbest.ini"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _settings.SourceIniPath = dialog.FileName;
            _sourcePathBox.Text = dialog.FileName;
            SettingsStore.Save(_settings);
            UpdateButtonStates();
        }
    }

    private void OnBrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select your website repository root",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _settings.RepoRootPath = dialog.SelectedPath;
            _repoRootBox.Text = dialog.SelectedPath;
            SettingsStore.Save(_settings);
            UpdateButtonStates();
        }
    }

    private async void OnCheckChanges(object? sender, EventArgs e)
    {
        if (!EnsurePaths())
        {
            return;
        }

        if (!await EnsureInternetAsync())
        {
            return;
        }

        if (!CopySourceIniToRepoData())
        {
            return;
        }

        SetBusy(true);
        _updateButton.Enabled = false;
        AppendOutput("Running git fetch (compare with remote)...");
        var fetchResult = await RunCommandAsync("git", "fetch --quiet");
        AppendCommandResult(fetchResult);

        AppendOutput("Running git status...");
        var statusResult = await RunCommandAsync("git", "status -sb");
        AppendCommandResult(statusResult);

        var statusLines = statusResult.Output
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        var hasAheadCommits = statusLines.FirstOrDefault()?.Contains("ahead", StringComparison.OrdinalIgnoreCase) == true;
        var hasUncommitted = statusLines.Length > 1; // any entry beyond the branch summary means local changes

        var hasChanges = hasUncommitted || hasAheadCommits;

        _updateButton.Enabled = hasChanges;
        if (!hasChanges)
        {
            AppendOutput("No changes detected (local + remote). ");
            SetStatus("No changes", SystemColors.ControlText);
        }
        else if (hasUncommitted)
        {
            SetStatus("Changes found", Color.Green);
        }
        else if (hasAheadCommits)
        {
            SetStatus("Commits pending push", Color.Green);
            AppendOutput("Local branch is ahead of remote: push required.");
        }

        SetBusy(false);
    }

    private async void OnUpdate(object? sender, EventArgs e)
    {
        if (!EnsurePaths())
        {
            return;
        }

        if (!await EnsureInternetAsync())
        {
            return;
        }

        SetBusy(true);
        _updateButton.Enabled = false;

        AppendOutput("Running git add .");
        var addResult = await RunCommandAsync("git", "add .");
        AppendCommandResult(addResult);

        AppendOutput("Running git commit...");
        var commitResult = await RunCommandAsync("git", "commit -m \"Updated: Laptime\"");
        AppendCommandResult(commitResult);

        var commitWasNoop = commitResult.ExitCode != 0
                            && (commitResult.Output.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase)
                                || commitResult.Error.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase));
        var commitSucceeded = commitResult.ExitCode == 0 || commitWasNoop;

        AppendOutput("Running git push...");
        var pushResult = await RunCommandAsync("git", "push");
        AppendCommandResult(pushResult);

        var success = addResult.ExitCode == 0 && commitSucceeded && pushResult.ExitCode == 0;
        if (success)
        {
            SetStatus("Changes sent", Color.Green);
            _settings.LastPushStatus = PushStatus.Success;
            _updateButton.Enabled = false;
        }
        else
        {
            SetStatus("An error occurred!", Color.DarkOrange);
            _settings.LastPushStatus = PushStatus.Failure;
            _updateButton.Enabled = true; // Allow retry (e.g., push failed due to connectivity)
        }

        SettingsStore.Save(_settings);

        SetBusy(false);
    }

    private bool EnsurePaths()
    {
        if (string.IsNullOrWhiteSpace(_settings.SourceIniPath) || !File.Exists(_settings.SourceIniPath))
        {
            MessageBox.Show(this, "Select a valid personalbest.ini first.", "Missing file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.RepoRootPath) || !Directory.Exists(_settings.RepoRootPath))
        {
            MessageBox.Show(this, "Select a valid repository root folder.", "Missing folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    private bool CopySourceIniToRepoData()
    {
        try
        {
            var targetDir = Path.Combine(_settings.RepoRootPath!, "data");
            Directory.CreateDirectory(targetDir);
            var targetPath = Path.Combine(targetDir, "personalbest.ini");
            File.Copy(_settings.SourceIniPath!, targetPath, overwrite: true);
            AppendOutput($"Copied personalbest.ini to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to copy personalbest.ini: {ex.Message}", "Copy failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        _checkChangesButton.Enabled = !isBusy && PathsReady();
        _updateButton.Enabled = !isBusy && _updateButton.Enabled;
        _openConfigMenuItem.Enabled = !isBusy && CanOpenConfig();
        _sideImageMenuItem.Enabled = !isBusy;
        _preferencesButton.Enabled = !isBusy && CanOpenPreference();
    }

    private bool PathsReady()
    {
        return !string.IsNullOrWhiteSpace(_settings.SourceIniPath) && File.Exists(_settings.SourceIniPath)
               && !string.IsNullOrWhiteSpace(_settings.RepoRootPath) && Directory.Exists(_settings.RepoRootPath);
    }

    private void UpdateButtonStates()
    {
        _checkChangesButton.Enabled = PathsReady() && !_isBusy;
        if (!_checkChangesButton.Enabled)
        {
            _updateButton.Enabled = false;
        }

        _openConfigMenuItem.Enabled = !_isBusy && CanOpenConfig();
        _sideImageMenuItem.Enabled = !_isBusy;
        _preferencesButton.Enabled = !_isBusy && CanOpenPreference();
    }

    private bool RepoRootReady()
    {
        return !string.IsNullOrWhiteSpace(_settings.RepoRootPath) && Directory.Exists(_settings.RepoRootPath);
    }

    private bool CanOpenConfig()
    {
        // Allow opening app config even before user selects any paths.
        return File.Exists(SettingsStore.GetConfigPath());
    }

    private bool CanOpenPreference()
    {
        // Enabled when website repo root is assigned.
        return RepoRootReady();
    }

    private void OnOpenConfig(object? sender, EventArgs e)
    {
        try
        {
            var configPath = SettingsStore.GetConfigPath();
            if (!File.Exists(configPath))
            {
                MessageBox.Show(this, "config.json not found yet.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateButtonStates();
                return;
            }

            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{configPath}\"")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open config location: {ex.Message}", "Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog(this);
    }

    private void OnGithub(object? sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/yeftakun/lap-updater")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to open link: {ex.Message}", "Github", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<CommandResult> RunCommandAsync(string fileName, string arguments)
    {
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = _settings.RepoRootPath ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                lock (outputLines)
                {
                    outputLines.Add(args.Data);
                }
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                lock (errorLines)
                {
                    errorLines.Add(args.Data);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        return new CommandResult(
            string.Join(Environment.NewLine, outputLines),
            string.Join(Environment.NewLine, errorLines),
            process.ExitCode);
    }

    private void AppendCommandResult(CommandResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            AppendOutput(result.Output);
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            AppendOutput(result.Error);
        }

        AppendOutput($"Exit code: {result.ExitCode}");
        AppendOutput(string.Empty);
    }

    private void AppendOutput(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendOutput), message);
            return;
        }

        _outputBox.AppendText(message + Environment.NewLine);
        _outputBox.ScrollToCaret();
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string, Color>(SetStatus), text, color);
            return;
        }

        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private void ApplyStoredStatus()
    {
        switch (_settings.LastPushStatus)
        {
            case PushStatus.Success:
                SetStatus("Changes sent", Color.Green);
                break;
            case PushStatus.Failure:
                SetStatus("Terdapat kesalahan!", Color.DarkOrange);
                break;
            default:
                SetStatus("An error occurred!", SystemColors.ControlText);
                break;
        }
    }

    private void OnClearConsole(object? sender, EventArgs e)
    {
        _outputBox.Clear();
    }

    private async Task<bool> EnsureInternetAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://www.google.com/generate_204");
            var response = await HttpClient.SendAsync(request);
            var ok = response.IsSuccessStatusCode;
            if (!ok)
            {
                throw new HttpRequestException($"Status code {(int)response.StatusCode}");
            }

            return true;
        }
        catch
        {
            MessageBox.Show(this, "No Internet Connection", "Network", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void OnOpenPreference(object? sender, EventArgs e)
    {
        if (!RepoRootReady())
        {
            MessageBox.Show(this, "Select a valid repository root folder first.", "Missing folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new PreferenceForm(_settings.RepoRootPath!);
        form.ShowDialog(this);

        if (form.WasSaved)
        {
            SetStatus("Preference saved!", Color.Green);
        }
    }
}

internal sealed record AppSettings
{
    [JsonPropertyName("sourceIniPath")]
    public string? SourceIniPath { get; set; } = string.Empty;

    [JsonPropertyName("repoRootPath")]
    public string? RepoRootPath { get; set; } = string.Empty;

    [JsonPropertyName("sideImageFileName")]
    public string? SideImageFileName { get; set; } = "shiroko-vert.bmp";

    [JsonPropertyName("sideImageWidth")]
    public int? SideImageWidth { get; set; } = 224;

    [JsonPropertyName("sideImageBasedOnPicture")]
    public bool SideImageBasedOnPicture { get; set; } = true;

    [JsonPropertyName("lastPushStatus")]
    public PushStatus LastPushStatus { get; set; } = PushStatus.None;
}

internal enum PushStatus
{
    None,
    Success,
    Failure
}

internal static class SettingsStore
{
    private const string SettingsFileName = "config.json";

    public static string GetConfigPath()
    {
        return GetSettingsPath();
    }

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                var legacyPath = GetLegacySettingsPath();
                if (!File.Exists(legacyPath))
                {
                    // First-run / cleared data: create a fresh config with defaults.
                    var defaults = new AppSettings();
                    Save(defaults);
                    return defaults;
                }

                var legacyJson = File.ReadAllText(legacyPath);
                var legacySettings = JsonSerializer.Deserialize<AppSettings>(legacyJson) ?? new AppSettings();

                // Fill newer fields if legacy config didn't contain them.
                legacySettings.SourceIniPath ??= string.Empty;
                legacySettings.RepoRootPath ??= string.Empty;
                legacySettings.SideImageFileName ??= "shiroko-vert.bmp";
                legacySettings.SideImageWidth ??= 224;

                // Best-effort migration: write to the new location.
                Save(legacySettings);
                return legacySettings;
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
            // Swallow errors to avoid crashing UI when saving settings.
        }
    }

    private static string GetSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "LapUpdater", SettingsFileName);
    }

    private static string GetLegacySettingsPath()
    {
        // Previous versions stored settings alongside the executable.
        return Path.Combine(AppContext.BaseDirectory, "settings.json");
    }
}

internal sealed record CommandResult(string Output, string Error, int ExitCode);
