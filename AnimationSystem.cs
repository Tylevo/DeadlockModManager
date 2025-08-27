using System;
using System.Drawing;
using System.Windows.Forms;

namespace Deadlock_Mod_Loader2
{
    public static class AnimationSystem
    {
        private static bool animationsEnabled = true;

        public static bool AnimationsEnabled
        {
            get => animationsEnabled;
            set => animationsEnabled = value;
        }

        /// <summary>
        /// Shows a slide-in notification panel
        /// </summary>
        public static void ShowSlideNotification(Control parentControl, string message, Color backgroundColor, int durationMs = 3000)
        {
            if (!animationsEnabled || parentControl == null) return;

            var notification = new Panel
            {
                Size = new Size(Math.Min(400, parentControl.Width - 40), 50),
                BackColor = backgroundColor,
                BorderStyle = BorderStyle.None
            };

            notification.Padding = new Padding(2);

            var innerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor
            };

            var label = new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            innerPanel.Controls.Add(label);
            notification.Controls.Add(innerPanel);

            var finalX = (parentControl.Width - notification.Width) / 2;
            var finalY = 10;
            notification.Location = new Point(finalX, -notification.Height);

            parentControl.Controls.Add(notification);
            notification.BringToFront();

            AnimateMovement(notification, new Point(finalX, finalY), 400, () =>
            {
                var hideTimer = new Timer { Interval = durationMs };
                hideTimer.Tick += (s, e) =>
                {
                    hideTimer.Stop();
                    hideTimer.Dispose();

                    AnimateFadeOut(notification, 300, () =>
                    {
                        try
                        {
                            parentControl.Controls.Remove(notification);
                            notification.Dispose();
                        }
                        catch { }
                    });
                };
                hideTimer.Start();
            });
        }

        /// <summary>
        /// Animates a control moving to a new position
        /// </summary>
        public static void AnimateMovement(Control control, Point targetLocation, int durationMs, Action onComplete = null)
        {
            if (!animationsEnabled || control == null || control.IsDisposed)
            {
                onComplete?.Invoke();
                return;
            }

            var timer = new Timer { Interval = 16 }; // ~60fps
            var startLocation = control.Location;
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (control.IsDisposed)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    var elapsed = DateTime.Now - startTime;
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / durationMs);

                    if (progress >= 1.0)
                    {
                        control.Location = targetLocation;
                        timer.Stop();
                        timer.Dispose();
                        onComplete?.Invoke();
                    }
                    else
                    {
                        var easedProgress = 1 - Math.Pow(1 - progress, 3);
                        var newX = (int)(startLocation.X + (targetLocation.X - startLocation.X) * easedProgress);
                        var newY = (int)(startLocation.Y + (targetLocation.Y - startLocation.Y) * easedProgress);
                        control.Location = new Point(newX, newY);
                    }
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };

            timer.Start();
        }

        /// <summary>
        /// Animates a fade-out effect on a control
        /// </summary>
        public static void AnimateFadeOut(Control control, int durationMs, Action onComplete = null)
        {
            if (!animationsEnabled || control == null || control.IsDisposed)
            {
                onComplete?.Invoke();
                return;
            }

            var timer = new Timer { Interval = 16 };
            var startTime = DateTime.Now;
            var originalColor = control.BackColor;

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (control.IsDisposed)
                    {
                        timer.Stop();
                        timer.Dispose();
                        onComplete?.Invoke();
                        return;
                    }

                    var elapsed = DateTime.Now - startTime;
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / durationMs);

                    if (progress >= 1.0)
                    {
                        timer.Stop();
                        timer.Dispose();
                        onComplete?.Invoke();
                    }
                    else
                    {
                        var alpha = (int)(255 * (1 - progress));
                        control.BackColor = Color.FromArgb(alpha, originalColor.R, originalColor.G, originalColor.B);
                    }
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };

            timer.Start();
        }

        /// <summary>
        /// Shows a loading spinner on a button
        /// </summary>
        public static void ShowButtonLoadingSpinner(Button button, string loadingText = "Loading...")
        {
            if (!animationsEnabled || button == null || button.IsDisposed) return;

            button.Tag = button.Text; // Store original text
            button.Text = loadingText;
            button.Enabled = false;

            var timer = new Timer { Interval = 200 };
            var spinChars = new[] { "|", "/", "-", "\\" };
            var spinIndex = 0;

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (button.IsDisposed || button.Enabled)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    button.Text = $"{loadingText} {spinChars[spinIndex]}";
                    spinIndex = (spinIndex + 1) % spinChars.Length;
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };

            button.Tag = timer; // Store timer for cleanup
            timer.Start();
        }

        /// <summary>
        /// Stops the loading spinner and restores button
        /// </summary>
        public static void StopButtonLoadingSpinner(Button button)
        {
            if (button == null || button.IsDisposed) return;

            if (button.Tag is Timer timer)
            {
                timer.Stop();
                timer.Dispose();
            }

            if (button.Tag is string originalText)
            {
                button.Text = originalText;
            }

            button.Enabled = true;
            button.Tag = null;
        }

        /// <summary>
        /// Shows success feedback on a button
        /// </summary>
        public static void ShowSuccessFeedback(Button button, Action onComplete = null)
        {
            if (!animationsEnabled || button == null || button.IsDisposed)
            {
                onComplete?.Invoke();
                return;
            }

            var originalColor = button.BackColor;
            var successColor = Color.FromArgb(100, 180, 100);

            button.BackColor = successColor;

            var timer = new Timer { Interval = 800 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();

                if (!button.IsDisposed)
                {
                    button.BackColor = originalColor;
                }

                onComplete?.Invoke();
            };
            timer.Start();
        }

        /// <summary>
        /// Shows error feedback on a button
        /// </summary>
        public static void ShowErrorFeedback(Button button, Action onComplete = null)
        {
            if (!animationsEnabled || button == null || button.IsDisposed)
            {
                onComplete?.Invoke();
                return;
            }

            var originalColor = button.BackColor;
            var errorColor = Color.FromArgb(180, 100, 100);

            button.BackColor = errorColor;

            var timer = new Timer { Interval = 800 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();

                if (!button.IsDisposed)
                {
                    button.BackColor = originalColor;
                }

                onComplete?.Invoke();
            };
            timer.Start();
        }

        /// <summary>
        /// Animates a control shaking (for error indication)
        /// </summary>
        public static void AnimateShake(Control control, int intensity = 5, int durationMs = 300)
        {
            if (!animationsEnabled || control == null || control.IsDisposed) return;

            var originalLocation = control.Location;
            var timer = new Timer { Interval = 30 };
            var startTime = DateTime.Now;
            var random = new Random();

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (control.IsDisposed)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    var elapsed = DateTime.Now - startTime;
                    if (elapsed.TotalMilliseconds >= durationMs)
                    {
                        control.Location = originalLocation;
                        timer.Stop();
                        timer.Dispose();
                    }
                    else
                    {
                        var offsetX = random.Next(-intensity, intensity + 1);
                        var offsetY = random.Next(-intensity, intensity + 1);
                        control.Location = new Point(originalLocation.X + offsetX, originalLocation.Y + offsetY);
                    }
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!control.IsDisposed)
                    {
                        control.Location = originalLocation;
                    }
                }
            };

            timer.Start();
        }

        /// <summary>
        /// Pulses a control's background color
        /// </summary>
        public static void AnimatePulse(Control control, Color pulseColor, int durationMs = 1000, int pulses = 2)
        {
            if (!animationsEnabled || control == null || control.IsDisposed) return;

            var originalColor = control.BackColor;
            var timer = new Timer { Interval = 50 };
            var startTime = DateTime.Now;
            var pulseDuration = durationMs / (pulses * 2); // Each pulse has fade in + fade out

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (control.IsDisposed)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    var elapsed = DateTime.Now - startTime;
                    if (elapsed.TotalMilliseconds >= durationMs)
                    {
                        control.BackColor = originalColor;
                        timer.Stop();
                        timer.Dispose();
                    }
                    else
                    {
                        var cycleProgress = (elapsed.TotalMilliseconds % pulseDuration) / pulseDuration;
                        var intensity = Math.Sin(cycleProgress * Math.PI); // 0 to 1 to 0

                        var r = (int)(originalColor.R + (pulseColor.R - originalColor.R) * intensity);
                        var g = (int)(originalColor.G + (pulseColor.G - originalColor.G) * intensity);
                        var b = (int)(originalColor.B + (pulseColor.B - originalColor.B) * intensity);

                        control.BackColor = Color.FromArgb(Math.Max(0, Math.Min(255, r)),
                                                          Math.Max(0, Math.Min(255, g)),
                                                          Math.Max(0, Math.Min(255, b)));
                    }
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                    if (!control.IsDisposed)
                    {
                        control.BackColor = originalColor;
                    }
                }
            };

            timer.Start();
        }

        /// <summary>
        /// Shows operation complete notification
        /// </summary>
        public static void ShowOperationComplete(Control parentControl, string message, bool success = true)
        {
            if (!animationsEnabled || parentControl == null) return;

            var backgroundColor = success ? Color.FromArgb(80, 180, 80) : Color.FromArgb(180, 80, 80);
            ShowSlideNotification(parentControl, message, backgroundColor, 2000);
        }

        /// <summary>
        /// Shows validation feedback
        /// </summary>
        public static void ShowValidationFeedback(Control control, bool isValid)
        {
            if (!animationsEnabled || control == null || control.IsDisposed) return;

            var feedbackColor = isValid ? Color.FromArgb(100, 180, 100) : Color.FromArgb(180, 100, 100);
            AnimatePulse(control, feedbackColor, 600, 1);
        }

        /// <summary>
        /// Animates a progress bar
        /// </summary>
        public static void AnimateProgressBar(ProgressBar progressBar, int targetValue, int durationMs = 500)
        {
            if (!animationsEnabled || progressBar == null || progressBar.IsDisposed) return;

            var startValue = progressBar.Value;
            var timer = new Timer { Interval = 16 };
            var startTime = DateTime.Now;

            timer.Tick += (s, e) =>
            {
                try
                {
                    if (progressBar.IsDisposed)
                    {
                        timer.Stop();
                        timer.Dispose();
                        return;
                    }

                    var elapsed = DateTime.Now - startTime;
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / durationMs);

                    if (progress >= 1.0)
                    {
                        progressBar.Value = Math.Min(progressBar.Maximum, Math.Max(progressBar.Minimum, targetValue));
                        timer.Stop();
                        timer.Dispose();
                    }
                    else
                    {
                        var easedProgress = 1 - Math.Pow(1 - progress, 3);
                        var currentValue = (int)(startValue + (targetValue - startValue) * easedProgress);
                        progressBar.Value = Math.Min(progressBar.Maximum, Math.Max(progressBar.Minimum, currentValue));
                    }
                }
                catch
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };

            timer.Start();
        }

        /// <summary>
        /// Creates a fade transition between two controls
        /// </summary>
        public static void FadeTransition(Control fromControl, Control toControl, int durationMs = 300, Action onComplete = null)
        {
            if (!animationsEnabled || fromControl == null || toControl == null)
            {
                if (fromControl != null) fromControl.Visible = false;
                if (toControl != null) toControl.Visible = true;
                onComplete?.Invoke();
                return;
            }

            try
            {
                toControl.Visible = true;
                toControl.BringToFront();

                AnimateFadeOut(fromControl, durationMs / 2, () =>
                {
                    try
                    {
                        fromControl.Visible = false;
                        onComplete?.Invoke();
                    }
                    catch { }
                });
            }
            catch
            {
                if (fromControl != null) fromControl.Visible = false;
                if (toControl != null) toControl.Visible = true;
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Loads animation settings from application settings
        /// </summary>
        public static void LoadAnimationSettings()
        {
            try
            {
                animationsEnabled = Properties.Settings.Default.EnableAnimations;
            }
            catch
            {
                animationsEnabled = true; // Default to enabled if settings fail to load
            }
        }

        /// <summary>
        /// Saves animation settings to application settings
        /// </summary>
        public static void SaveAnimationSettings()
        {
            try
            {
                Properties.Settings.Default.EnableAnimations = animationsEnabled;
                Properties.Settings.Default.Save();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Smart operation notification - redirects to NotificationManager
        /// </summary>
        [Obsolete("Use NotificationManager.ShowSmartOperationNotification instead")]
        public static void ShowSmartOperationNotification(Control parentControl, string operation, bool success, int count = 1)
        {
            NotificationManager.ShowSmartOperationNotification(parentControl, operation, success, count);
        }
    }
}