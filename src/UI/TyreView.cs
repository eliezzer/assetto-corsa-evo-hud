// Tyre overlay drawing: the hot pressure of the four tyres (colored by how close it is to the target) and, after the valid laps
// asked for, the cold pressure to put in the setup so that it ends up at the target.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimracingBR
{
    public class TyreView : FrameworkElement, IOverlayView
    {
        const double PAD = 12, W = 292, HEAD_H = 24, CARD_H = 106, GAP = 8, FOOT_H = 22;

        public Tele T = new Tele();
        public TyreModel M = new TyreModel();
        public bool Unlocked { get; set; }
        public bool Hover { get; set; }
        public bool ShowTemps = true, ShowAdvice = true;

        static readonly string[] Names = { "FL", "FR", "RL", "RR" };
        readonly Brush gray = Chrome.Br(255, 140, 140, 150), white = Chrome.Br(255, 245, 245, 245);
        readonly Brush amber = Chrome.Br(255, 255, 200, 0), green = Chrome.Br(255, 51, 214, 138), red = Chrome.Br(255, 255, 70, 60);

        public TyreView() { SnapsToDevicePixels = false; Relayout(); }

        public Rect LockBtn { get { return Chrome.LockRect(this, Width, Unlocked); } }

        public void Relayout()
        {
            Width = W + 2 * PAD;
            Height = PAD + HEAD_H + GAP + 2 * CARD_H + GAP + FOOT_H + PAD;
        }

        // within the tolerance of the target = green, up to twice that = amber, beyond = red
        Brush Level(double psi)
        {
            double d = Math.Abs(psi - M.Target);
            return d <= TyreModel.Tolerance ? green : (d <= 2 * TyreModel.Tolerance ? amber : red);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Chrome.Panel(dc, Width, Height, Unlocked);
            double x = PAD, y = PAD;
            // header: target on the left, progress on the right
            Chrome.Text(this, dc, L.T("TARGET ", "ALVO ") + M.Target.ToString("0.0", CultureInfo.InvariantCulture) + " PSI", 13, white, true, x + 2, y + 2, 0);
            string status;
            if (!M.HasData) status = "";
            else if (M.Ready) status = L.T("READY", "PRONTO");
            else if (!M.ColdKnown(T)) status = L.T("setup PSI unknown", "PSI do setup desconhecido");
            else status = L.T("valid laps ", "voltas válidas ") + Math.Min(M.ValidLaps, M.LapsNeeded) + "/" + M.LapsNeeded;
            Chrome.Text(this, dc, status, 12, M.Ready ? green : gray, true, x + W - 2, y + 3, 2);
            y += HEAD_H + GAP;

            if (!M.HasData)
            {
                Chrome.Text(this, dc, L.T("no tyre pressure from this game", "este jogo não publica a pressão dos pneus"), 12, gray, true, Width / 2, Height / 2 - 8, 1);
            }
            else
            {
                double cw = (W - GAP) / 2;
                for (int i = 0; i < 4; i++) DrawCard(dc, i, x + (i % 2) * (cw + GAP), y + (i / 2) * (CARD_H + GAP) - (i / 2) * 0, cw);
                y += 2 * CARD_H + GAP;
                string foot = "";
                double worst = 0;
                for (int i = 0; i < 4; i++) if (Math.Abs(M.Trend(i)) > Math.Abs(worst)) worst = M.Trend(i);
                if (M.ValidLaps >= 2 && Math.Abs(worst) > 0.3)
                    foot = L.T("still moving ", "ainda mudando ") + (worst > 0 ? "+" : "") + worst.ToString("0.0", CultureInfo.InvariantCulture) + " psi" + L.T("/lap: check again next lap", "/volta: confira na próxima volta");
                else if (M.Ready) foot = L.T("put these cold values in the setup", "coloque estes valores a frio no setup");
                Chrome.Text(this, dc, foot, 10.5, gray, false, x + 2, y + 4, 0);
            }
            if (Unlocked) Chrome.UnlockedBanner(this, dc, Width, Height);
            Chrome.LockButton(this, dc, Unlocked, Hover);
        }

        void DrawCard(DrawingContext dc, int i, double x, double y, double w)
        {
            dc.DrawRoundedRectangle(Chrome.Br(10, 255, 255, 255), null, new Rect(x, y, w, CARD_H), 3, 3);
            Chrome.Text(this, dc, Names[i], 11, gray, true, x + 10, y + 7, 0);
            if (ShowTemps && M.Temp[i] >= 0)
                Chrome.Text(this, dc, Math.Round(M.Temp[i]).ToString(CultureInfo.InvariantCulture) + " °C", 11, gray, true, x + w - 10, y + 7, 2);
            double hot = M.Hot[i];
            Chrome.Text(this, dc, hot.ToString("0.0", CultureInfo.InvariantCulture), 26, Level(hot), true, x + 10, y + 20, 0);
            Chrome.Text(this, dc, "psi", 12, gray, false, x + 10 + Chrome.TextWidth(this, hot.ToString("0.0", CultureInfo.InvariantCulture), 26, true) + 4, y + 36, 0);
            if (!ShowAdvice) return;
            double ly = y + 60;
            if (M.Ready && M.Advice[i] > 0)
            {
                double cold = M.ColdFor(i, T), adv = Math.Round(M.Advice[i], 1), delta = adv - cold;
                bool keep = Math.Abs(delta) < 0.15;
                Chrome.Text(this, dc, L.T("SET ", "SETUP "), 11, gray, true, x + 10, ly + 6, 0);
                Chrome.Text(this, dc, adv.ToString("0.0", CultureInfo.InvariantCulture), 20, keep ? green : amber, true, x + 10 + Chrome.TextWidth(this, L.T("SET ", "SETUP "), 11, true) + 4, ly, 0);
                string sub = (keep ? L.T("keep as is", "manter como está") : (delta > 0 ? "▲ +" : "▼ ") + delta.ToString("0.0", CultureInfo.InvariantCulture)) + "  ·  " + L.T("now ", "hoje ") + cold.ToString("0.0", CultureInfo.InvariantCulture);
                Chrome.Text(this, dc, sub, 10.5, gray, false, x + 10, ly + 28, 0);
            }
            else
            {
                double cold = M.ColdFor(i, T);
                string s = cold > 0 ? L.T("setup ", "setup ") + cold.ToString("0.0", CultureInfo.InvariantCulture) + "  ·  " + L.T("waiting for laps", "aguardando voltas")
                                    : L.T("setup PSI unknown", "PSI do setup desconhecido");
                Chrome.Text(this, dc, s, 10.5, gray, false, x + 10, ly + 8, 0);
            }
        }
    }
}
