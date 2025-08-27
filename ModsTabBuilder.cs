using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class ModsTabBuilder
    {
        private readonly Panel contentPanel;
        private readonly ModLoader modLoader;

        private FileDropModListBox availableModsListBox;
        private FileDropModListBox activeModsListBox;
        private TextBox searchTextBox;
        private ComboBox filterComboBox;
        private GroupBox modInfoPanel;
        private TextBox descriptionTextBox;
        private Label fileCountLabel, modTypeLabel, authorLabel, modNameLabel;
        private Button activateButton, deactivateButton;
        private Button moveUpButton, moveDownButton;
        private Button deleteModButton, addonsButton, importModsButton;

        public ModsTabBuilder(Panel contentPanel, ModLoader modLoader)
        {
            this.contentPanel = contentPanel;
            this.modLoader = modLoader;
        }

        public void CreateModsTab()
        {
            var modsPanel = new Panel
            {
                Name = "ModsPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false
            };

            CreateHeader(modsPanel);
            CreateSearchControls(modsPanel);
            CreateModLists(modsPanel);
            CreateActionButtons(modsPanel);
            CreateModInfoPanel(modsPanel);

            modsPanel.Resize += (s, e) => LayoutModsUI(modsPanel);
            LayoutModsUI(modsPanel);

            contentPanel.Controls.Add(modsPanel);
        }

        private void CreateHeader(Panel modsPanel)
        {
            var headerLabel = new Label
            {
                Text = "Mod Management",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(20, 20),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = "Use buttons to activate/deactivate mods • Drag files onto lists to install new mods",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(20, 50),
                AutoSize = true
            };

            modsPanel.Controls.AddRange(new Control[] { headerLabel, subtitleLabel });
        }

        private void CreateSearchControls(Panel modsPanel)
        {
            var searchLabel = new Label
            {
                Text = "Search:",
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(20, 82),
                AutoSize = true
            };

            searchTextBox = new TextBox
            {
                Location = new Point(70, 80),
                Size = new Size(250, 22),
                Text = "Search mods...",
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(55, 55, 58),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F)
            };

            filterComboBox = new ComboBox
            {
                Location = new Point(330, 80),
                Size = new Size(120, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(55, 55, 58),
                ForeColor = Color.FromArgb(222, 214, 196),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            filterComboBox.Items.AddRange(new[] { "All Types", "VPK Only", "Directory", "Mixed" });
            filterComboBox.SelectedIndex = 0;

            var resultLabel = new Label
            {
                Text = "0 mods found",
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(70, 105),
                AutoSize = true,
                Name = "searchResultLabel",
                Font = new Font("Segoe UI", 8F)
            };

            modsPanel.Controls.AddRange(new Control[] { searchLabel, searchTextBox, filterComboBox, resultLabel });
        }

        private void CreateModLists(Panel modsPanel)
        {
            var label1 = new Label
            {
                Text = "Available Mods",
                ForeColor = Color.FromArgb(222, 214, 196),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true
            };

            var label2 = new Label
            {
                Text = "Active Mods",
                ForeColor = Color.FromArgb(222, 214, 196),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true
            };

            availableModsListBox = new FileDropModListBox
            {
                BackColor = Color.FromArgb(50, 50, 53),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.MultiExtended,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            activeModsListBox = new FileDropModListBox
            {
                BackColor = Color.FromArgb(50, 50, 53),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                SelectionMode = SelectionMode.MultiExtended,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            ThemeManager.RegisterEnhancedListBox(availableModsListBox);
            ThemeManager.RegisterEnhancedListBox(activeModsListBox);

            availableModsListBox.UpdateTheme(ThemeManager.GetCurrentTheme()?.Colors);
            activeModsListBox.UpdateTheme(ThemeManager.GetCurrentTheme()?.Colors);

            modsPanel.Controls.AddRange(new Control[] { label1, label2, availableModsListBox, activeModsListBox });
        }

        public void Dispose()
        {
            try
            {
                if (availableModsListBox != null)
                {
                    ThemeManager.UnregisterEnhancedListBox(availableModsListBox);
                }
                if (activeModsListBox != null)
                {
                    ThemeManager.UnregisterEnhancedListBox(activeModsListBox);
                }
            }
            catch
            {
            }
        }

        private void CreateActionButtons(Panel modsPanel)
        {
            deleteModButton = new Button
            {
                Text = "Delete Mod",
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(150, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };
            deleteModButton.FlatAppearance.BorderSize = 0;

            addonsButton = new Button
            {
                Text = "Open Addons",
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };
            addonsButton.FlatAppearance.BorderSize = 0;

            importModsButton = new Button
            {
                Text = "Import Mods",
                Size = new Size(90, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 120, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };
            importModsButton.FlatAppearance.BorderSize = 0;
            var actionPanel = new Panel
            {
                BackColor = Color.FromArgb(45, 45, 48),
                Size = new Size(80, 150)
            };

            activateButton = new Button
            {
                Size = new Size(70, 30),
                Text = "→",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 130, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(5, 10)
            };
            activateButton.FlatAppearance.BorderSize = 0;
            activateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 150, 200);

            deactivateButton = new Button
            {
                Size = new Size(70, 30),
                Text = "←",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(5, 50)
            };
            deactivateButton.FlatAppearance.BorderSize = 0;
            deactivateButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 90, 90);

            moveUpButton = new Button
            {
                Size = new Size(30, 24),
                Text = "▲",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 150, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(5, 90)
            };
            moveUpButton.FlatAppearance.BorderSize = 0;
            moveUpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(120, 170, 120);

            moveDownButton = new Button
            {
                Size = new Size(30, 24),
                Text = "▼",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 150, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Location = new Point(40, 90)
            };
            moveDownButton.FlatAppearance.BorderSize = 0;
            moveDownButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(120, 170, 120);

            actionPanel.Controls.AddRange(new Control[] {
                activateButton, deactivateButton, moveUpButton, moveDownButton
            });

            modsPanel.Controls.AddRange(new Control[] {
                deleteModButton, addonsButton, importModsButton, actionPanel
            });
        }

        private void CreateModInfoPanel(Panel modsPanel)
        {
            modInfoPanel = new GroupBox
            {
                Text = "Mod Information",
                BackColor = Color.FromArgb(40, 40, 43),
                ForeColor = Color.FromArgb(222, 214, 196),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            modNameLabel = new Label
            {
                Text = "Select a mod to view details",
                ForeColor = Color.FromArgb(222, 214, 196),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 30),
                Size = new Size(200, 20)
            };

            authorLabel = new Label
            {
                ForeColor = Color.FromArgb(180, 180, 180),
                Font = new Font("Segoe UI", 8F),
                Location = new Point(220, 30),
                Size = new Size(150, 20)
            };

            modTypeLabel = new Label
            {
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Segoe UI", 8F),
                Location = new Point(380, 30),
                Size = new Size(100, 20)
            };

            fileCountLabel = new Label
            {
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Segoe UI", 8F),
                Location = new Point(490, 30),
                Size = new Size(100, 20)
            };

            descriptionTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(222, 214, 196),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8F),
                Location = new Point(12, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            modInfoPanel.Controls.AddRange(new Control[] {
                modNameLabel, authorLabel, modTypeLabel, fileCountLabel, descriptionTextBox
            });

            modsPanel.Controls.Add(modInfoPanel);
        }

        public void AddEventHandlers()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Adding event handlers");

            if (activateButton != null) activateButton.Click += ActivateButton_Click;
            if (deactivateButton != null) deactivateButton.Click += DeactivateButton_Click;
            if (moveUpButton != null) moveUpButton.Click += MoveUpButton_Click;
            if (moveDownButton != null) moveDownButton.Click += MoveDownButton_Click;
            if (deleteModButton != null) deleteModButton.Click += DeleteModButton_Click;
            if (addonsButton != null) addonsButton.Click += AddonsButton_Click;
            if (importModsButton != null) importModsButton.Click += ImportModsButton_Click;

            if (availableModsListBox != null)
            {
                availableModsListBox.SelectedIndexChanged += AvailableModsListBox_SelectedIndexChanged;
                availableModsListBox.KeyDown += ModListBox_KeyDown;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Available mods events wired");
            }

            if (activeModsListBox != null)
            {
                activeModsListBox.SelectedIndexChanged += ActiveModsListBox_SelectedIndexChanged;
                activeModsListBox.KeyDown += ModListBox_KeyDown;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Active mods events wired");
            }

            if (searchTextBox != null)
            {
                searchTextBox.GotFocus += (s, e) =>
                {
                    if (searchTextBox.Text == "Search mods...")
                    {
                        searchTextBox.Text = "";
                        searchTextBox.ForeColor = Color.FromArgb(222, 214, 196);
                    }
                };

                searchTextBox.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(searchTextBox.Text))
                    {
                        searchTextBox.Text = "Search mods...";
                        searchTextBox.ForeColor = Color.Gray;
                    }
                };

                searchTextBox.TextChanged += SearchMods;
            }

            if (filterComboBox != null) filterComboBox.SelectedIndexChanged += SearchMods;

            System.Diagnostics.Debug.WriteLine("[DEBUG] All event handlers added");
        }

        public void RefreshModLists()
        {
            try
            {
                if (availableModsListBox == null || activeModsListBox == null) return;

                System.Diagnostics.Debug.WriteLine("[DEBUG] === RefreshModLists START ===");

                availableModsListBox.ClearSelected();
                activeModsListBox.ClearSelected();

                var availableMods = modLoader.GetAvailableMods();
                var activeMods = modLoader.GetActiveMods();

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Available mods count: {availableMods?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Active mods count: {activeMods?.Count ?? 0}");

                availableModsListBox.SetDataSourceSafely(availableMods);
                availableModsListBox.DisplayMember = "Name";

                activeModsListBox.SetDataSourceSafely(activeMods);
                activeModsListBox.DisplayMember = "ModName";

                ClearModInfoDisplay();

                SearchMods(this, EventArgs.Empty);

                System.Diagnostics.Debug.WriteLine("[DEBUG] === RefreshModLists END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshModLists error: {ex.Message}");
                ClearModInfoDisplay();
            }
        }

        #region Event Handlers
        private void ActivateButton_Click(object sender, EventArgs e)
        {
            if (availableModsListBox?.SelectedItems == null || availableModsListBox.SelectedItems.Count == 0) return;

            var selectedMods = availableModsListBox.SelectedItems.Cast<ModInfo>().ToList();

            int successCount = 0;
            foreach (var mod in selectedMods)
            {
                try
                {
                    modLoader.ActivateMod(mod);
                    successCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to activate '{mod.Name}': {ex.Message}", "Activation Error");
                    return;
                }
            }

            if (successCount > 0)
            {
                RefreshModLists();
                var modsPanel = contentPanel.Controls.Find("ModsPanel", false).FirstOrDefault();
                if (modsPanel != null)
                {
                    NotificationManager.ShowSmartOperationNotification(modsPanel, "Activated", true, successCount);
                }
            }
        }

        private void DeactivateButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox?.SelectedItems == null || activeModsListBox.SelectedItems.Count == 0) return;

            var selectedMods = activeModsListBox.SelectedItems.Cast<ActiveModInfo>().ToList();

            int successCount = 0;
            foreach (var mod in selectedMods)
            {
                try
                {
                    modLoader.DeactivateMod(mod);
                    successCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to deactivate '{mod.ModName}': {ex.Message}", "Deactivation Error");
                    return;
                }
            }

            if (successCount > 0)
            {
                RefreshModLists();
                var modsPanel = contentPanel.Controls.Find("ModsPanel", false).FirstOrDefault();
                if (modsPanel != null)
                {
                    NotificationManager.ShowSmartOperationNotification(modsPanel, "Deactivated", true, successCount);
                }
            }
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox?.SelectedIndices.Count > 0)
            {
                int selectedIndex = activeModsListBox.SelectedIndices[0]; // Take first selected
                if (selectedIndex > 0)
                {
                    try
                    {
                        var currentList = modLoader.GetActiveMods().ToList();
                        string selectedModName = currentList[selectedIndex].OriginalFolderName;

                        var itemToMove = currentList[selectedIndex];
                        currentList.RemoveAt(selectedIndex);
                        currentList.Insert(selectedIndex - 1, itemToMove);

                        modLoader.UpdateModSearchPaths(currentList);
                        RefreshModLists();
                        activeModsListBox.ClearSelected();
                        for (int i = 0; i < activeModsListBox.Items.Count; i++)
                        {
                            if (activeModsListBox.Items[i] is ActiveModInfo item &&
                                item.OriginalFolderName == selectedModName)
                            {
                                activeModsListBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error moving mod up: {ex.Message}", "Error");
                    }
                }
            }
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox?.SelectedIndices.Count > 0)
            {
                int selectedIndex = activeModsListBox.SelectedIndices[0]; // Take first selected
                var currentList = modLoader.GetActiveMods().ToList();

                if (selectedIndex < currentList.Count - 1)
                {
                    try
                    {
                        string selectedModName = currentList[selectedIndex].OriginalFolderName;

                        var itemToMove = currentList[selectedIndex];
                        currentList.RemoveAt(selectedIndex);
                        currentList.Insert(selectedIndex + 1, itemToMove);

                        modLoader.UpdateModSearchPaths(currentList);
                        RefreshModLists();
                        activeModsListBox.ClearSelected();
                        for (int i = 0; i < activeModsListBox.Items.Count; i++)
                        {
                            if (activeModsListBox.Items[i] is ActiveModInfo item &&
                                item.OriginalFolderName == selectedModName)
                            {
                                activeModsListBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error moving mod down: {ex.Message}", "Error");
                    }
                }
            }
        }
        private void DeleteModButton_Click(object sender, EventArgs e)
        {
            if (availableModsListBox?.SelectedItems == null || availableModsListBox.SelectedItems.Count == 0) return;

            var selectedMods = availableModsListBox.SelectedItems.Cast<ModInfo>().ToList();

            string message = selectedMods.Count == 1
                ? $"Delete '{selectedMods[0].Name}'?"
                : $"Delete {selectedMods.Count} selected mod(s)?";

            var result = MessageBox.Show(message, "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                AnimationSystem.ShowButtonLoadingSpinner(deleteModButton, "Deleting");

                int successCount = 0;
                bool hasErrors = false;

                foreach (var mod in selectedMods)
                {
                    if (modLoader.DeleteModFromLibrary(mod))
                    {
                        successCount++;
                    }
                    else
                    {
                        hasErrors = true;
                    }
                }

                AnimationSystem.StopButtonLoadingSpinner(deleteModButton);
                RefreshModLists();

                if (hasErrors)
                {
                    AnimationSystem.ShowErrorFeedback(deleteModButton);
                }
                else if (successCount > 0)
                {
                    AnimationSystem.ShowSuccessFeedback(deleteModButton, () => {
                        var parentForm = this.availableModsListBox?.FindForm();
                        if (parentForm != null)
                        {
                            AnimationSystem.ShowOperationComplete(parentForm,
                                $"Deleted {successCount} mod(s)", true);
                        }
                    });
                }
            }
        }

        private void AddonsButton_Click(object sender, EventArgs e)
        {
            string addonsPath = modLoader.GetAddonsPath();
            if (!string.IsNullOrEmpty(addonsPath) && Directory.Exists(addonsPath))
                System.Diagnostics.Process.Start(addonsPath);
        }

        private void ImportModsButton_Click(object sender, EventArgs e)
        {
            AnimationSystem.ShowButtonLoadingSpinner(importModsButton, "Importing");

            try
            {
                modLoader.ImportUnmanagedMods();
                RefreshModLists();

                AnimationSystem.StopButtonLoadingSpinner(importModsButton);
                AnimationSystem.ShowSuccessFeedback(importModsButton, () => {
                    var parentForm = this.importModsButton?.FindForm();
                    if (parentForm != null)
                    {
                        AnimationSystem.ShowOperationComplete(parentForm,
                            "Import completed successfully", true);
                    }
                });
            }
            catch (Exception ex)
            {
                AnimationSystem.StopButtonLoadingSpinner(importModsButton);
                AnimationSystem.ShowErrorFeedback(importModsButton);

                var parentForm = this.importModsButton?.FindForm();
                if (parentForm != null)
                {
                    AnimationSystem.ShowOperationComplete(parentForm,
                        "Import failed: " + ex.Message, false);
                }
            }
        }

        private void AvailableModsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (availableModsListBox == null) return;

                ModInfo selectedMod = null;
                int selectedIndex = availableModsListBox.SelectedIndex;

                if (selectedIndex >= 0)
                {
                    if (availableModsListBox.DataSource is System.Collections.IList dataSourceList &&
                        selectedIndex < dataSourceList.Count)
                    {
                        selectedMod = dataSourceList[selectedIndex] as ModInfo;
                    }

                    if (selectedMod == null && selectedIndex < availableModsListBox.Items.Count)
                    {
                        selectedMod = availableModsListBox.Items[selectedIndex] as ModInfo;
                    }
                }

                if (selectedMod != null)
                {
                    UpdateModInfoDisplay(selectedMod);
                }
                else
                {
                    ClearModInfoDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Available selection error: {ex.Message}");
                ClearModInfoDisplay();
            }
        }

        private void ActiveModsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (activeModsListBox == null) return;

                ActiveModInfo selectedActiveMod = null;
                int selectedIndex = activeModsListBox.SelectedIndex;

                if (selectedIndex >= 0)
                {
                    if (activeModsListBox.DataSource is System.Collections.IList dataSourceList &&
                        selectedIndex < dataSourceList.Count)
                    {
                        selectedActiveMod = dataSourceList[selectedIndex] as ActiveModInfo;
                    }

                    if (selectedActiveMod == null && selectedIndex < activeModsListBox.Items.Count)
                    {
                        selectedActiveMod = activeModsListBox.Items[selectedIndex] as ActiveModInfo;
                    }
                }

                if (selectedActiveMod != null)
                {
                    var catalog = modLoader.GetAllCatalogMods();
                    var modInfo = catalog?.FirstOrDefault(m => m.FolderName.Equals(selectedActiveMod.OriginalFolderName, StringComparison.OrdinalIgnoreCase));

                    if (modInfo != null)
                    {
                        UpdateModInfoDisplay(modInfo);
                    }
                    else
                    {
                        UpdateActiveModInfoDisplay(selectedActiveMod);
                    }
                }
                else
                {
                    ClearModInfoDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Active selection error: {ex.Message}");
                ClearModInfoDisplay();
            }
        }

        private void ModListBox_KeyDown(object sender, KeyEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null || listBox.Items == null) return;
            if (e.Control && e.KeyCode == Keys.A)
            {
                try
                {
                    listBox.BeginUpdate();
                    listBox.ClearSelected();

                    int itemCount = listBox.Items.Count;
                    for (int i = 0; i < itemCount; i++)
                    {
                        if (i < listBox.Items.Count)
                        {
                            listBox.SetSelected(i, true);
                        }
                    }
                    listBox.EndUpdate();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                catch (Exception ex)
                {
                    try
                    {
                        listBox.ClearSelected();
                        System.Diagnostics.Debug.WriteLine($"Ctrl+A selection error: {ex.Message}");
                    }
                    catch
                    {
                    }
                    finally
                    {
                        listBox.EndUpdate();
                    }
                }
            }
            else if (e.KeyCode == Keys.Delete && listBox == availableModsListBox)
            {
                if (listBox.SelectedItems != null && listBox.SelectedItems.Count > 0)
                {
                    DeleteModButton_Click(sender, e);
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (listBox.SelectedItems != null && listBox.SelectedItems.Count > 0)
                {
                    if (listBox == availableModsListBox)
                    {
                        ActivateButton_Click(sender, e);
                    }
                    else if (listBox == activeModsListBox)
                    {
                        DeactivateButton_Click(sender, e);
                    }
                }
                e.Handled = true;
            }
        }

        private void SearchMods(object sender, EventArgs e)
        {
            try
            {
                if (availableModsListBox == null || searchTextBox == null || filterComboBox == null) return;

                var searchTerm = searchTextBox.Text == "Search mods..." ? "" : searchTextBox.Text.ToLower();
                var filterType = filterComboBox.Text;

                bool hasValidSearch = !string.IsNullOrWhiteSpace(searchTerm) && searchTerm != "search mods...";
                if (sender == searchTextBox && hasValidSearch)
                {
                    AnimationSystem.ShowValidationFeedback(searchTextBox, true);
                }

                var allMods = modLoader.GetAvailableMods();
                var filteredMods = allMods.Where(mod =>
                {
                    bool matchesSearch = string.IsNullOrEmpty(searchTerm) ||
                        mod.Name.ToLower().Contains(searchTerm) ||
                        mod.Author.ToLower().Contains(searchTerm);

                    bool matchesType = filterType == "All Types" ||
                        (filterType == "VPK Only" && mod.Type == ModType.VpkOnly) ||
                        (filterType == "Directory" && mod.Type == ModType.DirectoryBased) ||
                        (filterType == "Mixed" && mod.Type == ModType.Mixed);

                    return matchesSearch && matchesType;
                }).ToList();

                availableModsListBox.DataSource = null;
                availableModsListBox.DataSource = filteredMods;
                availableModsListBox.DisplayMember = "Name";

                var resultLabel = availableModsListBox.Parent?.Controls.Find("searchResultLabel", true).FirstOrDefault() as Label;
                if (resultLabel != null)
                {
                    resultLabel.Text = $"{filteredMods.Count} mod{(filteredMods.Count != 1 ? "s" : "")} found";

                    if (filteredMods.Count == 0 && hasValidSearch)
                    {
                        resultLabel.ForeColor = Color.FromArgb(200, 100, 100);
                    }
                    else
                    {
                        resultLabel.ForeColor = Color.FromArgb(140, 140, 140);
                    }
                }
            }
            catch { }
        }
        #endregion

        #region Helper Methods
        private void UpdateModInfoDisplay(ModInfo modInfo)
        {
            try
            {
                if (modInfo == null)
                {
                    ClearModInfoDisplay();
                    return;
                }

                if (modNameLabel != null) modNameLabel.Text = modInfo.Name ?? "Unknown Mod";
                if (authorLabel != null) authorLabel.Text = $"by {modInfo.Author ?? "Unknown"}";
                if (descriptionTextBox != null) descriptionTextBox.Text = modInfo.Description ?? "No description available.";

                UpdateModTypeDisplay(modInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateModInfoDisplay error: {ex.Message}");
                ClearModInfoDisplay();
            }
        }

        private void UpdateActiveModInfoDisplay(ActiveModInfo activeMod)
        {
            try
            {
                if (modNameLabel != null) modNameLabel.Text = activeMod.ModName ?? "Unknown Mod";
                if (authorLabel != null) authorLabel.Text = "by Unknown";
                if (descriptionTextBox != null) descriptionTextBox.Text = "Active mod - no description available";
                if (modTypeLabel != null)
                {
                    modTypeLabel.Text = $"Type: {GetModTypeDisplayText(activeMod.Type)}";
                    modTypeLabel.ForeColor = GetModTypeColor(activeMod.Type);
                }
                if (fileCountLabel != null)
                {
                    var prefixCount = activeMod.PakPrefixes?.Count ?? 0;
                    var dirCount = activeMod.ActiveDirectories?.Count ?? 0;
                    fileCountLabel.Text = $"Prefixes: {prefixCount}, Directories: {dirCount}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateActiveModInfoDisplay error: {ex.Message}");
                ClearModInfoDisplay();
            }
        }

        private void UpdateModTypeDisplay(ModInfo mod)
        {
            try
            {
                string typeText = GetModTypeDisplayText(mod.Type);
                if (modTypeLabel != null)
                {
                    modTypeLabel.Text = $"Type: {typeText}";
                    modTypeLabel.ForeColor = GetModTypeColor(mod.Type);
                }

                if (fileCountLabel != null && mod.FileMappings != null)
                {
                    int vpkCount = mod.FileMappings.Count(f => f.Type == FileType.Vpk);
                    int otherCount = mod.FileMappings.Count - vpkCount;

                    var fileCounts = new List<string>();
                    if (vpkCount > 0) fileCounts.Add($"{vpkCount} VPK");
                    if (otherCount > 0) fileCounts.Add($"{otherCount} Other");

                    fileCountLabel.Text = fileCounts.Any() ? $"Files: {string.Join(", ", fileCounts)}" : "Files: None";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateModTypeDisplay error: {ex.Message}");
                if (modTypeLabel != null) modTypeLabel.Text = "Type: Unknown";
                if (fileCountLabel != null) fileCountLabel.Text = "Files: Unknown";
            }
        }

        private void ClearModInfoDisplay()
        {
            try
            {
                if (modNameLabel != null) modNameLabel.Text = "Select a mod to view details";
                if (authorLabel != null) authorLabel.Text = "";
                if (descriptionTextBox != null) descriptionTextBox.Text = "";
                if (modTypeLabel != null) modTypeLabel.Text = "";
                if (fileCountLabel != null) fileCountLabel.Text = "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearModInfoDisplay error: {ex.Message}");
            }
        }

        private string GetModTypeDisplayText(ModType modType)
        {
            switch (modType)
            {
                case ModType.VpkOnly: return "VPK Only";
                case ModType.DirectoryBased: return "Directory";
                case ModType.Mixed: return "Mixed";
                default: return "Unknown";
            }
        }

        private Color GetModTypeColor(ModType modType)
        {
            switch (modType)
            {
                case ModType.VpkOnly: return Color.FromArgb(100, 200, 100);
                case ModType.DirectoryBased: return Color.FromArgb(100, 150, 200);
                case ModType.Mixed: return Color.FromArgb(200, 150, 100);
                default: return Color.FromArgb(180, 180, 180);
            }
        }

        public void LayoutModsUI(Panel modsPanel)
        {
            if (modsPanel == null) return;

            try
            {
                int leftPad = 20;
                int rightPad = 20;
                int gap = 15;
                int topLabelY = 130;
                int listTop = 155;
                int infoHeight = 100;
                int actionPanelWidth = 80;

                int searchControlsY = 80;
                int btnHeight = 22;
                if (importModsButton != null)
                {
                    importModsButton.Location = new Point(modsPanel.ClientSize.Width - rightPad - 90, searchControlsY);
                    importModsButton.Size = new Size(90, btnHeight);
                }
                if (addonsButton != null)
                {
                    addonsButton.Location = new Point(modsPanel.ClientSize.Width - rightPad - 190, searchControlsY);
                    addonsButton.Size = new Size(90, btnHeight);
                }
                if (deleteModButton != null)
                {
                    deleteModButton.Location = new Point(modsPanel.ClientSize.Width - rightPad - 290, searchControlsY);
                    deleteModButton.Size = new Size(90, btnHeight);
                }

                int totalWidth = modsPanel.ClientSize.Width - leftPad - rightPad;
                int availableForLists = totalWidth - actionPanelWidth - (gap * 2);
                int listWidth = availableForLists / 2;

                int calculatedListHeight = modsPanel.ClientSize.Height - listTop - infoHeight - 20;

                var label1 = modsPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Available Mods");
                if (label1 != null)
                {
                    label1.Location = new Point(leftPad, topLabelY);
                }

                if (availableModsListBox != null)
                {
                    availableModsListBox.Location = new Point(leftPad, listTop);
                    availableModsListBox.Size = new Size(listWidth, calculatedListHeight);
                }

                int actionPanelX = leftPad + listWidth + gap;
                int actionPanelY = listTop + (calculatedListHeight - 150) / 2;

                var actionPanel = modsPanel.Controls.OfType<Panel>().FirstOrDefault(p =>
                    p.Controls.Cast<Control>().Any(c => c == activateButton));

                if (actionPanel != null)
                {
                    actionPanel.Location = new Point(actionPanelX, actionPanelY);
                    actionPanel.Size = new Size(actionPanelWidth, 150);

                    if (moveUpButton != null)
                    {
                        moveUpButton.Size = new Size(30, 24);
                        moveUpButton.Location = new Point(5, 90);
                        moveUpButton.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    if (moveDownButton != null)
                    {
                        moveDownButton.Size = new Size(30, 24);
                        moveDownButton.Location = new Point(42, 90); // Fixed alignment
                        moveDownButton.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
                    }
                    if (activateButton != null)
                    {
                        activateButton.Size = new Size(70, 30);
                        activateButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                    if (deactivateButton != null)
                    {
                        deactivateButton.Size = new Size(70, 30);
                        deactivateButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    }
                }

                int activeListX = actionPanelX + actionPanelWidth + gap;
                var label2 = modsPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Active Mods");
                if (label2 != null)
                {
                    label2.Location = new Point(activeListX, topLabelY);
                }

                if (activeModsListBox != null)
                {
                    activeModsListBox.Location = new Point(activeListX, listTop);
                    activeModsListBox.Size = new Size(listWidth, calculatedListHeight);
                }

                if (modInfoPanel != null)
                {
                    int infoPanelY = modsPanel.ClientSize.Height - infoHeight - 10;
                    modInfoPanel.Location = new Point(leftPad, infoPanelY);
                    modInfoPanel.Size = new Size(modsPanel.ClientSize.Width - leftPad - rightPad, infoHeight);

                    if (descriptionTextBox != null)
                    {
                        descriptionTextBox.Size = new Size(modInfoPanel.Width - 24, 30);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LayoutModsUI error: {ex.Message}");
            }
        }
    }
    #endregion
}