// The pedals overlay (graph, bars, ABS/TC, gear/speed, wheel, fuel): its view, show/hide flags and fuel model.
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimracingBR
{
    public class PedalsWindow : OverlayWindow
    {
        public static readonly string[] FlagKeys = { "ShowGraph", "ShowBars", "ShowClutch", "ShowAids", "ShowGear", "ShowSpeed",
            "ShowShiftLights", "ShowWheel", "ShowFuel", "ShowFuelPerLap", "ShowLapsLeft", "ShowBias" };

        public readonly HudView View;
        // fuel-per-lap fallback for games that don't publish it
        int prevLapMs; float lapStartFuel; double measuredPerLap;

        PedalsWindow(Settings s, HudView view) : base(s, "", "PedalsEnabled", L.T("Pedals", "Pedais"), view, 40) { View = view; }

        public static PedalsWindow Create(Settings s, string dir, Tele t)
        {
            var view = new HudView();
            view.T = t;
            view.CClutch = s.Col("ColorClutch", "#0A30FF"); view.CBrake = s.Col("ColorBrake", "#FF1010");
            view.CThrottle = s.Col("ColorThrottle", "#33D68A");
            view.CAbs = s.Col("ColorAbs", "#FF8A00"); view.CTc = s.Col("ColorTc", "#FFE000");
            view.CShiftA = s.Col("ColorShiftA", "#FF1E1E"); view.CShiftB = s.Col("ColorShiftB", "#28282F");
            view.AidColors = s.Bool("AidColors", true); view.InvertSteer = s.Bool("InvertSteer", false);
            view.Wheel = LoadWheel(Path.Combine(dir, s.Str("Wheel", "assets\\wheel.png")));
            var w = new PedalsWindow(s, view);
            w.LoadFlags();
            return w;
        }

        static ImageSource LoadWheel(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var bi = new BitmapImage();
                bi.BeginInit(); bi.CacheOption = BitmapCacheOption.OnLoad; bi.UriSource = new Uri(Path.GetFullPath(path)); bi.EndInit();
                bi.Freeze(); return bi;
            }
            catch { return null; }
        }

        // ---- what to show -----------------------------------------------------------------
        void LoadFlags()
        {
            View.ShowGraph = cfg.Bool("ShowGraph", true); View.ShowBars = cfg.Bool("ShowBars", true);
            View.ShowClutch = cfg.Bool("ShowClutch", true); View.ShowAids = cfg.Bool("ShowAids", true); View.ShowBias = cfg.Bool("ShowBias", true);
            View.ShowGear = cfg.Bool("ShowGear", true); View.ShowSpeed = cfg.Bool("ShowSpeed", true);
            View.ShowShiftLights = cfg.Bool("ShowShiftLights", true); View.ShowWheel = cfg.Bool("ShowWheel", true);
            View.ShowFuel = cfg.Bool("ShowFuel", true); View.ShowFuelPerLap = cfg.Bool("ShowFuelPerLap", true);
            View.ShowLapsLeft = cfg.Bool("ShowLapsLeft", true);
            View.Relayout();
        }

        // After a show/hide change: reflow the HUD and keep its center where it was.
        public void LayoutChanged() { Reflow(View.Relayout); }

        public void ResetDefaults()
        {
            foreach (string k in FlagKeys) cfg.Set(k, "1");
            Reflow(LoadFlags);
            ApplyScale(1.0, true);
            SetOpacity(0.94);
        }

        // ---- per frame --------------------------------------------------------------------
        public void Tick(Tele t, bool tcc)
        {
            bool bias = t.BrakeBias > 0.2f;
            if (tcc != View.HasTcc || bias != View.HasBias) { View.HasTcc = tcc; View.HasBias = bias; LayoutChanged(); }
            UpdateFuel(t);
            View.AbsDuty += ((t.AbsOn ? 1.0 : 0.0) - View.AbsDuty) * 0.04;
            View.TcDuty += ((t.TcOn ? 1.0 : 0.0) - View.TcDuty) * 0.04;
            View.Push();
            if (Visibility == System.Windows.Visibility.Visible) View.InvalidateVisual();
        }

        // Another game took over: forget what we learned about the previous one.
        public void SourceChanged() { prevLapMs = 0; lapStartFuel = 0; measuredPerLap = 0; }

        void UpdateFuel(Tele t)
        {
            // lap rollover: current lap time drops back near zero
            if (lapStartFuel <= 0 && t.Fuel > 0) lapStartFuel = t.Fuel;
            if (prevLapMs > 5000 && t.LapMs < prevLapMs - 2000)
            {
                float used = lapStartFuel - t.Fuel;
                if (used > 0.05f && used < 60f) measuredPerLap = measuredPerLap <= 0 ? used : (measuredPerLap * 0.6 + used * 0.4);
                lapStartFuel = t.Fuel;
            }
            prevLapMs = t.LapMs;
            double per = t.FuelPerLap > 0.01f ? t.FuelPerLap : measuredPerLap;
            View.PerLap = per;
            View.LapsLeft = (per > 0.01 && t.Fuel > 0) ? t.Fuel / per : 0;
        }
    }
}
