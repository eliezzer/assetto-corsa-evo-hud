// Look shared by every overlay: panel background, text helpers, the lock / unlock button and the "unlocked" banner.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SimracingBR
{
    // What the overlay window needs from its view.
    public interface IOverlayView
    {
        double Width { get; }
        double Height { get; }
        bool Hover { get; set; }          // the mouse is over the overlay (set by the window, which polls the cursor)
        bool Unlocked { get; set; }
        Rect LockBtn { get; }
        void InvalidateVisual();
    }

    public static class Chrome
    {
        public static readonly FontFamily Font = new FontFamily("Bahnschrift, Segoe UI");
        public const string StripeDefault = "#E10600";
        public static Color Stripe = Color.FromRgb(225, 6, 0);     // the vertical stripe on the left of every panel (ColorStripe)

        public static Brush Br(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        public static Brush Br(byte a, byte r, byte g, byte b) { return Br(Color.FromArgb(a, r, g, b)); }
        public static Color Mix(Color a, Color b, double k)
        {
            return Color.FromArgb(255, (byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k));
        }

        static FormattedText Make(Visual v, string s, double size, Brush b, bool bold)
        {
            return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(Font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, b, VisualTreeHelper.GetDpi(v).PixelsPerDip);
        }

        // align: 0 left, 1 center, 2 right of x
        public static void Text(Visual v, DrawingContext dc, string s, double size, Brush b, bool bold, double x, double y, int align)
        {
            var ft = Make(v, s, size, b, bold);
            double px = align == 0 ? x : (align == 1 ? x - ft.Width / 2 : x - ft.Width);
            dc.DrawText(ft, new Point(px, y));
        }

        public static double TextWidth(Visual v, string s, double size, bool bold) { return Make(v, s, size, Brushes.White, bold).Width; }

        // m:ss.mmm, or -:--.--- while there is no time
        public static string Time(int ms)
        {
            if (ms <= 0) return "-:--.---";
            return (ms / 60000).ToString(CultureInfo.InvariantCulture) + ":" + ((ms / 1000) % 60).ToString("00", CultureInfo.InvariantCulture)
                   + "." + (ms % 1000).ToString("000", CultureInfo.InvariantCulture);
        }

        // dark rounded panel with the red stripe on the left; dashed amber outline while the overlay is unlocked
        public static void Panel(DrawingContext dc, double w, double h, bool unlocked)
        {
            dc.DrawRoundedRectangle(Br(235, 14, 14, 18), unlocked ? new Pen(Br(255, 255, 200, 0), 1.5) { DashStyle = DashStyles.Dash } : null,
                new Rect(0, 0, w, h), 6, 6);
            dc.DrawRectangle(Br(Stripe), null, new Rect(0, 0, 3, h));
        }

        // ---- lock / unlock button, shown while the mouse is over the overlay ------------
        static string LockText(bool unlocked) { return unlocked ? L.T("LOCK", "TRAVAR") : L.T("UNLOCK", "DESTRAVAR"); }

        public static Rect LockRect(Visual v, double width, bool unlocked)
        {
            double w = TextWidth(v, LockText(unlocked), 11, true) + 32;
            return new Rect(width - w - 8, 6, w, 24);
        }

        public static void LockButton(FrameworkElement v, DrawingContext dc, bool unlocked, bool hover)
        {
            if (!hover) return;
            Rect b = LockRect(v, v.Width, unlocked);
            bool over = b.Contains(Mouse.GetPosition(v));
            Brush amber = Br(255, 255, 200, 0), ink = over ? Br(255, 10, 10, 10) : Br(255, 245, 245, 245);
            dc.DrawRoundedRectangle(over ? amber : Br(240, 30, 30, 38), new Pen(amber, 1), b, 4, 4);
            // padlock: body + shackle (closed while locked, open while unlocked)
            double ix = b.X + 9, iy = b.Y + 6;
            var pen = new Pen(ink, 1.6);
            dc.DrawRoundedRectangle(ink, null, new Rect(ix, iy + 6, 10, 8), 1.5, 1.5);
            var sh = new StreamGeometry();
            using (StreamGeometryContext c = sh.Open())
            {
                double sx = unlocked ? ix + 5 : ix + 2;
                c.BeginFigure(new Point(sx, iy + 6), false, false);
                c.LineTo(new Point(sx, iy + 3), true, false);
                c.ArcTo(new Point(sx + 6, iy + 3), new Size(3, 3), 0, false, SweepDirection.Clockwise, true, false);
                if (!unlocked) c.LineTo(new Point(sx + 6, iy + 6), true, false);
            }
            sh.Freeze();
            dc.DrawGeometry(null, pen, sh);
            Text(v, dc, LockText(unlocked), 11, ink, true, b.X + 24, b.Y + 4, 0);
        }

        public static void UnlockedBanner(Visual v, DrawingContext dc, double w, double h)
        {
            bool wide = w >= 520;
            string msg = wide ? "UNLOCKED  ·  drag to move  ·  scroll or drag corner to resize  ·  Ctrl+Shift+L to lock"
                              : "UNLOCKED  ·  drag / scroll  ·  Ctrl+Shift+L";
            double bw = Math.Min(wide ? 470 : 260, w - 16);
            Rect banner = new Rect(w / 2 - bw / 2, h / 2 - 12, bw, 24);
            dc.DrawRoundedRectangle(Br(235, 255, 200, 0), null, banner, 4, 4);
            Text(v, dc, msg, wide ? 10 : 9, Br(255, 10, 10, 10), true, w / 2, banner.Y + 6, 1);
            // resize grip (bottom-right corner)
            var grip = new StreamGeometry();
            using (StreamGeometryContext c = grip.Open())
            {
                c.BeginFigure(new Point(w - 4, h - 26), true, true);
                c.LineTo(new Point(w - 4, h - 4), true, false);
                c.LineTo(new Point(w - 26, h - 4), true, false);
            }
            grip.Freeze();
            dc.DrawGeometry(Br(255, 255, 200, 0), null, grip);
        }
    }
}
