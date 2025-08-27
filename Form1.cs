using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Deadlock_Mod_Loader2
{
    public partial class Form1 : Form
    {
        private ModLoader modLoader = new ModLoader();
        private bool _handlingDrop;

        private Panel sidebarPanel;
        private Panel contentPanel;
        private List<SidebarTabButton> tabButtons = new List<SidebarTabButton>();
        private string currentTabName = "Mods";

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel modCountLabel;
        private ToolStripStatusLabel gamePathLabel;
        private ToolStripProgressBar progressBar;

        private Panel globalProgressPanel;
        private Label globalProgressLabel;
        private ProgressBar globalProgressBar;
        private ModsTabBuilder modsTabBuilder;
        private GameSetupTabBuilder gameSetupTabBuilder;
        private ProfilesTabBuilder profilesTabBuilder;

        private Dictionary<Control, OriginalFontInfo> originalFonts = new Dictionary<Control, OriginalFontInfo>();

        public Form1()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            ApplyTextRendering(this);
        }

        public class OriginalFontInfo
        {
            public Font Font { get; set; }
            public string ControlName { get; set; }

            public OriginalFontInfo(Font font, string controlName)
            {
                Font = new Font(font.FontFamily, font.Size, font.Style);
                ControlName = controlName;
            }
        }
        private void InitializeComponent()
        {
            try
            {
                SuspendLayout();

                SetupFormProperties();

                AnimationSystem.LoadAnimationSettings();

                CreateMainLayout();
                CreateSidebar();
                CreateStatusBar();
                CreateGlobalProgress();
                CreateAllTabPanels();
                AddEventHandlers();
                ApplyEnhancedStyles();

                ShowTab("Mods");

                ResumeLayout(false);
                PerformLayout();

                LoadSavedPath();
                LoadSavedBackground();
                UpdateStatusInfo();


                this.Load += async (s, e) =>
                {
                    CheckGameSetupOnStartup();

                    RefreshModLists();
                    await Task.Delay(2000);
                    CheckForUpdatesOnStartup();
                };


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating form: {ex.Message}", "Initialization Error");
            }
        }
        private void InitializeThemeSystem()
        {
            try
            {
                string savedTheme = Properties.Settings.Default.SelectedTheme ?? "Default Dark";
                ThemeManager.SetTheme(savedTheme, this);
                foreach (var button in tabButtons)
                {
                    ThemeManager.UpdateSidebarTabButton(button, button.IsSelected);
                }

                ApplyThemeToModLists();
            }
            catch
            {
                ThemeManager.SetTheme("Default Dark", this);
            }
        }
        private void SetupFormProperties()
        {
            this.AllowDrop = true;
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ClientSize = new Size(1000, 650);
            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "Deadlock Mod Manager V1.4";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 650);

            try { this.Icon = new Icon("icon.ico"); } catch { }
        }

        private void CreateMainLayout()
        {
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(37, 37, 38),
                BorderStyle = BorderStyle.FixedSingle
            };

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebarPanel);
        }
        private async void CheckForUpdatesOnStartup()
        {
            try
            {
                Console.WriteLine("[DEBUG] Starting startup update check...");

                bool autoCheck = Properties.Settings.Default.AutoCheckUpdates;
                Console.WriteLine($"[DEBUG] Auto-check enabled: {autoCheck}");
                if (!autoCheck)
                {
                    Console.WriteLine("[DEBUG] Auto-check disabled, skipping");
                    return;
                }
                string lastCheckStr = Properties.Settings.Default.LastUpdateCheck;
                Console.WriteLine($"[DEBUG] Last check string: '{lastCheckStr}'");

                if (!string.IsNullOrEmpty(lastCheckStr) && DateTime.TryParse(lastCheckStr, out DateTime lastCheck))
                {
                    var timeSinceCheck = DateTime.Now - lastCheck;
                    Console.WriteLine($"[DEBUG] Time since last check: {timeSinceCheck.TotalHours:F1} hours");

                    if (timeSinceCheck < TimeSpan.FromHours(24))
                    {
                        Console.WriteLine("[DEBUG] Skipping update check - checked recently");
                        return; // Checked recently
                    }
                }

                Console.WriteLine("[DEBUG] Performing startup update check...");

                var updateInfo = await UpdateChecker.CheckForUpdatesAsync();
                Console.WriteLine($"[DEBUG] Update check completed. Is update available: {updateInfo?.IsUpdateAvailable}");

                Properties.Settings.Default.LastUpdateCheck = DateTime.Now.ToString();
                Properties.Settings.Default.Save();
                Console.WriteLine("[DEBUG] Saved last check time");

                if (updateInfo != null && updateInfo.IsUpdateAvailable)
                {
                    Console.WriteLine($"[DEBUG] Update available: {updateInfo.LatestVersion}");
                    Console.WriteLine("[DEBUG] Showing update notification...");
                    ShowUpdateNotification(updateInfo);
                }
                else
                {
                    Console.WriteLine("[DEBUG] No updates available");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Startup update check failed: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }
        private void CheckGameSetupOnStartup()
        {
            try
            {
                string savedPath = Properties.Settings.Default.GamePath;
                if (string.IsNullOrEmpty(savedPath) || !Directory.Exists(savedPath))
                {
                    var result = MessageBox.Show(
                        "Welcome to Deadlock Mod Manager!\n\nTo get started, we need to locate your Deadlock game directory. " +
                        "We'll try to find it automatically, or you can browse for it manually.\n\n" +
                        "Click OK to set up your game directory now.",
                        "First Time Setup",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.OK)
                    {
                        ShowTab("Game Setup");

                        TryAutoDetectGamePath();
                    }
                }
                else
                {
                    if (!modLoader.SetGamePath(savedPath))
                    {
                        MessageBox.Show(
                            "Your saved game directory is no longer valid. Please set it up again.",
                            "Game Directory Invalid",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        ShowTab("Game Setup");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Startup game setup check failed: {ex.Message}");
            }
        }
        private void TryAutoDetectGamePath()
        {
            try
            {
                string autoDetectedPath = gameSetupTabBuilder?.FindDeadlockInstallation();

                if (!string.IsNullOrEmpty(autoDetectedPath))
                {
                    var gameSetupPanel = contentPanel.Controls.Find("GameSetupPanel", false).FirstOrDefault();
                    if (gameSetupPanel != null)
                    {
                        var pathTextBox = gameSetupPanel.Controls.Find("gameSetupPathTextBox", true).FirstOrDefault() as TextBox;
                        if (pathTextBox != null)
                        {
                            pathTextBox.Text = autoDetectedPath;
                            pathTextBox.ForeColor = Color.FromArgb(222, 214, 196); // Reset to normal color
                        }
                    }

                    var result = MessageBox.Show(
                        $"Found Deadlock installation at:\n{autoDetectedPath}\n\nWould you like to use this directory?",
                        "Game Directory Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        if (modLoader.SetGamePath(autoDetectedPath))
                        {
                            Properties.Settings.Default.GamePath = autoDetectedPath;
                            Properties.Settings.Default.Save();
                            modLoader.PatchGameInfoFile();

                            MessageBox.Show("Game setup completed successfully!", "Setup Complete",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            ShowTab("Mods");
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Could not automatically detect your Deadlock installation.\n\n" +
                        "Please use the Browse button to locate your game directory manually.\n\n" +
                        "Look for: SteamLibrary\\steamapps\\common\\Deadlock\\game",
                        "Auto-Detection Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-detection failed: {ex.Message}");
                MessageBox.Show(
                    "Auto-detection encountered an error. Please use the Browse button to set your game directory manually.",
                    "Auto-Detection Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        private void ShowUpdateNotification(UpdateInfo updateInfo)
        {
            try
            {
                Console.WriteLine("[DEBUG] Creating notification panel...");

                var notificationPanel = new Panel
                {
                    Height = 35,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(70, 130, 180),
                    Visible = true,
                    Name = "updateNotificationPanel"
                };

                var messageLabel = new Label
                {
                    Text = $"Version {updateInfo.LatestVersion} is available!",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Location = new Point(10, 8),
                    AutoSize = true
                };

                var updateButton = new Button
                {
                    Text = "Download",
                    Size = new Size(80, 25),
                    Location = new Point(notificationPanel.Width - 170, 5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(50, 100, 150),
                    ForeColor = Color.White,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Font = new Font("Segoe UI", 8F)
                };
                updateButton.FlatAppearance.BorderSize = 0;

                var dismissButton = new Button
                {
                    Text = "✕",
                    Size = new Size(25, 25),
                    Location = new Point(notificationPanel.Width - 35, 5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(50, 100, 150),
                    ForeColor = Color.White,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };
                dismissButton.FlatAppearance.BorderSize = 0;

                updateButton.Click += (s, e) =>
                {
                    Console.WriteLine("[DEBUG] Download button clicked");
                    try
                    {
                        System.Diagnostics.Process.Start("https://github.com/Tylevo/DeadlockModManager/releases");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Failed to open browser: {ex.Message}");
                    }
                    this.Controls.Remove(notificationPanel);
                    notificationPanel.Dispose();
                };

                dismissButton.Click += (s, e) =>
                {
                    Console.WriteLine("[DEBUG] Dismiss button clicked");
                    this.Controls.Remove(notificationPanel);
                    notificationPanel.Dispose();
                };

                notificationPanel.Resize += (s, e) =>
                {
                    updateButton.Location = new Point(notificationPanel.Width - 170, 5);
                    dismissButton.Location = new Point(notificationPanel.Width - 35, 5);
                };

                notificationPanel.Controls.AddRange(new Control[] { messageLabel, updateButton, dismissButton });

                Console.WriteLine("[DEBUG] Adding notification panel to form...");

                this.Controls.Add(notificationPanel);
                notificationPanel.BringToFront();

                Console.WriteLine($"[DEBUG] Notification panel added. Form has {this.Controls.Count} controls");

                var timer = new Timer { Interval = 15000 };
                timer.Tick += (s, e) =>
                {
                    Console.WriteLine("[DEBUG] Auto-dismissing notification panel");
                    if (this.Controls.Contains(notificationPanel))
                    {
                        this.Controls.Remove(notificationPanel);
                        notificationPanel.Dispose();
                    }
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();

                Console.WriteLine("[DEBUG] Notification panel setup complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error showing notification: {ex.Message}");
                Console.WriteLine($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }
        private void CreateSidebar()
        {
            var sidebarHeader = new Panel
            {
                Height = 70,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var titleLabel = new Label
            {
                Text = "DEADLOCK\nMOD MANAGER",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            sidebarHeader.Controls.Add(titleLabel);

            var sidebarFooter = new Panel
            {
                Height = 28, // FIXED: Match status strip height
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var footerLabel = new Label
            {
                Text = "Made by Tylevo",
                Font = new Font("Segoe UI", 7F),
                ForeColor = Color.FromArgb(120, 120, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            sidebarFooter.Controls.Add(footerLabel);

            var tabContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            CreateTabButtons(tabContainer);

            sidebarPanel.Controls.Add(tabContainer);
            sidebarPanel.Controls.Add(sidebarHeader);
            sidebarPanel.Controls.Add(sidebarFooter);
        }
        private void LoadSavedBackground()
        {
            try
            {
                string savedBackgroundPath = Properties.Settings.Default.BackgroundImagePath;
                if (!string.IsNullOrEmpty(savedBackgroundPath) && File.Exists(savedBackgroundPath))
                {

                }
                else
                {
                    InitializeThemeSystem();
                }
            }
            catch
            {
                InitializeThemeSystem();
            }
        }
        private void CreateTabButtons(Panel tabContainer)
        {
            var tabs = new[]
            {
        new { Name = "Mods", Icon = "M" },
        new { Name = "Browse Mods", Icon = "B" },
        new { Name = "Game Setup", Icon = "G" },
        new { Name = "Profiles", Icon = "P" },
        new { Name = "Settings", Icon = "S" }
    };

            int yPos = 5;
            foreach (var tab in tabs)
            {
                var tabButton = new SidebarTabButton(tab.Name, tab.Icon)
                {
                    Location = new Point(3, yPos),
                    Width = 194
                };

                tabButton.TabClicked += (s, e) => ShowTab(tabButton.TabName);


                tabButtons.Add(tabButton);
                tabContainer.Controls.Add(tabButton);
                yPos += 50;
            }
        }
        private void CreateStatusBar()
        {
            var colorTable = new DarkColorTable();

            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(37, 37, 38),
                ForeColor = Color.FromArgb(222, 214, 196),
                SizingGrip = true,
                Renderer = new ToolStripProfessionalRenderer(colorTable),
                Height = 28 // FIXED: Set consistent height
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Ready",
                ForeColor = Color.FromArgb(222, 214, 196),
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            modCountLabel = new ToolStripStatusLabel
            {
                Text = "Active: 0 | Available: 0",
                ForeColor = Color.FromArgb(180, 180, 180),
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            gamePathLabel = new ToolStripStatusLabel
            {
                Text = "No game path set",
                ForeColor = Color.FromArgb(200, 100, 100),
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            progressBar = new ToolStripProgressBar
            {
                Visible = false,
                Size = new Size(120, 20), // FIXED: Slightly larger to fit in 28px height
                Style = ProgressBarStyle.Continuous
            };

            statusStrip.Items.AddRange(new ToolStripItem[] {
        statusLabel, modCountLabel, gamePathLabel, progressBar
    });

            this.Controls.Add(statusStrip);
        }
        private void CreateGlobalProgress()
        {
            globalProgressPanel = new Panel
            {
                Height = 28, // FIXED: Match status strip height
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(37, 37, 38),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            globalProgressLabel = new Label
            {
                Text = "Downloading...",
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(15, 6), // FIXED: Adjusted for smaller height
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) // FIXED: Slightly smaller font
            };

            globalProgressBar = new ProgressBar
            {
                Location = new Point(150, 4), // FIXED: Adjusted for smaller height
                Size = new Size(300, 20),
                Style = ProgressBarStyle.Continuous,
                MarqueeAnimationSpeed = 30
            };

            var cancelDownloadButton = new Button
            {
                Text = "Cancel",
                Location = new Point(460, 3), // FIXED: Adjusted for smaller height
                Size = new Size(60, 22), // FIXED: Adjusted for smaller height
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 60, 60),
                ForeColor = Color.FromArgb(222, 214, 196),
                Font = new Font("Segoe UI", 7.5F), // FIXED: Slightly smaller font
                Visible = false,
                Name = "cancelDownloadButton"
            };
            cancelDownloadButton.FlatAppearance.BorderSize = 0;

            globalProgressPanel.Controls.AddRange(new Control[] {
        globalProgressLabel, globalProgressBar, cancelDownloadButton
    });

            globalProgressPanel.Resize += (s, e) =>
            {
                int centerX = (globalProgressPanel.Width - globalProgressBar.Width) / 2;
                globalProgressBar.Location = new Point(Math.Max(150, centerX), 4);
                cancelDownloadButton.Location = new Point(globalProgressBar.Right + 10, 3);
            };

            this.Controls.Add(globalProgressPanel);
        }
        private void CreateAllTabPanels()
        {
            modsTabBuilder = new ModsTabBuilder(contentPanel, modLoader);
            gameSetupTabBuilder = new GameSetupTabBuilder(contentPanel, modLoader);
            profilesTabBuilder = new ProfilesTabBuilder(contentPanel, modLoader);

            modsTabBuilder.CreateModsTab();
            CreateBrowseTabPanel();
            gameSetupTabBuilder.CreateGameSetupTab();
            profilesTabBuilder.CreateProfilesTab();
            CreateSettingsTabPanel();
        }

        private void CreateBrowseTabPanel()
        {
            var browsePanel = new Panel
            {
                Name = "BrowsePanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false
            };

            var browser = new GameBananaBrowser(modLoader, this)
            {
                Dock = DockStyle.Fill
            };

            browser.OnModInstalled += () =>
            {
                if (currentTabName == "Mods")
                {
                    RefreshModLists();
                }
                UpdateStatusInfo();
            };

            browsePanel.Controls.Add(browser);
            contentPanel.Controls.Add(browsePanel);
        }
        private void CreateSettingsTabPanel()
        {
            var settingsPanel = new Panel
            {
                Name = "SettingsPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false,
                AutoScroll = true
            };

            var headerLabel = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(20, 20),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = "Configure your preferences and application behavior",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(20, 50),
                AutoSize = true
            };

            var appearanceGroup = new GroupBox
            {
                Text = "Appearance Settings",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 90),
                Size = new Size(660, 90)
            };

            var themeLabel = new Label
            {
                Text = "Theme:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(15, 30),
                AutoSize = true
            };

            var themeCombo = new ComboBox
            {
                Location = new Point(15, 50),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(222, 214, 196),
                FlatStyle = FlatStyle.Flat
            };

            foreach (var theme in ThemeManager.GetAvailableThemes())
            {
                themeCombo.Items.Add(theme.Name);
            }
            themeCombo.Text = ThemeManager.GetCurrentTheme().Name;

            var themeDescLabel = new Label
            {
                Text = ThemeManager.GetCurrentTheme().Description,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(180, 55),
                Size = new Size(280, 20)
            };

            appearanceGroup.Controls.AddRange(new Control[] {
        themeLabel, themeCombo, themeDescLabel
    });

            var advancedGroup = new GroupBox
            {
                Text = "Advanced Settings",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 200), // Moved up from 340
                Size = new Size(660, 120)
            };

            var autoBackupCheck = new CheckBox
            {
                Text = "Auto-backup mod configuration",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(15, 30),
                Size = new Size(220, 20),
                Checked = true
            };

            var validateModsCheck = new CheckBox
            {
                Text = "Validate mods on startup",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(250, 30),
                Size = new Size(180, 20),
                Checked = false
            };

            var highContrastCheck = new CheckBox
            {
                Text = "High contrast mode",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(15, 60),
                Size = new Size(160, 20),
                Checked = false
            };

            var clearCacheButton = new Button
            {
                Text = "Clear Cache",
                Size = new Size(100, 25),
                Location = new Point(450, 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                Visible = false
            };
            clearCacheButton.FlatAppearance.BorderSize = 0;

            var enableDebugCheck = new CheckBox
            {
                Text = "Enable debug logging",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(450, 90),
                Size = new Size(160, 20),
                Checked = false,
                Visible = false
            };

            advancedGroup.Controls.AddRange(new Control[] {
        autoBackupCheck, validateModsCheck, highContrastCheck, clearCacheButton, enableDebugCheck
    });
            var updateGroup = new GroupBox
            {
                Text = "Updates & Community",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 340), // Moved up from 480
                Size = new Size(660, 120)
            };

            var checkUpdatesButton = new Button
            {
                Text = "Check for Updates",
                Size = new Size(130, 30),
                Location = new Point(15, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 120, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            checkUpdatesButton.FlatAppearance.BorderSize = 0;

            var githubButton = new Button
            {
                Text = "View on GitHub",
                Size = new Size(130, 30),
                Location = new Point(160, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };
            githubButton.FlatAppearance.BorderSize = 0;

            var gameBananaButton = new Button
            {
                Text = "GameBanana Page",
                Size = new Size(130, 30),
                Location = new Point(305, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F)
            };
            gameBananaButton.FlatAppearance.BorderSize = 0;

            var updateStatusLabel = new Label
            {
                Text = "Click 'Check for Updates' to see if a newer version is available",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(15, 70),
                Size = new Size(400, 20)
            };

            updateGroup.Controls.AddRange(new Control[] {
        checkUpdatesButton, githubButton, gameBananaButton, updateStatusLabel
    });

            var appInfoGroup = new GroupBox
            {
                Text = "Application Information",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 480), // Moved up from 620
                Size = new Size(660, 80)
            };

            var versionLabel = new Label
            {
                Text = "Deadlock Mod Manager v1.4",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(15, 30),
                AutoSize = true
            };

            var authorLabel = new Label
            {
                Text = "Created by Tylevo • Updates available on GitHub",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(15, 50),
                AutoSize = true
            };

            appInfoGroup.Controls.AddRange(new Control[] { versionLabel, authorLabel });

            themeCombo.SelectedIndexChanged += (s, e) =>
            {
                try
                {
                    ThemeManager.SetTheme(themeCombo.Text, this);
                    themeDescLabel.Text = ThemeManager.GetCurrentTheme().Description;

                    Properties.Settings.Default.SelectedTheme = themeCombo.Text;
                    Properties.Settings.Default.Save();

                    RefreshSidebarColors();
                    if (currentTabName == "Browse Mods")
                    {
                        var browsePanel = contentPanel.Controls.Find("BrowsePanel", false).FirstOrDefault();
                        if (browsePanel != null)
                        {
                            var gameBananaBrowser = browsePanel.Controls.OfType<GameBananaBrowser>().FirstOrDefault();
                            if (gameBananaBrowser != null)
                            {
                                gameBananaBrowser.ApplyTheme();

                                Application.DoEvents();

                                browsePanel.Invalidate(true);
                                browsePanel.Update();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
                }
            };

            checkUpdatesButton.Click += async (s, e) =>
            {
                try
                {
                    checkUpdatesButton.Enabled = false;
                    checkUpdatesButton.Text = "Checking...";
                    updateStatusLabel.Text = "Checking for updates...";

                    var updateInfo = await UpdateChecker.CheckForUpdatesAsync();

                    if (updateInfo.IsUpdateAvailable)
                    {
                        updateStatusLabel.Text = $"Update available: v{updateInfo.LatestVersion}";
                        updateStatusLabel.ForeColor = Color.FromArgb(100, 200, 100);

                        var result = MessageBox.Show(
                            $"A new version (v{updateInfo.LatestVersion}) is available!\n\n{updateInfo.Message}\n\nWould you like to download it?",
                            "Update Available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(updateInfo.DownloadUrl);
                        }
                    }
                    else
                    {
                        updateStatusLabel.Text = "You have the latest version";
                        updateStatusLabel.ForeColor = Color.FromArgb(140, 140, 140);
                    }
                }
                catch (Exception ex)
                {
                    updateStatusLabel.Text = "Failed to check for updates";
                    updateStatusLabel.ForeColor = Color.FromArgb(200, 100, 100);
                    System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                }
                finally
                {
                    checkUpdatesButton.Enabled = true;
                    checkUpdatesButton.Text = "Check for Updates";
                }
            };

            githubButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://github.com/Tylevo/DeadlockModManager");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open GitHub: {ex.Message}");
                }
            };

            gameBananaButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://gamebanana.com/tools/20525");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open GameBanana: {ex.Message}");
                }
            };

            clearCacheButton.Click += (s, e) =>
            {
                try
                {
                    ClearModCache();
                    MessageBox.Show("Cache cleared successfully!", "Cache Cleared",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing cache: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            enableDebugCheck.CheckedChanged += (s, e) =>
            {
                try
                {
                    Properties.Settings.Default.EnableDebugLogging = enableDebugCheck.Checked;
                    Properties.Settings.Default.Save();
                }
                catch { }
            };

            highContrastCheck.CheckedChanged += (s, e) =>
            {
                try
                {
                    if (highContrastCheck.Checked)
                    {
                        ThemeManager.SetTheme("High Contrast Dark", this);
                        themeCombo.Text = "High Contrast Dark";
                    }
                    Properties.Settings.Default.HighContrast = highContrastCheck.Checked;
                    Properties.Settings.Default.Save();
                    RefreshSidebarColors();
                }
                catch { }
            };

            autoBackupCheck.CheckedChanged += (s, e) =>
            {
                try
                {
                    Properties.Settings.Default.AutoBackup = autoBackupCheck.Checked;
                    Properties.Settings.Default.Save();
                }
                catch { }
            };

            validateModsCheck.CheckedChanged += (s, e) =>
            {
                try
                {
                    Properties.Settings.Default.ValidateOnStartup = validateModsCheck.Checked;
                    Properties.Settings.Default.Save();
                }
                catch { }
            };

            try
            {
                themeCombo.Text = Properties.Settings.Default.SelectedTheme ?? "Default Dark";
                enableDebugCheck.Checked = Properties.Settings.Default.EnableDebugLogging;
                highContrastCheck.Checked = Properties.Settings.Default.HighContrast;
                autoBackupCheck.Checked = Properties.Settings.Default.AutoBackup;
                validateModsCheck.Checked = Properties.Settings.Default.ValidateOnStartup;
            }
            catch
            {
                themeCombo.SelectedIndex = 0;
            }

            settingsPanel.Controls.AddRange(new Control[] {
        headerLabel, subtitleLabel, appearanceGroup, advancedGroup, updateGroup, appInfoGroup
    });

            contentPanel.Controls.Add(settingsPanel);
        }

        private void RefreshSidebarColors()
        {
            try
            {
                foreach (var tabButton in tabButtons)
                {
                    if (tabButton is SidebarTabButton sidebarButton)
                    {
                        sidebarButton.ApplyTheme(); // This method already exists in your SidebarTabButton class
                    }
                }

                var sidebarPanel = Controls.Find("sidebarPanel", true).FirstOrDefault();
                if (sidebarPanel != null)
                {
                    sidebarPanel.Invalidate();
                    sidebarPanel.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshSidebarColors error: {ex.Message}");
            }
        }
        private void InitializeAnimations()
        {
            AnimationSystem.AnimationsEnabled = true;
            try
            {
                Properties.Settings.Default.EnableAnimations = true;
                Properties.Settings.Default.Save();
            }
            catch
            {
            }
        }

        private void StoreOriginalFonts(Control control)
        {
            try
            {
                if (control.Font != null && !originalFonts.ContainsKey(control))
                {
                    originalFonts[control] = new OriginalFontInfo(control.Font, control.Name ?? control.GetType().Name);
                }

                foreach (Control child in control.Controls)
                {
                    StoreOriginalFonts(child);
                }
            }
            catch
            {
            }
        }

        private void ClearModCache()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                var deadlockTempDirs = Directory.GetDirectories(tempPath, "DeadlockModLoader_*");

                int deletedCount = 0;
                foreach (var dir in deadlockTempDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }

                var modCheckDirs = Directory.GetDirectories(tempPath, "ModCheck_*");
                foreach (var dir in modCheckDirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }

                if (deletedCount == 0)
                {
                    throw new Exception("No cache files found to clear.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to clear cache: " + ex.Message);
            }
        }

        private void ApplyCardSizeSettings()
        {
            if (currentTabName == "Browse Mods")
            {
                var browsePanel = contentPanel.Controls.Find("BrowsePanel", false).FirstOrDefault();
                if (browsePanel != null)
                {
                    var gameBananaBrowser = browsePanel.Controls.OfType<GameBananaBrowser>().FirstOrDefault();
                    if (gameBananaBrowser != null)
                    {
                        string cardSize = "Normal";
                        try { cardSize = Properties.Settings.Default.CardSize ?? "Normal"; } catch { }
                        gameBananaBrowser.ApplyCardSizeSettings(cardSize);
                    }
                }
            }
        }


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (currentTabName == "Browse Mods")
            {
                switch (keyData)
                {
                    case Keys.Control | Keys.F:
                        var browsePanel = contentPanel.Controls.Find("BrowsePanel", false).FirstOrDefault();
                        var browser = browsePanel?.Controls.OfType<GameBananaBrowser>().FirstOrDefault();
                        browser?.searchBox?.Focus();
                        return true;

                    case Keys.F5:
                        var browsePanel2 = contentPanel.Controls.Find("BrowsePanel", false).FirstOrDefault();
                        var browser2 = browsePanel2?.Controls.OfType<GameBananaBrowser>().FirstOrDefault();
                        browser2?.RefreshModList();
                        return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate(); // Repaint to show focus state
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate(); // Repaint to remove focus state
        }
        private void ShowTab(string tabName)
        {
            foreach (var button in tabButtons)
            {
                bool isSelected = button.TabName == tabName;
                button.SetSelected(isSelected);
                ThemeManager.UpdateSidebarTabButton(button, isSelected);
            }


            string panelName = tabName == "Browse Mods" ? "BrowsePanel" : tabName.Replace(" ", "") + "Panel";

            foreach (Control control in contentPanel.Controls)
            {
                if (control is Panel panel)
                {
                    panel.Visible = panel.Name == panelName;
                    if (panel.Visible)
                    {
                        panel.BringToFront();
                        currentTabName = tabName;
                    }
                }
            }

            switch (tabName)
            {
                case "Mods":
                    RefreshModLists();
                    break;
                case "Browse Mods":
                    HandleBrowseTabSwitch();
                    break;
                case "Profiles":
                    profilesTabBuilder?.RefreshProfiles();
                    break;
            }
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                foreach (var button in tabButtons)
                {
                    ThemeManager.UnregisterSidebarTabButton(button);
                }

                var modsPanel = contentPanel.Controls.Find("ModsPanel", false).FirstOrDefault();
                if (modsPanel != null)
                {
                    var listBoxes = modsPanel.Controls.OfType<FileDropModListBox>();
                    foreach (var listBox in listBoxes)
                    {
                        ThemeManager.UnregisterEnhancedListBox(listBox);
                    }
                }

                NotificationManager.Cleanup();
            }
            catch
            {
            }

            base.OnFormClosed(e);
        }
        private void ApplyThemeToModLists()
        {
            try
            {
                var modsPanel = contentPanel.Controls.Find("ModsPanel", false).FirstOrDefault();
                if (modsPanel != null)
                {
                    foreach (Control control in modsPanel.Controls)
                    {
                        if (control is FileDropModListBox listBox)
                        {
                            var currentTheme = ThemeManager.GetCurrentTheme();
                            if (currentTheme?.Colors != null)
                            {
                                listBox.UpdateTheme(currentTheme.Colors);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyThemeToModLists error: {ex.Message}");
            }
        }
        private void HandleBrowseTabSwitch()
        {
            try
            {
                var browsePanel = contentPanel.Controls.Find("BrowsePanel", false).FirstOrDefault();
                if (browsePanel == null) return;

                var browser = browsePanel.Controls.OfType<GameBananaBrowser>().FirstOrDefault();
                if (browser == null) return;

                browser.ApplyTheme();

                bool autoRefresh = false;
                try
                {
                    autoRefresh = Properties.Settings.Default.AutoRefreshBrowser;
                }
                catch { }

                if (autoRefresh || !browser.HasModsLoaded())
                {
                    browser.RefreshDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HandleBrowseTabSwitch error: {ex.Message}");
            }
        }

        private DateTime lastThemeChange = DateTime.MinValue;
        private const int THEME_CHANGE_DEBOUNCE_MS = 1000;
        private DateTime lastThemeApply = DateTime.MinValue;
        private void AddEventHandlers()
        {
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;
            this.Paint += Form1_Paint;
            this.KeyDown += Form1_KeyDown;

            modsTabBuilder?.AddEventHandlers();
            gameSetupTabBuilder?.AddEventHandlers();
            profilesTabBuilder?.AddEventHandlers();
        }

        private void ApplyEnhancedStyles()
        {
            ThemeManager.ApplyTheme(this);

            ApplyThemeToModLists();

            foreach (var button in tabButtons)
            {
                ThemeManager.UpdateSidebarTabButton(button, button.TabName == currentTabName);
            }
        }

        private void ApplyStyleToControl(Control control, Color logoBeige, Color darkBackground, Color buttonBackground)
        {
            if (control is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = logoBeige;
                if (btn.BackColor == Color.FromArgb(63, 63, 70) || btn.BackColor == Color.FromArgb(120, 60, 60))
                {
                }
                else
                {
                    btn.BackColor = buttonBackground;
                }
                btn.ForeColor = logoBeige;
            }
            else if (control is TextBox tb)
            {
                if (tb.BackColor != Color.FromArgb(45, 45, 48) && tb.BackColor != Color.FromArgb(60, 60, 60))
                {
                    tb.BackColor = Color.FromArgb(60, 60, 60);
                }
                tb.ForeColor = logoBeige;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox cb)
            {
                cb.BackColor = Color.FromArgb(60, 60, 60);
                cb.ForeColor = logoBeige;
                cb.FlatStyle = FlatStyle.Flat;
            }
            else if (control is GroupBox gb)
            {
                gb.ForeColor = logoBeige;
            }

            if (control.HasChildren)
            {
                foreach (Control child in control.Controls)
                {
                    ApplyStyleToControl(child, logoBeige, darkBackground, buttonBackground);
                }
            }
        }

        #region Global Progress Methods
        public void ShowGlobalProgress(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ShowGlobalProgress(message)));
                return;
            }
            globalProgressLabel.Text = message;
            globalProgressPanel.Visible = true;
            globalProgressPanel.BringToFront();
        }
        private static void ApplyTextRendering(Control root)
        {
            void Walk(Control c)
            {
                var prop = c.GetType().GetProperty("UseCompatibleTextRendering");
                if (prop != null && prop.CanWrite) prop.SetValue(c, false, null); // use GDI (crisper)
                foreach (Control ch in c.Controls) Walk(ch);
            }
            Walk(root);
        }

        internal static void EnableDoubleBuffer(Control c)
        {
            try
            {
                var prop = typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                prop?.SetValue(c, true, null);
            }
            catch { }
        }
        public void UpdateGlobalProgress(int percentage)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateGlobalProgress(percentage)));
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[FORM] Setting progress to {percentage}%");
            globalProgressBar.Value = Math.Min(100, Math.Max(0, percentage));
        }

        public void HideGlobalProgress()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HideGlobalProgress()));
                return;
            }
            globalProgressPanel.Visible = false;
        }
        #endregion

        #region Status and Progress Methods
        private void UpdateStatusInfo()
        {
            try
            {
                if (statusLabel != null) statusLabel.Text = "Ready";

                if (modCountLabel != null)
                {
                    var active = modLoader.GetActiveMods().Count;
                    var available = modLoader.GetAvailableMods().Count;
                    modCountLabel.Text = $"Active: {active} | Available: {available}";
                }

                var savedPath = "";
                try { savedPath = Properties.Settings.Default.GamePath; } catch { }

                if (gamePathLabel != null)
                {
                    if (string.IsNullOrEmpty(savedPath) || !Directory.Exists(savedPath))
                    {
                        gamePathLabel.Text = "No game path set";
                        gamePathLabel.ForeColor = Color.FromArgb(200, 100, 100);
                    }
                    else
                    {
                        gamePathLabel.Text = $"Game: {Path.GetFileName(savedPath)}";
                        gamePathLabel.ForeColor = Color.FromArgb(100, 200, 100);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void ShowProgress(bool show, string message = "")
        {
            if (progressBar != null)
            {
                progressBar.Visible = show;
                if (!show) progressBar.Value = 0;
            }

            if (statusLabel != null && !string.IsNullOrEmpty(message))
            {
                statusLabel.Text = message;
            }
        }

        private void UpdateProgress(int percentage)
        {
            if (progressBar != null)
            {
                progressBar.Value = Math.Min(100, Math.Max(0, percentage));
            }
        }
        #endregion

        #region Data Refresh Methods
        private void RefreshModLists()
        {
            modsTabBuilder?.RefreshModLists();
            UpdateStatusInfo();
        }

        private void LoadSavedPath()
        {
            try
            {
                string savedPath = Properties.Settings.Default.GamePath;
                if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                {
                    if (modLoader.SetGamePath(savedPath))
                    {
                        UpdateStatusInfo();
                    }
                    gameSetupTabBuilder?.LoadCurrentGamePath();
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Event Handlers
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var currentTheme = ThemeManager.GetCurrentTheme();
            using (var brush = new SolidBrush(currentTheme.Colors.PrimaryBackground))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (_handlingDrop) return;
            _handlingDrop = true;

            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int installedCount = 0;

                ShowGlobalProgress("Preparing installation...");
                AnimationSystem.AnimateProgressBar(globalProgressBar, 10, 200);

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    string extension = Path.GetExtension(file).ToLowerInvariant();

                    if (extension == ".zip" || extension == ".rar" || extension == ".7z" || extension == ".vpk")
                    {
                        globalProgressLabel.Text = $"Installing {Path.GetFileName(file)}...";

                        int targetProgress = 20 + (i * 70 / files.Length);
                        AnimationSystem.AnimateProgressBar(globalProgressBar, targetProgress, 150);

                        if (modLoader.InstallDroppedFile(file, this))
                        {
                            installedCount++;

                            if (AnimationSystem.AnimationsEnabled)
                            {
                                globalProgressLabel.Text = $"✓ Installed {Path.GetFileName(file)}";
                                await Task.Delay(300);
                            }
                        }
                        else
                        {
                            globalProgressLabel.Text = $"✗ Failed to install {Path.GetFileName(file)}";
                            await Task.Delay(500);
                        }
                    }
                }

                globalProgressLabel.Text = "Finalizing...";
                AnimationSystem.AnimateProgressBar(globalProgressBar, 100, 200);

                HideGlobalProgress();

                if (installedCount > 0)
                {
                    AnimationSystem.ShowOperationComplete(this,
                        $"{installedCount} mod(s) installed successfully!", true);

                    if (currentTabName == "Mods")
                    {
                        RefreshModLists();
                    }
                }
                else
                {
                    AnimationSystem.ShowOperationComplete(this,
                        "No mods were installed", false);
                }
            }
            catch (Exception ex)
            {
                HideGlobalProgress();
                AnimationSystem.ShowOperationComplete(this,
                    $"Installation failed: {ex.Message}", false);
            }
            finally
            {
                _handlingDrop = false;
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.R)
            {
                if (currentTabName == "Mods")
                {
                    RefreshModLists();
                }
                e.Handled = true;
            }
        }

        #endregion
    }
}