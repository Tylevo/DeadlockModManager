using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace Deadlock_Mod_Loader2 // Make sure this matches your project's namespace
{
    public partial class Form1 : Form
    {
        private ModLoader modLoader = new ModLoader();

        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.Paint += new PaintEventHandler(Form1_Paint);
            availableModsListBox.SelectedIndexChanged += new EventHandler(availableModsListBox_SelectedIndexChanged);

            ApplyCustomStyles(this.Controls);
            LoadSavedPath();
        }

        private void RefreshModLists()
        {
            // Store the currently selected items to re-select them after refresh
            var selectedAvailable = availableModsListBox.SelectedItem as ModInfo;
            var selectedActive = activeModsListBox.SelectedItem as ActiveModInfo;
            int selectedActiveIndex = activeModsListBox.SelectedIndex;

            availableModsListBox.DataSource = null;
            List<ModInfo> availableMods = modLoader.GetAvailableMods();
            availableModsListBox.DataSource = availableMods;
            availableModsListBox.DisplayMember = "Name";

            activeModsListBox.DataSource = null;
            List<ActiveModInfo> activeMods = modLoader.GetActiveMods();
            activeModsListBox.DataSource = activeMods;
            activeModsListBox.DisplayMember = "ModName";

            // Attempt to re-select the previously selected available mod
            if (selectedAvailable != null)
            {
                var reselectAvailable = availableMods.FirstOrDefault(m => m.FolderName == selectedAvailable.FolderName);
                if (reselectAvailable != null) availableModsListBox.SelectedItem = reselectAvailable;
            }

            // Attempt to re-select the previously selected active mod by its original index
            if (selectedActive != null)
            {
                if (selectedActiveIndex >= 0 && selectedActiveIndex < activeModsListBox.Items.Count)
                {
                    activeModsListBox.SelectedIndex = selectedActiveIndex;
                }
            }
        }

        private void availableModsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (availableModsListBox.SelectedItem is ModInfo selectedMod)
            {
                modNameLabel.Text = selectedMod.Name;
                authorLabel.Text = $"by {selectedMod.Author}";
                descriptionTextBox.Text = selectedMod.Description;
            }
            else
            {
                modNameLabel.Text = "Mod Name";
                authorLabel.Text = "by Author";
                descriptionTextBox.Text = "Description";
            }
        }

        private void activateButton_Click(object sender, EventArgs e)
        {
            if (availableModsListBox.SelectedItem is ModInfo selectedMod)
            {
                modLoader.ActivateMod(selectedMod);
                RefreshModLists();
            }
        }

        private void deactivateButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox.SelectedItem is ActiveModInfo selectedMod)
            {
                modLoader.DeactivateMod(selectedMod);
                RefreshModLists();
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            int installedCount = 0;
            int skippedCount = 0;

            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLowerInvariant();

                if (extension == ".zip" || extension == ".rar" || extension == ".7z" || extension == ".vpk")
                {
                    if (modLoader.InstallDroppedFile(file))
                    {
                        installedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                else
                {
                    skippedCount++;
                }
            }

            if (installedCount > 0)
            {
                string message = $"{installedCount} mod(s) added to your library.";
                if (skippedCount > 0)
                {
                    message += $" ({skippedCount} file(s) skipped - unsupported format)";
                }
                MessageBox.Show(message, "Success");
                RefreshModLists();
            }
            else if (skippedCount > 0)
            {
                MessageBox.Show($"No mods installed. {skippedCount} file(s) skipped.\nSupported formats: .zip, .rar, .7z, .vpk", "No Mods Installed");
            }
        }

        private void deleteModButton_Click(object sender, EventArgs e)
        {
            if (availableModsListBox.SelectedItem is ModInfo selectedMod)
            {
                var confirmResult = MessageBox.Show($"Are you sure you want to permanently delete '{selectedMod.Name}'?",
                                                      "Confirm Deletion",
                                                      MessageBoxButtons.YesNo);
                if (confirmResult == DialogResult.Yes)
                {
                    if (modLoader.DeleteModFromLibrary(selectedMod))
                    {
                        RefreshModLists();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a mod from the 'Available Mods' list to delete.", "No Mod Selected");
            }
        }

        // ++ IMPLEMENTED ++
        private void moveUpButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox.SelectedItem is ActiveModInfo selectedMod)
            {
                int selectedIndex = activeModsListBox.SelectedIndex;
                if (selectedIndex > 0)
                {
                    var list = activeModsListBox.DataSource as List<ActiveModInfo>;
                    if (list == null) return;

                    var itemToMove = list[selectedIndex];
                    list.RemoveAt(selectedIndex);
                    list.Insert(selectedIndex - 1, itemToMove);

                    modLoader.UpdateActiveModsOrder(list);
                    RefreshModLists();
                    activeModsListBox.SelectedIndex = selectedIndex - 1;
                }
            }
        }

        // ++ IMPLEMENTED ++
        private void moveDownButton_Click(object sender, EventArgs e)
        {
            if (activeModsListBox.SelectedItem is ActiveModInfo selectedMod)
            {
                int selectedIndex = activeModsListBox.SelectedIndex;
                var list = activeModsListBox.DataSource as List<ActiveModInfo>;
                if (list == null) return;

                if (selectedIndex < list.Count - 1)
                {
                    var itemToMove = list[selectedIndex];
                    list.RemoveAt(selectedIndex);
                    list.Insert(selectedIndex + 1, itemToMove);

                    modLoader.UpdateActiveModsOrder(list);
                    RefreshModLists();
                    activeModsListBox.SelectedIndex = selectedIndex + 1;
                }
            }
        }

        #region UI, Styling, and Path Management

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Color color1 = Color.FromArgb(45, 45, 48);
            Color color2 = Color.FromArgb(28, 28, 28);
            Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
            using (LinearGradientBrush brush = new LinearGradientBrush(rect, color1, color2, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }

        private void LoadSavedPath()
        {
            string savedPath = Properties.Settings.Default.GamePath;
            if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
            {
                gamePathTextBox.Text = savedPath;
                setPathButton_Click(this, EventArgs.Empty);
            }
        }

        private void SaveGamePath(string path)
        {
            Properties.Settings.Default.GamePath = path;
            Properties.Settings.Default.Save();
        }

        private void ApplyCustomStyles(Control.ControlCollection controls)
        {
            Color logoBeige = Color.FromArgb(222, 214, 196);
            Color darkBackground = Color.FromArgb(45, 45, 48);
            Color buttonBackground = Color.FromArgb(63, 63, 70);

            foreach (Control control in controls)
            {
                if (control is PictureBox || control is Label || control is GroupBox)
                {
                    control.BackColor = Color.Transparent;
                }

                if (control is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = logoBeige;
                    btn.BackColor = buttonBackground;
                    btn.ForeColor = logoBeige;
                }
                else if (control is ListBox lb)
                {
                    lb.BackColor = darkBackground;
                    lb.ForeColor = logoBeige;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is TextBox tb)
                {
                    tb.BackColor = darkBackground;
                    tb.ForeColor = logoBeige;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                if (control.HasChildren)
                {
                    ApplyCustomStyles(control.Controls);
                }
            }
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Please select your Deadlock game folder";
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    gamePathTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void setPathButton_Click(object sender, EventArgs e)
        {
            string pathFromTextBox = gamePathTextBox.Text;
            if (string.IsNullOrWhiteSpace(pathFromTextBox))
            {
                if (sender != this)
                {
                    MessageBox.Show("Please enter or browse for the game path.", "Path is empty");
                }
                return;
            }
            if (modLoader.SetGamePath(pathFromTextBox))
            {
                if (sender != this)
                {
                    // -- FIXED -- Removed the duplicate message box that was here
                    MessageBox.Show("Deadlock path set successfully!", "Success");
                }
                SaveGamePath(pathFromTextBox);
                modLoader.InitialSetup();
                RefreshModLists();
            }
            else
            {
                MessageBox.Show("Invalid Deadlock directory. Please make sure the path is correct.", "Error");
            }
        }

        private void openAddonsFolderButton_Click(object sender, EventArgs e)
        {
            string addonsPath = modLoader.GetAddonsPath();
            if (!string.IsNullOrEmpty(addonsPath) && Directory.Exists(addonsPath))
            {
                Process.Start(addonsPath);
            }
            else
            {
                MessageBox.Show("Game path not set. Cannot open addons folder.", "Error");
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        #endregion

        private void availableModsListBox_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            // This event handler appears to be a duplicate and is unused.
            // You can likely delete it from the code and from the event handler list in the designer.
        }
    }
}