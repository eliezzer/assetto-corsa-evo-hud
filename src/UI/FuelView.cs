// Fuel overlay drawing: fuel in the tank (with a bar), consumption per lap, laps left and the fuel for the whole race.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimracingBR
{
    public class FuelView : FrameworkElement, IOverlayView
    {
        const double PAD = 12, W = 258, ROW = 30, BAR_H = 6, GAP = 8, SUG_H = 58;
        const double FL = 12, FV = 20, FU = 12;           // label / value / unit font sizes (same as the pedals overlay)

        public Tele T = new Tele();
        public FuelModel M = new FuelModel(null);
        public bool Unlocked { get; set; }
        public bool Hover { get; set; }
        public bool ShowFuel = true, ShowPerLap = true, ShowLapsLeft = true, ShowSuggested = true;

        readonly Brush gray = Chrome.Br(255, 140, 140, 150), white = Chrome.Br(255, 245, 245, 245);
        readonly Brush amber = Chrome.Br(255, 255, 200, 0), red = Chrome.Br(255, 255, 70, 60), green = Chrome.Br(255, 51, 214, 138);
        double cardH;

        public FuelView() { SnapsToDevicePixels = false; Relayout(); }

        public Rect LockBtn { get { return Chrome.LockRect(this, Width, Unlocked); } }

        public void Relayout()
        {
            int rows = (ShowFuel ? 1 : 0) + (ShowPerLap ? 1 : 0) + (ShowLapsLeft ? 1 : 0);
            cardH = rows > 0 ? rows * ROW + (ShowFuel ? BAR_H + 4 : 0) + 12 : 0;
            double h = cardH + (ShowSuggested ? (cardH > 0 ? GAP : 0) + SUG_H : 0);
            Width = W + 2 * PAD;
            Height = Math.Max(h, 44) + 2 * PAD;
        }

        protected override void OnRender(DrawingContext dc)
        {
            Chrome.Panel(dc, Width, Height, Unlocked);
            double x = PAD, y = PAD, w = W;
            if (cardH == 0 && !ShowSuggested)
            {
                Chrome.Text(this, dc, L.T("Nothing selected", "Nada selecionado"), 13, white, true, Width / 2, 12, 1);
                Chrome.Text(this, dc, L.T("Tray icon > Settings", "Ícone da bandeja > Configurações"), 10, gray, false, Width / 2, 32, 1);
            }
            if (cardH > 0)
            {
                dc.DrawRoundedRectangle(Chrome.Br(10, 255, 255, 255), null, new Rect(x, y, w, cardH), 3, 3);
                double ry = y + 6;
                if (ShowFuel)
                {
                    Row(dc, x, ry, w, L.T("FUEL", "COMBUSTÍVEL"), T.Fuel > 0 ? T.Fuel.ToString("0.0", CultureInfo.InvariantCulture) : "--", "L", white);
                    ry += ROW;
                    DrawBar(dc, x + 10, ry - 2, w - 20);
                    ry += BAR_H + 4;
                }
                if (ShowPerLap)
                {
                    Row(dc, x, ry, w, L.T("PER LAP", "POR VOLTA"), M.PerLap > 0 ? M.PerLap.ToString("0.00", CultureInfo.InvariantCulture) : "--", "L", white);
                    ry += ROW;
                }
                if (ShowLapsLeft)
                {
                    Brush b = M.LapsLeft <= 0 ? white : (M.LapsLeft < 2 ? red : (M.LapsLeft < 4 ? amber : white));
                    Row(dc, x, ry, w, L.T("LAPS LEFT", "VOLTAS RESTANTES"), M.LapsLeft > 0 ? M.LapsLeft.ToString("0.0", CultureInfo.InvariantCulture) : "--", "", b);
                }
                y += cardH + GAP;
            }
            if (ShowSuggested) DrawSuggested(dc, x, y, w);
            if (Unlocked) Chrome.UnlockedBanner(this, dc, Width, Height);
            Chrome.LockButton(this, dc, Unlocked, Hover);
        }

        // label on the left, value (and unit) right-aligned
        void Row(DrawingContext dc, double x, double y, double w, string label, string val, string unit, Brush valueBrush)
        {
            double rx = x + w - 10;
            if (unit.Length > 0) { Chrome.Text(this, dc, unit, FU, gray, false, rx, y + 9, 2); rx -= Chrome.TextWidth(this, unit, FU, false) + 3; }
            Chrome.Text(this, dc, val, FV, valueBrush, true, rx, y + 3, 2);
            Chrome.Text(this, dc, label, FL, gray, true, x + 10, y + 8, 0);
        }

        // tank level
        void DrawBar(DrawingContext dc, double x, double y, double w)
        {
            dc.DrawRoundedRectangle(Chrome.Br(255, 30, 30, 38), null, new Rect(x, y, w, BAR_H), 2, 2);
            if (T.FuelCap <= 0 || T.Fuel <= 0) return;
            double k = Math.Max(0, Math.Min(1, T.Fuel / T.FuelCap));
            Brush b = M.LapsLeft > 0 && M.LapsLeft < 2 ? red : (M.LapsLeft > 0 && M.LapsLeft < 4 ? amber : green);
            if (k > 0.004) dc.DrawRoundedRectangle(b, null, new Rect(x, y, w * k, BAR_H), 2, 2);
        }

        // the fuel for the whole race, with the numbers behind it (never the time left or what is in the tank)
        void DrawSuggested(DrawingContext dc, double x, double y, double w)
        {
            dc.DrawRoundedRectangle(Chrome.Br(10, 255, 255, 255), null, new Rect(x, y, w, SUG_H), 3, 3);
            Chrome.Text(this, dc, L.T("SUGGESTED RACE FUEL", "COMBUSTÍVEL SUGERIDO"), FL, gray, true, x + 10, y + 11, 0);
            string val = M.RaceFuel < 0 ? "--" : M.RaceFuel.ToString("0.0", CultureInfo.InvariantCulture) + " L";
            Chrome.Text(this, dc, val, 24, M.RaceFuel < 0 ? white : amber, true, x + w - 10, y + 5, 2);
            string sub;
            if (M.RaceFuel >= 0)
            {
                sub = M.RaceLaps.ToString("0.0", CultureInfo.InvariantCulture) + L.T(" laps", " voltas");
                if (M.TimedRace && M.RaceMinutes > 0) sub += " (" + M.RaceMinutes + " min)";
                sub += "  ·  " + M.PerLap.ToString("0.00", CultureInfo.InvariantCulture) + " L" + L.T("/lap", "/volta");
                if (M.TimedRace) sub += "  ·  " + L.T("lap ", "volta ") + Chrome.Time(M.BestValidLapMs > 0 ? M.BestValidLapMs : T.BestLapMs);
                if (M.RaceSource == 3) sub += "  ·  " + L.T("last race", "última corrida");
                else if (M.RaceSource == 4) sub += "  ·  " + L.T("from the game", "do jogo");
                else if (M.RaceSource == 1) sub += "  ·  " + L.T("set by you", "definido por você");
            }
            else if (M.PerLap <= 0) sub = L.T("measuring consumption (one full lap)...", "medindo o consumo (uma volta completa)...");
            else if (M.RaceLaps < 0 && !M.TimedRace) sub = L.T("set the race length in Settings (laps or minutes)", "defina a duração da corrida nas Configurações (voltas ou minutos)");
            else sub = L.T("needs a valid lap to know the lap time", "precisa de uma volta válida para saber o tempo de volta");
            Chrome.Text(this, dc, sub, 10.5, gray, false, x + 10, y + 38, 0);
        }
    }
}
