using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class GameSetupTabBuilder
    {
        private readonly Panel contentPanel;
        private readonly ModLoader modLoader;

        private TextBox gameSetupPathTextBox;
        private Button gameSetupBrowseButton;
        private Button gameSetupSetPathButton;
        private Button patchGameInfoButton;
        private Button restoreBackupButton;

        public GameSetupTabBuilder(Panel contentPanel, ModLoader modLoader)
        {
            this.contentPanel = contentPanel;
            this.modLoader = modLoader;
        }

        public void CreateGameSetupTab()
        {
            var gameSetupPanel = new Panel
            {
                Name = "GameSetupPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false
            };

            CreateHeader(gameSetupPanel);
            CreateGamePathSection(gameSetupPanel);
            CreateGameConfigSection(gameSetupPanel);

            gameSetupPanel.Resize += (s, e) => LayoutGameSetup(gameSetupPanel);
            LayoutGameSetup(gameSetupPanel);

            contentPanel.Controls.Add(gameSetupPanel);
        }

        private void CreateHeader(Panel gameSetupPanel)
        {
            var headerLabel = new Label
            {
                Text = "Game Setup",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(20, 20),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = "Configure your Deadlock game directory and settings",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(20, 50),
                AutoSize = true
            };

            gameSetupPanel.Controls.AddRange(new Control[] { headerLabel, subtitleLabel });
        }

        private void CreateGamePathSection(Panel gameSetupPanel)
        {
            var gamePathGroup = new GroupBox
            {
                Text = "Game Directory",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 90),
                Size = new Size(500, 120)
            };

            var gamePathLabel = new Label
            {
                Text = "Game Path:",
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(15, 30),
                AutoSize = true
            };

            gameSetupPathTextBox = new TextBox
            {
                Name = "gameSetupPathTextBox", // Add name for finding control
                Location = new Point(15, 50),
                Size = new Size(280, 23), // Made smaller to fit 3 buttons
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(222, 214, 196),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Click Auto-Find or Browse for your Deadlock game folder..."
            };
            var autoFindButton = new Button
            {
                Location = new Point(305, 49),
                Size = new Size(70, 25),
                Text = "Auto-Find",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 120, 70),
                ForeColor = Color.White,
                Padding = new Padding(0, 0, 0, 2),
                Font = new Font("Segoe UI", 8F)
            };
            autoFindButton.FlatAppearance.BorderSize = 0;

            gameSetupBrowseButton = new Button
            {
                Location = new Point(380, 49),
                Size = new Size(60, 25),
                Text = "Browse",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.FromArgb(222, 214, 196),
                Padding = new Padding(0, 0, 0, 2),
                Font = new Font("Segoe UI", 8F)
            };
            gameSetupBrowseButton.FlatAppearance.BorderSize = 0;

            gameSetupSetPathButton = new Button
            {
                Location = new Point(445, 49),
                Size = new Size(60, 25),
                Text = "Set Path",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.FromArgb(222, 214, 196),
                Padding = new Padding(0, 0, 0, 2),
                Font = new Font("Segoe UI", 8F)
            };
            gameSetupSetPathButton.FlatAppearance.BorderSize = 0;

            var pathHintLabel = new Label
            {
                Text = "Auto-Find will search common Steam library locations. Use Browse if auto-detection fails.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(15, 80),
                Size = new Size(475, 30),
                AutoSize = false
            };

            autoFindButton.Click += (s, e) =>
            {
                autoFindButton.Enabled = false;
                autoFindButton.Text = "Searching...";

                try
                {
                    string foundPath = FindDeadlockInstallation();
                    if (!string.IsNullOrEmpty(foundPath))
                    {
                        gameSetupPathTextBox.Text = foundPath;
                        gameSetupPathTextBox.ForeColor = Color.FromArgb(222, 214, 196);
                        MessageBox.Show($"Found Deadlock at: {foundPath}", "Auto-Find Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Could not automatically detect Deadlock installation.\nPlease use Browse button.",
                            "Auto-Find Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Auto-find error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    autoFindButton.Enabled = true;
                    autoFindButton.Text = "Auto-Find";
                }
            };

            gamePathGroup.Controls.AddRange(new Control[] {
        gamePathLabel, gameSetupPathTextBox, autoFindButton, gameSetupBrowseButton,
        gameSetupSetPathButton, pathHintLabel
    });

            gameSetupPanel.Controls.Add(gamePathGroup);
        }
        public string FindDeadlockInstallation()
        {
            try
            {
                string steamPath = null;
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam") ??
                                      Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
                    {
                        steamPath = key?.GetValue("InstallPath") as string;
                    }
                }
                catch { }
                if (string.IsNullOrEmpty(steamPath))
                {
                    string[] commonSteamPaths = {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
                @"D:\Steam",
                @"E:\Steam",
                @"F:\Steam"
            };

                    foreach (var path in commonSteamPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            steamPath = path;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(steamPath))
                {
                    string deadlockPath = Path.Combine(steamPath, "steamapps", "common", "Deadlock", "game");
                    if (Directory.Exists(deadlockPath))
                    {
                        return deadlockPath;
                    }
                    string libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                    if (File.Exists(libraryFoldersPath))
                    {
                        string content = File.ReadAllText(libraryFoldersPath);

                        var matches = System.Text.RegularExpressions.Regex.Matches(content, @"""path""\s*""([^""]+)""");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            if (match.Groups.Count > 1)
                            {
                                string libraryPath = match.Groups[1].Value.Replace(@"\\", @"\");
                                deadlockPath = Path.Combine(libraryPath, "steamapps", "common", "Deadlock", "game");
                                if (Directory.Exists(deadlockPath))
                                {
                                    return deadlockPath;
                                }
                            }
                        }
                    }
                }
                string[] commonGamePaths = {
            @"C:\Program Files (x86)\Steam\steamapps\common\Deadlock\game",
            @"C:\Program Files\Steam\steamapps\common\Deadlock\game",
            @"D:\SteamLibrary\steamapps\common\Deadlock\game",
            @"C:\SteamLibrary\steamapps\common\Deadlock\game",
            @"E:\SteamLibrary\steamapps\common\Deadlock\game",
            @"F:\SteamLibrary\steamapps\common\Deadlock\game",
            @"C:\Games\Steam\steamapps\common\Deadlock\game",
            @"D:\Games\Steam\steamapps\common\Deadlock\game",
            @"E:\Games\Steam\steamapps\common\Deadlock\game",
            @"D:\Steam\steamapps\common\Deadlock\game",
            @"E:\Steam\steamapps\common\Deadlock\game"
        };

                foreach (var path in commonGamePaths)
                {
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FindDeadlockInstallation error: {ex.Message}");
            }

            return null;
        }

        private void CreateGameConfigSection(Panel gameSetupPanel)
        {
            var gameConfigGroup = new GroupBox
            {
                Text = "Game Configuration",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 230),
                Size = new Size(500, 120)
            };

            patchGameInfoButton = new Button
            {
                Text = "Patch Game Config",
                Location = new Point(15, 30),
                Size = new Size(150, 37),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.FromArgb(222, 214, 196),
                Padding = new Padding(0, 0, 0, 2)
            };

            var patchDesc = new Label
            {
                Text = "Updates gameinfo.gi to support mod loading",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(180, 40),
                Size = new Size(300, 20)
            };

            restoreBackupButton = new Button
            {
                Text = "Restore Backup",
                Location = new Point(15, 75),
                Size = new Size(150, 37),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 60, 60),
                ForeColor = Color.FromArgb(222, 214, 196),
                Padding = new Padding(0, 0, 0, 2)
            };

            var restoreDesc = new Label
            {
                Text = "Restores original gameinfo.gi from backup",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(180, 85),
                Size = new Size(300, 20)
            };

            gameConfigGroup.Controls.AddRange(new Control[] {
                patchGameInfoButton, patchDesc, restoreBackupButton, restoreDesc
            });

            gameSetupPanel.Controls.Add(gameConfigGroup);
        }

        public void AddEventHandlers()
        {
            if (gameSetupBrowseButton != null) gameSetupBrowseButton.Click += GameSetupBrowseButton_Click;
            if (gameSetupSetPathButton != null) gameSetupSetPathButton.Click += GameSetupSetPathButton_Click;
            if (patchGameInfoButton != null) patchGameInfoButton.Click += PatchGameInfoButton_Click;
            if (restoreBackupButton != null) restoreBackupButton.Click += RestoreBackupButton_Click;
        }

        public void LoadCurrentGamePath()
        {
            try
            {
                string savedPath = Properties.Settings.Default.GamePath;
                if (!string.IsNullOrEmpty(savedPath) && gameSetupPathTextBox != null)
                {
                    gameSetupPathTextBox.Text = savedPath;
                }
            }
            catch { }
        }

        #region Event Handlers
        private void GameSetupBrowseButton_Click(object sender, EventArgs e)
        {
            try
            {
                string startPath = "";
                if (gameSetupPathTextBox != null && !string.IsNullOrEmpty(gameSetupPathTextBox.Text) &&
                    Directory.Exists(gameSetupPathTextBox.Text))
                {
                    startPath = gameSetupPathTextBox.Text;
                }
                else
                {
                    string[] steamPaths = {
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Steam\steamapps\common",
                @"D:\SteamLibrary\steamapps\common",
                @"C:\SteamLibrary\steamapps\common"
            };

                    foreach (var path in steamPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            startPath = path;
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(startPath))
                    {
                        startPath = @"C:\Program Files (x86)";
                    }
                }

                if (Directory.Exists(startPath))
                {
                    Process.Start("explorer.exe", $"\"{startPath}\"");
                }
                else
                {
                    Process.Start("explorer.exe");
                }

                MessageBox.Show(
                    "Navigate to your Deadlock game directory and copy the path.\n\n" +
                    "Look for: ...\\steamapps\\common\\Deadlock\\game\n\n" +
                    "Then paste the path into the text box and click 'Set Path'.",
                    "Browse for Deadlock Directory",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening File Explorer: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void GameSetupSetPathButton_Click(object sender, EventArgs e)
        {
            if (gameSetupPathTextBox == null) return;
            string pathFromTextBox = gameSetupPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(pathFromTextBox)) return;

            if (modLoader.SetGamePath(pathFromTextBox))
            {
                SaveGamePath(pathFromTextBox);
                modLoader.PatchGameInfoFile();
                MessageBox.Show("Game path set successfully!", "Success");
            }
            else
            {
                MessageBox.Show("Invalid Deadlock directory.", "Error");
            }
        }

        private void PatchGameInfoButton_Click(object sender, EventArgs e)
        {
            modLoader.PatchGameInfoFile();
        }

        private void RestoreBackupButton_Click(object sender, EventArgs e)
        {
            modLoader.RestoreGameInfoBackup();
        }
        #endregion

        #region Helper Methods
        private void SaveGamePath(string path)
        {
            try
            {
                Properties.Settings.Default.GamePath = path;
                Properties.Settings.Default.Save();
            }
            catch { }
        }

        private void LayoutGameSetup(Panel gameSetupPanel)
        {
            int margin = 20;
            int panelW = gameSetupPanel.ClientSize.Width - (margin * 2);

            var gamePathGroup = gameSetupPanel.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Game Directory");
            var gameConfigGroup = gameSetupPanel.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Game Configuration");

            if (gamePathGroup != null)
            {
                gamePathGroup.Location = new Point(margin, 90);
                gamePathGroup.Size = new Size(Math.Max(500, panelW), 120);

                int innerPad = 15;
                int btnGap = 5;
                int btnW = 80;
                int rowY = 50;

                int textW = gamePathGroup.Width - innerPad - innerPad - (btnW + btnGap + btnW);
                gameSetupPathTextBox.Location = new Point(innerPad, rowY);
                gameSetupPathTextBox.Size = new Size(Math.Max(220, textW), 23);

                gameSetupBrowseButton.Location = new Point(gameSetupPathTextBox.Right + btnGap, rowY - 1);
                gameSetupSetPathButton.Location = new Point(gameSetupBrowseButton.Right + btnGap, rowY - 1);
            }

            if (gameConfigGroup != null)
            {
                gameConfigGroup.Location = new Point(margin, 230);
                gameConfigGroup.Size = new Size(gamePathGroup?.Width ?? 500, 120);
            }
        }
        #endregion
    }
}