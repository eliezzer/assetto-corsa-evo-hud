// Base of every overlay window: transparent always-on-top window that is click-through while locked, can be dragged
// and resized while unlocked, remembers its position / size / opacity in settings.ini (keys start with `prefix`),
// and can be switched on or off.
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace SimracingBR
{
    public abstract class OverlayWindow : Window
    {
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
        [StructLayout(LayoutKind.Sequential)] struct PT { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] struct RC { public int L, T, R, B; }
        [DllImport("user32.dll")] static extern bool GetCursorPos(out PT p);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RC r);
        const int GWL_EXSTYLE = -20, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

        public const double MinScale = 0.4, MaxScale = 3.0;

        protected readonly Settings cfg;
        readonly string prefix, enabledKey;
        readonly FrameworkElement elem;
        readonly IOverlayView iv;
        IntPtr hwnd;
        bool locked = true, hover, resizing;
        double scale = 1.0;
        DispatcherTimer saveTimer;
        DateTime visChangeSince = DateTime.UtcNow;

        public string Title2 { get; private set; }                 // name shown in menus
        public bool Locked { get { return locked; } }
        public bool Hovered { get { return hover; } }
        public double Scale { get { return scale; } }
        public bool Enabled { get; private set; }
        // where the overlay is shown: 0 on the track, 1 on the setup screen, 2 both (only for the overlays that offer the choice)
        public bool HasVisMode { get; private set; }
        public int VisMode { get; private set; }

        public void SetVisMode(int mode)
        {
            VisMode = Math.Max(0, Math.Min(2, mode));
            cfg.Set(prefix + "Vis", VisMode.ToString(CultureInfo.InvariantCulture)); QueueSave();
        }

        // should the overlay be on screen, given whether the track overlays and the setup screen are showing
        public bool WantShown(bool onTrack, bool inSetup)
        {
            if (!HasVisMode) return onTrack;
            return (onTrack && VisMode != 1) || (inSetup && VisMode != 0);
        }

        public event Action LockToggleRequested;     // the lock button on the overlay was clicked
        public event Action ScaleChanged;

        // defaultTop: where the overlay goes the first time, as a distance from the bottom edge of the screen
        protected OverlayWindow(Settings s, string prefix, string enabledKey, string title, FrameworkElement view, double defaultBottom,
                                double defaultCenterX = 0.5, bool defaultEnabled = true, int defaultVis = -1)
        {
            cfg = s; this.prefix = prefix; this.enabledKey = enabledKey; Title2 = title;
            elem = view; iv = (IOverlayView)view;
            Enabled = s.Bool(enabledKey, defaultEnabled);
            HasVisMode = defaultVis >= 0;
            VisMode = HasVisMode ? (int)Math.Max(0, Math.Min(2, s.Num(prefix + "Vis", defaultVis))) : 0;
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
            Topmost = true; ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize; SizeToContent = SizeToContent.WidthAndHeight;
            ShowActivated = false;
            Opacity = Math.Max(0.2, Math.Min(1, s.Num(prefix + "Opacity", 0.94)));
            scale = Math.Max(MinScale, Math.Min(MaxScale, s.Num(prefix + "Scale", 1.0)));
            view.LayoutTransform = new ScaleTransform(scale, scale);
            Content = view;

            double x = s.Num(prefix + "X", double.NaN), y = s.Num(prefix + "Y", double.NaN);
            WindowStartupLocation = WindowStartupLocation.Manual;
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                x = SystemParameters.PrimaryScreenWidth * defaultCenterX - iv.Width * scale / 2;
                y = SystemParameters.PrimaryScreenHeight - iv.Height * scale - defaultBottom;
            }
            Left = x; Top = y;
            defaultBottomOffset = defaultBottom; defaultCenter = defaultCenterX;

            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            MouseWheel += OnWheel;
            SourceInitialized += delegate { hwnd = new WindowInteropHelper(this).Handle; ApplyClickThrough(); };
            Closed += delegate { SaveState(); };
            // create the native window now (without showing it) so click-through is set before the first frame
            new WindowInteropHelper(this).EnsureHandle();
        }

        readonly double defaultBottomOffset, defaultCenter;

        // ---- on / off and visibility ------------------------------------------------------
        public void SetEnabled(bool on)
        {
            Enabled = on; cfg.Set(enabledKey, on ? "1" : "0"); QueueSave();
        }

        // Debounced so the overlay doesn't flicker on short transitions.
        public void Present(bool want)
        {
            want = want && Enabled;
            bool shown = Visibility == Visibility.Visible;
            if (want == shown) { visChangeSince = DateTime.UtcNow; return; }
            if ((DateTime.UtcNow - visChangeSince).TotalMilliseconds < (want ? 150 : 500)) return;
            Visibility = want ? Visibility.Visible : Visibility.Hidden;
            visChangeSince = DateTime.UtcNow;
        }

        // ---- lock / click-through ---------------------------------------------------------
        // Locked: click-through and never takes focus from the game; while the mouse is over it, clickable (for the
        // lock button) but still without focus. Unlocked: normal window that can take focus, so the game's mouse
        // capture doesn't block dragging.
        void ApplyClickThrough()
        {
            if (hwnd == IntPtr.Zero) return;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW;
            ex = locked ? (hover ? ((ex | WS_EX_NOACTIVATE) & ~WS_EX_TRANSPARENT) : (ex | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE))
                         : (ex & ~(WS_EX_TRANSPARENT | WS_EX_NOACTIVATE));
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
        }

        public void SetLocked(bool value)
        {
            if (locked == value) return;
            locked = value;
            iv.Unlocked = !locked;
            ApplyClickThrough();
            if (locked) { resizing = false; Cursor = Cursors.Arrow; SaveState(); }
            else if (Visibility == Visibility.Visible) { Activate(); Topmost = false; Topmost = true; }
            elem.InvalidateVisual();
        }

        // The locked overlay is click-through, so it never sees the mouse. Instead the cursor is polled every frame:
        // while it is over the overlay the window becomes clickable and the lock / unlock button is drawn.
        public void PollHover()
        {
            bool over = false;
            if (hwnd != IntPtr.Zero && Visibility == Visibility.Visible && !resizing)
            {
                PT p; RC r;
                over = GetCursorPos(out p) && GetWindowRect(hwnd, out r) && p.X >= r.L && p.X < r.R && p.Y >= r.T && p.Y < r.B;
            }
            if (over == hover) return;
            hover = over; iv.Hover = over;
            ApplyClickThrough();
            elem.InvalidateVisual();
        }

        // ---- moving and resizing ----------------------------------------------------------
        bool OnGrip()
        {
            Point p = Mouse.GetPosition(elem);
            return p.X > iv.Width - 34 && p.Y > iv.Height - 34;
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (iv.Hover && iv.LockBtn.Contains(Mouse.GetPosition(elem)))
            {
                if (LockToggleRequested != null) LockToggleRequested();
                e.Handled = true; return;
            }
            if (locked) return;
            if (OnGrip()) { resizing = true; CaptureMouse(); return; }
            try { DragMove(); } catch { }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (resizing) { ApplyScale(Mouse.GetPosition(this).X / iv.Width, false); return; }
            if (iv.Hover && iv.LockBtn.Contains(Mouse.GetPosition(elem))) Cursor = Cursors.Hand;
            else if (!locked) Cursor = OnGrip() ? Cursors.SizeNWSE : Cursors.SizeAll;
            else Cursor = Cursors.Arrow;
        }

        void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!resizing) return;
            resizing = false; ReleaseMouseCapture(); SaveState();
        }

        void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (locked) return;
            Step(e.Delta > 0 ? 1.05 : 1 / 1.05);
            e.Handled = true;
        }

        // Anchor = center (so it grows in place) unless the user is dragging the corner grip.
        public void ApplyScale(double s, bool keepCenter)
        {
            s = Math.Max(MinScale, Math.Min(MaxScale, Math.Round(s, 3)));
            if (Math.Abs(s - scale) < 0.0005) return;
            double oldW = iv.Width * scale, oldH = iv.Height * scale, newW = iv.Width * s, newH = iv.Height * s;
            scale = s;
            elem.LayoutTransform = new ScaleTransform(s, s);
            if (keepCenter) { Left -= (newW - oldW) / 2; Top -= (newH - oldH) / 2; }
            if (ScaleChanged != null) ScaleChanged();
        }

        public void Step(double factor) { ApplyScale(scale * factor, true); SaveState(); }

        public void SetOpacity(double v)
        {
            Opacity = Math.Max(0.2, Math.Min(1, v));
            cfg.Set(prefix + "Opacity", Opacity.ToString("0.##", CultureInfo.InvariantCulture)); QueueSave();
        }

        public void ResetPosition()
        {
            Left = SystemParameters.PrimaryScreenWidth * defaultCenter - iv.Width * scale / 2;
            Top = SystemParameters.PrimaryScreenHeight - iv.Height * scale - defaultBottomOffset;
            SaveState();
        }

        // After a show/hide change inside the view: reflow it and keep its center where it was.
        public void Reflow(Action relayout, bool keepCenter = true)
        {
            double oldW = iv.Width * scale, oldH = iv.Height * scale;
            relayout();
            if (keepCenter) { Left -= (iv.Width * scale - oldW) / 2; Top -= (iv.Height * scale - oldH) / 2; }
            elem.InvalidateVisual();
            QueueSave();
        }

        // ---- persistence ------------------------------------------------------------------
        public void SaveState()
        {
            cfg.Set(prefix + "X", Left.ToString("0", CultureInfo.InvariantCulture));
            cfg.Set(prefix + "Y", Top.ToString("0", CultureInfo.InvariantCulture));
            cfg.Set(prefix + "Scale", scale.ToString("0.###", CultureInfo.InvariantCulture));
            try { cfg.Save(); } catch { }
        }

        public void QueueSave()
        {
            if (saveTimer == null)
            {
                saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                saveTimer.Tick += delegate { saveTimer.Stop(); SaveState(); };
            }
            saveTimer.Stop(); saveTimer.Start();
        }
    }
}
