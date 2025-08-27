using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class ProfilesTabBuilder
    {
        private readonly Panel contentPanel;
        private readonly ModLoader modLoader;

        private ListBox profilesListBox;
        private TextBox profileNameTextBox;
        private TextBox profileDescriptionTextBox;
        private Button saveProfileButton;
        private Button loadProfileButton;
        private Button deleteProfileButton;
        private Label currentProfileLabel;

        public ProfilesTabBuilder(Panel contentPanel, ModLoader modLoader)
        {
            this.contentPanel = contentPanel;
            this.modLoader = modLoader;
        }

        public void CreateProfilesTab()
        {
            var profilesPanel = new Panel
            {
                Name = "ProfilesPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                Visible = false
            };

            CreateHeader(profilesPanel);
            CreateProfilesList(profilesPanel);
            CreateProfileDetails(profilesPanel);

            contentPanel.Controls.Add(profilesPanel);
        }

        private void CreateHeader(Panel profilesPanel)
        {
            var headerLabel = new Label
            {
                Text = "Mod Profiles",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                Location = new Point(20, 20),
                AutoSize = true
            };

            var subtitleLabel = new Label
            {
                Text = "Save and load different mod configurations",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(20, 50),
                AutoSize = true
            };

            currentProfileLabel = new Label
            {
                Text = $"Current Profile: Default",
                Location = new Point(20, 80),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            profilesPanel.Controls.AddRange(new Control[] { headerLabel, subtitleLabel, currentProfileLabel });
        }

        private void CreateProfilesList(Panel profilesPanel)
        {
            var profilesLabel = new Label
            {
                Text = "Saved Profiles:",
                Location = new Point(20, 110),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            profilesListBox = new ListBox
            {
                Location = new Point(20, 135),
                Size = new Size(250, 220),
                DisplayMember = "Name",
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(222, 214, 196),
                BorderStyle = BorderStyle.FixedSingle
            };

            profilesPanel.Controls.AddRange(new Control[] { profilesLabel, profilesListBox });
        }

        private void CreateProfileDetails(Panel profilesPanel)
        {
            var profileDetailsLabel = new Label
            {
                Text = "Profile Details:",
                Location = new Point(290, 110),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            var nameLabel = new Label
            {
                Text = "Name:",
                Location = new Point(290, 135),
                Size = new Size(50, 20),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            profileNameTextBox = new TextBox
            {
                Location = new Point(290, 155),
                Size = new Size(200, 20),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(222, 214, 196),
                BorderStyle = BorderStyle.FixedSingle
            };

            var descLabel = new Label
            {
                Text = "Description:",
                Location = new Point(290, 185),
                Size = new Size(80, 20),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            profileDescriptionTextBox = new TextBox
            {
                Location = new Point(290, 205),
                Size = new Size(200, 60),
                Multiline = true,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(222, 214, 196),
                BorderStyle = BorderStyle.FixedSingle
            };

            saveProfileButton = new Button
            {
                Text = "Save Current As...",
                Location = new Point(290, 280),
                Size = new Size(95, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            loadProfileButton = new Button
            {
                Text = "Load Profile",
                Location = new Point(395, 280),
                Size = new Size(95, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(63, 63, 70),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            deleteProfileButton = new Button
            {
                Text = "Delete Profile",
                Location = new Point(290, 320),
                Size = new Size(95, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 60, 60),
                ForeColor = Color.FromArgb(222, 214, 196)
            };

            profilesPanel.Controls.AddRange(new Control[] {
                profileDetailsLabel, nameLabel, profileNameTextBox, descLabel, profileDescriptionTextBox,
                saveProfileButton, loadProfileButton, deleteProfileButton
            });
        }

        public void AddEventHandlers()
        {
            if (profilesListBox != null) profilesListBox.SelectedIndexChanged += ProfilesListBox_SelectedIndexChanged;
            if (saveProfileButton != null) saveProfileButton.Click += SaveProfileButton_Click;
            if (loadProfileButton != null) loadProfileButton.Click += LoadProfileButton_Click;
            if (deleteProfileButton != null) deleteProfileButton.Click += DeleteProfileButton_Click;
        }

        public void RefreshProfiles()
        {
            try
            {
                Console.WriteLine("[DEBUG] RefreshProfiles called");

                var profiles = modLoader.GetProfiles();
                Console.WriteLine($"[DEBUG] Found {profiles?.Count ?? 0} profiles");

                if (profilesListBox != null)
                {
                    var selectedProfile = profilesListBox.SelectedItem as ModProfile;
                    string selectedName = selectedProfile?.Name;

                    profilesListBox.DataSource = null;
                    profilesListBox.DataSource = profiles;
                    profilesListBox.DisplayMember = "Name";

                    Console.WriteLine($"[DEBUG] ProfilesListBox updated with {profilesListBox.Items.Count} items");

                    if (!string.IsNullOrEmpty(selectedName))
                    {
                        var profileToSelect = profiles.FirstOrDefault(p => p.Name == selectedName);
                        if (profileToSelect != null)
                        {
                            profilesListBox.SelectedItem = profileToSelect;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[DEBUG] profilesListBox is null!");
                }

                if (currentProfileLabel != null)
                {
                    currentProfileLabel.Text = $"Current Profile: {modLoader.GetCurrentProfileName()}";
                }
                else
                {
                    Console.WriteLine("[DEBUG] currentProfileLabel is null!");
                }

                if (profilesListBox?.SelectedItem == null)
                {
                    if (profileNameTextBox != null) profileNameTextBox.Clear();
                    if (profileDescriptionTextBox != null) profileDescriptionTextBox.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] RefreshProfiles error: {ex.Message}");
                MessageBox.Show($"Error refreshing profiles: {ex.Message}", "Error");
            }
        }

        #region Event Handlers
        private void ProfilesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (profilesListBox?.SelectedItem is ModProfile profile)
            {
                if (profileNameTextBox != null) profileNameTextBox.Text = profile.Name;
                if (profileDescriptionTextBox != null) profileDescriptionTextBox.Text = profile.Description;
            }
        }

        private void SaveProfileButton_Click(object sender, EventArgs e)
        {
            if (profileNameTextBox == null) return;
            string profileName = profileNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string description = profileDescriptionTextBox?.Text.Trim() ?? "";
                modLoader.SaveProfile(profileName, description);
                MessageBox.Show($"Profile '{profileName}' saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                RefreshProfiles();

                var profiles = modLoader.GetProfiles();
                var newProfile = profiles.FirstOrDefault(p => p.Name == profileName);
                if (newProfile != null && profilesListBox != null)
                {
                    profilesListBox.SelectedItem = newProfile;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProfileButton_Click(object sender, EventArgs e)
        {
            if (profilesListBox?.SelectedItem is ModProfile profile)
            {
                var result = MessageBox.Show($"This will deactivate all current mods and load the '{profile.Name}' profile. Continue?",
                                           "Confirm Profile Load", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (modLoader.LoadProfile(profile.Name))
                    {
                        var activeMods = modLoader.GetActiveMods();
                        MessageBox.Show($"Profile '{profile.Name}' loaded successfully!\nActive mods: {activeMods.Count}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        RefreshProfiles();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to load profile '{profile.Name}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a profile to load.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteProfileButton_Click(object sender, EventArgs e)
        {
            if (profilesListBox?.SelectedItem is ModProfile profile)
            {
                if (profile.Name == "Default")
                {
                    MessageBox.Show("Cannot delete the Default profile.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete profile '{profile.Name}'?",
                                           "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    if (modLoader.DeleteProfile(profile.Name))
                    {
                        MessageBox.Show($"Profile '{profile.Name}' deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshProfiles();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to delete profile '{profile.Name}'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a profile to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion
    }
}