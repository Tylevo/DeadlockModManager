using System;
using System.Drawing;
using System.Windows.Forms;

public static class NotificationManager
{
    private static bool animationsEnabled = true;
    private static NotificationBatch currentBatch;
    private static readonly object batchLock = new object();

    private class NotificationBatch
    {
        public DateTime StartTime { get; set; }
        public int Count { get; set; }
        public string BaseMessage { get; set; }
        public bool IsSuccess { get; set; }
        public Control ParentControl { get; set; }
        public Panel NotificationPanel { get; set; }
        public Timer BatchTimer { get; set; }
        public bool IsDisposed { get; set; }
    }

    public static void ShowSmartOperationNotification(Control parentControl, string operation, bool success, int count = 1)
    {
        if (!animationsEnabled || parentControl == null || parentControl.IsDisposed)
            return;
        if (parentControl.InvokeRequired)
        {
            parentControl.BeginInvoke(new Action(() => ShowSmartOperationNotification(parentControl, operation, success, count)));
            return;
        }

        lock (batchLock)
        {
            try
            {
                var now = DateTime.Now;
                var batchWindow = TimeSpan.FromSeconds(2);

                if (ShouldBatchWithCurrent(parentControl, operation, success, now, batchWindow))
                {
                    UpdateExistingBatch(count);
                }
                else
                {
                    StartNewBatch(parentControl, operation, success, count);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification error: {ex.Message}");
                try
                {
                    var message = count == 1 ? $"{operation} 1 mod" : $"{operation} {count} mods";
                    var icon = success ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
                    MessageBox.Show(message, success ? "Success" : "Warning", MessageBoxButtons.OK, icon);
                }
                catch { }
            }
        }
    }

    private static bool ShouldBatchWithCurrent(Control parentControl, string operation, bool success, DateTime now, TimeSpan batchWindow)
    {
        return currentBatch != null &&
               !currentBatch.IsDisposed &&
               currentBatch.ParentControl != null &&
               !currentBatch.ParentControl.IsDisposed &&
               currentBatch.ParentControl == parentControl &&
               currentBatch.IsSuccess == success &&
               string.Equals(currentBatch.BaseMessage, operation, StringComparison.OrdinalIgnoreCase) &&
               (now - currentBatch.StartTime) < batchWindow;
    }

    private static void UpdateExistingBatch(int count)
    {
        if (currentBatch == null || currentBatch.IsDisposed)
            return;

        try
        {
            currentBatch.Count += count;
            UpdateBatchNotification();

            if (currentBatch.BatchTimer != null && !currentBatch.IsDisposed)
            {
                currentBatch.BatchTimer.Stop();
                currentBatch.BatchTimer.Start();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update batch error: {ex.Message}");
            SafeFinalizeBatch();
        }
    }

    private static void StartNewBatch(Control parentControl, string operation, bool success, int count)
    {
        SafeFinalizeBatch();

        try
        {
            currentBatch = new NotificationBatch
            {
                StartTime = DateTime.Now,
                Count = count,
                BaseMessage = operation,
                IsSuccess = success,
                ParentControl = parentControl,
                IsDisposed = false
            };

            CreateAndShowNotification(success);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Start batch error: {ex.Message}");
            SafeFinalizeBatch();
        }
    }

    private static void CreateAndShowNotification(bool success)
    {
        if (currentBatch == null || currentBatch.IsDisposed || currentBatch.ParentControl == null)
            return;

        try
        {
            var backgroundColor = success ? Color.FromArgb(80, 180, 80) : Color.FromArgb(180, 80, 80);

            var notification = new Panel
            {
                Size = new Size(Math.Min(350, currentBatch.ParentControl.Width - 40), 45),
                BackColor = backgroundColor,
                BorderStyle = BorderStyle.None
            };

            var label = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            notification.Controls.Add(label);
            currentBatch.NotificationPanel = notification;

            var finalX = Math.Max(0, (currentBatch.ParentControl.Width - notification.Width) / 2);
            var finalY = 10;
            notification.Location = new Point(finalX, -notification.Height);

            currentBatch.ParentControl.Controls.Add(notification);
            notification.BringToFront();

            UpdateBatchNotification();
            AnimateMovement(notification, new Point(finalX, finalY), 300, null);

            SetupBatchTimer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create notification error: {ex.Message}");
            SafeFinalizeBatch();
        }
    }

    private static void SetupBatchTimer()
    {
        if (currentBatch == null || currentBatch.IsDisposed)
            return;

        try
        {
            currentBatch.BatchTimer = new Timer { Interval = 3000 };
            currentBatch.BatchTimer.Tick += (s, e) =>
            {
                lock (batchLock)
                {
                    SafeFinalizeBatch();
                }
            };
            currentBatch.BatchTimer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Timer setup error: {ex.Message}");
            SafeFinalizeBatch();
        }
    }

    private static void UpdateBatchNotification()
    {
        if (currentBatch?.NotificationPanel?.Controls == null ||
            currentBatch.IsDisposed ||
            currentBatch.NotificationPanel.IsDisposed)
            return;

        try
        {
            if (currentBatch.NotificationPanel.Controls.Count > 0)
            {
                var label = currentBatch.NotificationPanel.Controls[0] as Label;
                if (label != null && !label.IsDisposed)
                {
                    var icon = currentBatch.IsSuccess ? "✓" : "✗";
                    var modText = currentBatch.Count == 1 ? "1 mod" : $"{currentBatch.Count} mods";
                    label.Text = $"{icon} {currentBatch.BaseMessage} {modText}";
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update notification error: {ex.Message}");
        }
    }

    private static void SafeFinalizeBatch()
    {
        if (currentBatch == null) return;

        try
        {
            currentBatch.IsDisposed = true;

            if (currentBatch.BatchTimer != null)
            {
                try
                {
                    currentBatch.BatchTimer.Stop();
                    currentBatch.BatchTimer.Dispose();
                }
                catch { }
                currentBatch.BatchTimer = null;
            }

            if (currentBatch.NotificationPanel != null && !currentBatch.NotificationPanel.IsDisposed)
            {
                try
                {
                    AnimateFadeOut(currentBatch.NotificationPanel, 400, () =>
                    {
                        CleanupNotificationPanel();
                    });
                }
                catch
                {
                    CleanupNotificationPanel();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Finalize batch error: {ex.Message}");
        }
        finally
        {
            currentBatch = null;
        }
    }

    private static void CleanupNotificationPanel()
    {
        try
        {
            if (currentBatch?.NotificationPanel != null && !currentBatch.NotificationPanel.IsDisposed)
            {
                if (currentBatch.ParentControl != null && !currentBatch.ParentControl.IsDisposed)
                {
                    if (currentBatch.ParentControl.InvokeRequired)
                    {
                        currentBatch.ParentControl.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (currentBatch?.ParentControl != null && !currentBatch.ParentControl.IsDisposed)
                                {
                                    currentBatch.ParentControl.Controls.Remove(currentBatch.NotificationPanel);
                                }
                            }
                            catch { }

                            try
                            {
                                currentBatch?.NotificationPanel?.Dispose();
                            }
                            catch { }
                        }));
                    }
                    else
                    {
                        currentBatch.ParentControl.Controls.Remove(currentBatch.NotificationPanel);
                        currentBatch.NotificationPanel.Dispose();
                    }
                }
                else
                {
                    currentBatch.NotificationPanel.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cleanup panel error: {ex.Message}");
        }
    }
    private static void AnimateMovement(Control control, Point targetLocation, int duration, Action onComplete)
    {
        if (control == null || control.IsDisposed) return;

        try
        {
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
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration);

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
                }
            };

            timer.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
            control.Location = targetLocation;
            onComplete?.Invoke();
        }
    }

    private static void AnimateFadeOut(Control control, int duration, Action onComplete)
    {
        if (control == null || control.IsDisposed)
        {
            onComplete?.Invoke();
            return;
        }

        try
        {
            var timer = new Timer { Interval = 16 };
            var startTime = DateTime.Now;
            var originalBackColor = control.BackColor;

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
                    var progress = Math.Min(1.0, elapsed.TotalMilliseconds / duration);

                    if (progress >= 1.0)
                    {
                        timer.Stop();
                        timer.Dispose();
                        onComplete?.Invoke();
                    }
                    else
                    {
                        var currentOpacity = 1.0 - progress;
                        var currentY = control.Location.Y - (int)(progress * 20); // Move up slightly

                        control.BackColor = Color.FromArgb(
                            (int)(255 * currentOpacity),
                            originalBackColor.R,
                            originalBackColor.G,
                            originalBackColor.B
                        );

                        control.Location = new Point(control.Location.X, currentY);

                        foreach (Control child in control.Controls)
                        {
                            if (child is Label label)
                            {
                                label.ForeColor = Color.FromArgb(
                                    (int)(255 * currentOpacity),
                                    label.ForeColor.R,
                                    label.ForeColor.G,
                                    label.ForeColor.B
                                );
                            }
                        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fade animation error: {ex.Message}");
            onComplete?.Invoke();
        }
    }

    public static void SetAnimationsEnabled(bool enabled)
    {
        animationsEnabled = enabled;
        if (!enabled)
        {
            lock (batchLock)
            {
                SafeFinalizeBatch();
            }
        }
    }

    public static void Cleanup()
    {
        lock (batchLock)
        {
            SafeFinalizeBatch();
        }
    }
}