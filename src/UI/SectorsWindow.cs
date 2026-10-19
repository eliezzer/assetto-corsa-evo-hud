// The sectors overlay: S1 / S2 / S3 with sub-sectors, plus current / best / last / leader lap times.
using System;
using System.Windows;

namespace SimracingBR
{
    public class SectorsWindow : OverlayWindow
    {
        public const string SubsKey = "SectorsSubs";
        public static readonly string[] FlagKeys = { "SectorsShowTimes", "SectorsShowLeader", "SectorsShowSectorTimes" };
        public const int DefaultSubs = 8;

        public readonly SectorsView View;
        readonly System.Diagnostics.Stopwatch demoClock = System.Diagnostics.Stopwatch.StartNew();
        readonly SectorTracker live, demoTracker;      // the demo (settings preview) never touches the real records

        SectorsWindow(Settings s, SectorsView view, SectorTracker live, SectorTracker demo)
            : base(s, "Sectors", "SectorsEnabled", L.T("Sectors", "Setores"), view, 240) { View = view; this.live = live; demoTracker = demo; }

        public static SectorsWindow Create(Settings s, Tele t)
        {
            int subs = (int)Math.Max(1, Math.Min(12, s.Num(SubsKey, DefaultSubs)));
            var live = new SectorTracker(s, subs);
            var demo = new SectorTracker(null, subs);
            DemoSource.Prime(demo);
            var view = new SectorsView(live);
            view.T = t;
            // same palette as the pedals overlay: green / yellow are the throttle and TC colors
            view.CPurple = s.Col("ColorSecPurple", "#B14DFF");
            view.CGreen = s.Col("ColorSecGreen", s.Str("ColorThrottle", "#33D68A"));
            view.CSlower = s.Col("ColorSecSlower", "#62626C");
            var w = new SectorsWindow(s, view, live, demo);
            w.LoadFlags();
            w.SetAssumePurple(s.Bool("SectorsPurpleBest", false));
            return w;
        }

        void LoadFlags()
        {
            View.ShowTimes = cfg.Bool("SectorsShowTimes", true);
            View.ShowLeader = cfg.Bool("SectorsShowLeader", true);
            View.ShowSectorTimes = cfg.Bool("SectorsShowSectorTimes", true);
            View.Relayout();
        }

        public int Subs { get { return live.Subs; } }
        public bool AssumePurple { get { return live.AssumePurple; } }
        public void SetAssumePurple(bool on) { live.AssumePurple = on; demoTracker.AssumePurple = on; }

        public void LayoutChanged() { Reflow(View.Relayout); }

        public void SetSubs(int n)
        {
            n = Math.Max(1, Math.Min(12, n));
            if (n == live.Subs) return;
            cfg.Set(SubsKey, n.ToString());
            live.Configure(n); demoTracker.Configure(n);
            LayoutChanged();
        }

        public void ResetDefaults()
        {
            foreach (string k in FlagKeys) cfg.Set(k, "1");
            cfg.Set(SubsKey, DefaultSubs.ToString()); cfg.Set("SectorsPurpleBest", "0"); SetAssumePurple(false);
            live.Configure(DefaultSubs); demoTracker.Configure(DefaultSubs);
            Reflow(LoadFlags);
            ApplyScale(1.0, true);
            SetOpacity(0.94);
        }

        public void Tick(Tele t, bool demo)
        {
            SectorTracker k = demo ? demoTracker : live;
            if (View.Tracker != k) { View.Tracker = k; View.Relayout(); }
            if (demo) k.Update(t, DemoSource.WarmUp * 1000 + demoClock.ElapsedMilliseconds); else k.Update(t);
            if (Visibility == Visibility.Visible) View.InvalidateVisual();
        }
    }
}
