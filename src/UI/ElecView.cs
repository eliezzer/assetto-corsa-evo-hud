// Electronics overlay drawing: ABS / TC / TCC / ESC chips that light up when they act, and cards for brake bias, engine map,
// differential, battery, energy and DRS. Only what the car and the game publish is shown.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SimracingBR
{
    public class ElecView : FrameworkElement, IOverlayView
    {
        const double PAD = 12, W = 286, CHIP_H = 30, CHIP_GAP = 6, CARD_H = 52, CARD_GAP = 8, GAP = 8;
        const double LF = 11, VF = 20;

        public Tele T = new Tele();
        public bool Unlocked { get; set; }
        public bool Hover { get; set; }
        public bool ShowAbs = true, ShowTc = true, ShowTcc = true, ShowEsc = true, ShowBias = true, ShowMap = true,
                    ShowDiff = true, ShowBattery = true, ShowEnergy = true, ShowDrs = true;
        public Color CAbs, CTc;
        public double AbsDuty, TcDuty;          // 0..1 smoothed share of time the aid is cutting in (same idea as the pedals overlay)

        readonly Brush gray = Chrome.Br(255, 140, 140, 150), white = Chrome.Br(255, 245, 245, 245);
        readonly Color idle = Color.FromRgb(36, 36, 44);
        readonly Brush amber = Chrome.Br(255, 255, 200, 0), green = Chrome.Br(255, 51, 214, 138), red = Chrome.Br(255, 255, 70, 60);
        int chipCount, cardCount;
        double cardsTop;

        public ElecView() { SnapsToDevicePixels = false; Relayout(); }

        public Rect LockBtn { get { return Chrome.LockRect(this, Width, Unlocked); } }

        // ---- what is available ---------------------------------------------------------------
        class Chip { public string Label; public int Level, Max; public Color Color; public bool Active; public double Duty; public bool IsAid; }
        class Card { public string Label, Value; public double Bar = -1; public Brush Ink; }

        List<Chip> Chips()
        {
            var l = new List<Chip>();
            if (ShowAbs && T.AbsLevel >= 0 && T.AbsMax != 0) l.Add(new Chip { Label = "ABS", Level = T.AbsLevel, Max = T.AbsMax, Color = CAbs, Active = T.AbsOn, Duty = AbsDuty, IsAid = true });
            if (ShowTc && T.TcLevel >= 0 && T.TcMax != 0) l.Add(new Chip { Label = "TC", Level = T.TcLevel, Max = T.TcMax, Color = CTc, Active = T.TcOn, Duty = TcDuty, IsAid = true });
            if (ShowTcc && T.TcCutLevel >= 0 && T.TcCutMax != 0 && T.HasTcCut) l.Add(new Chip { Label = "TCC", Level = T.TcCutLevel, Max = T.TcCutMax, Color = CTc, Active = T.TcOn, Duty = TcDuty, IsAid = true });
            if (ShowEsc && T.EscLevel >= 0 && T.EscMax > 0) l.Add(new Chip { Label = "ESC", Level = T.EscLevel, Max = T.EscMax, Color = Color.FromRgb(80, 200, 255) });
            return l;
        }

        List<Card> Cards()
        {
            var l = new List<Card>();
            if (ShowBias && T.BrakeBias > 0)
                l.Add(new Card { Label = L.T("BRAKE BIAS", "BALANÇA DE FREIO"), Value = (T.BrakeBias * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %" });
            if (ShowMap && T.EngineMap >= 0)
                l.Add(new Card { Label = L.T("ENGINE MAP", "MAPA DO MOTOR"), Value = T.EngineMap + (T.EngineMapMax > 0 ? " / " + T.EngineMapMax : "") });
            if (ShowDiff && T.DiffPower >= 0 && T.DiffCoast >= 0)
                l.Add(new Card { Label = L.T("DIFF  PWR / COAST", "DIFERENCIAL"),
                    Value = (T.DiffPower * 100).ToString("0", CultureInfo.InvariantCulture) + " / " + (T.DiffCoast * 100).ToString("0", CultureInfo.InvariantCulture) });
            if (ShowBattery && T.BatteryPct >= 0)
                l.Add(new Card { Label = L.T("BATTERY", "BATERIA"), Value = T.BatteryPct.ToString("0", CultureInfo.InvariantCulture) + " %", Bar = T.BatteryPct / 100,
                    Ink = T.BatteryPct < 15 ? red : white });
            if (ShowEnergy && T.EnergyPct >= 0)
                l.Add(new Card { Label = L.T("VIRTUAL ENERGY", "ENERGIA VIRTUAL"), Value = T.EnergyPct.ToString("0", CultureInfo.InvariantCulture) + " %", Bar = T.EnergyPct / 100,
                    Ink = T.EnergyPct < 10 ? red : white });
            if (ShowDrs && T.Drs >= 0)
                l.Add(new Card { Label = "DRS", Value = T.Drs == 2 ? L.T("OPEN", "ABERTO") : (T.Drs == 1 ? L.T("READY", "PRONTO") : L.T("OFF", "OFF")),
                    Ink = T.Drs == 2 ? green : (T.Drs == 1 ? amber : gray) });
            return l;
        }

        // changes when the set of visible items changes (another car, another game): the window then reflows
        public string Signature()
        {
            var s = new System.Text.StringBuilder();
            foreach (Chip c in Chips()) s.Append(c.Label).Append(',');
            foreach (Card c in Cards()) s.Append(c.Label).Append(';');
            return s.ToString();
        }

        public void Relayout()
        {
            chipCount = Chips().Count; cardCount = Cards().Count;
            double h = 0;
            if (chipCount > 0) h += CHIP_H;
            cardsTop = PAD + h + (chipCount > 0 && cardCount > 0 ? GAP : 0);
            if (cardCount > 0) h += (chipCount > 0 ? GAP : 0) + ((cardCount + 1) / 2) * CARD_H + ((cardCount + 1) / 2 - 1) * CARD_GAP;
            Width = W + 2 * PAD;
            Height = Math.Max(h, 40) + 2 * PAD;
        }

        // ---- drawing ----------------------------------------------------------------------------
        protected override void OnRender(DrawingContext dc)
        {
            Chrome.Panel(dc, Width, Height, Unlocked);
            var chips = Chips(); var cards = Cards();
            if (chips.Count == 0 && cards.Count == 0)
                Chrome.Text(this, dc, L.T("waiting for the car data", "aguardando os dados do carro"), 12, gray, true, Width / 2, Height / 2 - 8, 1);

            if (chips.Count > 0)
            {
                double cw = (W - (chips.Count - 1) * CHIP_GAP) / chips.Count;
                for (int i = 0; i < chips.Count; i++) DrawChip(dc, chips[i], PAD + i * (cw + CHIP_GAP), PAD, cw);
            }
            double cardW = (W - CARD_GAP) / 2;
            for (int i = 0; i < cards.Count; i++)
            {
                double cx = PAD + (i % 2) * (cardW + CARD_GAP), cy = cardsTop + (i / 2) * (CARD_H + CARD_GAP);
                // a lone card on the last row takes the whole width
                double w = (i == cards.Count - 1 && cards.Count % 2 == 1) ? W : cardW;
                DrawCard(dc, cards[i], cx, cy, w);
            }
            if (Unlocked) Chrome.UnlockedBanner(this, dc, Width, Height);
            Chrome.LockButton(this, dc, Unlocked, Hover);
        }

        // lights up while the aid acts, fades back with the share of time it has been acting (like the pedals overlay chips)
        void DrawChip(DrawingContext dc, Chip c, double x, double y, double w)
        {
            double k = c.IsAid ? (c.Active ? 1.0 : Math.Min(1, c.Duty * 1.6)) : 0;
            Color fill = Chrome.Mix(idle, c.Color, k);
            dc.DrawRoundedRectangle(Chrome.Br(fill), null, new Rect(x, y, w, CHIP_H), 3, 3);
            string lv = c.Level == 0 ? "OFF" : c.Level.ToString(CultureInfo.InvariantCulture) + (c.Max > 0 ? "/" + c.Max : "");
            Brush ink = k > 0.55 ? Chrome.Br(255, 10, 10, 10) : Chrome.Br(255, 200, 200, 208);
            Chrome.Text(this, dc, c.Label + " " + lv, 14, ink, true, x + w / 2, y + (CHIP_H - 14 * 1.25) / 2, 1);
        }

        void DrawCard(DrawingContext dc, Card c, double x, double y, double w)
        {
            dc.DrawRoundedRectangle(Chrome.Br(10, 255, 255, 255), null, new Rect(x, y, w, CARD_H), 3, 3);
            Chrome.Text(this, dc, c.Label, LF, gray, true, x + 10, y + 7, 0);
            Chrome.Text(this, dc, c.Value, VF, c.Ink ?? white, true, x + 10, y + 22, 0);
            if (c.Bar >= 0)
            {
                double bw = w - 20, k = Math.Max(0, Math.Min(1, c.Bar));
                dc.DrawRoundedRectangle(Chrome.Br(255, 30, 30, 38), null, new Rect(x + 10, y + CARD_H - 9, bw, 4), 2, 2);
                if (k > 0.004) dc.DrawRoundedRectangle(c.Ink == red ? red : green, null, new Rect(x + 10, y + CARD_H - 9, bw * k, 4), 2, 2);
            }
        }
    }
}
