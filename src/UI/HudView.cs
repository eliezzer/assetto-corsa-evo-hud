// HUD drawing: graph, pedal bars, ABS/TC chips, fuel, gear/speed/shift lights, wheel.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace SimracingBR
{
    public class HudView : FrameworkElement, IOverlayView
    {
        const int N = 420;                       // history samples (~7 s at 60 Hz)
        const double PAD = 12, ROW = 104, STRIP = 34, VGAP = 8, HGAP = 14;
        const double FUEL_ROW = 30;
        const double FL = 12, FV = 20, FU = 12;          // fuel label / value / unit font sizes
        const double CHIP_FS = 15;                       // ABS / TC / TCC chip font size
        const double CHIPROW = CHIP_FS + 8;              // chip height

        public Tele T = new Tele();
        public bool Unlocked { get; set; }
        public bool HasTcc = true;               // the game has a TC cut setting: show the TCC chip
        public double AbsDuty, TcDuty;           // 0..1 smoothed share of time the aid is cutting in
        public double LapsLeft, PerLap;          // computed by the model

        // what to show (Settings window / settings.ini); Relayout() reflows the HUD after any change
        public bool ShowGraph = true, ShowBars = true, ShowClutch = true, ShowAids = true,
                    ShowGear = true, ShowSpeed = true, ShowShiftLights = true, ShowWheel = true,
                    ShowFuel = true, ShowFuelPerLap = true, ShowLapsLeft = true,
                    AidColors = true, InvertSteer;
        public Color CClutch, CBrake, CThrottle, CAbs, CTc, CShiftA, CShiftB;
        public ImageSource Wheel;

        // computed by Relayout()
        double colW, colX, colY, colH, contentH, graphW, gearX, wheelX, wheelS;
        double chipsW, infoW, infoH, fuelCardH, compactH;
        int nBars;
        bool rowAids, rowMain, rowFuel, hasCol, hasGear, nothing, compact;

        readonly float[] hGas = new float[N], hBrk = new float[N], hClu = new float[N];
        readonly bool[] hAbs = new bool[N], hTc = new bool[N];
        int head;
        readonly FontFamily font = new FontFamily("Bahnschrift, Segoe UI");

        public HudView()
        {
            SnapsToDevicePixels = false;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);   // smooth wheel downscale/rotation
            Relayout();
        }

        // Lays the visible blocks out left to right: [graph + bars (+ fuel strip below)] [gear/speed] [wheel].
        // Hidden blocks take no space; the HUD shrinks to fit.
        public void Relayout()
        {
            nBars = ShowBars ? (ShowClutch ? 3 : 2) : 0;
            double barsW = nBars > 0 ? 22 * nBars - 6 : 0;
            bool fuelAny = ShowFuel || ShowFuelPerLap || ShowLapsLeft;
            hasGear = ShowGear || ShowSpeed || ShowShiftLights;
            rowAids = false;
            // With the graph: [graph + bars] with the fuel strip below (chips live inside the graph).
            // Without it: bars, ABS/TC/TCC chips and fuel sit side by side, so there is no wide empty strip.
            // (fuel alone next to the dash/wheel also stacks, otherwise a 28 px strip would float in a 104 px row)
            compact = !ShowGraph && (ShowBars || ChipsOn || (fuelAny && (hasGear || ShowWheel)));
            if (compact)
            {
                // info column: fuel card on top, ABS / TC / TCC chips in one row below it (bigger font)
                int nFuel = (ShowFuel ? 1 : 0) + (ShowFuelPerLap ? 1 : 0) + (ShowLapsLeft ? 1 : 0);
                chipsW = ChipsOn ? ChipsW() : 0;
                fuelCardH = nFuel > 0 ? nFuel * FUEL_ROW + 12 : 0;
                double chipsH = ChipsOn ? CHIPROW : 0;
                infoW = Math.Max(nFuel > 0 ? CompactFuelW() : 0, chipsW);
                infoH = fuelCardH + (fuelCardH > 0 && chipsH > 0 ? 6 : 0) + chipsH;
                compactH = Math.Max(ROW, infoH);
                bool info = infoW > 0;
                rowMain = true; rowFuel = false;
                colW = barsW + (nBars > 0 && info ? 12 : 0) + infoW;
                graphW = 0;
            }
            else
            {
                rowMain = ShowGraph || ShowBars;
                rowFuel = fuelAny;
                rowAids = ChipsOn && ShowGraph;      // ABS / TC / TCC chips: own row below the fuel strip
                chipsW = rowAids ? ChipsW() : 0;
                double wMain = (ShowGraph ? 300 : 0) + (ShowGraph && ShowBars ? 10 : 0) + barsW;
                double wFuel = rowFuel ? 20 + (ShowFuel ? FuelItemW(LblFuel, "000.0", "L") : 0) + (ShowFuelPerLap ? FuelItemW(LblPerLap, "0.00", "L") : 0) + (ShowLapsLeft ? FuelItemW(LblLaps, "00.0", "") : 0) : 0;
                colW = Math.Max(wMain, Math.Max(wFuel, chipsW));
                graphW = ShowGraph ? colW - (ShowBars ? barsW + 10 : 0) : 0;   // graph absorbs any extra width
            }

            int rows = (rowAids ? 1 : 0) + (rowMain ? 1 : 0) + (rowFuel ? 1 : 0);
            colH = (rowAids ? CHIPROW : 0) + (rowMain ? ROW : 0) + (rowFuel ? STRIP : 0) + (rows > 1 ? VGAP * (rows - 1) : 0);
            hasCol = rows > 0;
            if (compact) colH = compactH;
            bool right = hasGear || ShowWheel;
            contentH = Math.Max(colH, right ? ROW : 0);
            nothing = !hasCol && !right;
            if (nothing) { Width = 250; Height = 52; return; }

            colX = PAD; colY = PAD + (contentH - colH) / 2;
            double x = PAD + (hasCol ? colW : 0);
            if (hasGear) { gearX = x + (hasCol ? HGAP : 0); x = gearX + GearColW; }
            if (ShowWheel) { wheelX = x + (hasGear ? 8 : (hasCol ? HGAP : 0)); wheelS = Math.Min(136, contentH + 8); x = wheelX + wheelS; }
            Width = x + PAD; Height = contentH + 2 * PAD;
        }

        public void Push()
        {
            hGas[head] = T.Gas; hBrk[head] = T.Brake; hClu[head] = T.Clutch;
            hAbs[head] = T.AbsOn; hTc[head] = T.TcOn;
            head = (head + 1) % N;
        }

        static Brush Br(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        static Brush Br(byte a, byte r, byte g, byte b) { return Br(Color.FromArgb(a, r, g, b)); }
        static Color Mix(Color a, Color b, double k)
        {
            return Color.FromArgb(255, (byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k));
        }

        void Text(DrawingContext dc, string s, double size, Brush b, bool bold, double x, double y, int align)
        {
            var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, b, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            double px = align == 0 ? x : (align == 1 ? x - ft.Width / 2 : x - ft.Width);
            dc.DrawText(ft, new Point(px, y));
        }

        double TextWidth(string s, double size, bool bold)
        {
            var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            return ft.Width;
        }

        Geometry Line(float[] a, bool[] flag, double gx, double gy, double gw, double gh)
        {
            var g = new StreamGeometry();
            using (StreamGeometryContext c = g.Open())
            {
                bool open = false;
                for (int i = 0; i < N; i++)
                {
                    int k = (head + i) % N;
                    if (flag != null && !flag[k])
                    {
                        // keep one trailing point so a run joins the base line
                        if (open && i > 0 && flag[(head + i - 1) % N])
                            c.LineTo(new Point(gx + i * gw / (N - 1), gy + gh - a[k] * gh), true, false);
                        open = false; continue;
                    }
                    var p = new Point(gx + i * gw / (N - 1), gy + gh - a[k] * gh);
                    if (!open)
                    {
                        c.BeginFigure(p, false, false);
                        if (flag != null) c.LineTo(new Point(p.X + 0.5, p.Y), true, false);
                        open = true;
                    }
                    else c.LineTo(p, true, false);
                }
            }
            g.Freeze();
            return g;
        }

        void Pedal(DrawingContext dc, double x, double y, double h, double v, Color baseCol, bool active)
        {
            dc.DrawRoundedRectangle(Br(255, 30, 30, 38), null, new Rect(x, y, 16, h), 2, 2);
            Color c = (AidColors && active) ? (baseCol == CBrake ? CAbs : CTc) : baseCol;
            double fh = Math.Max(0, Math.Min(1, v)) * h;
            if (fh > 0) dc.DrawRoundedRectangle(Br(c), null, new Rect(x, y + h - fh, 16, fh), 2, 2);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush white = Br(255, 245, 245, 245), gray = Br(255, 140, 140, 150);
            dc.DrawRoundedRectangle(Br(235, 14, 14, 18), Unlocked ? new Pen(Br(255, 255, 200, 0), 1.5) { DashStyle = DashStyles.Dash } : null,
                new Rect(0, 0, Width, Height), 6, 6);
            dc.DrawRectangle(Br(Chrome.Stripe), null, new Rect(0, 0, 3, Height));

            if (nothing)
            {
                Text(dc, "Nothing selected", 13, white, true, Width / 2, 8, 1);
                Text(dc, "Tray icon > Settings", 10, gray, false, Width / 2, 28, 1);
                DrawUnlocked(dc); DrawLockButton(dc);
                return;
            }

            double y = colY;
            if (compact) DrawCompact(dc, colX, y);
            else
            {
                if (rowMain)
                {
                    if (ShowGraph) DrawGraph(dc, colX, y, graphW, ROW);
                    if (ShowBars) DrawBars(dc, colX + (ShowGraph ? graphW + 10 : 0), y, ROW);
                    y += ROW + VGAP;
                }
                if (rowFuel) { DrawFuel(dc, colX, y, colW); y += STRIP + VGAP; }
                if (rowAids) DrawAidsRow(dc, colX, y, colW);
            }
            if (hasGear) DrawGear(dc, white, gray);
            if (ShowWheel) DrawWheel(dc);
            DrawUnlocked(dc); DrawLockButton(dc);
        }

        void DrawGraph(DrawingContext dc, double gx, double gy, double gw, double gh)
        {
            dc.DrawRoundedRectangle(Br(10, 255, 255, 255), null, new Rect(gx, gy, gw, gh), 3, 3);
            var grid = new Pen(Br(22, 255, 255, 255), 1);
            for (int i = 0; i <= 4; i++) dc.DrawLine(grid, new Point(gx, gy + gh * i / 4), new Point(gx + gw, gy + gh * i / 4));
            if (ShowClutch) dc.DrawGeometry(null, new Pen(Br(CClutch), 1.6), Line(hClu, null, gx, gy, gw, gh));
            dc.DrawGeometry(null, new Pen(Br(CBrake), 1.8), Line(hBrk, null, gx, gy, gw, gh));
            dc.DrawGeometry(null, new Pen(Br(CThrottle), 1.8), Line(hGas, null, gx, gy, gw, gh));
            if (AidColors)
            {
                dc.DrawGeometry(null, new Pen(Br(CAbs), 3.0), Line(hBrk, hAbs, gx, gy, gw, gh));
                dc.DrawGeometry(null, new Pen(Br(CTc), 3.0), Line(hGas, hTc, gx, gy, gw, gh));
            }
            if (T.Note.Length > 0) Text(dc, T.Note, 10, Br(255, 140, 140, 150), true, gx + 6, gy + gh - 16, 0);
        }

        // no graph: [pedal bars] [fuel card, with the ABS / TC / TCC chips in a row below it]
        void DrawCompact(DrawingContext dc, double x, double y)
        {
            if (nBars > 0) { DrawBars(dc, x, y, compactH); x += 22 * nBars - 6 + 12; }
            double iy = y + (compactH - infoH) / 2;
            if (fuelCardH > 0)
            {
                dc.DrawRoundedRectangle(Br(10, 255, 255, 255), null, new Rect(x, iy, infoW, fuelCardH), 3, 3);
                double ry = iy + 6;
                if (ShowFuel) { FuelRow(dc, x, ry, infoW, LblFuel, T.Fuel > 0 ? T.Fuel.ToString("0.0", CultureInfo.InvariantCulture) : "--", "L"); ry += FUEL_ROW; }
                if (ShowFuelPerLap) { FuelRow(dc, x, ry, infoW, LblPerLap, PerLap > 0 ? PerLap.ToString("0.00", CultureInfo.InvariantCulture) : "--", "L"); ry += FUEL_ROW; }
                if (ShowLapsLeft) FuelRow(dc, x, ry, infoW, LblLaps, LapsLeft > 0 ? LapsLeft.ToString("0.0", CultureInfo.InvariantCulture) : "--", "");
                iy += fuelCardH + 6;
            }
            if (ChipsOn) DrawAidsRow(dc, x, iy, infoW);
        }
        // ABS / TC (/ TCC) chips and the brake bias chip in one row, spread across width w
        // (graph layout: below the fuel strip; compact layout: below the fuel card)
        public bool ShowBias = true;             // show the brake bias chip (when the car and the game have it)
        public bool HasBias;                     // set by the window: the game publishes the brake bias
        bool BiasOn { get { return ShowBias && HasBias; } }
        bool ChipsOn { get { return ShowAids || BiasOn; } }
        double BiasW() { return TextWidth("BB 88.8%", CHIP_FS, true) + 14; }

        void DrawAidsRow(DrawingContext dc, double x, double y, double w)
        {
            var ws = new List<double>();
            var draws = new List<Action<double, double>>();
            if (ShowAids)
            {
                ws.Add(ChipW("ABS", true, CHIP_FS)); draws.Add(delegate (double xr, double cw) { DrawChip(dc, xr, y, cw, "ABS", T.AbsLevel, CAbs, AbsDuty, T.AbsOn, true, CHIP_FS); });
                ws.Add(ChipW("TC", true, CHIP_FS)); draws.Add(delegate (double xr, double cw) { DrawChip(dc, xr, y, cw, "TC", T.TcLevel, CTc, TcDuty, T.TcOn, true, CHIP_FS); });
                if (HasTcc) { ws.Add(ChipW("TCC", false, CHIP_FS)); draws.Add(delegate (double xr, double cw) { DrawChip(dc, xr, y, cw, "TCC", T.TcCutLevel, CTc, TcDuty, T.TcOn, false, CHIP_FS); }); }
            }
            if (BiasOn) { ws.Add(BiasW()); draws.Add(delegate (double xr, double cw) { DrawBiasChip(dc, xr, y, cw); }); }
            double sum = 0; foreach (double v in ws) sum += v;
            double gap = ws.Count > 1 ? (w - sum) / (ws.Count - 1) : 0, right = x;
            for (int i = 0; i < ws.Count; i++) { right += ws[i]; draws[i](right, ws[i]); right += gap; }
        }

        // "BB 58.0%": share of the braking on the front axle (the car's brake bias setting)
        void DrawBiasChip(DrawingContext dc, double xr, double y, double w)
        {
            double h = CHIP_FS + 8;
            dc.DrawRoundedRectangle(Br(Color.FromRgb(36, 36, 44)), null, new Rect(xr - w, y, w, h), 3, 3);
            string txt = "BB " + (T.BrakeBias * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            Text(dc, txt, CHIP_FS, Br(255, 200, 200, 208), true, xr - w / 2, y + (h - CHIP_FS * 1.25) / 2, 1);
        }

        double ChipsW()
        {
            double sum = 0; int n = 0;
            if (ShowAids) { sum += ChipW("ABS", true, CHIP_FS) + ChipW("TC", true, CHIP_FS); n += 2; if (HasTcc) { sum += ChipW("TCC", false, CHIP_FS); n++; } }
            if (BiasOn) { sum += BiasW(); n++; }
            return sum + 5 * Math.Max(0, n - 1);
        }

        // one line of the fuel card: label on the left, value (and unit) right-aligned
        void FuelRow(DrawingContext dc, double x, double y, double w, string label, string val, string unit)
        {
            Brush gray = Br(255, 140, 140, 150);
            double rx = x + w - 10;
            if (unit.Length > 0) { Text(dc, unit, FU, gray, false, rx, y + 9, 2); rx -= TextWidth(unit, FU, false) + 3; }
            Text(dc, val, FV, Br(255, 245, 245, 245), true, rx, y + 3, 2);
            Text(dc, label, FL, gray, true, x + 10, y + 8, 0);
        }

        // labels of the fuel items (the HUD is bilingual like the rest of the UI)
        static string LblFuel { get { return "FUEL"; } }
        static string LblPerLap { get { return L.T("FUEL / LAP", "CONSUMO / VOLTA"); } }
        static string LblLaps { get { return L.T("LAPS LEFT", "VOLTAS RESTANTES"); } }

        // width of one item of the fuel strip: label, widest value, unit and a gap before the next item
        double FuelItemW(string label, string sample, string unit)
        {
            return TextWidth(label, FL, true) + 6 + TextWidth(sample, FV, true) + (unit.Length > 0 ? 2 + TextWidth(unit, FU, false) : 0) + 22;
        }

        // widest line of the fuel card in the compact layout
        double CompactFuelW()
        {
            double w = 0;
            if (ShowFuel) w = Math.Max(w, TextWidth(LblFuel, FL, true) + TextWidth("000.0", FV, true) + TextWidth("L", FU, false) + 3);
            if (ShowFuelPerLap) w = Math.Max(w, TextWidth(LblPerLap, FL, true) + TextWidth("0.00", FV, true) + TextWidth("L", FU, false) + 3);
            if (ShowLapsLeft) w = Math.Max(w, TextWidth(LblLaps, FL, true) + TextWidth("00.0", FV, true));
            return w + 32;
        }

        void DrawBars(DrawingContext dc, double bx, double by, double bh)
        {
            double x = bx;
            if (ShowClutch) { Pedal(dc, x, by, bh, T.Clutch, CClutch, false); x += 22; }
            Pedal(dc, x, by, bh, T.Brake, CBrake, T.AbsOn); x += 22;
            Pedal(dc, x, by, bh, T.Gas, CThrottle, T.TcOn);
        }

        const double CAP = 0.70;                 // digit height / font size (Bahnschrift)
        double GearColW { get { return ShowSpeed ? 124 : 92; } }

        // Draws a number centered on cx with the top of its digits at `top` (not the top of the text box),
        // so gear and speed line up on the same axis. Returns the bottom of the digits.
        double Digits(DrawingContext dc, string s, double size, Brush b, double cx, double top)
        {
            var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(font, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                size, b, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(cx - ft.Width / 2, top - (ft.Baseline - CAP * size)));
            return top + CAP * size;
        }

        void DrawGear(DrawingContext dc, Brush white, Brush gray)
        {
            double gw = GearColW;
            double cx = gearX + (ShowSpeed ? 47 : gw / 2);      // gear and speed digits share this axis (also the center of the shift lights); "km/h" sits to the right
            bool tall = contentH >= 120, both = ShowGear && ShowSpeed;
            double r = T.MaxRpm > 0 ? (double)T.Rpm / T.MaxRpm : 0;
            // shift point: whole bar flashes, alternating the two shift colors
            bool flashA = (DateTime.UtcNow.Millisecond / 100) % 2 == 0;
            bool digits = ShowGear || ShowSpeed;
            if (ShowShiftLights)
            {
                double ly = digits ? PAD : (Height - 6) / 2, lx = cx - 47;
                for (int i = 0; i < 10; i++)
                {
                    bool on = T.ShiftUp || r >= 0.72 + 0.028 * i;
                    Color lc = T.ShiftUp ? (flashA ? CShiftA : CShiftB)
                        : (i < 4 ? Color.FromRgb(40, 220, 90) : (i < 7 ? Color.FromRgb(255, 210, 0) : Color.FromRgb(255, 40, 30)));
                    dc.DrawRoundedRectangle(on ? Br(lc) : Br(255, 40, 40, 48), null, new Rect(lx + i * 9.4, ly, 7.4, 6), 1.5, 1.5);
                }
            }
            // the digits fill the space under the shift lights down to the bottom padding
            double top = PAD + (ShowShiftLights && digits ? 16 : 0), avail = Height - PAD - top, gap = 12;
            double gs = 0, ss = 0;
            if (both) { ss = tall ? 46 : 34; gs = Math.Min(120, (avail - gap - CAP * ss) / CAP); }
            else if (ShowGear) gs = Math.Min(120, avail / CAP);
            else if (ShowSpeed) ss = Math.Min(64, avail / CAP);
            double used = (ShowGear ? CAP * gs : 0) + (both ? gap : 0) + (ShowSpeed ? CAP * ss : 0);
            double y = top + Math.Max(0, (avail - used) / 2);
            if (ShowGear)
            {
                string gs2 = T.Gear <= 0 ? "R" : (T.Gear == 1 ? "N" : (T.Gear - 1).ToString(CultureInfo.InvariantCulture));
                Brush gearBrush = (T.ShiftUp && flashA) ? Br(CShiftA) : ((T.Gear == 1) ? Br(255, 225, 6, 0) : white);
                y = Digits(dc, gs2, gs, gearBrush, cx, y) + gap;
            }
            if (ShowSpeed)
            {
                string sp = Math.Max(0, (int)Math.Round(T.SpeedKmh)).ToString(CultureInfo.InvariantCulture);
                double bottom = Digits(dc, sp, ss, white, cx, y);
                double w1 = TextWidth(sp, ss, true);
                var fu = new FormattedText("km/h", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(font, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    12, gray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(fu, new Point(cx + w1 / 2 + 4, bottom - fu.Baseline));   // unit on the digits' baseline
            }
        }

        void DrawWheel(DrawingContext dc)
        {
            double s = wheelS, wy = (Height - s) / 2, cx2 = wheelX + s / 2, cy2 = wy + s / 2;
            double deg = InvertSteer ? -T.SteerDeg : T.SteerDeg;
            dc.PushTransform(new RotateTransform(deg, cx2, cy2));
            if (Wheel != null) dc.DrawImage(Wheel, new Rect(wheelX, wy, s, s));
            else dc.DrawEllipse(null, new Pen(Br(255, 120, 120, 128), 9), new Point(cx2, cy2), s * 0.31, s * 0.31);
            dc.Pop();
        }

        // fuel strip: one line with the fuel items that are switched on
        void DrawFuel(DrawingContext dc, double x, double y, double w)
        {
            dc.DrawRoundedRectangle(Br(10, 255, 255, 255), null, new Rect(x, y, w, STRIP), 3, 3);
            double ix = x + 10;
            if (ShowFuel) { FuelItem(dc, ix, y, LblFuel, T.Fuel > 0 ? T.Fuel.ToString("0.0", CultureInfo.InvariantCulture) : "--", "L"); ix += FuelItemW(LblFuel, "000.0", "L"); }
            if (ShowFuelPerLap) { FuelItem(dc, ix, y, LblPerLap, PerLap > 0 ? PerLap.ToString("0.00", CultureInfo.InvariantCulture) : "--", "L"); ix += FuelItemW(LblPerLap, "0.00", "L"); }
            if (ShowLapsLeft) FuelItem(dc, ix, y, LblLaps, LapsLeft > 0 ? LapsLeft.ToString("0.0", CultureInfo.InvariantCulture) : "--", "");
        }

        // ---- lock / unlock button, shown while the mouse is over the HUD ----------------
        public bool Hover { get; set; }           // set by the window (the HUD is click-through, so it polls the cursor)
        public Rect LockBtn { get { return Chrome.LockRect(this, Width, Unlocked); } }
        void DrawLockButton(DrawingContext dc) { Chrome.LockButton(this, dc, Unlocked, Hover); }
        void DrawUnlocked(DrawingContext dc) { if (Unlocked) Chrome.UnlockedBanner(this, dc, Width, Height); }

        void FuelItem(DrawingContext dc, double x, double y, string label, string val, string unit)
        {
            Brush gray = Br(255, 140, 140, 150);
            Text(dc, label, FL, gray, true, x, y + 11, 0);
            double vx = x + TextWidth(label, FL, true) + 6;
            Text(dc, val, FV, Br(255, 245, 245, 245), true, vx, y + 5, 0);
            if (unit.Length > 0) Text(dc, unit, FU, gray, false, vx + TextWidth(val, FV, true) + 2, y + 12, 0);
        }

        // Aid chip: "ABS 4 · 12%". Level 0 shows OFF; brightness follows how often the aid is cutting in.
        static string ChipText(string label, int level, double duty, bool showDuty)
        {
            string lv = level < 0 ? "-" : (level == 0 ? "OFF" : level.ToString(CultureInfo.InvariantCulture));
            string txt = label + " " + lv;
            if (showDuty) txt += "  ·  " + ((int)Math.Round(duty * 100)).ToString(CultureInfo.InvariantCulture) + "%";
            return txt;
        }

        // width for the widest text (a 1-digit level and "100%") so the chips don't jitter as the numbers change
        double ChipW(string label, bool showDuty, double fs) { return TextWidth(ChipText(label, 8, 1.0, showDuty), fs, true) + 14; }

        // right edge at xr, fixed width w, font size fs (height = fs + 8)
        void DrawChip(DrawingContext dc, double xr, double y, double w, string label, int level, Color c, double duty, bool now, bool showDuty, double fs)
        {
            double k = now ? 1.0 : Math.Min(1, duty * 1.6);
            Color fill = Mix(Color.FromRgb(36, 36, 44), c, k);
            double h = fs + 8;
            dc.DrawRoundedRectangle(Br(fill), null, new Rect(xr - w, y, w, h), 3, 3);
            Text(dc, ChipText(label, level, duty, showDuty), fs, k > 0.55 ? Br(255, 10, 10, 10) : Br(255, 200, 200, 208), true, xr - w / 2, y + (h - fs * 1.25) / 2, 1);
        }
    }
}
