// Tyre pressure advisor: after a few valid laps it tells you which cold (setup) pressure to put on each wheel so that the hot
// pressure on track ends up at the target (27.0 psi for slicks by default).
//
//   hot      = the average pressure of the tyre over the last valid lap (above 80 km/h, not in the pits)
//   cold     = the pressure at the start of the session, when the car is still standing in the pits (that is the setup value),
//              or the value typed in Settings, or what the game says when it publishes it
//   advice   = target * cold / hot                                   (the pressure rises by about the same ratio whatever the start value)
//
// Example: setup 25.5, hot 27.9 -> 27.0 * 25.5 / 27.9 = 24.7.
using System;

namespace SimracingBR
{
    public class TyreModel
    {
        public const double Tolerance = 0.5;                // psi around the target that doesn't cost much performance

        // settings
        public double Target = 27.0;
        public int LapsNeeded = 2;
        public readonly double[] ColdOverride = new double[4];   // 0 = automatic

        // live
        public readonly float[] Hot = { -1, -1, -1, -1 };
        public readonly float[] Temp = { -1, -1, -1, -1 };
        public bool HasData;
        // measured
        public readonly double[] Cold = new double[4];           // automatic cold pressure, 0 = unknown
        public readonly double[] LapAvg = new double[4];         // average hot pressure of the last valid lap, 0 = none yet
        public readonly double[] PrevAvg = new double[4];        // ... of the lap before
        public int ValidLaps;                                    // valid laps since the cold pressure was taken
        // result
        public readonly double[] Advice = new double[4];         // cold pressure to set, 0 = not ready
        public bool Ready;

        readonly double[] sum = new double[4];
        int n;
        bool moved, observedFromStart, lapValid = true, lapPit;
        int prevLapMs, prevLapCount = -1;
        string trackId = "";

        public void Update(Tele t)
        {
            if (!t.Live) return;
            if (t.TrackId.Length > 0 && t.TrackId != trackId) { trackId = t.TrackId; Reset(); }

            HasData = t.TyrePsi[0] > 0 && t.TyrePsi[1] > 0 && t.TyrePsi[2] > 0 && t.TyrePsi[3] > 0;
            for (int i = 0; i < 4; i++) { Hot[i] = t.TyrePsi[i]; Temp[i] = t.TyreTemp[i]; }
            if (!HasData) { Ready = false; return; }

            if (t.LapCount >= 0 && prevLapCount >= 0 && t.LapCount < prevLapCount) Reset();      // new session on the same track
            if (!t.LapValid) lapValid = false;
            if (t.InPitLane || t.InPitBox) lapPit = true;

            // the cold pressure: while the car has not moved yet, whatever it shows standing in the pits is the setup
            if (!moved)
            {
                if (t.SpeedKmh < 3 && t.LapCount <= 0) { for (int i = 0; i < 4; i++) Cold[i] = Hot[i]; }   // only at the very start of a session
                else if (t.SpeedKmh > 20) moved = true;
            }

            // average pressure over the lap, only while driving properly
            if (t.SpeedKmh > 80 && !t.InPitLane) { for (int i = 0; i < 4; i++) sum[i] += Hot[i]; n++; }

            bool rollover = (t.LapCount >= 0 && prevLapCount >= 0 && t.LapCount > prevLapCount)
                         || (t.LapCount < 0 && prevLapMs > 5000 && t.LapMs < prevLapMs - 2000);
            if (rollover) EndLap();
            prevLapMs = t.LapMs; prevLapCount = t.LapCount;

            Compute(t);
        }

        void Reset()
        {
            for (int i = 0; i < 4; i++) { Cold[i] = 0; LapAvg[i] = 0; PrevAvg[i] = 0; sum[i] = 0; Advice[i] = 0; }
            n = 0; ValidLaps = 0; moved = false; observedFromStart = false; lapValid = true; lapPit = false;
            prevLapMs = 0; prevLapCount = -1; Ready = false;
        }

        void EndLap()
        {
            if (observedFromStart && lapValid && !lapPit && n > 300)    // ~ 5 s at 60 Hz: a real lap
            {
                for (int i = 0; i < 4; i++) { PrevAvg[i] = LapAvg[i]; LapAvg[i] = sum[i] / n; }
                ValidLaps++;
            }
            for (int i = 0; i < 4; i++) sum[i] = 0;
            n = 0; observedFromStart = true; lapValid = true; lapPit = false;
        }

        // the cold pressure in use for wheel i: typed in Settings, else what the game says, else the one measured at the start
        public double ColdFor(int i, Tele t)
        {
            if (ColdOverride[i] > 0) return ColdOverride[i];
            if (t != null && t.TyreColdPsi[i] > 0) return t.TyreColdPsi[i];
            return Cold[i];
        }

        public bool ColdKnown(Tele t) { for (int i = 0; i < 4; i++) if (ColdFor(i, t) <= 0) return false; return true; }

        void Compute(Tele t)
        {
            Ready = ValidLaps >= LapsNeeded && ColdKnown(t);
            for (int i = 0; i < 4; i++)
                Advice[i] = Ready && LapAvg[i] > 1 ? Target * ColdFor(i, t) / LapAvg[i] : 0;
        }

        // psi the pressure is still moving per lap (positive = rising), 0 until two laps are known
        public double Trend(int i) { return ValidLaps >= 2 && PrevAvg[i] > 0 ? LapAvg[i] - PrevAvg[i] : 0; }
    }
}
