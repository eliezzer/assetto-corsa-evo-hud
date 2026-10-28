// Fuel maths for the fuel overlay: consumption per lap, laps left on the tank and the fuel needed for the WHOLE race.
//
//   per lap      = the average of the last clean laps measured here (kept between sessions and runs), otherwise what the game says
//                  (AC EVO resets its own value when the session changes)
//   laps left    = fuel in the tank / per lap
//   race laps    = the race's total laps                                           (lap race)
//                = total race time / fastest valid lap + 1                           (timed race; the lap running when time runs out is finished,
//                                                                                     and AC EVO online was seen giving one more after it)
//   race fuel    = (race laps + margin) * per lap                                    <- what the overlay suggests loading
//
// It never looks at the time left or at what is in the tank: the suggestion is for the full race distance.
// The race length is the "race laps" / "race minutes" setting, or the event definition / game log (remembered as the last race
// seen), or, in a race session, what the game says (laps, or the highest
// session clock seen, i.e. the start of a timed race). The lap time is the fastest valid lap seen on this track and car in any
// session (so the qualifying lap counts), and it, like the consumption, is remembered between runs.
using System;
using System.Globalization;

namespace SimracingBR
{
    public class FuelModel
    {
        readonly Settings cfg;                     // where the fastest lap and the consumption are remembered (may be null)

        // settings
        public double MarginLaps = 1.0;            // extra laps of fuel on top of the estimate
        public int ManualRaceLaps;                 // 0 = use what the game says
        public int ManualRaceMinutes;              // 0 = use what the game says

        // results
        public double PerLap;                      // litres per lap, 0 = unknown
        public double LapsLeft;                    // laps the tank lasts, 0 = unknown
        public int BestValidLapMs;                 // fastest valid lap on this track and car, 0 = none yet
        public double RaceLaps = -1;               // laps of the whole race, -1 = unknown
        public int RaceMinutes;                    // total time of a timed race, 0 = lap race / unknown
        public double RaceFuel = -1;               // litres for the whole race, -1 = unknown
        public bool TimedRace;                     // the race laps came from the clock
        public int RaceSource;                     // where the race length comes from: 0 unknown, 1 your setting, 2 the game (race session), 3 the last race seen, 4 the game log (solo event)

        // lap in progress
        int prevLapMs, prevLapCount = -1;
        float lapStartFuel, lastFuel;
        bool observedFromStart, lapValid = true, lapPit;
        readonly double[] recent = new double[3];
        int recentN;
        double storedPerLap;
        int raceStartClockMs;                      // highest session clock seen in a race session
        string trackId = "";
        int learnedLaps, learnedMinutes; bool learnedIsLaps;     // the last race seen, kept between runs

        public FuelModel(Settings cfg)
        {
            this.cfg = cfg;
            if (cfg != null) { learnedLaps = (int)cfg.Num("FuelLastRaceLaps", 0); learnedMinutes = (int)cfg.Num("FuelLastRaceMinutes", 0); learnedIsLaps = cfg.Bool("FuelLastRaceIsLaps", false); }
        }

        public void Update(Tele t)
        {
            if (!t.Live) return;
            if (t.TrackId.Length > 0 && t.TrackId != trackId) { trackId = t.TrackId; Reset(); Load(); }

            if (!t.LapValid) lapValid = false;
            if (t.InPitLane || t.InPitBox) lapPit = true;
            if (lapStartFuel <= 0 && t.Fuel > 0) lapStartFuel = t.Fuel;
            // refuelled, fuel changed for a new session or the lap counter went back: the lap in progress can't be measured
            if (Math.Abs(t.Fuel - lastFuel) > 0.5f || (t.LapCount >= 0 && t.LapCount < prevLapCount)) { observedFromStart = false; lapStartFuel = t.Fuel; }
            lastFuel = t.Fuel;

            bool rollover = (t.LapCount >= 0 && prevLapCount >= 0 && t.LapCount > prevLapCount)
                         || (t.LapCount < 0 && prevLapMs > 5000 && t.LapMs < prevLapMs - 2000);
            if (rollover) EndLap(t);
            prevLapMs = t.LapMs; prevLapCount = t.LapCount;

            // a new race session starts its clock over: forget the old one
            if (t.SessionIsRace && t.SessionTimeLeftMs > raceStartClockMs) raceStartClockMs = t.SessionTimeLeftMs;
            if (!t.SessionIsRace) raceStartClockMs = 0;

            double m = Measured();
            PerLap = m > 0.01 ? m : (t.FuelPerLap > 0.01f ? t.FuelPerLap : 0);
            LapsLeft = PerLap > 0.01 && t.Fuel > 0 ? t.Fuel / PerLap : 0;
            Race(t);
        }

        void Reset()
        {
            recentN = 0; BestValidLapMs = 0; storedPerLap = 0; observedFromStart = false; lapValid = true; lapPit = false;
            lapStartFuel = lastFuel = 0; prevLapMs = 0; prevLapCount = -1; raceStartClockMs = 0;
        }

        double Measured()
        {
            if (recentN == 0) return storedPerLap;
            double s = 0; int n = Math.Min(recentN, recent.Length);
            for (int i = 0; i < n; i++) s += recent[i];
            return s / n;
        }

        void EndLap(Tele t)
        {
            if (observedFromStart)
            {
                float used = lapStartFuel - t.Fuel;
                if (!lapPit && used > 0.05f && used < 80f) { recent[recentN % recent.Length] = used; recentN++; Save(); }
                int lapTime = t.LastLapMs > 0 && Math.Abs(t.LastLapMs - prevLapMs) < 1500 ? t.LastLapMs : prevLapMs;   // the game's own time when it already has it
                if (!lapPit && lapValid && lapTime > 20000 && lapTime < 3600000 && (BestValidLapMs == 0 || lapTime < BestValidLapMs))
                { BestValidLapMs = lapTime; Save(); }
            }
            observedFromStart = true; lapValid = true; lapPit = false; lapStartFuel = t.Fuel;
        }

        void Race(Tele t)
        {
            RaceLaps = -1; RaceFuel = -1; RaceMinutes = 0; TimedRace = false; RaceSource = 0;
            int lap = BestValidLapMs > 0 ? BestValidLapMs : (t.BestLapMs > 0 ? t.BestLapMs : 0);
            int laps = 0, seconds = 0;

            // the race length: your setting, else the game's log (solo events: known before the race), else what the game says
            // in the race session itself, else the last race seen
            if (ManualRaceLaps > 0) { laps = ManualRaceLaps; RaceSource = 1; }
            else if (ManualRaceMinutes > 0) { seconds = ManualRaceMinutes * 60; RaceSource = 1; }
            else if (t.RaceLengthLaps > 0) { laps = t.RaceLengthLaps; RaceSource = 4; Learn(laps, 0); }
            else if (t.RaceLengthSec > 0) { seconds = t.RaceLengthSec; RaceSource = 4; Learn(0, (int)Math.Round(seconds / 60.0)); }
            else if (t.SessionIsRace && t.SessionLaps > 0) { laps = t.SessionLaps; RaceSource = 2; Learn(laps, 0); }
            else if (t.SessionIsRace && raceStartClockMs > 0) { int min = (int)Math.Round(raceStartClockMs / 60000.0); seconds = min * 60; RaceSource = 2; Learn(0, min); }
            else if (learnedIsLaps && learnedLaps > 0) { laps = learnedLaps; RaceSource = 3; }
            else if (learnedMinutes > 0) { seconds = learnedMinutes * 60; RaceSource = 3; }

            if (laps > 0) RaceLaps = laps;
            else if (seconds > 0)
            {
                TimedRace = true; RaceMinutes = (int)Math.Round(seconds / 60.0);
                if (lap > 0) RaceLaps = seconds * 1000.0 / lap + 1;
            }
            if (RaceLaps >= 0 && PerLap > 0.01) RaceFuel = (RaceLaps + MarginLaps) * PerLap;
        }

        // the race just seen becomes the one assumed next time, so the total is ready in the next qualifying
        void Learn(int laps, int minutes)
        {
            bool isLaps = laps > 0;
            if (cfg == null || (isLaps == learnedIsLaps && laps == learnedLaps && minutes == learnedMinutes)) return;
            learnedIsLaps = isLaps; learnedLaps = laps; learnedMinutes = minutes;
            cfg.Set("FuelLastRaceLaps", laps.ToString(CultureInfo.InvariantCulture));
            cfg.Set("FuelLastRaceMinutes", minutes.ToString(CultureInfo.InvariantCulture));
            cfg.Set("FuelLastRaceIsLaps", isLaps ? "1" : "0");
            try { cfg.Save(); } catch { }
        }

        // ---- remembered between runs (per track + car) -----------------------------------------
        string Key(string what)
        {
            uint h = 2166136261;
            foreach (char c in trackId) { h ^= c; h *= 16777619; }
            return what + h.ToString("x8");
        }

        void Load()
        {
            if (cfg == null || trackId.Length == 0) return;
            BestValidLapMs = (int)cfg.Num(Key("FuelBest_"), 0);
            storedPerLap = cfg.Num(Key("FuelPer_"), 0);
        }

        void Save()
        {
            if (cfg == null || trackId.Length == 0) return;
            if (BestValidLapMs > 0) cfg.Set(Key("FuelBest_"), BestValidLapMs.ToString(CultureInfo.InvariantCulture));
            double per = Measured();
            if (per > 0) cfg.Set(Key("FuelPer_"), per.ToString("0.000", CultureInfo.InvariantCulture));
            try { cfg.Save(); } catch { }
        }
    }
}
