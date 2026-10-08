using System.Diagnostics;
using USBSilentCopy.Configuration;
using USBSilentCopy.Core;
using USBSilentCopy.Infrastructure;

namespace USBSilentCopy.UI;

/// <summary>
/// فرم اصلی داشبورد، مدیریت تنظیمات، مشاهده تاریخچه و لاگ‌ها
/// </summary>
public sealed class MainDashboardForm : Form
{
    private readonly AppCoordinator _coordinator;

    // تب‌ها
    private TabControl _tabControl = null!;
    private TabPage _tabDashboard = null!;
    private TabPage _tabSettings = null!;
    private TabPage _tabHistory = null!;
    private TabPage _tabLogs = null!;

    // عناصر داشبورد
    private Label _lblStatus = null!;
    private Label _lblCurrentDevice = null!;
    private Label _lblCurrentFile = null!;
    private Label _lblStats = null!;
    private ProgressBar _progressBar = null!;
    private Button _btnPauseResume = null!;
    private Button _btnCancelAll = null!;
    private Button _btnScanNow = null!;
    private Button _btnOpenBackup = null!;

    // عناصر تنظیمات
    private TextBox _txtDest = null!;
    private Button _btnBrowseDest = null!;
    private CheckBox _chkSecondary = null!;
    private TextBox _txtSecondaryDest = null!;
    private Button _btnBrowseSecDest = null!;
    private CheckBox _chkSilent = null!;
    private CheckBox _chkStartup = null!;
    private NumericUpDown _numDelay = null!;
    private NumericUpDown _numSpeedLimit = null!;
    private NumericUpDown _numMinFreeSpace = null!;
    private NumericUpDown _numRetryCount = null!;
    private NumericUpDown _numRetryDelay = null!;
    private ComboBox _cmbCopyMode = null!;
    private ComboBox _cmbExistingPolicy = null!;
    private CheckBox _chkCompareHash = null!;
    private CheckBox _chkCopyEmptyDirs = null!;
    private TextBox _txtExcludeFolders = null!;
    private TextBox _txtExcludeExts = null!;
    private TextBox _txtIncludeExts = null!;
    private Button _btnSaveConfig = null!;
    private Button _btnResetConfig = null!;

    // عناصر تاریخچه
    private DataGridView _gridHistory = null!;
    private TextBox _txtHistorySearch = null!;
    private Button _btnSearchHistory = null!;
    private Button _btnExportCsv = null!;
    private Button _btnExportJson = null!;
    private Button _btnClearHistory = null!;

    // عناصر لاگ
    private TextBox _txtLogs = null!;
    private Button _btnRefreshLogs = null!;
    private Button _btnOpenLogsFolder = null!;

    public MainDashboardForm(AppCoordinator coordinator)
    {
        _coordinator = coordinator;

        InitializeComponent();
        BindCoordinatorEvents();
        LoadCurrentConfigToUi();
        LoadHistoryGrid();
        LoadRecentLogs();
    }

    private void InitializeComponent()
    {
        Text = $"{Constants.ApplicationName} - پشتیبان‌گیری خودکار USB (نسخه {Constants.ApplicationVersion})";
        Size = new Size(860, 620);
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        Icon = IconHelper.GetAppIcon();

        _tabControl = new TabControl { Dock = DockStyle.Fill };
        _tabDashboard = new TabPage("داشبورد زنده");
        _tabSettings = new TabPage("تنظیمات برنامه");
        _tabHistory = new TabPage("سوابق پشتیبان‌گیری");
        _tabLogs = new TabPage("گزارش‌ها (Logs)");

        _tabControl.TabPages.AddRange(new[] { _tabDashboard, _tabSettings, _tabHistory, _tabLogs });
        Controls.Add(_tabControl);

        BuildDashboardTab();
        BuildSettingsTab();
        BuildHistoryTab();
        BuildLogsTab();
    }

    private void BuildDashboardTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var grpStatus = new GroupBox
        {
            Text = "وضعیت پشتیبان‌گیری زنده",
            Location = new Point(20, 20),
            Size = new Size(780, 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblStatus = new Label
        {
            Text = "وضعیت: آماده‌باش - منتظر اتصال حافظه USB",
            Location = new Point(20, 30),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkSlateBlue
        };

        _lblCurrentDevice = new Label
        {
            Text = "دستگاه فعلی: هیچ دستگاهی در حال پردازش نیست",
            Location = new Point(20, 65),
            AutoSize = true
        };

        _lblCurrentFile = new Label
        {
            Text = "فایل در حال انتقال: -",
            Location = new Point(20, 95),
            Size = new Size(740, 25),
            AutoEllipsis = true
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 130),
            Size = new Size(740, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };

        _lblStats = new Label
        {
            Text = "پیشرفت: ۰٪ | سرعت: ۰.۰ MB/s | حجم انتقال: ۰ MB",
            Location = new Point(20, 170),
            AutoSize = true,
            ForeColor = Color.DimGray
        };

        grpStatus.Controls.AddRange(new Control[] { _lblStatus, _lblCurrentDevice, _lblCurrentFile, _progressBar, _lblStats });

        var flowButtons = new FlowLayoutPanel
        {
            Location = new Point(20, 300),
            Size = new Size(780, 50),
            FlowDirection = FlowDirection.RightToLeft
        };

        _btnPauseResume = new Button
        {
            Text = "توقف موقت (Pause)",
            Size = new Size(160, 38),
            BackColor = Color.FromArgb(235, 240, 248)
        };
        _btnPauseResume.Click += (s, e) => _coordinator.BackupQueue.TogglePause();

        _btnCancelAll = new Button
        {
            Text = "لغو تمام وظایف",
            Size = new Size(140, 38),
            BackColor = Color.FromArgb(254, 238, 238)
        };
        _btnCancelAll.Click += (s, e) => _coordinator.BackupQueue.CancelAll();

        _btnScanNow = new Button
        {
            Text = "بررسی مجدد درایوها",
            Size = new Size(150, 38)
        };
        _btnScanNow.Click += (s, e) => _coordinator.DeviceDetector.CheckConnectedDrivesImmediately();

        _btnOpenBackup = new Button
        {
            Text = "باز کردن پوشه Backup",
            Size = new Size(160, 38)
        };
        _btnOpenBackup.Click += (s, e) => OpenDirectory(_coordinator.ConfigManager.Current.DestinationDirectory);

        flowButtons.Controls.AddRange(new Control[] { _btnPauseResume, _btnCancelAll, _btnScanNow, _btnOpenBackup });

        var grpInfo = new GroupBox
        {
            Text = "راهنمای کلیدهای میانبر و عملکرد",
            Location = new Point(20, 370),
            Size = new Size(780, 130),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        var lblHelp = new Label
        {
            Text = "• کلید میانبر سراسری ویندوز: Ctrl + O + P برای توقف موقت / ادامه سریع و خاموش پشتیبان‌گیری.\n" +
                   "• حالت سایلنت (Silent Mode): اجرای مخفی بدون آیکون سینی و اعلان، صرفاً در پس‌زمینه ویندوز.\n" +
                   "• پشتیبان‌گیری افزایشی (Incremental): فقط فایل‌های تغییریافته و جدید کپی می‌شوند.",
            Location = new Point(15, 30),
            Size = new Size(750, 80),
            ForeColor = Color.DarkSlateGray
        };
        grpInfo.Controls.Add(lblHelp);

        panel.Controls.AddRange(new Control[] { grpStatus, flowButtons, grpInfo });
        _tabDashboard.Controls.Add(panel);
    }

    private void BuildSettingsTab()
    {
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        var y = 15;

        // مقصد اصلی
        var lblDest = new Label { Text = "پوشه مقصد اصلی (Destination):", Location = new Point(20, y), AutoSize = true };
        _txtDest = new TextBox { Location = new Point(20, y + 25), Size = new Size(620, 27) };
        _btnBrowseDest = new Button { Text = "انتخاب...", Location = new Point(650, y + 24), Size = new Size(90, 29) };
        _btnBrowseDest.Click += (s, e) => BrowseFolder(_txtDest);
        scrollPanel.Controls.AddRange(new Control[] { lblDest, _txtDest, _btnBrowseDest });
        y += 65;

        // مقصد ثانویه
        _chkSecondary = new CheckBox { Text = "فعال‌سازی مقصد ثانویه (Secondary Destination):", Location = new Point(20, y), AutoSize = true };
        _txtSecondaryDest = new TextBox { Location = new Point(20, y + 25), Size = new Size(620, 27) };
        _btnBrowseSecDest = new Button { Text = "انتخاب...", Location = new Point(650, y + 24), Size = new Size(90, 29) };
        _btnBrowseSecDest.Click += (s, e) => BrowseFolder(_txtSecondaryDest);
        scrollPanel.Controls.AddRange(new Control[] { _chkSecondary, _txtSecondaryDest, _btnBrowseSecDest });
        y += 65;

        // گزینه‌های رفتاری
        _chkSilent = new CheckBox { Text = "حالت خاموش (Silent Mode - بدون آیکون سینی و اعلان)", Location = new Point(20, y), AutoSize = true };
        _chkStartup = new CheckBox { Text = "اجرا با شروع ویندوز کاربر فعلی (Startup)", Location = new Point(420, y), AutoSize = true };
        scrollPanel.Controls.AddRange(new Control[] { _chkSilent, _chkStartup });
        y += 40;

        // تأخیر و سرعت
        var lblDelay = new Label { Text = "تأخیر پس از اتصال USB (ثانیه):", Location = new Point(20, y), AutoSize = true };
        _numDelay = new NumericUpDown { Location = new Point(20, y + 25), Size = new Size(140, 27), Minimum = 0, Maximum = 60 };

        var lblSpeed = new Label { Text = "محدودیت سرعت (MB/s - ۰=نامحدود):", Location = new Point(220, y), AutoSize = true };
        _numSpeedLimit = new NumericUpDown { Location = new Point(220, y + 25), Size = new Size(180, 27), Minimum = 0, Maximum = 1000 };

        var lblMinFree = new Label { Text = "حداقل فضای خالی مجاز دیسک (MB):", Location = new Point(460, y), AutoSize = true };
        _numMinFreeSpace = new NumericUpDown { Location = new Point(460, y + 25), Size = new Size(180, 27), Minimum = 10, Maximum = 100000 };

        scrollPanel.Controls.AddRange(new Control[] { lblDelay, _numDelay, lblSpeed, _numSpeedLimit, lblMinFree, _numMinFreeSpace });
        y += 65;

        // حالت کپی و سیاست فایل‌های موجود
        var lblMode = new Label { Text = "حالت انتقال:", Location = new Point(20, y), AutoSize = true };
        _cmbCopyMode = new ComboBox { Location = new Point(20, y + 25), Size = new Size(160, 27), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbCopyMode.Items.AddRange(new object[] { "Copy (کپی امن)", "Move (انتقال)" });

        var lblPolicy = new Label { Text = "سیاست فایل موجود:", Location = new Point(220, y), AutoSize = true };
        _cmbExistingPolicy = new ComboBox { Location = new Point(220, y + 25), Size = new Size(200, 27), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbExistingPolicy.Items.AddRange(new object[] { "ReplaceIfNewer (جایگزینی در صورت جدیدتر بودن)", "Skip (صرف‌نظر)", "Overwrite (همیشه بازنویسی)" });

        _chkCompareHash = new CheckBox { Text = "مقایسه هش فایل‌ها (Hash SHA256)", Location = new Point(460, y + 25), AutoSize = true };

        scrollPanel.Controls.AddRange(new Control[] { lblMode, _cmbCopyMode, lblPolicy, _cmbExistingPolicy, _chkCompareHash });
        y += 65;

        // تلاش مجدد و پوشه‌های خالی
        var lblRetry = new Label { Text = "تعداد تلاش مجدد (Retry Count):", Location = new Point(20, y), AutoSize = true };
        _numRetryCount = new NumericUpDown { Location = new Point(20, y + 25), Size = new Size(140, 27), Minimum = 0, Maximum = 20 };

        var lblRetryDelay = new Label { Text = "تأخیر تلاش مجدد (ثانیه):", Location = new Point(190, y), AutoSize = true };
        _numRetryDelay = new NumericUpDown { Location = new Point(190, y + 25), Size = new Size(140, 27), Minimum = 1, Maximum = 120 };

        _chkCopyEmptyDirs = new CheckBox { Text = "کپی ساختار پوشه‌های خالی", Location = new Point(370, y + 25), AutoSize = true };

        scrollPanel.Controls.AddRange(new Control[] { lblRetry, _numRetryCount, lblRetryDelay, _numRetryDelay, _chkCopyEmptyDirs });
        y += 65;

        // پوشه‌ها و پسوندها
        var lblExFolders = new Label { Text = "پوشه‌های مستثنی (با کاما جدا کنید):", Location = new Point(20, y), AutoSize = true };
        _txtExcludeFolders = new TextBox { Location = new Point(20, y + 25), Size = new Size(720, 27) };
        scrollPanel.Controls.AddRange(new Control[] { lblExFolders, _txtExcludeFolders });
        y += 60;

        var lblExExt = new Label { Text = "پسوندهای مستثنی (مانند .tmp, .log):", Location = new Point(20, y), AutoSize = true };
        _txtExcludeExts = new TextBox { Location = new Point(20, y + 25), Size = new Size(350, 27) };

        var lblInExt = new Label { Text = "فقط این پسوندها کپی شوند (اختیاری):", Location = new Point(390, y), AutoSize = true };
        _txtIncludeExts = new TextBox { Location = new Point(390, y + 25), Size = new Size(350, 27) };

        scrollPanel.Controls.AddRange(new Control[] { lblExExt, _txtExcludeExts, lblInExt, _txtIncludeExts });
        y += 65;

        // دکمه‌های ذخیره
        _btnSaveConfig = new Button { Text = "ذخیره تنظیمات", Location = new Point(20, y), Size = new Size(150, 38), BackColor = Color.FromArgb(220, 245, 220) };
        _btnSaveConfig.Click += (s, e) => SaveUiConfig();

        _btnResetConfig = new Button { Text = "بازنشانی به پیش‌فرض", Location = new Point(190, y), Size = new Size(160, 38) };
        _btnResetConfig.Click += (s, e) => ResetConfig();

        scrollPanel.Controls.AddRange(new Control[] { _btnSaveConfig, _btnResetConfig });

        _tabSettings.Controls.Add(scrollPanel);
    }

    private void BuildHistoryTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        _txtHistorySearch = new TextBox { Location = new Point(10, 8), Size = new Size(250, 27), PlaceholderText = "جستجو در تاریخچه..." };
        _btnSearchHistory = new Button { Text = "جستجو", Location = new Point(270, 7), Size = new Size(80, 29) };
        _btnSearchHistory.Click += (s, e) => LoadHistoryGrid(_txtHistorySearch.Text);

        _btnExportCsv = new Button { Text = "خروجی CSV", Location = new Point(460, 7), Size = new Size(100, 29) };
        _btnExportCsv.Click += (s, e) => ExportHistory(true);

        _btnExportJson = new Button { Text = "خروجی JSON", Location = new Point(570, 7), Size = new Size(100, 29) };
        _btnExportJson.Click += (s, e) => ExportHistory(false);

        _btnClearHistory = new Button { Text = "پاکسازی تاریخچه", Location = new Point(680, 7), Size = new Size(110, 29) };
        _btnClearHistory.Click += (s, e) => ClearHistory();

        topPanel.Controls.AddRange(new Control[] { _txtHistorySearch, _btnSearchHistory, _btnExportCsv, _btnExportJson, _btnClearHistory });

        _gridHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White
        };

        _gridHistory.Columns.Add("SessionId", "شناسه سشن");
        _gridHistory.Columns.Add("Drive", "درایو");
        _gridHistory.Columns.Add("Volume", "نام فلش");
        _gridHistory.Columns.Add("Files", "فایل‌های کپی‌شده");
        _gridHistory.Columns.Add("Bytes", "حجم کپی");
        _gridHistory.Columns.Add("Speed", "میانگین سرعت");
        _gridHistory.Columns.Add("Status", "وضعیت");
        _gridHistory.Columns.Add("Date", "تاریخ و زمان");

        panel.Controls.Add(_gridHistory);
        panel.Controls.Add(topPanel);
        _tabHistory.Controls.Add(panel);
    }

    private void BuildLogsTab()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        _btnRefreshLogs = new Button { Text = "بازخوانی لاگ", Location = new Point(10, 8), Size = new Size(110, 29) };
        _btnRefreshLogs.Click += (s, e) => LoadRecentLogs();

        _btnOpenLogsFolder = new Button { Text = "باز کردن پوشه لاگ‌ها", Location = new Point(130, 8), Size = new Size(150, 29) };
        _btnOpenLogsFolder.Click += (s, e) => OpenDirectory(_coordinator.ConfigManager.Current.LogsDirectory);

        topPanel.Controls.AddRange(new Control[] { _btnRefreshLogs, _btnOpenLogsFolder });

        _txtLogs = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            BackColor = Color.FromArgb(28, 30, 34),
            ForeColor = Color.FromArgb(230, 235, 240),
            Font = new Font("Consolas", 9.5F, FontStyle.Regular),
            WordWrap = false
        };

        panel.Controls.Add(_txtLogs);
        panel.Controls.Add(topPanel);
        _tabLogs.Controls.Add(panel);
    }

    private void BindCoordinatorEvents()
    {
        _coordinator.BackupQueue.ItemStarted += item =>
        {
            InvokeSafe(() =>
            {
                _lblStatus.Text = $"وضعیت: در حال پشتیبان‌گیری از {item.Device.DisplayName}...";
                _lblStatus.ForeColor = Color.RoyalBlue;
                _lblCurrentDevice.Text = $"دستگاه: {item.Device.DisplayName} | سریال: {item.Device.SerialNumber}";
                _progressBar.Value = 0;
            });
        };

        _coordinator.BackupQueue.ItemProgress += info =>
        {
            InvokeSafe(() =>
            {
                _lblCurrentFile.Text = $"فایل ({info.CurrentFileIndex}/{info.TotalFiles}): {info.CurrentFile}";
                _progressBar.Value = (int)Math.Min(100, Math.Max(0, info.OverallPercent));
                _lblStats.Text = $"پیشرفت: {info.OverallPercent:N1}٪ | سرعت: {info.SpeedMBps:N2} MB/s | حجم: {info.BytesTransferred / (1024 * 1024):N1} MB";
            });
        };

        _coordinator.BackupQueue.ItemCompleted += (item, result) =>
        {
            InvokeSafe(() =>
            {
                _lblStatus.Text = result.Status == BackupStatus.Completed
                    ? $"وضعیت: پشتیبان‌گیری {item.Device.DisplayName} با موفقیت پایان یافت."
                    : $"وضعیت: پشتیبان‌گیری با وضعیت {result.Status} خاتمه یافت.";
                _lblStatus.ForeColor = result.Status == BackupStatus.Completed ? Color.DarkGreen : Color.Crimson;
                _progressBar.Value = 100;
                LoadHistoryGrid();
            });
        };

        _coordinator.BackupQueue.PauseStateChanged += isPaused =>
        {
            InvokeSafe(() =>
            {
                _btnPauseResume.Text = isPaused ? "ادامه عملیات (Resume)" : "توقف موقت (Pause)";
                _lblStatus.Text = isPaused ? "وضعیت: متوقف موقت (Paused)" : "وضعیت: در حال اجرا";
            });
        };

        _coordinator.LogManager.LogMessageReceived += line =>
        {
            InvokeSafe(() =>
            {
                if (_txtLogs.Lines.Length > 2000)
                {
                    _txtLogs.Clear();
                }
                _txtLogs.AppendText(line + Environment.NewLine);
            });
        };
    }

    private void LoadCurrentConfigToUi()
    {
        var cfg = _coordinator.ConfigManager.Current;
        _txtDest.Text = cfg.DestinationDirectory;
        _chkSecondary.Checked = cfg.EnableSecondaryDestination;
        _txtSecondaryDest.Text = cfg.SecondaryDestination;
        _chkSilent.Checked = cfg.SilentMode;
        _chkStartup.Checked = cfg.StartupCurrentUser;
        _numDelay.Value = cfg.DelayAfterUsbConnectionSeconds;
        _numSpeedLimit.Value = cfg.SpeedLimitMBps;
        _numMinFreeSpace.Value = cfg.MinimumFreeSpaceMB;
        _cmbCopyMode.SelectedIndex = cfg.CopyMode == BackupMode.Move ? 1 : 0;
        _cmbExistingPolicy.SelectedIndex = cfg.ExistingFilePolicy switch
        {
            ExistingFilePolicy.Skip => 1,
            ExistingFilePolicy.Overwrite => 2,
            _ => 0
        };
        _chkCompareHash.Checked = cfg.CompareHash;
        _numRetryCount.Value = cfg.RetryCount;
        _numRetryDelay.Value = cfg.RetryDelaySeconds;
        _chkCopyEmptyDirs.Checked = cfg.CopyEmptyDirectories;
        _txtExcludeFolders.Text = string.Join(", ", cfg.Filters.ExcludeFolders);
        _txtExcludeExts.Text = string.Join(", ", cfg.Filters.ExcludeExtensions);
        _txtIncludeExts.Text = string.Join(", ", cfg.Filters.IncludeExtensions);
    }

    private void SaveUiConfig()
    {
        var cfg = _coordinator.ConfigManager.Current;
        cfg.DestinationDirectory = _txtDest.Text.Trim();
        cfg.EnableSecondaryDestination = _chkSecondary.Checked;
        cfg.SecondaryDestination = _txtSecondaryDest.Text.Trim();
        cfg.SilentMode = _chkSilent.Checked;
        cfg.StartupCurrentUser = _chkStartup.Checked;
        cfg.DelayAfterUsbConnectionSeconds = (int)_numDelay.Value;
        cfg.SpeedLimitMBps = (int)_numSpeedLimit.Value;
        cfg.MinimumFreeSpaceMB = (long)_numMinFreeSpace.Value;
        cfg.RetryCount = (int)_numRetryCount.Value;
        cfg.RetryDelaySeconds = (int)_numRetryDelay.Value;
        cfg.CopyEmptyDirectories = _chkCopyEmptyDirs.Checked;
        cfg.CopyMode = _cmbCopyMode.SelectedIndex == 1 ? BackupMode.Move : BackupMode.Copy;
        cfg.ExistingFilePolicy = _cmbExistingPolicy.SelectedIndex switch
        {
            1 => ExistingFilePolicy.Skip,
            2 => ExistingFilePolicy.Overwrite,
            _ => ExistingFilePolicy.ReplaceIfNewer
        };
        cfg.CompareHash = _chkCompareHash.Checked;

        cfg.Filters.ExcludeFolders = _txtExcludeFolders.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        cfg.Filters.ExcludeExtensions = _txtExcludeExts.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        cfg.Filters.IncludeExtensions = _txtIncludeExts.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (cfg.StartupCurrentUser)
        {
            StartupManager.SetStartupCurrentUser(true);
        }
        else
        {
            StartupManager.SetStartupCurrentUser(false);
        }

        var saved = _coordinator.ConfigManager.Save(cfg);
        if (saved)
        {
            MessageBox.Show(this, "تنظیمات با موفقیت ذخیره شد.", "تأیید", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(this, "خطا در اعتبارسنجی یا ذخیره تنظیمات.", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResetConfig()
    {
        if (MessageBox.Show(this, "آیا مایل به بازنشانی تمام تنظیمات به حالت اولیه هستید؟", "تأیید بازنشانی", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _coordinator.ConfigManager.ResetToDefault();
            LoadCurrentConfigToUi();
            MessageBox.Show(this, "تنظیمات به حالت پیش‌فرض بازگردانده شد.", "پیام", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void LoadHistoryGrid(string? search = null)
    {
        _gridHistory.Rows.Clear();
        var sessions = _coordinator.HistoryManager.GetSessions(search);
        foreach (var s in sessions)
        {
            var bytesMb = $"{s.CopiedBytes / (1024.0 * 1024.0):N1} MB";
            var speed = $"{s.AverageSpeedMBps:N2} MB/s";
            _gridHistory.Rows.Add(s.SessionId, s.DriveLetter, s.DisplayName, $"{s.CopiedFiles}/{s.TotalFiles}", bytesMb, speed, s.Status.ToString(), s.StartTime.ToString("yyyy-MM-dd HH:mm"));
        }
    }

    private void ExportHistory(bool csv)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = csv ? "CSV File (*.csv)|*.csv" : "JSON File (*.json)|*.json",
            FileName = csv ? "backup_history.csv" : "backup_history.json"
        };

        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            var ok = csv ? _coordinator.HistoryManager.ExportSessionsToCsv(sfd.FileName) : _coordinator.HistoryManager.ExportSessionsToJson(sfd.FileName);
            if (ok)
            {
                MessageBox.Show(this, "خروجی تاریخچه با موفقیت ذخیره شد.", "پیام", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void ClearHistory()
    {
        if (MessageBox.Show(this, "آیا از پاک کردن سوابق تاریخچه اطمینان دارید؟", "پاکسازی", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            _coordinator.HistoryManager.ClearHistory();
            LoadHistoryGrid();
        }
    }

    private void LoadRecentLogs()
    {
        var lines = _coordinator.LogManager.ReadRecentLogs("application.log", 200);
        _txtLogs.Text = string.Join(Environment.NewLine, lines);
        _txtLogs.SelectionStart = _txtLogs.TextLength;
        _txtLogs.ScrollToCaret();
    }

    private void BrowseFolder(TextBox target)
    {
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = fbd.SelectedPath;
        }
    }

    private void OpenDirectory(string path)
    {
        try
        {
            var resolved = _coordinator.ConfigManager.ResolveAbsolutePath(path);
            if (!Directory.Exists(resolved))
            {
                Directory.CreateDirectory(resolved);
            }
            Process.Start(new ProcessStartInfo { FileName = resolved, UseShellExecute = true });
        }
        catch { }
    }

    private void InvokeSafe(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            BeginInvoke(action);
        }
        else
        {
            action();
        }
    }
}
