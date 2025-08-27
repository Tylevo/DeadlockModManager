using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public class ThemeColors
    {
        public Color PrimaryBackground { get; set; }
        public Color SecondaryBackground { get; set; }
        public Color TertiaryBackground { get; set; }
        public Color SidebarBackground { get; set; }
        public Color SidebarHeaderBackground { get; set; }
        public Color ContentBackground { get; set; }

        public Color PrimaryText { get; set; }
        public Color SecondaryText { get; set; }
        public Color AccentText { get; set; }
        public Color HeaderText { get; set; }
        public Color SubtitleText { get; set; }

        public Color ButtonBackground { get; set; }
        public Color ButtonHover { get; set; }
        public Color ButtonPressed { get; set; }
        public Color TextBoxBackground { get; set; }
        public Color BorderColor { get; set; }
        public Color FocusBorder { get; set; }

        public Color SuccessColor { get; set; }
        public Color WarningColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color InfoColor { get; set; }

        public Color SelectionBackground { get; set; }
        public Color HoverBackground { get; set; }
        public Color ActiveBackground { get; set; }

        public Color StatusBarBackground { get; set; }
        public Color ProgressBarFill { get; set; }

        public Color VpkModColor { get; set; }
        public Color DirectoryModColor { get; set; }
        public Color MixedModColor { get; set; }
    }

    public class Theme
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ThemeColors Colors { get; set; }
        public bool IsHighContrast { get; set; }
        public bool IsDarkTheme { get; set; }
    }

    public static class ThemeManager
    {
        private static Dictionary<string, Theme> themes = new Dictionary<string, Theme>();
        private static Theme currentTheme;
        private static Form mainForm;

        static ThemeManager()
        {
            InitializeThemes();
        }

        private static void InitializeThemes()
        {
            themes["Default Dark"] = new Theme
            {
                Name = "Default Dark",
                Description = "The classic dark theme",
                IsDarkTheme = true,
                IsHighContrast = false,
                Colors = new ThemeColors
                {
                    PrimaryBackground = Color.FromArgb(30, 30, 30),
                    SecondaryBackground = Color.FromArgb(45, 45, 48),
                    TertiaryBackground = Color.FromArgb(37, 37, 38),
                    SidebarBackground = Color.FromArgb(37, 37, 38),
                    SidebarHeaderBackground = Color.FromArgb(30, 30, 30),
                    ContentBackground = Color.FromArgb(45, 45, 48),

                    PrimaryText = Color.FromArgb(222, 214, 196),
                    SecondaryText = Color.FromArgb(180, 180, 180),
                    AccentText = Color.FromArgb(222, 214, 196),
                    HeaderText = Color.FromArgb(222, 214, 196),
                    SubtitleText = Color.FromArgb(160, 160, 160),

                    ButtonBackground = Color.FromArgb(63, 63, 70),
                    ButtonHover = Color.FromArgb(80, 80, 87),
                    ButtonPressed = Color.FromArgb(50, 50, 57),
                    TextBoxBackground = Color.FromArgb(60, 60, 60),
                    BorderColor = Color.FromArgb(222, 214, 196),
                    FocusBorder = Color.FromArgb(100, 150, 200),

                    SuccessColor = Color.FromArgb(100, 200, 100),
                    WarningColor = Color.FromArgb(200, 150, 100),
                    ErrorColor = Color.FromArgb(200, 100, 100),
                    InfoColor = Color.FromArgb(100, 150, 200),

                    SelectionBackground = Color.FromArgb(63, 63, 70),
                    HoverBackground = Color.FromArgb(60, 60, 65),
                    ActiveBackground = Color.FromArgb(50, 50, 53),

                    StatusBarBackground = Color.FromArgb(37, 37, 38),
                    ProgressBarFill = Color.FromArgb(100, 150, 200),

                    VpkModColor = Color.FromArgb(100, 200, 100),
                    DirectoryModColor = Color.FromArgb(100, 150, 200),
                    MixedModColor = Color.FromArgb(200, 150, 100)
                }
            };

            themes["Midnight Blue"] = new Theme
            {
                Name = "Midnight Blue",
                Description = "Cool blue tones for night coding",
                IsDarkTheme = true,
                IsHighContrast = false,
                Colors = new ThemeColors
                {
                    PrimaryBackground = Color.FromArgb(15, 25, 35),
                    SecondaryBackground = Color.FromArgb(25, 35, 50),
                    TertiaryBackground = Color.FromArgb(20, 30, 45),
                    SidebarBackground = Color.FromArgb(20, 30, 45),
                    SidebarHeaderBackground = Color.FromArgb(15, 25, 35),
                    ContentBackground = Color.FromArgb(25, 35, 50),

                    PrimaryText = Color.FromArgb(220, 235, 255),
                    SecondaryText = Color.FromArgb(180, 200, 220),
                    AccentText = Color.FromArgb(120, 180, 255),
                    HeaderText = Color.FromArgb(220, 235, 255),
                    SubtitleText = Color.FromArgb(150, 170, 190),

                    ButtonBackground = Color.FromArgb(40, 60, 80),
                    ButtonHover = Color.FromArgb(60, 80, 100),
                    ButtonPressed = Color.FromArgb(30, 50, 70),
                    TextBoxBackground = Color.FromArgb(35, 50, 70),
                    BorderColor = Color.FromArgb(120, 180, 255),
                    FocusBorder = Color.FromArgb(80, 160, 255),

                    SuccessColor = Color.FromArgb(80, 200, 120),
                    WarningColor = Color.FromArgb(255, 180, 80),
                    ErrorColor = Color.FromArgb(255, 120, 120),
                    InfoColor = Color.FromArgb(120, 180, 255),

                    SelectionBackground = Color.FromArgb(40, 60, 80),
                    HoverBackground = Color.FromArgb(35, 55, 75),
                    ActiveBackground = Color.FromArgb(30, 45, 65),

                    StatusBarBackground = Color.FromArgb(20, 30, 45),
                    ProgressBarFill = Color.FromArgb(120, 180, 255),

                    VpkModColor = Color.FromArgb(80, 200, 120),
                    DirectoryModColor = Color.FromArgb(120, 180, 255),
                    MixedModColor = Color.FromArgb(255, 180, 80)
                }
            };

            themes["Carbon"] = new Theme
            {
                Name = "Carbon",
                Description = "Sleek carbon fiber inspired theme",
                IsDarkTheme = true,
                IsHighContrast = false,
                Colors = new ThemeColors
                {
                    PrimaryBackground = Color.FromArgb(18, 18, 18),
                    SecondaryBackground = Color.FromArgb(28, 28, 28),
                    TertiaryBackground = Color.FromArgb(22, 22, 22),
                    SidebarBackground = Color.FromArgb(22, 22, 22),
                    SidebarHeaderBackground = Color.FromArgb(18, 18, 18),
                    ContentBackground = Color.FromArgb(28, 28, 28),

                    PrimaryText = Color.FromArgb(240, 240, 240),
                    SecondaryText = Color.FromArgb(180, 180, 180),
                    AccentText = Color.FromArgb(255, 140, 0),
                    HeaderText = Color.FromArgb(240, 240, 240),
                    SubtitleText = Color.FromArgb(140, 140, 140),

                    ButtonBackground = Color.FromArgb(50, 50, 50),
                    ButtonHover = Color.FromArgb(70, 70, 70),
                    ButtonPressed = Color.FromArgb(40, 40, 40),
                    TextBoxBackground = Color.FromArgb(35, 35, 35),
                    BorderColor = Color.FromArgb(255, 140, 0),
                    FocusBorder = Color.FromArgb(255, 160, 40),

                    SuccessColor = Color.FromArgb(46, 204, 113),
                    WarningColor = Color.FromArgb(255, 140, 0),
                    ErrorColor = Color.FromArgb(231, 76, 60),
                    InfoColor = Color.FromArgb(52, 152, 219),

                    SelectionBackground = Color.FromArgb(50, 50, 50),
                    HoverBackground = Color.FromArgb(45, 45, 45),
                    ActiveBackground = Color.FromArgb(40, 40, 40),

                    StatusBarBackground = Color.FromArgb(22, 22, 22),
                    ProgressBarFill = Color.FromArgb(255, 140, 0),

                    VpkModColor = Color.FromArgb(46, 204, 113),
                    DirectoryModColor = Color.FromArgb(52, 152, 219),
                    MixedModColor = Color.FromArgb(255, 140, 0)
                }
            };

            themes["High Contrast Dark"] = new Theme
            {
                Name = "High Contrast Dark",
                Description = "High contrast for accessibility",
                IsDarkTheme = true,
                IsHighContrast = true,
                Colors = new ThemeColors
                {
                    PrimaryBackground = Color.Black,
                    SecondaryBackground = Color.FromArgb(20, 20, 20),
                    TertiaryBackground = Color.FromArgb(10, 10, 10),
                    SidebarBackground = Color.FromArgb(10, 10, 10),
                    SidebarHeaderBackground = Color.Black,
                    ContentBackground = Color.FromArgb(20, 20, 20),

                    PrimaryText = Color.White,
                    SecondaryText = Color.FromArgb(220, 220, 220),
                    AccentText = Color.Yellow,
                    HeaderText = Color.White,
                    SubtitleText = Color.FromArgb(200, 200, 200),

                    ButtonBackground = Color.FromArgb(60, 60, 60),
                    ButtonHover = Color.FromArgb(100, 100, 100),
                    ButtonPressed = Color.FromArgb(40, 40, 40),
                    TextBoxBackground = Color.FromArgb(40, 40, 40),
                    BorderColor = Color.White,
                    FocusBorder = Color.Yellow,

                    SuccessColor = Color.Lime,
                    WarningColor = Color.Yellow,
                    ErrorColor = Color.Red,
                    InfoColor = Color.Cyan,

                    SelectionBackground = Color.FromArgb(80, 80, 80),
                    HoverBackground = Color.FromArgb(60, 60, 60),
                    ActiveBackground = Color.FromArgb(100, 100, 100),

                    StatusBarBackground = Color.FromArgb(10, 10, 10),
                    ProgressBarFill = Color.Yellow,

                    VpkModColor = Color.Lime,
                    DirectoryModColor = Color.Cyan,
                    MixedModColor = Color.Yellow
                }
            };

            themes["Light"] = new Theme
            {
                Name = "Light",
                Description = "You are clinically insane if you use this",
                IsDarkTheme = false,
                IsHighContrast = false,
                Colors = new ThemeColors
                {
                    PrimaryBackground = Color.FromArgb(250, 250, 250),
                    SecondaryBackground = Color.White,
                    TertiaryBackground = Color.FromArgb(245, 245, 245),
                    SidebarBackground = Color.FromArgb(240, 240, 240),
                    SidebarHeaderBackground = Color.FromArgb(230, 230, 230),
                    ContentBackground = Color.White,

                    PrimaryText = Color.FromArgb(50, 50, 50),
                    SecondaryText = Color.FromArgb(100, 100, 100),
                    AccentText = Color.FromArgb(0, 120, 215),
                    HeaderText = Color.FromArgb(30, 30, 30),
                    SubtitleText = Color.FromArgb(120, 120, 120),

                    ButtonBackground = Color.FromArgb(225, 225, 225),
                    ButtonHover = Color.FromArgb(200, 200, 200),
                    ButtonPressed = Color.FromArgb(180, 180, 180),
                    TextBoxBackground = Color.White,
                    BorderColor = Color.FromArgb(0, 120, 215),
                    FocusBorder = Color.FromArgb(0, 100, 180),

                    SuccessColor = Color.FromArgb(16, 124, 16),
                    WarningColor = Color.FromArgb(157, 93, 0),
                    ErrorColor = Color.FromArgb(196, 43, 28),
                    InfoColor = Color.FromArgb(0, 120, 215),

                    SelectionBackground = Color.FromArgb(230, 230, 230),
                    HoverBackground = Color.FromArgb(240, 240, 240),
                    ActiveBackground = Color.FromArgb(220, 220, 220),

                    StatusBarBackground = Color.FromArgb(240, 240, 240),
                    ProgressBarFill = Color.FromArgb(0, 120, 215),

                    VpkModColor = Color.FromArgb(16, 124, 16),
                    DirectoryModColor = Color.FromArgb(0, 120, 215),
                    MixedModColor = Color.FromArgb(157, 93, 0)
                }
            };

            currentTheme = themes["Default Dark"];
        }

        public static List<Theme> GetAvailableThemes()
        {
            return themes.Values.ToList();
        }

        public static Theme GetCurrentTheme()
        {
            return currentTheme;
        }

        public static void SetTheme(string themeName, Form form = null)
        {
            if (themes.ContainsKey(themeName))
            {
                currentTheme = themes[themeName];
                if (form != null)
                {
                    mainForm = form;
                    ApplyTheme(form);
                }
            }
        }

        public static void ApplyTheme(Form form)
        {
            if (currentTheme == null) return;

            mainForm = form;
            ApplyThemeToControl(form, currentTheme.Colors);
            UpdateAllSidebarButtons(form);
            ApplyThemeToSpecialControls(form);

            form.Invalidate(true);
            form.Update();
        }
        private static void UpdateAllSidebarButtons(Form form)
        {
            try
            {
                foreach (Control control in form.Controls)
                {
                    if (control is Panel sidebarPanel && sidebarPanel.Name == "sidebarPanel")
                    {
                        UpdateSidebarButtonsRecursive(sidebarPanel);
                        break;
                    }
                }
            }
            catch
            {
            }
        }
        private static void UpdateSidebarButtonsRecursive(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is SidebarTabButton button)
                {
                    button.ApplyTheme();
                }
                else if (child.HasChildren)
                {
                    UpdateSidebarButtonsRecursive(child);
                }
            }
        }
        private static void ApplyThemeToSpecialControls(Control control)
        {
            if (control is GameBananaBrowser browser)
            {
                browser.ApplyTheme();
                return;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToSpecialControls(child);
            }
        }
        private static void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            try
            {
                if (control is Form form)
                {
                    form.BackColor = colors.PrimaryBackground;
                    foreach (Control child in form.Controls)
                    {
                        if (child.GetType().Name.Contains("MdiClient") ||
                            child.AccessibleRole == AccessibleRole.TitleBar ||
                            child.AccessibleRole == AccessibleRole.MenuBar)
                        {
                            continue; // Skip system controls
                        }
                        ApplyThemeToControl(child, colors);
                    }
                    return;

                }

                else if (control.Name == "sidebarPanel" || control.Parent?.Name == "sidebarPanel")
                {
                    control.BackColor = colors.SidebarBackground;
                }
                else if (control.Name == "contentPanel")
                {
                    control.BackColor = colors.ContentBackground;
                }
                else if (control is Panel panel)
                {
                    if (panel.Name.Contains("Header") || panel.Parent?.Name.Contains("Header") == true)
                    {
                        panel.BackColor = colors.SidebarHeaderBackground;
                    }
                    else if (panel.Name.Contains("Status") || panel.Dock == DockStyle.Bottom)
                    {
                        panel.BackColor = colors.StatusBarBackground;
                    }
                    else if (panel.Name.EndsWith("Panel"))
                    {
                        panel.BackColor = colors.ContentBackground;
                    }
                    else
                    {
                        panel.BackColor = colors.SecondaryBackground;
                    }
                }
                else if (control is Button btn)
                {
                    if (btn.BackColor == Color.FromArgb(150, 70, 70) || btn.BackColor == Color.FromArgb(120, 60, 60))
                    {
                        btn.BackColor = colors.ErrorColor;
                    }
                    else if (btn.BackColor == Color.FromArgb(70, 120, 70))
                    {
                        btn.BackColor = colors.SuccessColor;
                    }
                    else if (btn.BackColor == Color.FromArgb(70, 70, 120))
                    {
                        btn.BackColor = colors.InfoColor;
                    }
                    else
                    {
                        btn.BackColor = colors.ButtonBackground;
                    }

                    btn.ForeColor = colors.PrimaryText;
                    btn.FlatAppearance.BorderColor = colors.BorderColor;
                    btn.FlatAppearance.MouseOverBackColor = colors.ButtonHover;
                }
                else if (control is TextBox tb)
                {
                    tb.BackColor = colors.TextBoxBackground;
                    tb.ForeColor = colors.PrimaryText;
                }
                else if (control is ComboBox cb)
                {
                    cb.BackColor = colors.TextBoxBackground;
                    cb.ForeColor = colors.PrimaryText;
                }
                else if (control is ListBox lb)
                {
                    lb.BackColor = colors.ActiveBackground;
                    lb.ForeColor = colors.PrimaryText;
                }
                else if (control is Label lbl)
                {
                    if (lbl.Font?.Bold == true && lbl.Font.Size >= 12)
                    {
                        lbl.ForeColor = colors.HeaderText;
                    }
                    else if (lbl.Font?.Size <= 8)
                    {
                        lbl.ForeColor = colors.SubtitleText;
                    }
                    else
                    {
                        lbl.ForeColor = colors.SecondaryText;
                    }
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = colors.HeaderText;
                }
                else if (control is CheckBox chk)
                {
                    chk.ForeColor = colors.SecondaryText;
                }
                else if (control is StatusStrip ss)
                {
                    ss.BackColor = colors.StatusBarBackground;
                    foreach (ToolStripItem item in ss.Items)
                    {
                        item.ForeColor = colors.SecondaryText;
                    }
                }
                else if (control is ProgressBar pb)
                {
                    pb.ForeColor = colors.ProgressBarFill;
                }

                foreach (Control child in control.Controls)
                {
                    ApplyThemeToControl(child, colors);
                }
            }
            catch
            {
            }
        }

        public static void RegisterEnhancedListBox(EnhancedModListBox listBox)
        {
            if (listBox != null)
            {
                UpdateEnhancedListBox(listBox);
            }
        }

        public static void UnregisterEnhancedListBox(EnhancedModListBox listBox)
        {
        }

        public static void RegisterSidebarTabButton(SidebarTabButton button)
        {
            if (button != null && currentTheme != null)
            {
                button.ApplyTheme();
            }
        }

        public static void UnregisterSidebarTabButton(SidebarTabButton button)
        {
        }

        public static void UpdateSidebarTabButton(SidebarTabButton button, bool isSelected)
        {
            if (currentTheme == null) return;

            var colors = currentTheme.Colors;

            if (isSelected)
            {
                button.BackColor = colors.SelectionBackground;
            }
            else
            {
                button.BackColor = colors.SidebarBackground;
            }

            foreach (Control child in button.Controls)
            {
                if (child is Label lbl)
                {
                    lbl.ForeColor = isSelected ? colors.AccentText : colors.SecondaryText;
                }
            }
        }

        public static void UpdateEnhancedListBox(EnhancedModListBox listBox)
        {
            if (currentTheme == null) return;

            var colors = currentTheme.Colors;
            listBox.BackColor = colors.ActiveBackground;
            listBox.ForeColor = colors.PrimaryText;
        }
    }
}