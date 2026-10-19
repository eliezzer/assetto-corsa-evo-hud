// The electronics overlay: ABS / TC / TCC / ESC, brake bias, engine map, differential, battery, energy and DRS.
using System;
using System.Windows;

namespace SimracingBR
{
    public class ElecWindow : OverlayWindow
    {
        public static readonly string[] FlagKeys = { "ElecShowAbs", "ElecShowTc", "ElecShowTcc", "ElecShowEsc", "ElecShowBias", "ElecShowMap",
            "ElecShowDiff", "ElecShowBattery", "ElecShowEnergy", "ElecShowDrs" };

        public readonly ElecView View;
        string lastSig = "";

        ElecWindow(Settings s, ElecView view) : base(s, "Elec", "ElecEnabled", L.T("Electronics", "Eletrônica"), view, 40, 0.8, false) { View = view; }

        public static ElecWindow Create(Settings s, Tele t)
        {
            var view = new ElecView();
            view.T = t;
            view.CAbs = s.Col("ColorAbs", "#FF8A00"); view.CTc = s.Col("ColorTc", "#FFE000");
            var w = new ElecWindow(s, view);
            w.LoadFlags();
            return w;
        }

        void LoadFlags()
        {
            View.ShowAbs = cfg.Bool("ElecShowAbs", true); View.ShowTc = cfg.Bool("ElecShowTc", true);
            View.ShowTcc = cfg.Bool("ElecShowTcc", true); View.ShowEsc = cfg.Bool("ElecShowEsc", true);
            View.ShowBias = cfg.Bool("ElecShowBias", true); View.ShowMap = cfg.Bool("ElecShowMap", true);
            View.ShowDiff = cfg.Bool("ElecShowDiff", true); View.ShowBattery = cfg.Bool("ElecShowBattery", true);
            View.ShowEnergy = cfg.Bool("ElecShowEnergy", true); View.ShowDrs = cfg.Bool("ElecShowDrs", true);
            View.Relayout();
        }

        public void LayoutChanged() { lastSig = ""; Reflow(View.Relayout); }

        public void ResetDefaults()
        {
            foreach (string k in FlagKeys) cfg.Set(k, "1");
            Reflow(LoadFlags);
            ApplyScale(1.0, true);
            SetOpacity(0.94);
        }

        public void Tick(Tele t)
        {
            View.AbsDuty += ((t.AbsOn ? 1.0 : 0.0) - View.AbsDuty) * 0.04;
            View.TcDuty += ((t.TcOn ? 1.0 : 0.0) - View.TcDuty) * 0.04;
            string sig = View.Signature();
            if (sig != lastSig) { lastSig = sig; Reflow(View.Relayout, false); }     // another car or game: reflow around what it has
            if (Visibility == Visibility.Visible) View.InvalidateVisual();
        }
    }
}
