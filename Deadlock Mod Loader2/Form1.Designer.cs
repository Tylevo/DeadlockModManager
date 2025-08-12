namespace Deadlock_Mod_Loader2
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.gamePathTextBox = new System.Windows.Forms.TextBox();
            this.setpathButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.activateButton = new System.Windows.Forms.Button();
            this.deactivateButton = new System.Windows.Forms.Button();
            this.availableModsListBox = new System.Windows.Forms.CheckedListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.Addons = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.modNameLabel = new System.Windows.Forms.Label();
            this.authorLabel = new System.Windows.Forms.Label();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.deleteModButton = new System.Windows.Forms.Button();
            this.activeModsListBox = new System.Windows.Forms.CheckedListBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::Deadlock_Mod_Loader2.Properties.Resources.deadlock;
            this.pictureBox1.Location = new System.Drawing.Point(700, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 95);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // gamePathTextBox
            // 
            this.gamePathTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.gamePathTextBox.Location = new System.Drawing.Point(3, 58);
            this.gamePathTextBox.Name = "gamePathTextBox";
            this.gamePathTextBox.Size = new System.Drawing.Size(546, 20);
            this.gamePathTextBox.TabIndex = 4;
            // 
            // setpathButton
            // 
            this.setpathButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.setpathButton.Location = new System.Drawing.Point(573, 58);
            this.setpathButton.Name = "setpathButton";
            this.setpathButton.Size = new System.Drawing.Size(86, 20);
            this.setpathButton.TabIndex = 5;
            this.setpathButton.Text = "Set Path";
            this.setpathButton.UseVisualStyleBackColor = true;
            this.setpathButton.Click += new System.EventHandler(this.setPathButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Avaliable Mods";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(276, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Active Mods";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.White;
            this.label3.Location = new System.Drawing.Point(612, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Made By Tylevo";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox2.Image = global::Deadlock_Mod_Loader2.Properties.Resources.Deadlock_Mod_Manager;
            this.pictureBox2.Location = new System.Drawing.Point(3, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(416, 42);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 5;
            this.pictureBox2.TabStop = false;
            // 
            // activateButton
            // 
            this.activateButton.Location = new System.Drawing.Point(6, 109);
            this.activateButton.Name = "activateButton";
            this.activateButton.Size = new System.Drawing.Size(76, 31);
            this.activateButton.TabIndex = 8;
            this.activateButton.Text = "Activate";
            this.activateButton.UseVisualStyleBackColor = true;
            this.activateButton.Click += new System.EventHandler(this.activateButton_Click);
            // 
            // deactivateButton
            // 
            this.deactivateButton.Location = new System.Drawing.Point(6, 146);
            this.deactivateButton.Name = "deactivateButton";
            this.deactivateButton.Size = new System.Drawing.Size(76, 31);
            this.deactivateButton.TabIndex = 9;
            this.deactivateButton.Text = "Deactivate";
            this.deactivateButton.UseVisualStyleBackColor = true;
            this.deactivateButton.Click += new System.EventHandler(this.deactivateButton_Click);
            // 
            // availableModsListBox
            // 
            this.availableModsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.availableModsListBox.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.availableModsListBox.FormattingEnabled = true;
            this.availableModsListBox.Location = new System.Drawing.Point(3, 18);
            this.availableModsListBox.Name = "availableModsListBox";
            this.availableModsListBox.Size = new System.Drawing.Size(248, 319);
            this.availableModsListBox.TabIndex = 6;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.deleteModButton);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.deactivateButton);
            this.panel1.Controls.Add(this.Addons);
            this.panel1.Controls.Add(this.activateButton);
            this.panel1.Location = new System.Drawing.Point(555, 104);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(244, 334);
            this.panel1.TabIndex = 3;
            // 
            // Addons
            // 
            this.Addons.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Addons.Location = new System.Drawing.Point(6, 2);
            this.Addons.Name = "Addons";
            this.Addons.Size = new System.Drawing.Size(178, 47);
            this.Addons.TabIndex = 4;
            this.Addons.Text = "Addons";
            this.Addons.UseVisualStyleBackColor = true;
            this.Addons.Click += new System.EventHandler(this.openAddonsFolderButton_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.descriptionTextBox);
            this.groupBox1.Controls.Add(this.authorLabel);
            this.groupBox1.Controls.Add(this.modNameLabel);
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(0, 234);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(241, 100);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Mod Information";
            // 
            // modNameLabel
            // 
            this.modNameLabel.AutoSize = true;
            this.modNameLabel.ForeColor = System.Drawing.Color.White;
            this.modNameLabel.Location = new System.Drawing.Point(6, 16);
            this.modNameLabel.Name = "modNameLabel";
            this.modNameLabel.Size = new System.Drawing.Size(35, 13);
            this.modNameLabel.TabIndex = 0;
            this.modNameLabel.Text = "label1";
            // 
            // authorLabel
            // 
            this.authorLabel.AutoSize = true;
            this.authorLabel.ForeColor = System.Drawing.Color.White;
            this.authorLabel.Location = new System.Drawing.Point(6, 29);
            this.authorLabel.Name = "authorLabel";
            this.authorLabel.Size = new System.Drawing.Size(35, 13);
            this.authorLabel.TabIndex = 1;
            this.authorLabel.Text = "label1";
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Location = new System.Drawing.Point(6, 45);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.ReadOnly = true;
            this.descriptionTextBox.Size = new System.Drawing.Size(188, 49);
            this.descriptionTextBox.TabIndex = 2;
            // 
            // deleteModButton
            // 
            this.deleteModButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteModButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.deleteModButton.Location = new System.Drawing.Point(6, 56);
            this.deleteModButton.Name = "deleteModButton";
            this.deleteModButton.Size = new System.Drawing.Size(178, 47);
            this.deleteModButton.TabIndex = 11;
            this.deleteModButton.Text = "Delete Selected Mod";
            this.deleteModButton.UseVisualStyleBackColor = true;
            this.deleteModButton.Click += new System.EventHandler(this.deleteModButton_Click);
            // 
            // activeModsListBox
            // 
            this.activeModsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.activeModsListBox.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.activeModsListBox.FormattingEnabled = true;
            this.activeModsListBox.Location = new System.Drawing.Point(295, 18);
            this.activeModsListBox.Name = "activeModsListBox";
            this.activeModsListBox.Size = new System.Drawing.Size(248, 319);
            this.activeModsListBox.TabIndex = 7;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.pictureBox2);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Controls.Add(this.gamePathTextBox);
            this.panel2.Controls.Add(this.setpathButton);
            this.panel2.Controls.Add(this.pictureBox1);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(800, 100);
            this.panel2.TabIndex = 13;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.activeModsListBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.availableModsListBox, 0, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 106);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4.347826F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95.65218F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(546, 345);
            this.tableLayoutPanel1.TabIndex = 14;
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gray;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Deadlock Mod Manager";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox gamePathTextBox;
        private System.Windows.Forms.Button setpathButton;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Button activateButton;
        private System.Windows.Forms.Button deactivateButton;
        private System.Windows.Forms.CheckedListBox availableModsListBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button deleteModButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.Label authorLabel;
        private System.Windows.Forms.Label modNameLabel;
        private System.Windows.Forms.Button Addons;
        private System.Windows.Forms.CheckedListBox activeModsListBox;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}

