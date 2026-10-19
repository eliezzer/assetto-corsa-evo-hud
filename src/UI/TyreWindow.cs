// SWITCHED OFF FOR NOW: the tyre overlay is not created by OverlayApp (it is commented out there) because it is not working well yet.
// The code stays here so it can be switched on again.
// The tyre overlay: hot pressure of the four tyres and the cold pressure to set after two valid laps.
using System;
using System.Globalization;
using System.Windows;

namespace SimracingBR
{
    public class TyreWindow : OverlayWindow
    {
        public static readonly string[] FlagKeys = { "TyreShowTemps", "TyreShowAdvice" };
        public const double DefaultTarget = 27.0;
        public const int DefaultLaps = 2;

        public readonly TyreView View;
        readonly TyreModel live = new TyreModel(), demoModel = new TyreModel();   // the demo (settings preview) never touches the real measurements

        TyreWindow(Settings s, TyreView view) : base(s, "Tyre", "TyreEnabled", L.T("Tyres", "Pneus"), view, 240, 0.65, false, 1) { View = view; view.M = live; }

        public static TyreWindow Create(Settings s, Tele t)
        {
            var view = new TyreView();
            view.T = t;
            var w = new TyreWindow(s, view);
            w.LoadFlags();
            DemoSource.PrimeTyre(w.demoModel);
            return w;
        }

        void Both(Action<TyreModel> a) { a(live); a(demoModel); }

        void LoadFlags()
        {
            View.ShowTemps = cfg.Bool("TyreShowTemps", true);
            View.ShowAdvice = cfg.Bool("TyreShowAdvice", true);
            double target = Math.Max(15, Math.Min(45, cfg.Num("TyreTarget", DefaultTarget)));
            int laps = (int)Math.Max(1, Math.Min(6, cfg.Num("TyreLaps", DefaultLaps)));
            Both(m => { m.Target = target; m.LapsNeeded = laps; });
            for (int i = 0; i < 4; i++) { int k = i; double c = Math.Max(0, cfg.Num("TyreCold" + i, 0)); Both(m => m.ColdOverride[k] = c); }
            View.Relayout();
        }

        public double Target { get { return live.Target; } }
        public int LapsNeeded { get { return live.LapsNeeded; } }
        public double ColdOverride(int i) { return live.ColdOverride[i]; }

        public void SetTarget(double v)
        {
            double x = Math.Max(15, Math.Min(45, v));
            Both(m => m.Target = x);
            cfg.Set("TyreTarget", x.ToString("0.0", CultureInfo.InvariantCulture)); QueueSave();
        }

        public void SetLapsNeeded(int n)
        {
            int x = Math.Max(1, Math.Min(6, n));
            Both(m => m.LapsNeeded = x);
            cfg.Set("TyreLaps", x.ToString(CultureInfo.InvariantCulture)); QueueSave();
        }

        public void SetColdOverride(int i, double v)
        {
            double x = Math.Max(0, v);
            Both(m => m.ColdOverride[i] = x);
            cfg.Set("TyreCold" + i, x.ToString("0.0", CultureInfo.InvariantCulture)); QueueSave();
        }

        public void LayoutChanged() { Reflow(View.Relayout); }

        public void ResetDefaults()
        {
            foreach (string k in FlagKeys) cfg.Set(k, "1");
            cfg.Set("TyreTarget", DefaultTarget.ToString("0.0", CultureInfo.InvariantCulture)); cfg.Set("TyreLaps", DefaultLaps.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < 4; i++) cfg.Set("TyreCold" + i, "0");
            Reflow(LoadFlags);
            ApplyScale(1.0, true);
            SetOpacity(0.94);
        }

        public void Tick(Tele t, bool preview)
        {
            TyreModel m = preview ? demoModel : live;
            if (View.M != m) View.M = m;
            m.Update(t);
            if (Visibility == Visibility.Visible) View.InvalidateVisual();
        }
    }
}
