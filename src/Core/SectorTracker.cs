// Sector / sub-sector timing, computed by the overlay itself.
// The games only publish the 3 main sectors (some not even that), but they all publish where the car is on the lap
// (0..1) and the current lap time. So the lap is cut into 3 main sectors x N sub-sectors and every crossing is timed.
//
// Colors:
//   purple = the best time of the session on that stretch (all drivers where the game publishes their times, else yours),
//   green = your own best there but someone else was faster, gray = slower than your best, dark = not driven yet in this lap.
using System;
using System.Diagnostics;
using System.Globalization;

namespace SimracingBR
{
    public enum SegState { None, Slower, Green, Purple }

    public class SectorTracker
    {
        public const int Main = 3;
        const double Nan = double.NaN;

        readonly Settings cfg;                 // learned sector boundaries are remembered per track here (may be null)
        string trackId = "";

        public int Subs { get; private set; }
        public int Count { get { return Main * Subs; } }

        // what the view shows (indexes: sub-segment 0..Count-1, main sector 0..2)
        public SegState[] Seg, Sec;
        public double[] SegMs, SecMs;          // time of the last crossing, NaN = none
        public int[] SegStamp, SecStamp;       // Environment.TickCount of the last crossing (for the flash)
        public int CurSeg, CurSec;             // where the car is now
        public float Progress;                 // 0..1 through the current sub-segment
        public bool HasPosition;               // the game gives a lap position at all
        public int OwnBestMs, OwnLastMs;       // lap times seen by the tracker (used when the game doesn't publish them)

        // references
        public bool AssumePurple;              // setting: paint your best purple even when the game says nothing about the other drivers
        bool alone = true;                     // you are the only driver in the session (or the game does not say)
        int[] othersSec = new int[Main];       // best main-sector times of all drivers (ms, 0 = the game doesn't say)
        double[] othersSeg, othersEst;         // best times of the other drivers timed by us (sub-sectors / main sectors, NaN = none yet)
        bool tracking;                         // the game says where the other cars are, so they are timed here too
        double[] secT, secBest;                // what each main sector was scored with (to score it again)
        int wrapTick; bool pendingClear;       // crossing of the finish line: colors go back to gray after HoldMs
        public int HoldMs = 2500;
        double[] bestSeg, bestSec, refCross;   // best time of every stretch / main sector; refCross: lap time at each boundary on the best lap
        public bool Ahead;                     // the lap in progress is ahead of the best lap so far
        double refTotal = Nan;
        // boundaries of the main sectors on the lap (0..1); the first two are learned from the game's sector changes
        double e1 = 1.0 / 3, e2 = 2.0 / 3;
        public bool BoundsKnown { get; private set; }
        double[] bound;

        // the lap in progress
        double[] cross;                        // lap time at each boundary, NaN = not timed
        int nextIdx;
        bool clean, lapValid, sawWrap;
        double prevPos = -1, prevMs;
        int prevSector = -1, transCount;
        double tr1;

        public SectorTracker(Settings cfg, int subs)
        {
            this.cfg = cfg;
            Alloc(Math.Max(1, Math.Min(12, subs)));
        }

        void Alloc(int subs)
        {
            Subs = subs;
            int n = Count;
            Seg = new SegState[n]; Sec = new SegState[Main];
            SegMs = Fill(n); SecMs = Fill(Main);
            SegStamp = new int[n]; SecStamp = new int[Main];
            refCross = Fill(n + 1); Ahead = false; secT = Fill(Main); secBest = Fill(Main);
            bestSeg = Fill(n); bestSec = Fill(Main); refTotal = Nan;
            cross = Fill(n + 1);
            othersSeg = Fill(n); othersEst = Fill(Main); rivals = new Rival[0];
            bound = new double[n + 1];
            Bounds();
            clean = false; nextIdx = Math.Max(1, IndexAfter((float)Math.Max(0, prevPos)));
        }

        static double[] Fill(int n) { var a = new double[n]; for (int i = 0; i < n; i++) a[i] = Nan; return a; }

        void Bounds()
        {
            double[] sb = { 0, e1, e2, 1 };
            for (int m = 0; m < Main; m++)
                for (int j = 0; j < Subs; j++) bound[m * Subs + j] = sb[m] + (sb[m + 1] - sb[m]) * j / Subs;
            bound[Count] = 1;
        }

        // first boundary beyond p (the next one to cross), 1..Count
        int IndexAfter(float p)
        {
            for (int i = 1; i <= Count; i++) if (bound[i] > p) return i;
            return Count;
        }

        public void Configure(int subs)
        {
            subs = Math.Max(1, Math.Min(12, subs));
            if (subs != Subs) Alloc(subs);
        }

        // forget every record (new track or car, new sector boundaries, changed number of sub-sectors)
        public void ResetRecords()
        {
            for (int i = 0; i < Count; i++) { Seg[i] = SegState.None; SegMs[i] = Nan; bestSeg[i] = Nan; }
            for (int m = 0; m < Main; m++) { Sec[m] = SegState.None; SecMs[m] = Nan; secT[m] = Nan; bestSec[m] = Nan; }
            refTotal = Nan; OwnBestMs = 0; OwnLastMs = 0; Ahead = false;
            for (int i = 0; i < Count; i++) othersSeg[i] = Nan;
            for (int m = 0; m < Main; m++) othersEst[m] = Nan;
            rivals = new Rival[0];
            for (int i = 0; i <= Count; i++) refCross[i] = Nan;
            clean = false;
        }

        // ---- per frame ---------------------------------------------------------------------
        public string Why = "";                   // why the current lap won't count (shown by --probe)
        public int LapsDone;                      // laps recorded as references
        double lapStart;                          // clock value at the start line

        public void Update(Tele t) { Update(t, Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency); }

        // Segment times are measured with our own clock between frames (the position and the game's lap timer are not
        // always updated in the same frame, which made laps look broken); the game's lap times are only displayed.
        public void Update(Tele t, double ms)
        {
            if (!t.Live) return;                      // paused / frozen: keep the lap as it is
            if (t.TrackPos < 0) { HasPosition = false; prevPos = -1; return; }
            HasPosition = true;
            if (t.TrackId.Length > 0 && t.TrackId != trackId)
            {
                if (trackId.Length == 0) trackId = t.TrackId;     // the name only became known: keep what we have
                else NewTrack(t.TrackId);
            }

            float pos = t.TrackPos;
            for (int m = 0; m < Main; m++) othersSec[m] = t.SessionBestSecMs[m];
            tracking = t.CarSlots > 1;
            alone = t.Drivers <= 1 || AssumePurple || tracking;   // unknown counts as alone; when the others are timed here, no time of theirs = nobody did better
            if (!t.LapValid) lapValid = false;
            if (t.InPitLane) Unclean("pit lane");

            if (prevPos < 0)
            {
                BeginLap(pos < 0.03f && !t.InPitLane, pos, t, ms);
                if (!clean && Why.Length == 0) Why = "joined mid-lap";
            }
            else
            {
                bool wrap = prevPos > 0.85 && pos < 0.15;
                if (ms - prevMs > 1000) Unclean("pause or frozen game");
                if (!wrap) LearnBounds(t, pos);
                if (wrap)
                {
                    double frac = (1 - prevPos) / ((1 - prevPos) + pos), tw = prevMs + frac * (ms - prevMs);
                    if (nextIdx <= Count)
                    {
                        cross[Count] = tw - lapStart;
                        if (segOk) EvalSeg(Count - 1);
                        if (clean) FinishLap(tw - lapStart);
                    }
                    BeginLap(!t.InPitLane, pos, t, tw);
                    wrapTick = Environment.TickCount + 1; pendingClear = true;   // +1: stamps made in this same millisecond belong to the lap that just ended
                    if (!clean) Why = "pit lane";
                }
                else if (pos < prevPos - 0.02 || pos > prevPos + 0.2)
                {
                    Unclean("position jump");         // teleport, rewind, restart: this lap won't count
                    nextIdx = IndexAfter(pos); cross[nextIdx - 1] = Nan;
                }
                else
                {
                    if (pos < prevPos) pos = (float)prevPos;   // ignore small backwards jitter
                    while (nextIdx < Count && pos >= bound[nextIdx])
                    {
                        double f = pos > prevPos ? (bound[nextIdx] - prevPos) / (pos - prevPos) : 1;
                        f = Math.Max(0, Math.Min(1, f));
                        cross[nextIdx] = prevMs + f * (ms - prevMs) - lapStart;
                        if (!segOk) secBad = true;
                        if (segOk) EvalSeg(nextIdx - 1);
                        if (nextIdx % Subs == 0) secBad = false;
                        if (clean && !double.IsNaN(refCross[nextIdx])) Ahead = cross[nextIdx] < refCross[nextIdx];
                        segOk = !t.InPitLane;
                        nextIdx++;
                    }
                }
            }
            prevPos = pos; prevMs = ms;
            if (tracking) UpdateRivals(t, ms);

            CurSeg = Math.Max(0, Math.Min(Count - 1, IndexAfter(pos) - 1));
            CurSec = CurSeg / Subs;
            double a = bound[CurSeg], b = bound[CurSeg + 1];
            Progress = b > a ? (float)Math.Max(0, Math.Min(1, (pos - a) / (b - a))) : 0;
            Rescore();
        }

        // clean = the whole lap can become the reference; segOk = the segment in progress can still be scored
        bool segOk = true, secBad;     // secBad: part of the main sector in progress was not driven normally
        void Unclean(string why) { if (clean) Why = why; clean = false; segOk = false; }

        void BeginLap(bool fromLine, float pos, Tele t, double start)
        {
            for (int i = 0; i <= Count; i++) cross[i] = Nan;
            cross[0] = fromLine ? 0 : Nan;
            lapStart = start;
            nextIdx = fromLine ? 1 : IndexAfter(pos);
            clean = fromLine; sawWrap = fromLine; lapValid = t.LapValid; transCount = 0; Ahead = false; segOk = !t.InPitLane; secBad = false;
            if (fromLine) Why = "";
            prevSector = t.Sector;
        }

        // sector changes at positions near the line are the S3 -> S1 flip, not a boundary
        void LearnBounds(Tele t, float pos)
        {
            int s = t.Sector;
            if (s >= 0 && prevSector >= 0 && s != prevSector && sawWrap && pos > 0.03f && pos < 0.97f)
            {
                transCount++;
                if (transCount == 1) tr1 = pos;
                else if (transCount == 2)
                {
                    if (tr1 > 0.05 && pos - tr1 > 0.05 && pos < 0.95) SetBounds(tr1, pos, true);
                }
            }
            prevSector = s;
        }

        void SetBounds(double a, double b, bool store)
        {
            bool known = BoundsKnown;
            BoundsKnown = true;
            if (known && Math.Abs(a - e1) < 0.004 && Math.Abs(b - e2) < 0.004) return;
            e1 = a; e2 = b;
            Bounds();
            ResetRecords();                           // records were cut at other places
            nextIdx = IndexAfter((float)Math.Max(0, prevPos));
            if (store && cfg != null && trackId.Length > 0)
            {
                cfg.Set(BoundsKey(), a.ToString("0.0000", CultureInfo.InvariantCulture) + "," + b.ToString("0.0000", CultureInfo.InvariantCulture));
                try { cfg.Save(); } catch { }
            }
        }

        string BoundsKey()
        {
            uint h = 2166136261;
            foreach (char c in trackId) { h ^= c; h *= 16777619; }
            return "SecBounds_" + h.ToString("x8");
        }

        void NewTrack(string id)
        {
            trackId = id;
            e1 = 1.0 / 3; e2 = 2.0 / 3; BoundsKnown = false;
            Bounds();
            ResetRecords();
            if (cfg != null && id.Length > 0)
            {
                string[] p = cfg.Str(BoundsKey(), "").Split(',');
                double a, b;
                if (p.Length == 2 && double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a)
                    && double.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b) && a > 0.05 && b > a + 0.05 && b < 0.95)
                { e1 = a; e2 = b; BoundsKnown = true; Bounds(); }
            }
            prevPos = -1; prevSector = -1;
        }

        // ---- scoring (the way the CMRT app does it in Assetto Corsa 1) -----------------------
        // Every stretch is timed as soon as it is driven and compared with the best times:
        //   purple = the best time of the session there (all drivers where the game tells us; otherwise just yours),
        //   green  = your own best there, but another driver was faster,
        //   gray   = slower than your best there (the "Slower" state),
        //   dark   = not driven yet in this lap.
        // The first time through a stretch is a record. `best` already contains the time being scored, so equal means "is the best".
        // `others` = the best time of all drivers on that stretch, 0 when the game doesn't publish it.
        static SegState Classify(double t, double best, double others, bool alone)
        {
            if (t > best + 2) return SegState.Slower;                            // within 2 ms counts as equal
            if (others > 0) return t <= others + 30 ? SegState.Purple : SegState.Green;   // our clock and the game's differ by a few ms
            return alone ? SegState.Purple : SegState.Green;      // nobody else to compare with: only when you are alone is your best the best
        }

        static void Record(ref double best, double t) { if (double.IsNaN(best) || t < best) best = t; }

        // segment i has just been crossed
        void EvalSeg(int i)
        {
            double a = cross[i], b = cross[i + 1];
            if (double.IsNaN(a) || double.IsNaN(b) || b <= a) return;
            int now = Environment.TickCount;
            Record(ref bestSeg[i], b - a);
            SegMs[i] = b - a; Seg[i] = Classify(b - a, bestSeg[i], Ms(othersSeg[i]), alone); SegStamp[i] = now;
            if ((i + 1) % Subs == 0)
            {
                int m = i / Subs;
                double s0 = cross[m * Subs];
                if (secBad || double.IsNaN(s0) || b <= s0) return;
                Record(ref bestSec[m], b - s0);
                secT[m] = b - s0; secBest[m] = bestSec[m];
                SecMs[m] = secT[m]; Sec[m] = Classify(secT[m], secBest[m], OthersSec(m), alone); SecStamp[m] = now;
            }
        }

        // Main sectors are scored again while the lap goes on: if another driver beats your time, your purple turns green.
        // After the finish line the old colors stay while the car is still in the first stretch (so the last stretch can be
        // seen), then everything goes back to dark.
        void Rescore()
        {
            for (int m = 0; m < Main; m++)
                if (!double.IsNaN(secT[m]) && Sec[m] != SegState.None) Sec[m] = Classify(secT[m], secBest[m], OthersSec(m), alone);
            for (int i = 0; i < Count; i++)
                if (Seg[i] == SegState.Purple && !double.IsNaN(SegMs[i])) Seg[i] = Classify(SegMs[i], SegMs[i], Ms(othersSeg[i]), alone);
            if (pendingClear && (CurSeg >= 1 || Environment.TickCount - wrapTick >= HoldMs))
            {
                pendingClear = false;
                for (int i = 0; i < Count; i++) if (SegStamp[i] - wrapTick <= 0) { Seg[i] = SegState.None; SegMs[i] = Nan; }
                for (int m = 0; m < Main; m++) if (SecStamp[m] - wrapTick <= 0) { Sec[m] = SegState.None; SecMs[m] = Nan; secT[m] = Nan; }
            }
        }

        static int Ms(double v) { return double.IsNaN(v) ? 0 : Math.Max(1, (int)Math.Round(v)); }

        // fastest time of the other drivers in main sector m: the game's or ours (ms, 0 = none)
        int OthersSec(int m)
        {
            int g = othersSec[m], e = Ms(othersEst[m]);
            return g > 0 && e > 0 ? Math.Min(g, e) : Math.Max(g, e);
        }

        // ---- the other drivers (games that only say where their cars are) --------------------------
        // Every other car is timed the same way as the player, through the same boundaries, from its lap position (see
        // TrackLine). A stretch only counts when the car was seen on the line all the way through it: pit lane, off track,
        // jumps and lost tracking drop it. The game doesn't say whether their laps are valid, so a cut can give them a time.
        class Rival
        {
            public double Prev = -1, PrevMs;
            public int Next;
            public bool SegOk, SecBad, LapOk;
            public double[] Cross;
        }
        Rival[] rivals;
        public Action<int, string, double> RivalTimed;     // slot, "s<i>" sub-sector / "S<m>" main sector / "L" lap, ms (for --cars)

        void UpdateRivals(Tele t, double ms)
        {
            int n = Math.Min(t.CarSlots, t.CarPos.Length);
            if (rivals.Length < n) Array.Resize(ref rivals, n);
            for (int k = 1; k < n; k++)
            {
                var r = rivals[k] ?? (rivals[k] = new Rival { Cross = Fill(Count + 1) });
                double p = t.CarPos[k];
                if (p < 0) { r.Prev = -1; continue; }
                if (p == r.Prev) continue;                                   // no new position from the network yet
                bool wrap = r.Prev > 0.85 && p < 0.15;
                double q = wrap ? p + 1 : p;
                if (r.Prev < 0 || ms - r.PrevMs > 1000 || q < r.Prev - 0.02 || q > r.Prev + 0.2)
                {
                    // (re)found: time from the next boundary on
                    for (int i = 0; i <= Count; i++) r.Cross[i] = Nan;
                    r.Next = IndexAfter((float)p); r.SegOk = false; r.SecBad = true; r.LapOk = false;
                }
                else if (q < r.Prev) continue;                                // small backwards jitter
                else
                {
                    double a = r.Prev;
                    while (true)
                    {
                        if (r.Next > Count) { NewRivalLap(r); a -= 1; q -= 1; continue; }
                        double b = bound[r.Next];
                        if (q < b) break;
                        double f = q > a ? Math.Max(0, Math.Min(1, (b - a) / (q - a))) : 1;
                        RivalCross(k, r, r.PrevMs + f * (ms - r.PrevMs));
                    }
                }
                r.Prev = p; r.PrevMs = ms;
            }
        }

        void NewRivalLap(Rival r)
        {
            double c = r.Cross[Count];
            for (int i = 0; i <= Count; i++) r.Cross[i] = Nan;
            r.Cross[0] = c; r.Next = 1; r.LapOk = !double.IsNaN(c);
        }

        // car k crossed boundary r.Next at clock time tc
        void RivalCross(int k, Rival r, double tc)
        {
            int c = r.Next;
            r.Cross[c] = tc;
            if (!r.SegOk) { r.SecBad = true; r.LapOk = false; }
            else
            {
                double a = r.Cross[c - 1];
                if (!double.IsNaN(a) && tc > a)
                {
                    Record(ref othersSeg[c - 1], tc - a);
                    if (RivalTimed != null) RivalTimed(k, "s" + (c - 1), tc - a);
                }
                if (c % Subs == 0 && !r.SecBad)
                {
                    double s0 = r.Cross[c - Subs];
                    if (!double.IsNaN(s0) && tc > s0)
                    {
                        Record(ref othersEst[c / Subs - 1], tc - s0);
                        if (RivalTimed != null) RivalTimed(k, "S" + (c / Subs - 1), tc - s0);
                    }
                }
                if (c == Count && r.LapOk && !double.IsNaN(r.Cross[0]) && RivalTimed != null) RivalTimed(k, "L", tc - r.Cross[0]);
            }
            if (c % Subs == 0) r.SecBad = false;
            r.SegOk = true;
            r.Next++;
        }

        // a whole lap was timed: it becomes the best lap if it is the fastest (used for the "ahead of the best lap" color of the lap time)
        void FinishLap(double total)
        {
            if (total <= 0) return;
            if (lapValid && clean) LapsDone++;
            OwnLastMs = (int)total;
            if (!lapValid) return;
            for (int i = 0; i <= Count; i++) if (double.IsNaN(cross[i])) return;
            if (OwnBestMs == 0 || total < OwnBestMs) OwnBestMs = (int)total;
            if (double.IsNaN(refTotal) || total < refTotal) { refTotal = total; Array.Copy(cross, refCross, cross.Length); }
        }
    }
}
