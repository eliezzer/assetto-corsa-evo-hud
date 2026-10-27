// Sectors overlay drawing: lap times on top, then S1 / S2 / S3 with their sub-sectors colored purple / green / yellow / gray.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimracingBR
{
    public class SectorsView : FrameworkElement, IOverlayView
    {
        const double PAD = 12, VGAP = 8, SEC_GAP = 8, HEAD_H = 22, SUB_H = 12, SUB_GAP = 3, HEAD_SUB_GAP = 4;
        const double TIME_COL = 150, TIME_ROW = 30, LF = 12, VF = 19;     // label / value font sizes
        const int FLASH_MS = 450;

        public Tele T = new Tele();
        public SectorTracker Tracker;
        public bool Unlocked { get; set; }
        public bool Hover { get; set; }
        public bool ShowTimes = true, ShowLeader = true, ShowSectorTimes = true;
        public Color CPurple = Color.FromRgb(177, 77, 255), CGreen = Color.FromRgb(51, 214, 138), CSlower = Color.FromRgb(98, 98, 108);

        // colors the cells are fading to / from (the same easing idea as the ABS / TC chips)
        Color[] segShown = new Color[0], secShown = new Color[0];
        readonly Color idle = Color.FromRgb(36, 36, 44);

        double contentW, secW, timesH, barY, subW;

        public SectorsView(SectorTracker tracker)
        {
            Tracker = tracker;
            SnapsToDevicePixels = false;
            Relayout();
        }

        public Rect LockBtn { get { return Chrome.LockRect(this, Width, Unlocked); } }

        // The sectors are as wide as the lap times block needs; sub-sector cells share what is left.
        public void Relayout()
        {
            int subs = Tracker.Subs;
            double minSecW = Math.Max(70, subs * 9 + (subs - 1) * SUB_GAP);
            double barW = 3 * minSecW + 2 * SEC_GAP;
            contentW = Math.Max(barW, ShowTimes ? 2 * TIME_COL : 0);
            secW = (contentW - 2 * SEC_GAP) / 3;
            subW = (secW - (subs - 1) * SUB_GAP) / subs;
            timesH = ShowTimes ? 3 * TIME_ROW + VGAP : 0;
            barY = PAD + timesH;
            Width = contentW + 2 * PAD;
            Height = PAD + timesH + HEAD_H + HEAD_SUB_GAP + SUB_H + PAD;
            if (segShown.Length != Tracker.Count)
            {
                segShown = new Color[Tracker.Count]; secShown = new Color[SectorTracker.Main];
                for (int i = 0; i < segShown.Length; i++) segShown[i] = idle;
                for (int i = 0; i < secShown.Length; i++) secShown[i] = idle;
            }
        }

        Color Target(SegState s)
        {
            switch (s)
            {
                case SegState.Purple: return CPurple;
                case SegState.Green: return CGreen;
                case SegState.Slower: return CSlower;
                default: return idle;
            }
        }

        // eased color of a cell, with a short white flash when it has just been crossed
        Color Shown(Color[] cache, int i, SegState state, int stamp)
        {
            Color c = Chrome.Mix(cache[i], Target(state), 0.22);
            cache[i] = c;
            int age = Environment.TickCount - stamp;
            if (state != SegState.None && age >= 0 && age < FLASH_MS) return Chrome.Mix(c, Colors.White, 0.65 * (1 - (double)age / FLASH_MS));
            return c;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Chrome.Panel(dc, Width, Height, Unlocked);
            Brush gray = Chrome.Br(255, 140, 140, 150), white = Chrome.Br(255, 245, 245, 245);
            SectorTracker k = Tracker;
            if (segShown.Length != k.Count) Relayout();

            if (ShowTimes) DrawTimes(dc, gray, white);

            int subs = k.Subs;
            double pulse = 0.5 + 0.5 * Math.Sin(Environment.TickCount / 140.0);
            for (int m = 0; m < SectorTracker.Main; m++)
            {
                double x = PAD + m * (secW + SEC_GAP);
                bool current = k.HasPosition && k.CurSec == m;
                Color hc = Shown(secShown, m, k.Sec[m], k.SecStamp[m]);
                dc.DrawRoundedRectangle(Grad(hc, 0.45, 0.50), null, new Rect(x, barY, secW, HEAD_H), 3, 3);
                Brush ink = k.Sec[m] == SegState.Green ? Chrome.Br(255, 10, 10, 10) : (k.Sec[m] == SegState.Purple || current ? white : Chrome.Br(255, 200, 200, 208));
                Chrome.Text(this, dc, "S" + (m + 1), 14, ink, true, x + secW / 2, barY + (HEAD_H - 14 * 1.25) / 2, 1);
                if (ShowSectorTimes && !double.IsNaN(k.SecMs[m]) && secW >= 110)
                    Chrome.Text(this, dc, (k.SecMs[m] / 1000).ToString("0.000", CultureInfo.InvariantCulture), 11,
                        ink, false, x + secW - 6, barY + (HEAD_H - 11 * 1.25) / 2, 2);
                if (current) dc.DrawRectangle(Chrome.Br(255, 225, 6, 0), null, new Rect(x, barY + HEAD_H - 2, secW, 2));

                double sy = barY + HEAD_H + HEAD_SUB_GAP;
                for (int j = 0; j < subs; j++)
                {
                    int i = m * subs + j;
                    double sx = x + j * (subW + SUB_GAP);
                    Color c = Shown(segShown, i, k.Seg[i], k.SegStamp[i]);
                    dc.DrawRoundedRectangle(Grad(c), null, new Rect(sx, sy, subW, SUB_H), 2, 2);
                    if (k.HasPosition && k.CurSeg == i)
                    {
                        // the car is here: pulsing outline and a bar growing through the cell
                        dc.DrawRectangle(Chrome.Br(70, 255, 255, 255), null, new Rect(sx, sy, subW * k.Progress, SUB_H));
                        dc.DrawRoundedRectangle(null, new Pen(Chrome.Br((byte)(110 + 140 * pulse), 255, 255, 255), 1.4),
                            new Rect(sx - 1, sy - 1, subW + 2, SUB_H + 2), 3, 3);
                    }
                }
            }
            if (!k.HasPosition && T.Note.Length == 0)
                Chrome.Text(this, dc, L.T("no lap position from this game", "sem posição na pista neste jogo"), 10, gray, true, Width / 2, Height - 15, 1);

            if (Unlocked) Chrome.UnlockedBanner(this, dc, Width, Height);
            Chrome.LockButton(this, dc, Unlocked, Hover);
        }

        // vertical gradient, lighter on top and darker at the bottom (the glossy look of the CMRT sectors)
        static Brush Grad(Color c, double up = 0.30, double down = 0.32)
        {
            var b = new LinearGradientBrush(Chrome.Mix(c, Colors.White, up), Chrome.Mix(c, Colors.Black, down), 90);
            b.Freeze(); return b;
        }

        static double Luma(Color c) { return 0.3 * c.R + 0.59 * c.G + 0.11 * c.B; }

        // CURRENT / BEST on the first line, LAST / LEADER on the second, POSITION / LAP on the third
        void DrawTimes(DrawingContext dc, Brush gray, Brush white)
        {
            int best = T.BestLapMs > 0 ? T.BestLapMs : Tracker.OwnBestMs;
            int last = T.LastLapMs > 0 ? T.LastLapMs : Tracker.OwnLastMs;
            Brush cur = !T.LapValid ? Chrome.Br(255, 255, 70, 60) : (Tracker.Ahead ? Chrome.Br(Chrome.Mix(CPurple, Colors.White, 0.25)) : white);   // red = invalid lap, purple = ahead of the best lap
            Item(dc, PAD + 4, PAD, L.T("CURRENT", "ATUAL"), Chrome.Time(T.LapMs), cur);
            Brush bestInk = best > 0 && T.SessionBestMs > 0 && best <= T.SessionBestMs ? Chrome.Br(Chrome.Mix(CPurple, Colors.White, 0.25)) : white;   // purple = fastest lap of the session (where the game shows everyone's times)
            Item(dc, PAD + TIME_COL + 4, PAD, L.T("BEST", "MELHOR"), Chrome.Time(best), bestInk);
            Item(dc, PAD + 4, PAD + TIME_ROW, L.T("LAST", "ÚLTIMA"), Chrome.Time(last), white);
            if (ShowLeader && T.HasLeader) Item(dc, PAD + TIME_COL + 4, PAD + TIME_ROW, L.T("LEADER", "LÍDER"), Chrome.Time(T.LeaderBestMs), white);
            // lap = the one being driven; "/N" only when the game says how many
            string pos = T.Position > 0 ? "P" + T.Position + (T.Drivers > 1 ? "/" + T.Drivers : "") : "--";
            string lap = T.LapCount >= 0 ? (T.LapCount + 1) + (T.SessionLaps > 0 ? "/" + T.SessionLaps : "") : "--";
            Item(dc, PAD + 4, PAD + 2 * TIME_ROW, L.T("POSITION", "POSIÇÃO"), pos, white);
            Item(dc, PAD + TIME_COL + 4, PAD + 2 * TIME_ROW, L.T("LAP", "VOLTA"), lap, white);
        }

        void Item(DrawingContext dc, double x, double y, string label, string value, Brush valueBrush)
        {
            Chrome.Text(this, dc, label, LF, Chrome.Br(255, 140, 140, 150), true, x, y + 9, 0);
            Chrome.Text(this, dc, value, VF, valueBrush, true, x + TIME_COL - 12, y + 3, 2);
        }
    }
}
