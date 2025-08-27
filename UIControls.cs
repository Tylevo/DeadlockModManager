using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class SidebarTabButton : Panel
    {
        public string TabName { get; set; }
        public string IconText { get; set; }
        public bool IsSelected { get; set; }

        private Label iconLabel;
        private Label nameLabel;
        private bool isHovered = false;

        public event EventHandler TabClicked;

        public SidebarTabButton(string tabName, string iconText)
        {
            TabName = tabName;
            IconText = iconText;
            SetupUI();
        }

        private void SetupUI()
        {
            this.Size = new Size(190, 45);
            this.BackColor = Color.FromArgb(37, 37, 38);
            this.Cursor = Cursors.Hand;
            this.Margin = new Padding(2);

            iconLabel = new Label
            {
                Text = IconText,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Size = new Size(25, 25),
                Location = new Point(15, 10),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            nameLabel = new Label
            {
                Text = TabName,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(222, 214, 196),
                Size = new Size(120, 20),
                Location = new Point(45, 13),
                BackColor = Color.Transparent
            };

            this.Controls.Add(iconLabel);
            this.Controls.Add(nameLabel);

            this.MouseEnter += OnMouseEnter;
            this.MouseLeave += OnMouseLeave;
            this.Click += OnClick;
            iconLabel.Click += OnClick;
            nameLabel.Click += OnClick;
            iconLabel.MouseEnter += OnMouseEnter;
            nameLabel.MouseEnter += OnMouseEnter;
            iconLabel.MouseLeave += OnMouseLeave;
            nameLabel.MouseLeave += OnMouseLeave;
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            isHovered = true;
            UpdateAppearance();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            isHovered = false;
            UpdateAppearance();
        }

        private void OnClick(object sender, EventArgs e)
        {
            TabClicked?.Invoke(this, EventArgs.Empty);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            var currentTheme = ThemeManager.GetCurrentTheme();
            if (currentTheme?.Colors == null)
            {
                ApplyDefaultColors();
                return;
            }

            if (IsSelected)
            {
                this.BackColor = currentTheme.Colors.SelectionBackground;
                nameLabel.ForeColor = currentTheme.Colors.AccentText;
                iconLabel.ForeColor = currentTheme.Colors.AccentText;
            }
            else if (isHovered)
            {
                this.BackColor = currentTheme.Colors.HoverBackground;
                nameLabel.ForeColor = currentTheme.Colors.SecondaryText;
                iconLabel.ForeColor = currentTheme.Colors.SecondaryText;
            }
            else
            {
                this.BackColor = currentTheme.Colors.SidebarBackground;
                nameLabel.ForeColor = currentTheme.Colors.SecondaryText;
                iconLabel.ForeColor = currentTheme.Colors.SecondaryText;
            }
        }

        private void ApplyDefaultColors()
        {
            if (IsSelected)
            {
                this.BackColor = Color.FromArgb(63, 63, 70);
                nameLabel.ForeColor = Color.FromArgb(222, 214, 196);
                iconLabel.ForeColor = Color.FromArgb(222, 214, 196);
            }
            else if (isHovered)
            {
                this.BackColor = Color.FromArgb(45, 45, 48);
                nameLabel.ForeColor = Color.FromArgb(180, 180, 180);
                iconLabel.ForeColor = Color.FromArgb(160, 160, 160);
            }
            else
            {
                this.BackColor = Color.FromArgb(37, 37, 38);
                nameLabel.ForeColor = Color.FromArgb(180, 180, 180);
                iconLabel.ForeColor = Color.FromArgb(160, 160, 160);
            }
        }

        public void ApplyTheme()
        {
            UpdateAppearance();
        }
    }

    public class EnhancedModListBox : ListBox
    {
        private ImageList typeIcons;

        public EnhancedModListBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 32;
            CreateTypeIcons();

            this.BackColor = Color.FromArgb(37, 37, 38);
            this.ForeColor = Color.White;
            this.ScrollAlwaysVisible = true;
            this.IntegralHeight = false;
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] EnhancedModListBox.OnSelectedIndexChanged - SelectedIndex: {this.SelectedIndex}");

            if (this.SelectedIndex >= 0 && this.SelectedIndex < this.Items.Count)
            {
                var item = this.Items[this.SelectedIndex];
                if (item is ModInfo mod)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Selected item is ModInfo: {mod.Name}");
                }
                else if (item is ActiveModInfo activeMod)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Selected item is ActiveModInfo: {activeMod.ModName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Selected item type: {item?.GetType()?.Name}");
                }
            }

            base.OnSelectedIndexChanged(e);
        }

        protected override void OnClick(EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Click event - SelectedIndex: {this.SelectedIndex}");

            Point mousePos = this.PointToClient(Cursor.Position);
            int clickedIndex = this.IndexFromPoint(mousePos);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Mouse position: {mousePos}, IndexFromPoint: {clickedIndex}");
            if (clickedIndex >= 0 && clickedIndex != this.SelectedIndex && clickedIndex < this.Items.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Mismatch detected! Fixing selection from {this.SelectedIndex} to {clickedIndex}");
                this.SelectedIndex = clickedIndex;
            }

            base.OnClick(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Keyboard navigation - SelectedIndex: {this.SelectedIndex}");
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }

        public void SetDataSourceSafely(object dataSource)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SetDataSourceSafely called with {dataSource?.GetType()?.Name}");

                this.ClearSelected();

                this.DataSource = null;

                this.DisplayMember = "";

                this.Refresh();

                this.DataSource = dataSource;

                System.Diagnostics.Debug.WriteLine($"[DEBUG] SetDataSourceSafely completed. Items.Count: {this.Items.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetDataSourceSafely error: {ex.Message}");
                try
                {
                    this.BeginUpdate();
                    this.Items.Clear();
                    this.DataSource = null;
                    this.DataSource = dataSource;
                    this.EndUpdate();
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"SetDataSourceSafely fallback error: {ex2.Message}");

                    try
                    {
                        this.BeginUpdate();
                        this.Items.Clear();
                        if (dataSource is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                this.Items.Add(item);
                            }
                        }
                        this.EndUpdate();
                    }
                    catch
                    {
                        try
                        {
                            this.Items.Clear();
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        public void UpdateTheme(ThemeColors colors)
        {
            if (colors != null)
            {
                this.BackColor = colors.ActiveBackground;
                this.ForeColor = colors.PrimaryText;
                this.Invalidate();
            }
        }

        private void CreateTypeIcons()
        {
            typeIcons = new ImageList();
            typeIcons.ImageSize = new Size(16, 16);
            typeIcons.ColorDepth = ColorDepth.Depth32Bit;

            typeIcons.Images.Add("vpk", CreateTypeIcon(Color.FromArgb(100, 200, 100)));
            typeIcons.Images.Add("directory", CreateTypeIcon(Color.FromArgb(100, 150, 200)));
            typeIcons.Images.Add("mixed", CreateTypeIcon(Color.FromArgb(200, 150, 100)));
            typeIcons.Images.Add("unknown", CreateTypeIcon(Color.FromArgb(180, 180, 180)));
        }

        private Bitmap CreateTypeIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 3, 3, 10, 10);
                }
                using (var pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                {
                    g.DrawEllipse(pen, 3, 3, 10, 10);
                }
            }
            return bitmap;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;
            var item = Items[e.Index];

            e.DrawBackground();

            var bounds = e.Bounds;

            var textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected ?
                            Color.White : Color.FromArgb(240, 240, 240);
            var secondaryColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected ?
                                Color.FromArgb(220, 220, 220) : Color.FromArgb(180, 180, 180);

            string iconKey = "unknown";
            string modName = "";
            string modInfo = "";

            if (item is ModInfo modInfo_)
            {
                iconKey = GetIconKey(modInfo_.Type);
                modName = modInfo_.Name ?? "Unknown Mod";
                modInfo = $"by {modInfo_.Author ?? "Unknown"} • {modInfo_.FileMappings.Count} files";
            }
            else if (item is ActiveModInfo activeInfo)
            {
                iconKey = GetIconKey(activeInfo.Type);
                modName = activeInfo.ModName ?? "Unknown Mod";
                modInfo = "Active";
            }
            else
            {
                modName = item?.ToString() ?? "Unknown";
                modInfo = "";
            }

            if (typeIcons != null && typeIcons.Images.ContainsKey(iconKey))
            {
                var icon = typeIcons.Images[iconKey];
                e.Graphics.DrawImage(icon, bounds.X + 6, bounds.Y + 8, 16, 16);
            }

            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var nameRect = new RectangleF(bounds.X + 28, bounds.Y + 4, bounds.Width - 32, 16);
                e.Graphics.DrawString(modName, font, brush, nameRect,
                    new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }

            if (!string.IsNullOrEmpty(modInfo))
            {
                using (var font = new Font("Segoe UI", 7.5F, FontStyle.Regular))
                using (var brush = new SolidBrush(secondaryColor))
                {
                    var infoRect = new RectangleF(bounds.X + 28, bounds.Y + 20, bounds.Width - 32, 12);
                    e.Graphics.DrawString(modInfo, font, brush, infoRect,
                        new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                }
            }

            e.DrawFocusRectangle();
        }

        private string GetIconKey(ModType modType)
        {
            switch (modType)
            {
                case ModType.VpkOnly: return "vpk";
                case ModType.DirectoryBased: return "directory";
                case ModType.Mixed: return "mixed";
                default: return "unknown";
            }
        }
    }

    public class FileDropModListBox : EnhancedModListBox
    {
        public FileDropModListBox()
        {
            this.AllowDrop = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            this.DragEnter += OnDragEnter;
            this.DragOver += OnDragOver;
            this.DragDrop += OnDragDrop;

            System.Diagnostics.Debug.WriteLine("[DEBUG] File drop events wired up");
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try
            {
                this.DragEnter -= OnDragEnter;
                this.DragOver -= OnDragOver;
                this.DragDrop -= OnDragDrop;
            }
            catch
            {
            }

            base.OnHandleDestroyed(e);
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                System.Diagnostics.Debug.WriteLine("[DEBUG] File drop detected for installation");
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Handling file drop for installation");
                var form = this.FindForm();
                if (form is Form1 mainForm)
                {
                    try
                    {
                        Point formPoint = form.PointToClient(new Point(e.X, e.Y));
                        var formArgs = new DragEventArgs(e.Data, e.KeyState, formPoint.X, formPoint.Y, e.AllowedEffect, e.Effect);

                        var dragDropMethod = typeof(Form1).GetMethod("Form1_DragDrop",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (dragDropMethod != null)
                        {
                            dragDropMethod.Invoke(mainForm, new object[] { form, formArgs });
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] File drop handling error: {ex.Message}");
                    }
                }
            }
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientMiddle => Color.FromArgb(45, 45, 48);
        public override Color ToolStripBorder => Color.FromArgb(63, 63, 70);
        public override Color ButtonSelectedBorder => Color.FromArgb(222, 214, 196);
        public override Color ButtonSelectedGradientBegin => Color.FromArgb(63, 63, 70);
        public override Color ButtonSelectedGradientEnd => Color.FromArgb(63, 63, 70);
        public override Color ButtonSelectedGradientMiddle => Color.FromArgb(63, 63, 70);
        public override Color ButtonPressedBorder => Color.FromArgb(222, 214, 196);
        public override Color ButtonPressedGradientBegin => Color.FromArgb(80, 80, 87);
        public override Color ButtonPressedGradientEnd => Color.FromArgb(80, 80, 87);
        public override Color ButtonPressedGradientMiddle => Color.FromArgb(80, 80, 87);
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}