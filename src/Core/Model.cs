// SimracingBR - driver overlay for sim racing games (pedal graph, ABS/TC, gear/speed, wheel, fuel).
// Core types shared by every game: the telemetry model, the source interface, settings and UI language.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Media;

namespace SimracingBR
{
    // ------------------------------------------------------------------ telemetry model
    // Every game source fills this in; the HUD only ever reads it.
    public class Tele
    {
        public bool Live;
        public float Gas, Brake, Clutch;
        public int Gear;              // 0 = R, 1 = N, 2.. = 1st..
        public int Rpm, MaxRpm;
        public float SpeedKmh;
        public float SteerDeg;        // signed wheel rotation in degrees
        public bool AbsOn, TcOn;
        public float Fuel;            // litres
        public float FuelPerLap;      // litres, 0 when unknown (the HUD then measures it)
        public int LapMs;             // current lap time
        public int TcLevel = -1, TcCutLevel = -1, AbsLevel = -1;   // car electronics settings, 0 = off, -1 = unknown
        public bool HasTcCut;         // the game has a separate TC cut setting (TCC chip)
        public bool ShiftUp;          // time to shift up
        public int Status = 2;        // AC convention: 0 off/menus, 1 replay, 2 live, 3 pause
        public bool InPitBox;
        public string Note = "";
        // lap / sector data (sectors overlay)
        public float TrackPos = -1;   // 0..1 position on the lap, -1 = unknown
        public int Sector = -1;       // the game's current sector (any numbering), -1 = unknown
        public bool LapValid = true;  // false after a cut / invalidated lap
        public bool InPitLane;
        public int LastLapMs, BestLapMs;           // the player's own, 0 = none yet
        public bool HasLeader;                     // the game publishes the race leader's lap times
        public int LeaderBestMs, LeaderLastMs;     // 0 = none yet
        public int Drivers;                        // drivers in the session, 0 = unknown
        public int Position;                       // the player's position in the session (1 = first), 0 = unknown
        public int[] SessionBestSecMs = new int[3]; // fastest main-sector times of anyone in the session (ms, 0 = unknown)
        public int SessionBestMs;                  // fastest lap of anyone in the session, 0 = unknown
        public string TrackId = "";                // track + car: start from scratch when either changes
        // session / fuel (fuel overlay)
        public int LapCount = -1;                  // laps the player has completed, -1 = unknown
        public int SessionLaps;                    // race length in laps, 0 = unknown or time based
        public int SessionTimeLeftMs = -1;         // time left in the session, -1 = unknown
        public bool SessionIsRace;
        public int RaceLengthSec, RaceLengthLaps;   // race length of a solo event read from the game log (any session), 0 = unknown
        public float FuelCap;                      // tank capacity in litres, 0 = unknown
        // electronics (electronics overlay), -1 = the car / game doesn't have it
        public int TcMax = -1, TcCutMax = -1, AbsMax = -1, EscLevel = -1, EscMax = -1, EngineMap = -1, EngineMapMax = -1;
        public float BrakeBias = -1;               // share of the braking on the FRONT axle, 0..1
        public float DiffPower = -1, DiffCoast = -1;   // 0..1
        public float BatteryPct = -1;              // hybrid battery / ERS charge, 0..100
        public float EnergyPct = -1;               // virtual energy left (LMU hypercars), 0..100
        public int Drs = -1;                       // -1 none, 0 off, 1 available, 2 open
        public bool InSetup;                       // the game is showing its car setup screen
        public float[] TyrePsi = { -1, -1, -1, -1 };      // pressure of each tyre in psi (FL, FR, RL, RR), -1 = unknown
        public float[] TyreTemp = { -1, -1, -1, -1 };     // core temperature of each tyre in degrees C, -1 = unknown
        public float[] TyreColdPsi = { -1, -1, -1, -1 };  // the cold / setup pressure when the game publishes it, -1 = unknown

        // forget the optional fuel / electronics values of the previous game or car
        public void ClearExtras()
        {
            LapCount = -1; Position = 0; SessionLaps = 0; SessionTimeLeftMs = -1; SessionIsRace = false; FuelCap = 0; RaceLengthSec = RaceLengthLaps = 0;
            TcMax = TcCutMax = AbsMax = EscLevel = EscMax = EngineMap = EngineMapMax = -1;
            BrakeBias = DiffPower = DiffCoast = BatteryPct = EnergyPct = -1; Drs = -1;
            for (int i = 0; i < 4; i++) TyrePsi[i] = TyreTemp[i] = TyreColdPsi[i] = -1;
        }
    }

    // One per game. Read() returns false while the game is not running / its shared memory is missing.
    public interface ITelemetrySource
    {
        string Name { get; }          // short name shown in the HUD and logs ("AC EVO", "ACC", "AMS2")
        string[] Processes { get; }   // game process names, used for "hide when the game is not focused"
        string Error { get; }
        bool Read(Tele t);
        string Probe();               // one line of raw values for --probe
    }

    // Base for games that publish memory-mapped files. Opens them lazily (retry once a second) and copies
    // each page into a byte buffer every tick; subclasses decode the buffers.
    public abstract class ShmSource : ITelemetrySource
    {
        readonly string[] maps;
        readonly int[] lens;
        MemoryMappedFile[] files;
        MemoryMappedViewAccessor[] views;
        protected readonly byte[][] Pages;
        DateTime nextTry = DateTime.MinValue, nextProcCheck = DateTime.MinValue;
        string error = "";

        public abstract string Name { get; }
        public abstract string[] Processes { get; }
        public string Error { get { return error; } }
        protected static int Pos(int p, int drivers) { return p >= 1 && p <= (drivers > 0 ? drivers : 999) ? p : 0; }   // 1-based position, 0 = unknown
        // Only open the maps while the game process runs (needed when another game uses the same map names).
        protected virtual bool RequireProcess { get { return false; } }

        protected ShmSource(string[] mapNames, int[] pageLens)
        {
            maps = mapNames; lens = pageLens;
            Pages = new byte[maps.Length][];
            for (int i = 0; i < maps.Length; i++) Pages[i] = new byte[lens[i]];
        }

        public abstract bool Read(Tele t);
        public abstract string Probe();

        void Close()
        {
            if (views != null) foreach (var v in views) if (v != null) v.Dispose();
            if (files != null) foreach (var f in files) if (f != null) f.Dispose();
            views = null; files = null;
        }

        bool Open()
        {
            if (views != null)
            {
                // don't keep the maps of a closed game alive
                if (RequireProcess && DateTime.UtcNow >= nextProcCheck)
                {
                    nextProcCheck = DateTime.UtcNow.AddSeconds(2);
                    if (!GameRunning()) { Close(); error = Name + " not running"; return false; }
                }
                return true;
            }
            if (DateTime.UtcNow < nextTry) return false;
            nextTry = DateTime.UtcNow.AddSeconds(1);
            if (RequireProcess && !GameRunning()) { error = Name + " not running"; return false; }
            try
            {
                files = new MemoryMappedFile[maps.Length];
                views = new MemoryMappedViewAccessor[maps.Length];
                for (int i = 0; i < maps.Length; i++)
                {
                    files[i] = MemoryMappedFile.OpenExisting(maps[i], MemoryMappedFileRights.Read);
                    views[i] = files[i].CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                }
                nextProcCheck = DateTime.UtcNow.AddSeconds(2);
                error = "";
                return true;
            }
            catch (Exception e) { error = e.Message; Close(); return false; }
        }

        // Copies every page into Pages. False when the game isn't there.
        protected bool Fetch()
        {
            if (!Open()) return false;
            try
            {
                for (int i = 0; i < maps.Length; i++)
                {
                    Array.Clear(Pages[i], 0, lens[i]);
                    views[i].ReadArray<byte>(0, Pages[i], 0, (int)Math.Min(lens[i], views[i].Capacity));
                }
                return true;
            }
            catch (Exception e) { error = e.Message; Close(); return false; }
        }

        // Copies count bytes at offset of map i into buf (for big maps where only a slice is needed). Call after Fetch().
        protected bool ReadRange(int i, long offset, byte[] buf, int count)
        {
            if (views == null) return false;
            try
            {
                if (offset + count > views[i].Capacity) return false;
                views[i].ReadArray<byte>(offset, buf, 0, count);
                return true;
            }
            catch (Exception e) { error = e.Message; Close(); return false; }
        }

        public bool GameRunning()
        {
            foreach (string p in Processes)
            {
                Process[] ps = Process.GetProcessesByName(p);
                bool any = ps.Length > 0;
                foreach (Process x in ps) x.Dispose();
                if (any) return true;
            }
            return false;
        }

        // fixed-size C string inside a page (UTF-8, or UTF-16 when wide)
        protected static string Str(byte[] b, int off, int len, bool wide)
        {
            try { return (wide ? System.Text.Encoding.Unicode : System.Text.Encoding.UTF8).GetString(b, off, len).Split('\0')[0]; }
            catch { return ""; }
        }


        // tyre pressure (psi) and core temperature of the four tyres from the physics page (AC layout: wheelsPressure @88, tyreCoreTemperature @152)
        protected static void FillTyres(Tele t, byte[] physics)
        {
            for (int i = 0; i < 4; i++)
            {
                float p = BitConverter.ToSingle(physics, AcPhys.PRESSURE + 4 * i), c = BitConverter.ToSingle(physics, AcPhys.CORETEMP + 4 * i);
                t.TyrePsi[i] = p > 8 && p < 80 ? p : -1;
                t.TyreTemp[i] = c > -20 && c < 400 ? c : -1;
            }
        }
        // a game lap time in ms, or 0 while it isn't a real lap time yet
        protected static int LapOrZero(long ms) { return ms > 0 && ms < 3600000 ? (int)ms : 0; }

        // "57:16", "1:02:30" or "12:34.567" -> milliseconds, -1 when it isn't a clock
        protected static int ParseClockMs(string s)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            string[] p = s.Trim().Split(':');
            if (p.Length < 2 || p.Length > 3) return -1;
            double total = 0;
            for (int i = 0; i < p.Length; i++)
            {
                double v;
                if (!double.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v) || v < 0) return -1;
                total = total * 60 + v;
            }
            return total > 100 * 3600 ? -1 : (int)(total * 1000);
        }

        protected static float Clamp01(float v) { return float.IsNaN(v) ? 0 : (v < 0 ? 0 : (v > 1 ? 1 : v)); }
    }

    // ------------------------------------------------------------------ settings
    public class Settings
    {
        readonly string path;
        readonly Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Settings(string p)
        {
            path = p;
            if (!File.Exists(p)) return;
            foreach (string line in File.ReadAllLines(p))
            {
                string l = line.Trim();
                if (l.Length == 0 || l[0] == '#' || l[0] == ';') continue;
                int i = l.IndexOf('='); if (i <= 0) continue;
                d[l.Substring(0, i).Trim()] = l.Substring(i + 1).Trim();
            }
        }
        public string Str(string k, string def) { string v; return d.TryGetValue(k, out v) ? v : def; }
        public double Num(string k, double def) { double v; return double.TryParse(Str(k, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : def; }
        public bool Bool(string k, bool def) { string v = Str(k, ""); if (v == "") return def; return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); }
        public Color Col(string k, string def)
        {
            try { return (Color)ColorConverter.ConvertFromString(Str(k, def)); }
            catch { return (Color)ColorConverter.ConvertFromString(def); }
        }
        public void Set(string k, string v) { d[k] = v; }
        public void Save()
        {
            var lines = new List<string>();
            lines.Add("# SimracingBR settings. Edit while the app is closed. Colors are #RRGGBB.");
            foreach (var kv in d) lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(path, lines.ToArray());
        }
    }

    // UI language: Portuguese when Windows is in Portuguese, English otherwise.
    public static class L
    {
        static readonly bool Pt = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";
        public static string T(string en, string pt) { return Pt ? pt : en; }
    }
}
