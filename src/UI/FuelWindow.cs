// The fuel overlay: tank, consumption per lap, laps left and the suggested refuel for the race.
using System;
using System.Globalization;
using System.Windows;

namespace SimracingBR
{
    public class FuelWindow : OverlayWindow
    {
        public static readonly string[] FlagKeys = { "FuelShowFuel", "FuelShowPerLap", "FuelShowLapsLeft", "FuelShowSuggested" };
        public const double DefaultMargin = 1.0;

        public readonly FuelView View;

        FuelWindow(Settings s, FuelView view) : base(s, "Fuel", "FuelEnabled", L.T("Fuel", "Combustível"), view, 40, 0.2, false, 2) { View = view; }

        public static FuelWindow Create(Settings s, Tele t)
        {
            var view = new FuelView { M = new FuelModel(s) };
            view.T = t;
            var w = new FuelWindow(s, view);
            w.LoadFlags();
            return w;
        }

        void LoadFlags()
        {
            View.ShowFuel = cfg.Bool("FuelShowFuel", true);
            View.ShowPerLap = cfg.Bool("FuelShowPerLap", true);
            View.ShowLapsLeft = cfg.Bool("FuelShowLapsLeft", true);
            View.ShowSuggested = cfg.Bool("FuelShowSuggested", true);
            View.M.MarginLaps = cfg.Num("FuelMargin", DefaultMargin);
            View.M.ManualRaceLaps = (int)Math.Max(0, cfg.Num("FuelRaceLaps", 0));
            View.M.ManualRaceMinutes = (int)Math.Max(0, cfg.Num("FuelRaceMinutes", 0));
            View.Relayout();
        }

        public double Margin { get { return View.M.MarginLaps; } }
        public int RaceLaps { get { return View.M.ManualRaceLaps; } }
        public int RaceMinutes { get { return View.M.ManualRaceMinutes; } }

        public void SetRaceMinutes(int n)
        {
            View.M.ManualRaceMinutes = Math.Max(0, Math.Min(600, n));
            cfg.Set("FuelRaceMinutes", View.M.ManualRaceMinutes.ToString(CultureInfo.InvariantCulture)); QueueSave();
        }

        public void SetMargin(double v)
        {
            View.M.MarginLaps = Math.Max(0, Math.Min(5, v));
            cfg.Set("FuelMargin", View.M.MarginLaps.ToString("0.##", CultureInfo.InvariantCulture)); QueueSave();
        }

        public void SetRaceLaps(int n)
        {
            View.M.ManualRaceLaps = Math.Max(0, Math.Min(999, n));
            cfg.Set("FuelRaceLaps", View.M.ManualRaceLaps.ToString(CultureInfo.InvariantCulture)); QueueSave();
        }

        public void LayoutChanged() { Reflow(View.Relayout); }

        public void ResetDefaults()
        {
            foreach (string k in FlagKeys) cfg.Set(k, "1");
            cfg.Set("FuelMargin", DefaultMargin.ToString(CultureInfo.InvariantCulture)); cfg.Set("FuelRaceLaps", "0"); cfg.Set("FuelRaceMinutes", "0");
            Reflow(LoadFlags);
            ApplyScale(1.0, true);
            SetOpacity(0.94);
        }

        public void Tick(Tele t)
        {
            View.M.Update(t);
            if (Visibility == Visibility.Visible) View.InvalidateVisual();
        }
    }
}
