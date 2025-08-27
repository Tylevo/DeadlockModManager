using System;
using System.Drawing;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class CollectionInstallForm : Form
    {
        public CollectionInstallForm()
        {
            SetupForm();
            ApplyStyles();
        }

        private void SetupForm()
        {
            SuspendLayout();

            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(45, 45, 48);
            ClientSize = new Size(450, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Multiple Mods Detected";
            Name = "CollectionInstallForm";

            var titleLabel = new Label
            {
                Text = "Multiple mods found in archive",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 20),
                Size = new Size(410, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var descLabel = new Label
            {
                Text = "This archive contains multiple mod files. How would you like to install them?",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(222, 214, 196),
                BackColor = Color.Transparent,
                Location = new Point(20, 55),
                Size = new Size(410, 40),
                TextAlign = ContentAlignment.TopCenter
            };

            var collectionButton = new Button
            {
                Text = "Install as Collection\n(Single mod entry)",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(30, 110),
                Size = new Size(160, 50),
                DialogResult = DialogResult.Yes,
                UseVisualStyleBackColor = false
            };

            var individualButton = new Button
            {
                Text = "Install Separately\n(Multiple mod entries)",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(200, 110),
                Size = new Size(160, 50),
                DialogResult = DialogResult.No,
                UseVisualStyleBackColor = false
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(375, 110),
                Size = new Size(60, 50),
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = false
            };

            Controls.Add(titleLabel);
            Controls.Add(descLabel);
            Controls.Add(collectionButton);
            Controls.Add(individualButton);
            Controls.Add(cancelButton);

            ResumeLayout(false);
        }

        private void ApplyStyles()
        {
            Color logoBeige = Color.FromArgb(222, 214, 196);
            Color buttonBackground = Color.FromArgb(63, 63, 70);

            foreach (Control control in Controls)
            {
                if (control is Button btn)
                {
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.BorderColor = logoBeige;
                    btn.BackColor = buttonBackground;
                    btn.ForeColor = logoBeige;
                }
            }
        }
    }
}