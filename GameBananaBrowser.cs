using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Deadlock_Mod_Loader2.SearchFilters;

namespace Deadlock_Mod_Loader2
{
    public class GameBananaBrowser : UserControl
    {
        private readonly ModLoader modLoader;
        private readonly Form1 mainForm;
        private static readonly HttpClient http = new HttpClient();

        private const int DEADLOCK_GAME_ID = 20948;
        private const string APIV11_ROOT = "https://gamebanana.com/apiv11/Mod";

        private List<GameBananaSubmission> currentMods = new List<GameBananaSubmission>();
        private bool isLoading = false;
        private int currentPage = 1;
        private string currentSearch = "";
        private string currentSort = "Newest";
        private bool pendingSearch;
        private string queuedSearchText = "";
        private string queuedSort = "Newest";
        private const string SearchPlaceholder = "Search mods...";
        private bool suppressSearchEvents = false;

        private Queue<QueuedMod> downloadQueue = new Queue<QueuedMod>();
        private bool isProcessingQueue = false;
        private Label queueStatusLabel;

        private class QueuedMod
        {
            public GameBananaSubmission Mod { get; set; }
            public GameBananaFile File { get; set; }
        }

        private Panel mainContainer;
        private Panel headerPanel;
        private Panel sidebarPanel;
        private Panel contentPanel;
        private FlowLayoutPanel modsFlow;
        private Panel statusPanel;
        private Label statusLabel;
        private ProgressBar loadingProgress;

        public TextBox searchBox;
        private ComboBox sortCombo;
        private ComboBox categoryCombo;
        private ComboBox characterCombo;
        private Button refreshButton;
        private SearchFilters currentFilters = new SearchFilters();

        public event Action OnModInstalled;

        static GameBananaBrowser()
        {
            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
            }
            catch { }

            http.DefaultRequestHeaders.UserAgent.Clear();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36 Deadlock-Mod-Manager/1.4");
            http.Timeout = TimeSpan.FromSeconds(30);
        }

        public GameBananaBrowser(ModLoader loader, Form1 parentForm = null)
        {
            modLoader = loader;
            mainForm = parentForm;
            InitializeComponent();

            this.Load += (s, e) =>
            {
                ApplyTheme();
                var _ = LoadModsAsync();
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Dock = DockStyle.Fill;

            CreateModernLayout();
            this.ResumeLayout(false);
        }

        private void CreateModernLayout()
        {
            mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48)
            };

            headerPanel = new Panel
            {
                BackColor = Color.FromArgb(40, 40, 43),
                Bounds = new Rectangle(0, 0, this.Width, 60)
            };

            var titleLabel = new Label
            {
                Text = "Browse Mods",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240),
                Location = new Point(20, 12),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = "Discover and install mods from GameBanana",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(20, 35),
                AutoSize = true
            };

            headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });

            statusPanel = new Panel
            {
                BackColor = Color.FromArgb(37, 37, 38),
                Bounds = new Rectangle(0, this.Height - 28, this.Width, 28)
            };

            statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(15, 6),
                AutoSize = true
            };

            loadingProgress = new ProgressBar
            {
                Size = new Size(120, 16),
                Location = new Point(statusPanel.Width - 135, 6),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            statusPanel.Controls.AddRange(new Control[] { statusLabel, loadingProgress });
            sidebarPanel = new Panel
            {
                BackColor = Color.FromArgb(37, 37, 38),
                Bounds = new Rectangle(this.Width - 270, 60, 270, this.Height - 88),
                Padding = new Padding(15, 15, 25, 15), // More padding on right to avoid scrollbar
                AutoScroll = true
            };

            CreateRefinedSidebarControls();
            contentPanel = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Bounds = new Rectangle(0, 60, this.Width - 270, this.Height - 88),
                Padding = new Padding(15, 12, 8, 12)
            };

            modsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 12, 8)
            };

            modsFlow.Scroll += OnModsFlowScroll;
            contentPanel.Controls.Add(modsFlow);

            mainContainer.Controls.AddRange(new Control[] {
        headerPanel, sidebarPanel, contentPanel, statusPanel
    });

            this.Controls.Add(mainContainer);
            this.Resize += GameBananaBrowser_Resize;
        }
        private void GameBananaBrowser_Resize(object sender, EventArgs e)
        {
            if (headerPanel != null)
                headerPanel.Bounds = new Rectangle(0, 0, this.Width, 60);

            if (statusPanel != null)
            {
                statusPanel.Bounds = new Rectangle(0, this.Height - 28, this.Width, 28);
                loadingProgress.Location = new Point(statusPanel.Width - 135, 6);
            }

            if (sidebarPanel != null)
                sidebarPanel.Bounds = new Rectangle(this.Width - 270, 60, 270, this.Height - 88);

            if (contentPanel != null)
                contentPanel.Bounds = new Rectangle(0, 60, this.Width - 270, this.Height - 88);

            AdjustCardWidths();
        }

        private void CreateRefinedSidebarControls()
        {
            int yPos = 5;
            int controlWidth = 230; // Centered width for all controls
            int leftMargin = (270 - 25 - controlWidth) / 2; // Center controls in the 270px wide sidebar (minus right padding)

            var searchLabel = new Label
            {
                Text = "SEARCH",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftMargin, yPos),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(searchLabel);
            yPos += 20;

            searchBox = new TextBox
            {
                Size = new Size(controlWidth, 28),
                Location = new Point(leftMargin, yPos),
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.FromArgb(140, 140, 140),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Text = "Search mods..."
            };

            searchBox.GotFocus += (s, e) =>
            {
                if (searchBox.Text == SearchPlaceholder)
                {
                    suppressSearchEvents = true;
                    searchBox.Text = "";
                    searchBox.ForeColor = Color.FromArgb(222, 214, 196);
                    suppressSearchEvents = false;
                }
            };
            searchBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    suppressSearchEvents = true;
                    searchBox.Text = SearchPlaceholder;
                    searchBox.ForeColor = Color.FromArgb(140, 140, 140);
                    suppressSearchEvents = false;
                }
            };

            var searchTimer = new Timer { Interval = 400 };
            searchTimer.Tick += async (s, e) =>
            {
                searchTimer.Stop();
                await PerformSearchAsync();
            };

            searchBox.TextChanged += (s, e) =>
            {
                if (suppressSearchEvents) return;
                if (searchBox.Focused && searchBox.Text != SearchPlaceholder)
                {
                    searchTimer.Stop();
                    searchTimer.Start();
                }
            };

            searchBox.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await PerformSearchAsync();
                }
            };

            sidebarPanel.Controls.Add(searchBox);
            yPos += 35;

            var categoryLabel = new Label
            {
                Text = "CATEGORY",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftMargin, yPos),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(categoryLabel);
            yPos += 20;

            categoryCombo = new ComboBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.FromArgb(222, 214, 196),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };

            categoryCombo.Items.AddRange(new object[] {
        "All Categories",
        Categories.Skins,
        Categories.GameplayModifications,
        Categories.HUD,
        Categories.ModelReplacement,
        Categories.OtherMisc
    });
            categoryCombo.SelectedIndex = 0;
            categoryCombo.SelectedIndexChanged += async (s, e) =>
            {
                currentFilters.Category = categoryCombo.SelectedIndex == 0 ? null : categoryCombo.SelectedItem.ToString();
                await PerformSearchAsync();
            };

            sidebarPanel.Controls.Add(categoryCombo);
            yPos += 35;

            var characterLabel = new Label
            {
                Text = "CHARACTER",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftMargin, yPos),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(characterLabel);
            yPos += 20;

            characterCombo = new ComboBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.FromArgb(222, 214, 196),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };

            characterCombo.Items.AddRange(new object[] {
        "All Characters",
        Characters.Abrams, Characters.Bebop, Characters.Dynamo,
        Characters.GreyTalon, Characters.Haze, Characters.Infernus,
        Characters.Ivy, Characters.Kelvin, Characters.LadyGeist,
        Characters.Lash, Characters.McGinnis, Characters.Mirage,
        Characters.Mo, Characters.Paradox, Characters.Pocket,
        Characters.Seven, Characters.Shiv, Characters.Vindicta,
        Characters.Viscous, Characters.Warden, Characters.Wraith,
        Characters.Yamato
    });
            characterCombo.SelectedIndex = 0;
            characterCombo.SelectedIndexChanged += async (s, e) =>
            {
                currentFilters.Character = characterCombo.SelectedIndex == 0 ? null : characterCombo.SelectedItem.ToString();
                await PerformSearchAsync();
            };

            sidebarPanel.Controls.Add(characterCombo);
            yPos += 35;

            var sortLabel = new Label
            {
                Text = "SORT BY",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftMargin, yPos),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(sortLabel);
            yPos += 20;

            sortCombo = new ComboBox
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.FromArgb(222, 214, 196),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };

            sortCombo.Items.AddRange(new object[] { "Newest", "Most Liked", "Most Downloaded", "Recently Updated" });
            sortCombo.SelectedIndex = 0;
            sortCombo.SelectedIndexChanged += async (s, e) => await PerformSearchAsync();

            sidebarPanel.Controls.Add(sortCombo);
            yPos += 35;

            var clearFiltersButton = new Button
            {
                Text = "Clear Filters",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 63, 63),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand
            };
            clearFiltersButton.FlatAppearance.BorderSize = 0;
            clearFiltersButton.Click += async (s, e) =>
            {
                categoryCombo.SelectedIndex = 0;
                characterCombo.SelectedIndex = 0;
                currentFilters = new SearchFilters();

                suppressSearchEvents = true;
                searchBox.Text = SearchPlaceholder;
                searchBox.ForeColor = Color.FromArgb(140, 140, 140);
                suppressSearchEvents = false;

                await PerformSearchAsync();
            };

            sidebarPanel.Controls.Add(clearFiltersButton);
            yPos += 35;

            queueStatusLabel = new Label
            {
                Text = "Download Queue: 0 items",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Name = "queueStatusLabel"
            };
            sidebarPanel.Controls.Add(queueStatusLabel);
            yPos += 25;

            var pageLabel = new Label
            {
                Text = "NAVIGATION",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftMargin, yPos),
                AutoSize = true
            };
            sidebarPanel.Controls.Add(pageLabel);
            yPos += 22;

            var navPanel = new Panel
            {
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 30),
                BackColor = Color.Transparent
            };

            var prevButton = new Button
            {
                Text = "← Prev",
                Size = new Size(70, 28),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            prevButton.FlatAppearance.BorderSize = 0;

            var pageInfoLabel = new Label
            {
                Text = $"Page {currentPage}",
                Location = new Point(80, 6),
                Size = new Size(70, 16),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Segoe UI", 8F)
            };

            var nextButton = new Button
            {
                Text = "Next →",
                Size = new Size(70, 28),
                Location = new Point(160, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8F),
                Cursor = Cursors.Hand
            };
            nextButton.FlatAppearance.BorderSize = 0;

            prevButton.Click += async (s, e) =>
            {
                if (currentPage > 1)
                {
                    currentPage--;
                    await LoadModsAsync();
                    UpdateNavigationButtons(prevButton, nextButton, pageInfoLabel);
                }
            };

            nextButton.Click += async (s, e) =>
            {
                currentPage++;
                await LoadModsAsync();
                UpdateNavigationButtons(prevButton, nextButton, pageInfoLabel);
            };

            navPanel.Controls.AddRange(new Control[] { prevButton, pageInfoLabel, nextButton });
            sidebarPanel.Controls.Add(navPanel);
            yPos += 40;

            refreshButton = new Button
            {
                Text = "↻ Refresh",
                Location = new Point(leftMargin, yPos),
                Size = new Size(controlWidth, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += async (s, e) => await RefreshAsync();

            sidebarPanel.Controls.Add(refreshButton);
            yPos += 45;

            var legendAdded = new Label
            {
                AutoSize = false,
                Size = new Size(70, 25),
                Padding = new Padding(6, 4, 6, 4),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 168, 83),
                Text = "＋ Added",
                Location = new Point(leftMargin, yPos),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var legendUpdated = new Label
            {
                AutoSize = false,
                Size = new Size(80, 25),
                Padding = new Padding(6, 4, 6, 4),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(255, 153, 0),
                Text = "↺ Updated",
                Location = new Point(leftMargin + 85, yPos),
                TextAlign = ContentAlignment.MiddleCenter
            };

            sidebarPanel.Controls.Add(legendAdded);
            sidebarPanel.Controls.Add(legendUpdated);

            var helpLabel1 = new Label
            {
                Text = "Tip: Ctrl+Click to queue downloads",
                Location = new Point(leftMargin, yPos + 35),
                Size = new Size(controlWidth, 15),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            sidebarPanel.Controls.Add(helpLabel1);

            var helpLabel2 = new Label
            {
                Text = "Click Details to view more downloads",
                Location = new Point(leftMargin, yPos + 50),
                Size = new Size(controlWidth, 15),
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            sidebarPanel.Controls.Add(helpLabel2);
        }
        public async Task QueueMod(GameBananaSubmission mod)
        {
            try
            {
                var files = await FetchFilesForModAsync(mod.Id);
                var installableFile = files?.FirstOrDefault(ShouldShowFile);

                if (installableFile?.DownloadUrl != null)
                {
                    downloadQueue.Enqueue(new QueuedMod { Mod = mod, File = installableFile });
                    ShowSuccess($"'{mod.Name}' added to download queue ({downloadQueue.Count} items)");
                    UpdateQueueStatus();

                    if (!isProcessingQueue)
                    {
                        _ = ProcessDownloadQueueAsync();
                    }
                }
                else
                {
                    ShowError($"No downloadable files found for '{mod.Name}'");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to queue '{mod.Name}': {ex.Message}");
            }
        }

        private async Task ProcessDownloadQueueAsync()
        {
            if (isProcessingQueue) return;

            isProcessingQueue = true;

            try
            {
                while (downloadQueue.Count > 0)
                {
                    var queuedMod = downloadQueue.Dequeue();
                    UpdateQueueStatus();

                    await DownloadAndInstallAsync(queuedMod.Mod, queuedMod.File);
                    OnModInstalled?.Invoke();

                    await Task.Delay(1000);
                }

                if (mainForm != null)
                {
                    mainForm.HideGlobalProgress();
                }

                ShowSuccess("All queued mods installed successfully!");
            }
            finally
            {
                isProcessingQueue = false;
                UpdateQueueStatus();
            }
        }

        private void UpdateQueueStatus()
        {
            if (queueStatusLabel != null)
            {
                queueStatusLabel.Text = $"Download Queue: {downloadQueue.Count} items";
            }
        }

        private void UpdateNavigationButtons(Button prevButton, Button nextButton, Label pageLabel)
        {
            prevButton.Enabled = currentPage > 1;
            pageLabel.Text = $"Page {currentPage}";

            prevButton.BackColor = prevButton.Enabled ? Color.FromArgb(60, 60, 63) : Color.FromArgb(40, 40, 43);
            prevButton.ForeColor = prevButton.Enabled ? Color.FromArgb(200, 200, 200) : Color.FromArgb(100, 100, 100);
        }

        private void AdjustCardWidths()
        {
            if (modsFlow == null) return;

            var targetWidth = Math.Max(480, contentPanel.Width - 35);
            foreach (Control control in modsFlow.Controls)
            {
                if (control is ModCard card)
                {
                    card.Width = targetWidth;
                }
            }
        }

        private ModCard CreateModCard(GameBananaSubmission mod)
        {
            var initialWidth = Math.Max(480, contentPanel.Width - 35);

            var card = new ModCard(mod, modLoader, mainForm)
            {
                Width = initialWidth,
                Margin = new Padding(0, 0, 0, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.OnInstallRequested += async (modToInstall) =>
            {
                await InstallModAsync(modToInstall);
                OnModInstalled?.Invoke();
            };

            card.OnQueueRequested += async (modToInstall) =>
            {
                await QueueMod(modToInstall);
            };

            card.OnInstallSpecificFileRequested += async (modToInstall, selectedFile) =>
            {
                try
                {
                    ShowLoading($"Installing {modToInstall.Name}…");
                    await DownloadAndInstallAsync(modToInstall, selectedFile);
                    OnModInstalled?.Invoke();
                }
                catch (Exception ex)
                {
                    ShowError($"Installation failed: {ex.Message}");
                }
                finally
                {
                    HideLoading();
                }
            };

            return card;
        }

        private async Task PerformSearchAsync()
        {
            var searchText = (searchBox.Text == SearchPlaceholder || string.IsNullOrWhiteSpace(searchBox.Text)) ? "" : searchBox.Text.Trim();
            var selectedSort = sortCombo.SelectedItem?.ToString() ?? "Newest";

            if (isLoading)
            {
                pendingSearch = true;
                queuedSearchText = searchText;
                queuedSort = selectedSort;
                return;
            }

            if (searchText == currentSearch && selectedSort == currentSort) return;

            try
            {
                isLoading = true;
                ShowLoading(string.IsNullOrEmpty(searchText) ? "Loading mods..." : $"Searching for '{searchText}'...");

                currentSearch = searchText;
                currentSort = selectedSort;
                currentPage = 1;

                List<GameBananaSubmission> mods = null;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    mods = await GameBananaSearchService.FetchModsAsync("", selectedSort, 1, currentFilters);
                }
                else
                {
                    mods = await GameBananaSearchService.SearchWithFallbackStrategiesAsync(searchText, selectedSort, currentFilters);

                    if (mods != null && mods.Count > 0)
                    {
                        mods = GameBananaSearchService.SortSearchResultsByRelevance(mods, searchText);
                    }
                }

                if (mods == null || mods.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(searchText) || currentFilters.Category != null || currentFilters.Character != null)
                    {
                        var filterText = BuildFilterText();
                        currentMods = new List<GameBananaSubmission>
                        {
                            new GameBananaSubmission
                            {
                                Id = -1,
                                Name = $"No results found{filterText}",
                                Description = "Try different keywords, adjust filters, or clear all filters to browse all mods.\n\nNote: Very new mods might not appear in search immediately.",
                                DateAddedTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                                Submitter = new GameBananaUser { Name = "Search Result" }
                            }
                        };
                    }
                    else
                    {
                        var fallback = await GameBananaSearchService.TryRssOrWebFallbackAsync();
                        currentMods = (fallback != null && fallback.Count > 0) ? fallback : GameBananaSearchService.BuildSampleList();
                    }
                }
                else
                {
                    currentMods = mods;
                }

                modsFlow.Controls.Clear();
                PopulateModCards();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError($"Search failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"PerformSearchAsync error: {ex}");
            }
            finally
            {
                isLoading = false;
                HideLoading();

                if (pendingSearch)
                {
                    pendingSearch = false;
                    if (queuedSearchText != currentSearch || queuedSort != currentSort)
                    {
                        await PerformSearchAsync();
                    }
                }
            }
        }

        private string BuildFilterText()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(currentSearch))
                parts.Add($"for '{currentSearch}'");

            if (!string.IsNullOrEmpty(currentFilters.Category))
                parts.Add($"in {currentFilters.Category}");

            if (!string.IsNullOrEmpty(currentFilters.Character))
                parts.Add($"for {currentFilters.Character}");

            return parts.Count > 0 ? " " + string.Join(" ", parts) : "";
        }

        private async Task<List<GameBananaSubmission>> FetchModsAsync(string search, string sort, int page)
        {
            return await GameBananaSearchService.FetchModsAsync(search, sort, page, currentFilters);
        }

        private async Task LoadModsAsync(bool append = false)
        {
            if (isLoading) return;

            try
            {
                isLoading = true;
                ShowLoading("Loading mods...");

                var mods = await FetchModsAsync(currentSearch, currentSort, currentPage);

                if (mods == null || mods.Count == 0)
                {
                    if (!append)
                    {
                        var fallback = await GameBananaSearchService.TryRssOrWebFallbackAsync();
                        mods = (fallback != null && fallback.Count > 0) ? fallback : GameBananaSearchService.BuildSampleList();
                    }
                    else
                    {
                        return;
                    }
                }

                if (append)
                {
                    currentMods.AddRange(mods);
                }
                else
                {
                    currentMods = mods;
                    modsFlow.Controls.Clear();
                }

                PopulateModCards();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load mods: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"LoadModsAsync error: {ex}");
            }
            finally
            {
                isLoading = false;
                HideLoading();

                if (pendingSearch)
                {
                    pendingSearch = false;
                    if (queuedSearchText != currentSearch || queuedSort != currentSort)
                    {
                        currentSearch = queuedSearchText;
                        currentSort = queuedSort;
                        currentPage = 1;
                        await LoadModsAsync();
                    }
                }
            }
        }

        private void PopulateModCards()
        {
            try
            {
                modsFlow.SuspendLayout();

                var startIndex = modsFlow.Controls.Count;
                var newMods = currentMods.Skip(startIndex).ToList();

                foreach (var mod in newMods)
                {
                    if (mod == null) continue;

                    var card = CreateModCard(mod);
                    if (card != null)
                    {
                        modsFlow.Controls.Add(card);
                    }
                }

                modsFlow.ResumeLayout();
                AdjustCardWidths();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PopulateModCards error: {ex}");
                modsFlow.ResumeLayout();
            }
        }

        private async Task RefreshAsync()
        {
            currentPage = 1;
            if (!string.IsNullOrEmpty(currentSearch))
            {
                currentSearch = "";
                suppressSearchEvents = true;
                searchBox.Text = SearchPlaceholder;
                searchBox.ForeColor = Color.FromArgb(140, 140, 140);
                suppressSearchEvents = false;
            }
            await LoadModsAsync();
        }

        public void RefreshModList()
        {
            if (!isLoading)
            {
                var _ = RefreshAsync();
            }
        }

        private async void OnModsFlowScroll(object sender, ScrollEventArgs e)
        {
            if (isLoading || modsFlow.Controls.Count == 0) return;

            var scrollPosition = modsFlow.VerticalScroll.Value;
            var maxScroll = modsFlow.VerticalScroll.Maximum;
            var clientHeight = modsFlow.ClientSize.Height;

            if (scrollPosition + clientHeight >= maxScroll - 100)
            {
                currentPage++;
                await LoadModsAsync(true);
            }
        }

        private async Task InstallModAsync(GameBananaSubmission mod)
        {
            try
            {
                ShowLoading($"Installing {mod.Name}...");

                var files = await FetchFilesForModAsync(mod.Id);
                var installableFile = files?.FirstOrDefault(ShouldShowFile);

                if (installableFile?.DownloadUrl == null)
                {
                    System.Diagnostics.Process.Start($"https://gamebanana.com/mods/{mod.Id}");
                    return;
                }

                await DownloadAndInstallAsync(mod, installableFile);
            }
            catch (Exception ex)
            {
                ShowError($"Installation failed: {ex.Message}");
            }
            finally
            {
                HideLoading();
            }
        }

        private static string BuildVariantInstallName(GameBananaSubmission mod, GameBananaFile file)
        {
            var baseName = System.IO.Path.GetFileNameWithoutExtension(file?.FileName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "file_" + (file?.Id.ToString() ?? Guid.NewGuid().ToString("N").Substring(0, 6));

            baseName = baseName.Replace('_', ' ');
            if (baseName.Length > 60) baseName = baseName.Substring(0, 60);

            return $"{mod.Name} - {baseName}";
        }

        private async Task<List<GameBananaFile>> FetchFilesForModAsync(int modId)
        {
            try
            {
                var url = $"https://gamebanana.com/apiv11/Mod/{modId}/Files";
                using (var response = await http.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var files = JsonConvert.DeserializeObject<GameBananaFile[]>(json);
                        return files?.Where(ShouldShowFile).ToList() ?? new List<GameBananaFile>();
                    }
                }
            }
            catch { }

            return new List<GameBananaFile>();
        }

        private bool ShouldShowFile(GameBananaFile file)
        {
            if (file?.FileName == null) return false;

            var lower = file.FileName.ToLowerInvariant();
            var validExtensions = new[] { ".zip", ".7z", ".rar", ".vpk", ".pak" };
            var invalidPatterns = new[] { "readme", "screenshot", ".txt", ".jpg", ".png", ".gif" };

            return validExtensions.Any(ext => lower.EndsWith(ext)) &&
                   !invalidPatterns.Any(pattern => lower.Contains(pattern));
        }

        private async Task DownloadAndInstallAsync(GameBananaSubmission mod, GameBananaFile file)
        {
            string tempFilePath = null;

            try
            {
                var extension = Path.GetExtension(file.FileName) ?? ".zip";
                tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + extension);

                if (mainForm != null)
                {
                    mainForm.ShowGlobalProgress($"Downloading {mod.Name} ({downloadQueue.Count} remaining)...");
                }

                using (var response = await http.GetAsync(file.DownloadUrl))
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? 0;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fs = System.IO.File.Create(tempFilePath))
                    {
                        var buffer = new byte[8192];
                        long downloadedBytes = 0;
                        int bytesRead;
                        int lastReportedProgress = -1;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            if (totalBytes > 0 && mainForm != null)
                            {
                                var progressPercentage = (int)((downloadedBytes * 100) / totalBytes);

                                if (progressPercentage != lastReportedProgress)
                                {
                                    mainForm.UpdateGlobalProgress(progressPercentage);
                                    lastReportedProgress = progressPercentage;
                                    await Task.Delay(10);
                                }
                            }
                        }
                    }
                }

                if (mainForm != null)
                {
                    mainForm.ShowGlobalProgress($"Installing {mod.Name}...");
                    mainForm.UpdateGlobalProgress(100);
                }

                var variantName = BuildVariantInstallName(mod, file);
                var success = modLoader.InstallDroppedFileWithName(tempFilePath, this.FindForm(), variantName, mod.AuthorName);

                if (success)
                {
                    ShowSuccess($"'{variantName}' installed successfully!");
                }
                else
                {
                    ShowError($"Failed to install '{mod.Name}'");
                }
            }
            finally
            {
                if (mainForm != null && !isProcessingQueue)
                {
                    await Task.Delay(500);
                    mainForm.HideGlobalProgress();
                }

                if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }
            }
        }

        private void ShowLoading(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(ShowLoading), message);
                return;
            }

            statusLabel.Text = message;
            loadingProgress.Visible = true;
        }

        private void HideLoading()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(HideLoading));
                return;
            }

            loadingProgress.Visible = false;
        }

        private void UpdateStatus()
        {
            var filterText = BuildFilterText();

            if (string.IsNullOrEmpty(currentSearch) && currentFilters.Category == null && currentFilters.Character == null)
            {
                statusLabel.Text = $"Showing {currentMods.Count} mods ({currentSort})";
            }
            else
            {
                statusLabel.Text = $"Found {currentMods.Count} results{filterText} (sorted by relevance)";
            }
            statusLabel.ForeColor = Color.FromArgb(200, 200, 200);
        }

        private void ShowError(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = Color.FromArgb(220, 100, 100);
        }

        private void ShowSuccess(string message)
        {
            statusLabel.Text = message;
            statusLabel.ForeColor = Color.FromArgb(100, 220, 100);

            var timer = new Timer { Interval = 3000 };
            timer.Tick += (s, e) =>
            {
                statusLabel.ForeColor = Color.FromArgb(200, 200, 200);
                UpdateStatus();
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        public void ApplyTheme()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ApplyTheme));
                return;
            }

            var theme = ThemeManager.GetCurrentTheme();
            if (theme?.Colors == null) return;

            try
            {
                this.SuspendLayout();

                this.BackColor = theme.Colors.ContentBackground;
                if (mainContainer != null) mainContainer.BackColor = theme.Colors.ContentBackground;
                if (headerPanel != null) headerPanel.BackColor = theme.Colors.SidebarBackground;
                if (sidebarPanel != null) sidebarPanel.BackColor = theme.Colors.SidebarBackground;
                if (statusPanel != null) statusPanel.BackColor = theme.Colors.StatusBarBackground;

                ApplyThemeToSidebarControls(theme);

                foreach (Control control in modsFlow.Controls)
                {
                    if (control is ModCard card)
                    {
                        card.ApplyTheme(theme);
                        card.Invalidate(true);
                        card.Update();
                    }
                }

                modsFlow.Invalidate(true);
                modsFlow.Update();

                RefreshAllPanels();

                this.ResumeLayout(true);
                this.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyTheme error: {ex.Message}");
                this.ResumeLayout(true);
            }
        }

        private void ApplyThemeToSidebarControls(Theme theme)
        {
            foreach (Control control in sidebarPanel.Controls)
            {
                if (control is TextBox textBox)
                {
                    textBox.BackColor = Color.FromArgb(55, 55, 58);
                    if (textBox.Text != SearchPlaceholder)
                        textBox.ForeColor = theme.Colors.PrimaryText;
                    textBox.Refresh();
                }
                else if (control is ComboBox combo)
                {
                    combo.BackColor = Color.FromArgb(55, 55, 58);
                    combo.ForeColor = theme.Colors.PrimaryText;
                    combo.Refresh();
                }
                else if (control is Button button)
                {
                    if (button.Text != "Clear Filters" && button.Text != "↻ Refresh")
                    {
                        button.Refresh();
                    }
                }
                else if (control is Label label)
                {
                    if (label.Text == "＋ Added")
                    {
                        label.ForeColor = Color.White;
                        label.BackColor = Color.FromArgb(52, 168, 83);
                    }
                    else if (label.Text == "↺ Updated")
                    {
                        label.ForeColor = Color.Black;
                        label.BackColor = Color.FromArgb(255, 153, 0);
                    }
                    else if (label.Text.StartsWith("SEARCH") || label.Text.StartsWith("CATEGORY") ||
                             label.Text.StartsWith("CHARACTER") || label.Text.StartsWith("SORT") ||
                             label.Text.StartsWith("NAVIGATION"))
                    {
                        label.ForeColor = Color.FromArgb(180, 180, 180);
                    }
                    else if (label.Text.StartsWith("Download Queue:") || label.Text.StartsWith("Tip:") || label.Text.StartsWith("Click Details"))
                    {
                        label.ForeColor = Color.FromArgb(120, 120, 120);
                    }
                    else if (label.Text.StartsWith("Page"))
                    {
                        label.ForeColor = Color.FromArgb(160, 160, 160);
                    }
                    label.Refresh();
                }
                else if (control is Panel panel)
                {
                    ApplyThemeToPanelControls(panel, theme);
                    panel.Refresh();
                }
            }
        }

        private void ApplyThemeToPanelControls(Panel panel, Theme theme)
        {
            foreach (Control control in panel.Controls)
            {
                if (control is Button button)
                {
                    if (button.Text.Contains("Prev") || button.Text.Contains("Next"))
                    {
                        if (button.Enabled)
                        {
                            button.BackColor = Color.FromArgb(60, 60, 63);
                            button.ForeColor = Color.FromArgb(200, 200, 200);
                        }
                        else
                        {
                            button.BackColor = Color.FromArgb(40, 40, 43);
                            button.ForeColor = Color.FromArgb(100, 100, 100);
                        }
                    }
                    button.Refresh();
                }
                else if (control is Label label)
                {
                    if (label.Text.StartsWith("Page"))
                    {
                        label.ForeColor = Color.FromArgb(160, 160, 160);
                    }
                    label.Refresh();
                }
            }
        }

        private void RefreshAllPanels()
        {
            headerPanel?.Refresh();
            sidebarPanel?.Refresh();
            contentPanel?.Refresh();
            statusPanel?.Refresh();
            mainContainer?.Refresh();

            modsFlow?.Refresh();

            this.Invalidate(true);
            this.Update();
        }

        public bool HasModsLoaded()
        {
            return currentMods.Count > 0;
        }

        public void RefreshDisplay()
        {
            ApplyTheme();
            var _ = RefreshAsync();
        }

        public void ApplyCardSizeSettings(string cardSize)
        {
            foreach (Control control in modsFlow.Controls)
            {
                if (control is ModCard card)
                {
                    card.ApplyCardSize(cardSize);
                }
            }
        }
    }

    public class ModCard : Panel
    {
        private readonly GameBananaSubmission mod;
        private readonly ModLoader modLoader;
        private readonly Form1 mainForm;

        private PictureBox thumbnailBox;
        private Label titleLabel;
        private Label authorLabel;
        private Label statsLabel;
        private Button installButton;
        private Button detailsButton;
        private Label addedBadge;
        private Label updatedBadge;
        private ToolTip badgeTip;

        public event Action<GameBananaSubmission> OnInstallRequested;
        public event Action<GameBananaSubmission, GameBananaFile> OnInstallSpecificFileRequested;
        public event Action<GameBananaSubmission> OnQueueRequested;

        private static string FormatRelativeShort(DateTime dt)
        {
            var now = DateTime.UtcNow;
            var utc = dt.ToUniversalTime();
            var span = now - utc;

            if (span.TotalSeconds < 90) return "now";
            if (span.TotalMinutes < 60) return ((int)Math.Round(span.TotalMinutes)).ToString() + "m"; // FIXED: Changed from 90 to 60
            if (span.TotalHours < 36) return ((int)Math.Round(span.TotalHours)).ToString() + "h";
            if (span.TotalDays < 14) return ((int)Math.Round(span.TotalDays)).ToString() + "d";
            if (span.TotalDays < 70) return ((int)Math.Round(span.TotalDays / 7.0)).ToString() + "w";

            return utc.ToString("MMM dd, yyyy");
        }

        private static Label MakeBadge(string text, Color back, Color fore)
        {
            var lbl = new Label
            {
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = fore,
                BackColor = back,
                Margin = new Padding(0)
            };
            lbl.Text = text;
            return lbl;
        }

        public ModCard(GameBananaSubmission submission, ModLoader loader, Form1 parent)
        {
            mod = submission;
            modLoader = loader;
            mainForm = parent;

            InitializeModernCard();
            LoadThumbnailAsync();
        }

        private void InitializeModernCard()
        {
            this.Height = 120;
            this.BackColor = Color.FromArgb(50, 50, 53);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Margin = new Padding(0, 0, 0, 8);
            this.MinimumSize = new Size(480, 120);
            this.Padding = new Padding(12, 10, 12, 10);

            thumbnailBox = new PictureBox
            {
                Size = new Size(100, 100),
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(40, 40, 43),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            titleLabel = new Label
            {
                Text = mod.Name ?? "Unknown Mod",
                Location = new Point(125, 12),
                Size = new Size(300, 26),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoEllipsis = true
            };

            authorLabel = new Label
            {
                Text = $"by {mod.AuthorName ?? "Unknown"}",
                Location = new Point(125, 38),
                Size = new Size(200, 18),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoEllipsis = true
            };

            var statsText = $"❤ {mod.LikeCount} • 👁 {mod.ViewCount} • 📥 Loading...";
            statsLabel = new Label
            {
                Text = statsText,
                Location = new Point(125, 60),
                Size = new Size(320, 16),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(140, 140, 140)
            };

            badgeTip = new ToolTip { InitialDelay = 200, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
            string addedRel = FormatRelativeShort(mod.DateAddedDateTime);
            string editedRel = (mod.DateModifiedTimestamp > mod.DateAddedTimestamp) ? FormatRelativeShort(mod.DateModifiedDateTime) : null;

            addedBadge = MakeBadge("＋ " + addedRel, Color.FromArgb(52, 168, 83), Color.White);
            this.Controls.Add(addedBadge);
            badgeTip.SetToolTip(addedBadge, "Added " + mod.DateAddedDateTime.ToString("MMM dd, yyyy"));

            if (!string.IsNullOrEmpty(editedRel))
            {
                updatedBadge = MakeBadge("↺ " + editedRel, Color.FromArgb(255, 153, 0), Color.Black);
                this.Controls.Add(updatedBadge);
                badgeTip.SetToolTip(updatedBadge, "Last updated " + mod.DateModifiedDateTime.ToString("MMM dd, yyyy"));
            }

            installButton = new Button
            {
                Text = "Install",
                Size = new Size(85, 28),
                Location = new Point(this.Width - 95, 30),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            installButton.FlatAppearance.BorderSize = 0;

            installButton.Click += async (s, e) =>
            {
                if (Control.ModifierKeys == Keys.Control)
                {
                    OnQueueRequested?.Invoke(mod);

                    installButton.Text = "Queued";
                    installButton.BackColor = Color.FromArgb(255, 193, 7);
                    installButton.Enabled = false;
                }
                else
                {
                    OnInstallRequested?.Invoke(mod);
                }
            };

            detailsButton = new Button
            {
                Text = "Details",
                Size = new Size(85, 28),
                Location = new Point(this.Width - 95, 65),
                BackColor = Color.FromArgb(90, 90, 93),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            detailsButton.FlatAppearance.BorderSize = 0;
            detailsButton.Click += (s, e) => ShowModDetailsDialog();

            this.Controls.AddRange(new Control[]
            {
                thumbnailBox, titleLabel, authorLabel, statsLabel, installButton, detailsButton
            });

            this.Resize += (s, e) =>
            {
                int rightInner = this.ClientSize.Width - 16;
                int buttonX = rightInner - installButton.Width;

                if (buttonX < 350) buttonX = 350;

                installButton.Location = new Point(buttonX, 30);
                detailsButton.Location = new Point(buttonX, 65);

                int leftCol = thumbnailBox.Right + 15;
                int textRight = buttonX - 15;
                if (textRight < leftCol + 150) textRight = leftCol + 150;
                int textWidth = textRight - leftCol;

                titleLabel.Location = new Point(leftCol, 12);
                titleLabel.Size = new Size(textWidth, 26);
                titleLabel.AutoEllipsis = true;

                authorLabel.Location = new Point(leftCol, 38);
                authorLabel.Size = new Size(Math.Min(200, textWidth), 18);
                authorLabel.AutoEllipsis = true;

                int badgesX = leftCol;
                int badgesY = 82;
                if (addedBadge != null)
                {
                    addedBadge.Location = new Point(badgesX, badgesY);
                    badgesX = addedBadge.Right + 6;
                }
                if (updatedBadge != null)
                {
                    updatedBadge.Location = new Point(badgesX, badgesY);
                    badgesX = updatedBadge.Right + 8;
                }

                int statsWidth = textWidth;
                if (statsWidth < 150) statsWidth = 150;
                statsLabel.Location = new Point(leftCol, 60);
                statsLabel.Size = new Size(statsWidth, 16);
            };

            _ = LoadAccurateStatsAsync();
        }

        private async Task LoadAccurateStatsAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://gamebanana.com/apiv11/Mod/{mod.Id}?_csvProperties=_nDownloadCount,_nViewCount,_nLikeCount";

                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var details = JsonConvert.DeserializeObject<JObject>(json);

                            if (details != null)
                            {
                                var downloadCount = details["_nDownloadCount"]?.Value<int>() ?? mod.DownloadCount;
                                var viewCount = details["_nViewCount"]?.Value<int>() ?? mod.ViewCount;
                                var likeCount = details["_nLikeCount"]?.Value<int>() ?? mod.LikeCount;

                                var updatedStatsText = $"❤ {likeCount} • 👁 {viewCount} • 📥 {downloadCount}";

                                if (statsLabel.InvokeRequired)
                                {
                                    statsLabel.Invoke(new Action(() =>
                                    {
                                        statsLabel.Text = updatedStatsText;
                                    }));
                                }
                                else
                                {
                                    statsLabel.Text = updatedStatsText;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading accurate stats for mod {mod.Id}: {ex.Message}");

                var fallbackStatsText = $"❤ {mod.LikeCount} • 👁 {mod.ViewCount} • 📥 {mod.DownloadCount}";

                if (statsLabel.InvokeRequired)
                {
                    statsLabel.Invoke(new Action(() =>
                    {
                        statsLabel.Text = fallbackStatsText;
                    }));
                }
                else
                {
                    statsLabel.Text = fallbackStatsText;
                }
            }
        }

        private async void ShowModDetailsDialog()
        {
            try
            {
                var dialog = new ModernModDetailsDialog(mod, modLoader, mainForm);
                dialog.OnInstallFileRequested += (file) => OnInstallSpecificFileRequested?.Invoke(mod, file);
                dialog.ShowDialog(this.FindForm());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing mod details: {ex.Message}");
            }
        }

        private async void LoadThumbnailAsync()
        {
            try
            {
                var imageUrl = await GetThumbnailUrlAsync();
                if (string.IsNullOrEmpty(imageUrl)) return;

                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(imageUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            var image = Image.FromStream(stream);
                            thumbnailBox.Image?.Dispose();
                            thumbnailBox.Image = image;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private async Task<string> GetThumbnailUrlAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(mod.PreviewImageUrl))
                    return mod.PreviewImageUrl;

                using (var httpClient = new HttpClient())
                {
                    var url = $"https://gamebanana.com/apiv11/Mod/{mod.Id}?_csvProperties=_aPreviewMedia";
                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var data = JArray.Parse(json);

                            if (data.Count > 0 && data[0]["_aPreviewMedia"] is JObject previewMedia)
                            {
                                var images = previewMedia["_aImages"] as JArray;
                                if (images?.Count > 0)
                                {
                                    var firstImage = images[0];
                                    var baseUrl = firstImage["_sBaseUrl"]?.Value<string>();
                                    var file220 = firstImage["_sFile220"]?.Value<string>();

                                    if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(file220))
                                        return $"{baseUrl}/{file220}";
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public void ApplyTheme(Theme theme)
        {
            try
            {
                this.SuspendLayout();

                this.BackColor = theme.Colors.ActiveBackground;
                if (thumbnailBox != null) thumbnailBox.BackColor = theme.Colors.SecondaryBackground;

                if (titleLabel != null) titleLabel.ForeColor = theme.Colors.PrimaryText;
                if (authorLabel != null) authorLabel.ForeColor = theme.Colors.SecondaryText;
                if (statsLabel != null) statsLabel.ForeColor = theme.Colors.SubtitleText;

                if (installButton != null && installButton.Text == "Install")
                {
                    installButton.BackColor = Color.FromArgb(0, 120, 212);
                    installButton.ForeColor = Color.White;
                }

                if (detailsButton != null)
                {
                    detailsButton.BackColor = Color.FromArgb(90, 90, 93);
                    detailsButton.ForeColor = Color.White;
                }

                if (addedBadge != null)
                {
                    addedBadge.BackColor = Color.FromArgb(52, 168, 83);
                    addedBadge.ForeColor = Color.White;
                }

                if (updatedBadge != null)
                {
                    updatedBadge.BackColor = Color.FromArgb(255, 153, 0);
                    updatedBadge.ForeColor = Color.Black;
                }

                this.ResumeLayout();
                this.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ModCard.ApplyTheme error: {ex.Message}");
                this.ResumeLayout();
            }
        }

        public void ApplyCardSize(string cardSize)
        {
            switch (cardSize)
            {
                case "Large":
                    this.Height = 140;
                    break;
                case "Extra Large":
                    this.Height = 160;
                    break;
                default:
                    this.Height = 120;
                    break;
            }
        }
    }

    public class ModernModDetailsDialog : Form
    {
        private readonly GameBananaSubmission mod;
        private readonly ModLoader modLoader;
        private readonly Form1 mainForm;

        private Panel contentPanel;
        private Panel footerPanel;
        private PictureBox previewImage;
        private Label titleLabel;
        private Label authorLabel;
        private Label statsLabel;
        private RichTextBox descriptionBox;
        private Button installButton;
        private Button openPageButton;

        public event Action<GameBananaFile> OnInstallFileRequested;

        public ModernModDetailsDialog(GameBananaSubmission submission, ModLoader loader, Form1 parent)
        {
            mod = submission;
            modLoader = loader;
            mainForm = parent;
            InitializeModernDialog();
            LoadPreviewImageAsync();
        }

        private void InitializeModernDialog()
        {
            this.Text = mod.Name ?? "Mod Details";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ShowInTaskbar = false;

            CreateModernContent();
            CreateModernFooter();

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) this.Close();
            };
        }

        private void CreateModernContent()
        {
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 53),
                Padding = new Padding(20, 20, 20, 15)
            };

            previewImage = new PictureBox
            {
                Size = new Size(260, 160),
                Location = new Point(20, 20),
                BackColor = Color.FromArgb(40, 40, 43),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None
            };

            var infoPanel = new Panel
            {
                Location = new Point(300, 20),
                Size = new Size(360, 160),
                BackColor = Color.Transparent
            };

            titleLabel = new Label
            {
                Text = mod.Name ?? "Unknown Mod",
                Location = new Point(0, 0),
                Size = new Size(360, 28),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                AutoEllipsis = true
            };

            authorLabel = new Label
            {
                Text = $"Created by {mod.AuthorName ?? "Unknown Author"}",
                Location = new Point(0, 32),
                Size = new Size(360, 18),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(180, 180, 180)
            };

            var uploadDate = mod.DateAddedDateTime.ToString("MMM dd, yyyy");
            var statsText = $"Published: {uploadDate} • Likes: {mod.LikeCount:N0} • Views: {mod.ViewCount:N0} • Downloads: {mod.DownloadCount:N0}";

            statsLabel = new Label
            {
                Text = statsText,
                Location = new Point(0, 54),
                Size = new Size(360, 40),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(160, 160, 160)
            };

            infoPanel.Controls.AddRange(new Control[] { titleLabel, authorLabel, statsLabel });

            var descLabel = new Label
            {
                Text = "DESCRIPTION",
                Location = new Point(20, 190),
                Size = new Size(100, 16),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            descriptionBox = new RichTextBox
            {
                Location = new Point(20, 210),
                Size = new Size(640, 80),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text = "Loading description..."
            };

            var filesLabel = new Label
            {
                Text = "AVAILABLE FILES",
                Location = new Point(20, 300),
                Size = new Size(120, 16),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 200, 200)
            };

            var filesListBox = new ListBox
            {
                Name = "filesListBox",
                Location = new Point(20, 320),
                Size = new Size(640, 90),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.FromArgb(230, 230, 230),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                SelectionMode = SelectionMode.One
            };

            contentPanel.Controls.AddRange(new Control[] {
                previewImage, infoPanel, descLabel, descriptionBox, filesLabel, filesListBox
            });

            LoadAvailableFilesAsync(filesListBox);
            _ = LoadDetailedDescriptionAsync();

            this.Controls.Add(contentPanel);
        }

        private void CreateModernFooter()
        {
            footerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(37, 37, 38),
                Padding = new Padding(20, 12, 20, 12)
            };

            installButton = new Button
            {
                Text = "Install Selected",
                Size = new Size(130, 36),
                Location = new Point(20, 12),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            installButton.FlatAppearance.BorderSize = 0;
            installButton.Click += HandleInstallClick;

            openPageButton = new Button
            {
                Text = "View on GameBanana",
                Size = new Size(160, 36),
                Location = new Point(170, 12),
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            openPageButton.FlatAppearance.BorderSize = 0;
            openPageButton.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start($"https://gamebanana.com/mods/{mod.Id}");
                }
                catch { }
            };

            footerPanel.Controls.AddRange(new Control[] { installButton, openPageButton });
            this.Controls.Add(footerPanel);
        }

        private async void LoadAvailableFilesAsync(ListBox filesListBox)
        {
            try
            {
                var files = await FetchFilesForModAsync();

                if (files != null && files.Count > 0)
                {
                    filesListBox.Items.Clear();
                    foreach (var file in files)
                    {
                        var displayText = $"{file.FileName} ({FormatFileSize(file.FileSize)}) - {file.DownloadCount} downloads";
                        filesListBox.Items.Add(new FileListItem { File = file, DisplayText = displayText });
                    }

                    if (filesListBox.Items.Count > 0)
                        filesListBox.SelectedIndex = 0;
                }
                else
                {
                    filesListBox.Items.Add("No downloadable files found - will open GameBanana page");
                }
            }
            catch (Exception ex)
            {
                filesListBox.Items.Add("Error loading files - will open GameBanana page");
                System.Diagnostics.Debug.WriteLine($"Error loading files: {ex.Message}");
            }
        }

        private async Task<List<GameBananaFile>> FetchFilesForModAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://gamebanana.com/apiv11/Mod/{mod.Id}/Files";
                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var files = JsonConvert.DeserializeObject<GameBananaFile[]>(json);
                            return files?.Where(ShouldShowFile).ToList() ?? new List<GameBananaFile>();
                        }
                    }
                }
            }
            catch { }
            return new List<GameBananaFile>();
        }

        private bool ShouldShowFile(GameBananaFile file)
        {
            if (file?.FileName == null) return false;
            var lower = file.FileName.ToLowerInvariant();
            var validExtensions = new[] { ".zip", ".7z", ".rar", ".vpk", ".pak" };
            var invalidPatterns = new[] { "readme", "screenshot", ".txt", ".jpg", ".png", ".gif" };
            return validExtensions.Any(ext => lower.EndsWith(ext)) &&
                   !invalidPatterns.Any(pattern => lower.Contains(pattern));
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "Unknown size";
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024.0;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void HandleInstallClick(object sender, EventArgs e)
        {
            try
            {
                var filesListBox = contentPanel.Controls.Find("filesListBox", true).FirstOrDefault() as ListBox;

                if (filesListBox != null && filesListBox.SelectedItem is FileListItem selectedItem)
                {
                    OnInstallFileRequested?.Invoke(selectedItem.File);
                    this.Close();
                }
                else if (filesListBox != null && filesListBox.Items.Count > 0 && filesListBox.Items[0] is FileListItem firstItem)
                {
                    filesListBox.SelectedIndex = 0;
                    OnInstallFileRequested?.Invoke(firstItem.File);
                    this.Close();
                }
                else
                {
                    try { System.Diagnostics.Process.Start($"https://gamebanana.com/mods/{mod.Id}"); } catch { }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleInstallClick: {ex.Message}");
                this.Close();
            }
        }

        private string CleanDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return "";

            if (description.Contains("<") && description.Contains(">"))
            {
                description = description.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
                description = description.Replace("<p>", "\n").Replace("</p>", "\n");
                description = description.Replace("<strong>", "").Replace("</strong>", "");
                description = description.Replace("<b>", "").Replace("</b>", "");

                description = System.Text.RegularExpressions.Regex.Replace(description, @"<[^>]+>", "");
                description = System.Text.RegularExpressions.Regex.Replace(description, @"\n\s*\n", "\n\n");
                description = System.Text.RegularExpressions.Regex.Replace(description, @"[ \t]+", " ");
            }

            return description.Trim();
        }

        private async Task LoadDetailedDescriptionAsync()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"https://gamebanana.com/apiv11/Mod/{mod.Id}?_csvProperties=_sText,_nDownloadCount,_nViewCount,_nLikeCount";

                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();

                            try
                            {
                                var details = JsonConvert.DeserializeObject<JObject>(json);
                                if (details != null)
                                {
                                    var description = details["_sText"]?.Value<string>();
                                    var downloadCount = details["_nDownloadCount"]?.Value<int>() ?? mod.DownloadCount;
                                    var viewCount = details["_nViewCount"]?.Value<int>() ?? mod.ViewCount;
                                    var likeCount = details["_nLikeCount"]?.Value<int>() ?? mod.LikeCount;

                                    if (!string.IsNullOrEmpty(description))
                                    {
                                        UpdateDescription(description);
                                    }

                                    UpdateStats(downloadCount, viewCount, likeCount);
                                    return;
                                }
                            }
                            catch (JsonException)
                            {
                                try
                                {
                                    var detailArray = JsonConvert.DeserializeObject<JArray>(json);
                                    if (detailArray != null && detailArray.Count > 0)
                                    {
                                        var details = detailArray[0];
                                        var description = details["_sText"]?.Value<string>();
                                        var downloadCount = details["_nDownloadCount"]?.Value<int>() ?? mod.DownloadCount;
                                        var viewCount = details["_nViewCount"]?.Value<int>() ?? mod.ViewCount;
                                        var likeCount = details["_nLikeCount"]?.Value<int>() ?? mod.LikeCount;

                                        if (!string.IsNullOrEmpty(description))
                                        {
                                            UpdateDescription(description);
                                        }

                                        UpdateStats(downloadCount, viewCount, likeCount);
                                        return;
                                    }
                                }
                                catch (JsonException) { }
                            }
                        }
                    }
                }

                UpdateDescription(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadDetailedDescriptionAsync error: {ex.Message}");
                UpdateDescription(null);
            }
        }

        private void UpdateDescription(string description)
        {
            var finalDescription = string.IsNullOrEmpty(description)
                ? "No description available for this mod.\n\nYou can visit the GameBanana page for more information."
                : CleanDescription(description);

            if (descriptionBox.InvokeRequired)
            {
                descriptionBox.Invoke(new Action(() =>
                {
                    descriptionBox.Text = finalDescription;
                }));
            }
            else
            {
                descriptionBox.Text = finalDescription;
            }
        }

        private void UpdateStats(int downloadCount, int viewCount, int likeCount)
        {
            var uploadDate = mod.DateAddedDateTime.ToString("MMM dd, yyyy");
            var statsText = $"Published: {uploadDate} • Likes: {likeCount:N0} • Views: {viewCount:N0} • Downloads: {downloadCount:N0}";

            if (statsLabel.InvokeRequired)
            {
                statsLabel.Invoke(new Action(() =>
                {
                    statsLabel.Text = statsText;
                }));
            }
            else
            {
                statsLabel.Text = statsText;
            }
        }

        private async void LoadPreviewImageAsync()
        {
            try
            {
                var imageUrl = await GetThumbnailUrlAsync();
                if (string.IsNullOrEmpty(imageUrl)) return;

                using (var httpClient = new HttpClient())
                using (var response = await httpClient.GetAsync(imageUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            var image = Image.FromStream(stream);
                            previewImage.Image?.Dispose();
                            previewImage.Image = image;
                        }
                    }
                }
            }
            catch { }
        }

        private async Task<string> GetThumbnailUrlAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(mod.PreviewImageUrl))
                    return mod.PreviewImageUrl;

                using (var httpClient = new HttpClient())
                {
                    var url = $"https://gamebanana.com/apiv11/Mod/{mod.Id}?_csvProperties=_aPreviewMedia";
                    using (var response = await httpClient.GetAsync(url))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var data = JArray.Parse(json);

                            if (data.Count > 0 && data[0]["_aPreviewMedia"] is JObject previewMedia)
                            {
                                var images = previewMedia["_aImages"] as JArray;
                                if (images?.Count > 0)
                                {
                                    var firstImage = images[0];
                                    var baseUrl = firstImage["_sBaseUrl"]?.Value<string>();
                                    var file530 = firstImage["_sFile530"]?.Value<string>();

                                    if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(file530))
                                        return $"{baseUrl}/{file530}";
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private class FileListItem
        {
            public GameBananaFile File { get; set; }
            public string DisplayText { get; set; }

            public override string ToString()
            {
                return DisplayText;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                previewImage?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}