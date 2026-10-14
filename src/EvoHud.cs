// EvoHud - driver overlay for Assetto Corsa EVO (pedal graph, ABS/TC, gear/speed, wheel, fuel).
// Reads the game's shared memory directly (Local\acevo_pmf_physics / _graphics). No SimHub needed.
// Build with build.ps1 (needs only the .NET Framework that ships with Windows).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace EvoHud
{
    // ------------------------------------------------------------------ telemetry model
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
        public float FuelPerLap;      // litres, 0 when unknown
        public int LapMs;             // current lap time
        public int TcLevel = -1, TcCutLevel = -1, AbsLevel = -1;   // car electronics settings, 0 = off, -1 = unknown
        public bool ShiftUp;          // time to shift up
        public int Status = 2;        // AC convention: 0 off, 1 replay, 2 live, 3 pause
        public bool InPitBox;
        public string Note = "";
    }

    // Prefix of the AC EVO graphics page (pack = 4). Only used to compute offsets at runtime.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GfxPrefix
    {
        public int packetId, status;
        public ulong focusedA, focusedB, playerA, playerB;
        public ushort rpm;
        public byte isRpmLimiterOn, isChangeUp, isChangeDown, tcActive, absActive, escActive, launchActive,
                    isIgnitionOn, isEngineRunning, kersIsCharging, isWrongWay, isDrsAvailable,
                    batteryIsCharging, isMaxKj, isMaxChargeKj;
        public short displaySpeedKmh, displaySpeedMph, displaySpeedMs;
        public float pitspeedingDelta;
        public short gearInt;
        public float rpmPercent, gasPercent, brakePercent, handbrakePercent, clutchPercent, steeringPercent,
                     ffbStrength, carFfbMultiplier, waterTemperaturePercent, waterPressureBar, fuelPressureBar;
        public sbyte waterTemperatureC, airTemperatureC;
        public float oilTemperatureC, oilPressureBar, exhaustTemperatureC, gX, gY, gZ,
                     turboBoost, turboBoostLevel, turboBoostPerc;
        public int steerDegrees;
        public float currentKm;
        public uint totalKm, totalDrivingTimeS;
        public int todH, todM, todS, deltaTimeMs, currentLapTimeMs, predictedLapTimeMs;
        public float fuelLiterCurrentQuantity, fuelLiterCurrentQuantityPercent, fuelLiterPerKm, kmPerFuelLiter, currentTorque;
        public int currentBhp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] tyreLf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] tyreRf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] tyreLr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] tyreRr;
        public float npos, kersChargePerc, kersCurrentPerc, controlLockTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] carDamage;
        public int carLocation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] pitInfo;
        public float fuelLiterUsed, fuelLiterPerLap, lapsPossibleWithFuel;
        public float batteryTemperature, batteryVoltage, instFuelLiterPerKm, instKmPerFuelLiter, gearRpmWindow;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] instrumentation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] instrumentationMin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] instrumentationMax;
        // _SMEvoElectronics: tc_level, tc_cut_level, abs_level, esc_level, ebb_level (int8), brake_bias (float @8) ...
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] electronics;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] electronicsMin;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] electronicsMax;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)] public byte[] electronicsIsModifiable;
        public int totalLapCount;
        public uint currentPos, totalDrivers;
        public int lastLaptimeMs, bestLaptimeMs, flag, globalFlag;
        public uint maxGears;
        public int engineType;
        public byte hasKers, isLastLap;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)] public byte[] performanceModeName;
        public float diffCoastRaw, diffPowerRaw;
        public int raceCutGainedTimeMs, distanceToDeadline;
        public float raceCutCurrentDelta;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] sessionState;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public byte[] timingState;
        public int playerPing, playerLatency, playerCpu, playerCpuAvg, playerQos, playerQosAvg, playerFps, playerFpsAvg;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)] public byte[] driverName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)] public byte[] driverSurname;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)] public byte[] carModel;
        public byte isInPitBox, isInPitLane, isValidLap;
    }

    public class EvoSource
    {
        // physics page offsets (documented, pack = 4)
        const int P_GAS = 4, P_BRAKE = 8, P_GEAR = 16, P_RPM = 20, P_STEER = 24, P_SPEED = 28,
                  P_TC = 204, P_ABS = 252, P_CLUTCH = 364, P_MAXRPM = 588, P_TCACT = 672, P_ABSACT = 676;
        const int PHYS_LEN = 800;

        static int G(string f) { return (int)Marshal.OffsetOf(typeof(GfxPrefix), f); }
        static readonly int G_STEERDEG = G("steerDegrees"), G_STEERPCT = G("steeringPercent"), G_GEAR = G("gearInt"),
            G_TCACT = G("tcActive"), G_ABSACT = G("absActive"), G_FUEL = G("fuelLiterCurrentQuantity"),
            G_FUELLAP = G("fuelLiterPerLap"), G_LAPMS = G("currentLapTimeMs"), G_LAPSPOSS = G("lapsPossibleWithFuel"),
            G_RPM = G("rpm"), G_PACKET = G("packetId"), G_ELEC = G("electronics"), G_SHIFTUP = G("isChangeUp"),
            G_STATUS = G("status"), G_PITBOX = G("isInPitBox"), G_PITLANE = G("isInPitLane"), G_LOCATION = G("carLocation");
        static readonly int GFX_LEN = G("isValidLap") + 1;

        MemoryMappedFile fPhy, fGfx;
        MemoryMappedViewAccessor aPhy, aGfx;
        readonly byte[] pb = new byte[PHYS_LEN];
        readonly byte[] gb = new byte[GFX_LEN];
        DateTime nextTry = DateTime.MinValue, lastChange = DateTime.MinValue;
        int lastPacket = -1;
        public string Error = "";

        public byte[] Phys { get { return pb; } }
        public byte[] Gfx { get { return gb; } }

        void Close()
        {
            if (aPhy != null) aPhy.Dispose(); if (aGfx != null) aGfx.Dispose();
            if (fPhy != null) fPhy.Dispose(); if (fGfx != null) fGfx.Dispose();
            aPhy = aGfx = null; fPhy = fGfx = null;
        }

        bool Open()
        {
            if (aPhy != null && aGfx != null) return true;
            if (DateTime.UtcNow < nextTry) return false;
            nextTry = DateTime.UtcNow.AddSeconds(1);
            try
            {
                Close();
                fPhy = MemoryMappedFile.OpenExisting("Local\\acevo_pmf_physics", MemoryMappedFileRights.Read);
                fGfx = MemoryMappedFile.OpenExisting("Local\\acevo_pmf_graphics", MemoryMappedFileRights.Read);
                aPhy = fPhy.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                aGfx = fGfx.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                Error = "";
                return true;
            }
            catch (Exception e) { Error = e.Message; Close(); return false; }
        }

        public bool Read(Tele t)
        {
            if (!Open()) { t.Live = false; t.Note = "WAITING FOR AC EVO"; return false; }
            try
            {
                aPhy.ReadArray<byte>(0, pb, 0, Math.Min(PHYS_LEN, (int)aPhy.Capacity));
                aGfx.ReadArray<byte>(0, gb, 0, Math.Min(GFX_LEN, (int)aGfx.Capacity));
            }
            catch (Exception e) { Error = e.Message; Close(); t.Live = false; return false; }

            int packet = BitConverter.ToInt32(pb, 0);
            if (packet != lastPacket) { lastPacket = packet; lastChange = DateTime.UtcNow; }
            t.Live = (DateTime.UtcNow - lastChange).TotalSeconds < 2.0;
            t.Note = t.Live ? "" : "NO LIVE DATA";

            t.Gas = Clamp01(BitConverter.ToSingle(pb, P_GAS));
            t.Brake = Clamp01(BitConverter.ToSingle(pb, P_BRAKE));
            t.Clutch = Clamp01(BitConverter.ToSingle(pb, P_CLUTCH));
            t.Gear = BitConverter.ToInt32(pb, P_GEAR);
            t.Rpm = BitConverter.ToInt32(pb, P_RPM);
            t.MaxRpm = BitConverter.ToInt32(pb, P_MAXRPM);
            t.SpeedKmh = BitConverter.ToSingle(pb, P_SPEED);
            t.AbsOn = BitConverter.ToInt32(pb, P_ABSACT) != 0 || gb[G_ABSACT] != 0;
            t.TcOn = BitConverter.ToInt32(pb, P_TCACT) != 0 || gb[G_TCACT] != 0;
            t.SteerDeg = BitConverter.ToInt32(gb, G_STEERDEG);
            t.Fuel = BitConverter.ToSingle(gb, G_FUEL);
            t.FuelPerLap = BitConverter.ToSingle(gb, G_FUELLAP);
            t.LapMs = BitConverter.ToInt32(gb, G_LAPMS);
            // the game's own shift indicator; fall back to rpm if it never fires
            t.ShiftUp = gb[G_SHIFTUP] != 0 || (t.MaxRpm > 0 && t.Rpm >= t.MaxRpm * 0.97);
            t.TcLevel = (sbyte)gb[G_ELEC]; t.TcCutLevel = (sbyte)gb[G_ELEC + 1]; t.AbsLevel = (sbyte)gb[G_ELEC + 2];
            t.Status = BitConverter.ToInt32(gb, G_STATUS);
            t.InPitBox = gb[G_PITBOX] != 0;
            if (float.IsNaN(t.Fuel) || t.Fuel < 0 || t.Fuel > 1000) t.Fuel = 0;
            if (float.IsNaN(t.FuelPerLap) || t.FuelPerLap < 0 || t.FuelPerLap > 100) t.FuelPerLap = 0;
            return true;
        }

        public string Probe()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "pktP={0} gas={1:0.00} brk={2:0.00} clu={3:0.00} gearP={4} gearG={5} rpmP={6} rpmG={7} maxRpm={8} spd={9:0.0} " +
                "steerRaw={10:0.000} steerDegG={11} steerPctG={12:0.000} tcF={13:0.000} absF={14:0.000} tcAct={15} absAct={16} " +
                "tcG={17} absG={18} fuel={19:0.00} fuelLapG={20:0.000} lapsPoss={21:0.0} lapMs={22} physFuel={23:0.00} " +
                "elec=[tc {24} tcCut {25} abs {26} esc {27} ebb {28} bb {29:0.000}] " +
                "status={30} pitBox={31} pitLane={32} location={33} shiftUp={34}",
                BitConverter.ToInt32(pb, 0), BitConverter.ToSingle(pb, P_GAS), BitConverter.ToSingle(pb, P_BRAKE),
                BitConverter.ToSingle(pb, P_CLUTCH), BitConverter.ToInt32(pb, P_GEAR), BitConverter.ToInt16(gb, G_GEAR),
                BitConverter.ToInt32(pb, P_RPM), BitConverter.ToUInt16(gb, G_RPM), BitConverter.ToInt32(pb, P_MAXRPM),
                BitConverter.ToSingle(pb, P_SPEED), BitConverter.ToSingle(pb, P_STEER),
                BitConverter.ToInt32(gb, G_STEERDEG), BitConverter.ToSingle(gb, G_STEERPCT),
                BitConverter.ToSingle(pb, P_TC), BitConverter.ToSingle(pb, P_ABS),
                BitConverter.ToInt32(pb, P_TCACT), BitConverter.ToInt32(pb, P_ABSACT), gb[G_TCACT], gb[G_ABSACT],
                BitConverter.ToSingle(gb, G_FUEL), BitConverter.ToSingle(gb, G_FUELLAP),
                BitConverter.ToSingle(gb, G_LAPSPOSS), BitConverter.ToInt32(gb, G_LAPMS),
                BitConverter.ToSingle(pb, 12),
                (sbyte)gb[G_ELEC], (sbyte)gb[G_ELEC + 1], (sbyte)gb[G_ELEC + 2], (sbyte)gb[G_ELEC + 3], (sbyte)gb[G_ELEC + 4],
                BitConverter.ToSingle(gb, G_ELEC + 8),
                BitConverter.ToInt32(gb, G_STATUS), gb[G_PITBOX], gb[G_PITLANE], BitConverter.ToInt32(gb, G_LOCATION), gb[G_SHIFTUP]);
        }

        static float Clamp01(float v) { return float.IsNaN(v) ? 0 : (v < 0 ? 0 : (v > 1 ? 1 : v)); }
    }

    public class DemoSource
    {
        readonly DateTime t0 = DateTime.UtcNow;
        public void Read(Tele t)
        {
            double s = (DateTime.UtcNow - t0).TotalSeconds;
            double lap = s % 60.0;
            double ph = s * 1.1;
            double gas = Math.Max(0, Math.Min(1, 0.55 + 0.6 * Math.Sin(ph) + 0.15 * Math.Sin(ph * 3.1)));
            double brk = Math.Max(0, Math.Min(1, 0.9 * Math.Sin(ph + 2.4) - 0.15));
            if (brk > 0.05) gas *= 0.1;
            double clu = Math.Max(0, Math.Sin(ph * 0.5 + 1.0) - 0.8) * 4.0;
            t.Live = true; t.Note = "DEMO";
            t.Gas = (float)gas; t.Brake = (float)brk; t.Clutch = (float)Math.Min(1, clu);
            t.SpeedKmh = (float)(110 + 90 * Math.Sin(ph * 0.9));
            t.Gear = 2 + (int)Math.Max(0, Math.Min(5, t.SpeedKmh / 40.0));
            t.MaxRpm = 8500;
            t.Rpm = (int)(4000 + 4400 * ((t.SpeedKmh % 40.0) / 40.0) * (0.5 + 0.5 * gas));
            t.SteerDeg = (float)(220 * Math.Sin(ph * 0.7));
            t.AbsOn = brk > 0.85; t.TcOn = gas > 0.93;
            t.FuelPerLap = 2.35f;
            t.Fuel = (float)(48.0 - 2.35 * (s / 60.0));
            if (t.Fuel < 0) t.Fuel = 0;
            t.LapMs = (int)(lap * 1000);
            t.TcLevel = 3; t.TcCutLevel = 2; t.AbsLevel = 4;
            t.ShiftUp = t.Rpm >= t.MaxRpm * 0.95;
        }
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
            lines.Add("# EvoHud settings. Edit while the app is closed. Colors are #RRGGBB.");
            foreach (var kv in d) lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(path, lines.ToArray());
        }
    }

    // ------------------------------------------------------------------ HUD drawing
    public class HudView : FrameworkElement
    {
        public const double W = 644, H = 162, H_NOFUEL = 128;
        const int N = 420;                       // history samples (~7 s at 60 Hz)

        public Tele T = new Tele();
        public bool Unlocked;
        public double AbsDuty, TcDuty;           // 0..1 smoothed share of time the aid is cutting in
        public double LapsLeft, PerLap;          // computed by the model
        public bool ShowGraph = true, ShowBars = true, ShowClutch = true, ShowGear = true,
                    ShowWheel = true, ShowFuel = true, AidColors = true, InvertSteer;
        public Color CClutch, CBrake, CThrottle, CAbs, CTc, CShiftA, CShiftB;
        public ImageSource Wheel;

        readonly float[] hGas = new float[N], hBrk = new float[N], hClu = new float[N];
        readonly bool[] hAbs = new bool[N], hTc = new bool[N];
        int head;
        readonly FontFamily font = new FontFamily("Bahnschrift, Segoe UI");

        public HudView()
        {
            Width = W; Height = H; SnapsToDevicePixels = false;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);   // smooth wheel downscale/rotation
        }

        public void Push()
        {
            hGas[head] = T.Gas; hBrk[head] = T.Brake; hClu[head] = T.Clutch;
            hAbs[head] = T.AbsOn; hTc[head] = T.TcOn;
            head = (head + 1) % N;
        }

        static Brush Br(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        static Brush Br(byte a, byte r, byte g, byte b) { return Br(Color.FromArgb(a, r, g, b)); }
        static Color Mix(Color a, Color b, double k)
        {
            return Color.FromArgb(255, (byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k));
        }

        void Text(DrawingContext dc, string s, double size, Brush b, bool bold, double x, double y, int align)
        {
            var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, b, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            double px = align == 0 ? x : (align == 1 ? x - ft.Width / 2 : x - ft.Width);
            dc.DrawText(ft, new Point(px, y));
        }

        double TextWidth(string s, double size, bool bold)
        {
            var ft = new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(font, FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
                size, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            return ft.Width;
        }

        Geometry Line(float[] a, bool[] flag, double gx, double gy, double gw, double gh)
        {
            var g = new StreamGeometry();
            using (StreamGeometryContext c = g.Open())
            {
                bool open = false;
                for (int i = 0; i < N; i++)
                {
                    int k = (head + i) % N;
                    if (flag != null && !flag[k])
                    {
                        // keep one trailing point so a run joins the base line
                        if (open && i > 0 && flag[(head + i - 1) % N])
                            c.LineTo(new Point(gx + i * gw / (N - 1), gy + gh - a[k] * gh), true, false);
                        open = false; continue;
                    }
                    var p = new Point(gx + i * gw / (N - 1), gy + gh - a[k] * gh);
                    if (!open)
                    {
                        c.BeginFigure(p, false, false);
                        if (flag != null) c.LineTo(new Point(p.X + 0.5, p.Y), true, false);
                        open = true;
                    }
                    else c.LineTo(p, true, false);
                }
            }
            g.Freeze();
            return g;
        }

        void Pedal(DrawingContext dc, double x, double y, double h, double v, Color baseCol, bool active)
        {
            dc.DrawRoundedRectangle(Br(255, 30, 30, 38), null, new Rect(x, y, 16, h), 2, 2);
            Color c = (AidColors && active) ? (baseCol == CBrake ? CAbs : CTc) : baseCol;
            double fh = Math.Max(0, Math.Min(1, v)) * h;
            if (fh > 0) dc.DrawRoundedRectangle(Br(c), null, new Rect(x, y + h - fh, 16, fh), 2, 2);
        }

        protected override void OnRender(DrawingContext dc)
        {
            Brush white = Br(255, 245, 245, 245), gray = Br(255, 140, 140, 150);
            dc.DrawRoundedRectangle(Br(235, 14, 14, 18), Unlocked ? new Pen(Br(255, 255, 200, 0), 1.5) { DashStyle = DashStyles.Dash } : null,
                new Rect(0, 0, W, Height), 6, 6);
            dc.DrawRectangle(Br(255, 225, 6, 0), null, new Rect(0, 0, 3, Height));

            // --- input graph
            double gx = 12, gy = 12, gw = 300, gh = 104;
            if (ShowGraph)
            {
                dc.DrawRoundedRectangle(Br(10, 255, 255, 255), null, new Rect(gx, gy, gw, gh), 3, 3);
                var grid = new Pen(Br(22, 255, 255, 255), 1);
                for (int i = 0; i <= 4; i++) dc.DrawLine(grid, new Point(gx, gy + gh * i / 4), new Point(gx + gw, gy + gh * i / 4));
                if (ShowClutch) dc.DrawGeometry(null, new Pen(Br(CClutch), 1.6), Line(hClu, null, gx, gy, gw, gh));
                dc.DrawGeometry(null, new Pen(Br(CBrake), 1.8), Line(hBrk, null, gx, gy, gw, gh));
                dc.DrawGeometry(null, new Pen(Br(CThrottle), 1.8), Line(hGas, null, gx, gy, gw, gh));
                if (AidColors)
                {
                    dc.DrawGeometry(null, new Pen(Br(CAbs), 3.0), Line(hBrk, hAbs, gx, gy, gw, gh));
                    dc.DrawGeometry(null, new Pen(Br(CTc), 3.0), Line(hGas, hTc, gx, gy, gw, gh));
                }
                // ABS / TC chips: brightness follows how often the aid is cutting in
                // and show the car's ABS / TC / TCC levels in the same chips
                double cx0 = gx + gw - 4;
                cx0 -= DrawChip(dc, cx0, gy + 4, "TCC", T.TcCutLevel, CTc, TcDuty, T.TcOn, false) + 4;
                cx0 -= DrawChip(dc, cx0, gy + 4, "TC", T.TcLevel, CTc, TcDuty, T.TcOn, true) + 4;
                DrawChip(dc, cx0, gy + 4, "ABS", T.AbsLevel, CAbs, AbsDuty, T.AbsOn, true);
                if (T.Note.Length > 0) Text(dc, T.Note, 10, gray, true, gx + 6, gy + gh - 16, 0);
            }

            // --- pedal bars
            if (ShowBars)
            {
                double bx = 322, by = 12, bh = 104;
                if (ShowClutch) Pedal(dc, bx, by, bh, T.Clutch, CClutch, false);
                Pedal(dc, bx + 22, by, bh, T.Brake, CBrake, T.AbsOn);
                Pedal(dc, bx + 44, by, bh, T.Gas, CThrottle, T.TcOn);
            }

            // --- gear + speed + shift lights
            if (ShowGear)
            {
                double cx = 396 + 46;
                bool tall = Height >= 150;
                double r = T.MaxRpm > 0 ? (double)T.Rpm / T.MaxRpm : 0;
                // shift point: whole bar flashes, alternating the two shift colors
                bool flashA = (DateTime.UtcNow.Millisecond / 100) % 2 == 0;
                for (int i = 0; i < 10; i++)
                {
                    bool on = T.ShiftUp || r >= 0.72 + 0.028 * i;
                    Color lc = T.ShiftUp ? (flashA ? CShiftA : CShiftB)
                        : (i < 4 ? Color.FromRgb(40, 220, 90) : (i < 7 ? Color.FromRgb(255, 210, 0) : Color.FromRgb(255, 40, 30)));
                    dc.DrawRoundedRectangle(on ? Br(lc) : Br(255, 40, 40, 48), null, new Rect(396 + i * 9.4, 12, 7.4, 6), 1.5, 1.5);
                }
                string gs = T.Gear <= 0 ? "R" : (T.Gear == 1 ? "N" : (T.Gear - 1).ToString(CultureInfo.InvariantCulture));
                Brush gearBrush = (T.ShiftUp && flashA) ? Br(CShiftA) : ((T.Gear == 1) ? Br(255, 225, 6, 0) : white);
                double gearSize = tall ? 64 : 54, spSize = tall ? 46 : 32, gearY = tall ? 14 : 14, spY = Height - spSize * 1.2 - 6;
                Text(dc, gs, gearSize, gearBrush, true, cx, gearY, 1);
                string sp = Math.Max(0, (int)Math.Round(T.SpeedKmh)).ToString(CultureInfo.InvariantCulture);
                double w1 = TextWidth(sp, spSize, true), w2 = TextWidth(" km/h", 11, false);
                double sx = cx - (w1 + w2) / 2;
                Text(dc, sp, spSize, white, true, sx, spY, 0);
                Text(dc, " km/h", 11, gray, false, sx + w1, spY + spSize * 0.82 - 11, 0);
            }

            // --- steering wheel
            if (ShowWheel)
            {
                double s = Math.Min(136, Height - 16), wx = 496, wy = (Height - s) / 2, cx2 = wx + s / 2, cy2 = wy + s / 2;
                double deg = InvertSteer ? -T.SteerDeg : T.SteerDeg;
                dc.PushTransform(new RotateTransform(deg, cx2, cy2));
                if (Wheel != null) dc.DrawImage(Wheel, new Rect(wx, wy, s, s));
                else dc.DrawEllipse(null, new Pen(Br(255, 120, 120, 128), 9), new Point(cx2, cy2), 42, 42);
                dc.Pop();
            }

            // --- fuel strip: one line under the pedal graph and bars
            if (ShowFuel)
            {
                double sy = 124, sh = 28;
                dc.DrawRoundedRectangle(Br(10, 255, 255, 255), null, new Rect(12, sy, 370, sh), 3, 3);
                FuelItem(dc, 22, sy, "FUEL", T.Fuel > 0 ? T.Fuel.ToString("0.0", CultureInfo.InvariantCulture) : "--", "L");
                FuelItem(dc, 144, sy, "PER LAP", PerLap > 0 ? PerLap.ToString("0.00", CultureInfo.InvariantCulture) : "--", "L");
                FuelItem(dc, 270, sy, "LAPS", LapsLeft > 0 ? LapsLeft.ToString("0.0", CultureInfo.InvariantCulture) : "--", "");
            }

            if (Unlocked)
            {
                Rect banner = new Rect(W / 2 - 235, Height / 2 - 12, 470, 24);
                dc.DrawRoundedRectangle(Br(235, 255, 200, 0), null, banner, 4, 4);
                Text(dc, "UNLOCKED  ·  drag to move  ·  scroll or drag corner to resize  ·  Ctrl+Shift+L to lock", 10, Br(255, 10, 10, 10), true, W / 2, banner.Y + 6, 1);
                // resize grip (bottom-right corner)
                var grip = new StreamGeometry();
                using (StreamGeometryContext c = grip.Open())
                {
                    c.BeginFigure(new Point(W - 4, Height - 26), true, true);
                    c.LineTo(new Point(W - 4, Height - 4), true, false);
                    c.LineTo(new Point(W - 26, Height - 4), true, false);
                }
                grip.Freeze();
                dc.DrawGeometry(Br(255, 255, 200, 0), null, grip);
            }
        }

        void FuelItem(DrawingContext dc, double x, double y, string label, string val, string unit)
        {
            Brush gray = Br(255, 140, 140, 150);
            Text(dc, label, 9, gray, true, x, y + 10, 0);
            double vx = x + TextWidth(label, 9, true) + 6;
            Text(dc, val, 18, Br(255, 245, 245, 245), true, vx, y + 3, 0);
            if (unit.Length > 0) Text(dc, unit, 10, gray, false, vx + TextWidth(val, 18, true) + 2, y + 10, 0);
        }

        // Aid chip, right-aligned at xr: "ABS 4 · 12%". Level 0 shows OFF; brightness follows activity.
        // Returns the chip width so chips can be laid out right to left.
        double DrawChip(DrawingContext dc, double xr, double y, string label, int level, Color c, double duty, bool now, bool showDuty)
        {
            string lv = level < 0 ? "-" : (level == 0 ? "OFF" : level.ToString(CultureInfo.InvariantCulture));
            string txt = label + " " + lv;
            if (showDuty) txt += "  ·  " + ((int)Math.Round(duty * 100)).ToString(CultureInfo.InvariantCulture) + "%";
            double w = TextWidth(txt, 10, true) + 12;
            double k = now ? 1.0 : Math.Min(1, duty * 1.6);
            Color fill = Mix(Color.FromRgb(36, 36, 44), c, k);
            dc.DrawRoundedRectangle(Br(fill), null, new Rect(xr - w, y, w, 16), 3, 3);
            Text(dc, txt, 10, k > 0.55 ? Br(255, 10, 10, 10) : Br(255, 200, 200, 208), true, xr - w / 2, y + 1.5, 1);
            return w;
        }
    }

    // ------------------------------------------------------------------ app
    public class HudWindow : Window
    {
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
        const int GWL_EXSTYLE = -20, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;

        readonly Settings cfg;
        readonly HudView view = new HudView();
        readonly EvoSource evo = new EvoSource();
        readonly DemoSource demo = new DemoSource();
        readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
        WF.NotifyIcon tray;
        WF.ToolStripMenuItem miLock, miDemo, miAutoHide;
        bool locked = true, useDemo;
        // auto-hide outside of driving (menus, replays, pause, setup in the pit box, game not focused)
        bool autoHide, hideInPitBox, hideUnfocused, sawLiveStatus;
        string gameProcess;
        DateTime visChangeSince = DateTime.UtcNow;
        uint lastFgPid; bool lastFgIsGame;
        static readonly uint OwnPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        IntPtr hwnd;
        // size: 0.4x .. 3x of the base layout (644 x 162 px)
        const double MinScale = 0.4, MaxScale = 3.0;
        const string Ver = "1.1";
        double scale = 1.0;
        bool resizing;
        readonly List<WF.ToolStripMenuItem> miSizes = new List<WF.ToolStripMenuItem>();
        readonly List<int> sizePcts = new List<int>();
        // fuel-per-lap fallback
        int prevLapMs; float lapStartFuel; double measuredPerLap;

        public HudWindow(Settings s, string dir, bool startDemo)
        {
            cfg = s; useDemo = startDemo || s.Bool("Demo", false);
            WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
            Topmost = true; ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize; SizeToContent = SizeToContent.WidthAndHeight;
            Opacity = Math.Max(0.2, Math.Min(1, s.Num("Opacity", 0.94)));

            view.CClutch = s.Col("ColorClutch", "#0A30FF"); view.CBrake = s.Col("ColorBrake", "#FF1010");
            view.CThrottle = s.Col("ColorThrottle", "#33D68A");
            view.CAbs = s.Col("ColorAbs", "#FF8A00"); view.CTc = s.Col("ColorTc", "#FFE000");
            view.CShiftA = s.Col("ColorShiftA", "#FF1E1E"); view.CShiftB = s.Col("ColorShiftB", "#28282F");
            autoHide = s.Bool("AutoHide", true); hideInPitBox = s.Bool("HideInPitBox", true);
            hideUnfocused = s.Bool("HideWhenGameNotFocused", true); gameProcess = s.Str("GameProcess", "AssettoCorsaEVO");
            ShowActivated = false;
            view.ShowGraph = s.Bool("ShowGraph", true); view.ShowBars = s.Bool("ShowBars", true);
            view.ShowClutch = s.Bool("ShowClutch", true); view.ShowGear = s.Bool("ShowGear", true);
            view.ShowWheel = s.Bool("ShowWheel", true); view.ShowFuel = s.Bool("ShowFuel", true);
            view.AidColors = s.Bool("AidColors", true); view.InvertSteer = s.Bool("InvertSteer", false);
            view.Height = view.ShowFuel ? HudView.H : HudView.H_NOFUEL;
            view.Wheel = LoadWheel(Path.Combine(dir, s.Str("Wheel", "assets\\wheel.png")));
            double sc = Math.Max(MinScale, Math.Min(MaxScale, s.Num("Scale", 1.0)));
            scale = sc;
            view.LayoutTransform = new ScaleTransform(sc, sc);
            Content = view;

            double x = s.Num("X", double.NaN), y = s.Num("Y", double.NaN);
            WindowStartupLocation = WindowStartupLocation.Manual;
            if (double.IsNaN(x) || double.IsNaN(y))
            {
                x = (SystemParameters.PrimaryScreenWidth - HudView.W * sc) / 2;
                y = SystemParameters.PrimaryScreenHeight - view.Height * sc - 40;
            }
            Left = x; Top = y;

            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            MouseWheel += OnWheel;
            SourceInitialized += OnSourceInit;
            Closed += delegate
            {
                SaveState(); if (tray != null) { tray.Visible = false; tray.Dispose(); }
                if (hwnd != IntPtr.Zero) for (int id = 1; id <= 5; id++) UnregisterHotKey(hwnd, id);
            };

            BuildTray();
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += Tick;
            timer.Start();
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

        void OnSourceInit(object sender, EventArgs e)
        {
            hwnd = new WindowInteropHelper(this).Handle;
            ApplyClickThrough();
            if (!RegisterHotKey(hwnd, 1, 0x0002 | 0x0004, 0x4C)) // Ctrl+Shift+L
                tray.ShowBalloonTip(5000, "EVO HUD", "Ctrl+Shift+L is used by another app (is the other EVO-HUD open?). " +
                    "Double-click this tray icon to lock/unlock instead.", WF.ToolTipIcon.Warning);
            // Ctrl+Shift + / -  (main keys and numpad) to resize without unlocking
            RegisterHotKey(hwnd, 2, 0x0006, 0xBB); RegisterHotKey(hwnd, 3, 0x0006, 0xBD);
            RegisterHotKey(hwnd, 4, 0x0006, 0x6B); RegisterHotKey(hwnd, 5, 0x0006, 0x6D);
            HwndSource.FromHwnd(hwnd).AddHook(delegate (IntPtr h, int msg, IntPtr w, IntPtr l, ref bool handled)
            {
                if (msg == 0x0312)
                {
                    int id = w.ToInt32();
                    if (id == 1) { ToggleLock(); handled = true; }
                    else if (id == 2 || id == 4) { Step(1.1); handled = true; }
                    else if (id == 3 || id == 5) { Step(1 / 1.1); handled = true; }
                }
                return IntPtr.Zero;
            });
        }

        // ---- resizing -------------------------------------------------------------------
        // Anchor = HUD center (so it grows in place) unless the user is dragging the corner grip.
        void ApplyScale(double s, bool keepCenter)
        {
            s = Math.Max(MinScale, Math.Min(MaxScale, Math.Round(s, 3)));
            if (Math.Abs(s - scale) < 0.0005) return;
            double oldW = HudView.W * scale, oldH = view.Height * scale, newW = HudView.W * s, newH = view.Height * s;
            scale = s;
            view.LayoutTransform = new ScaleTransform(s, s);
            if (keepCenter) { Left -= (newW - oldW) / 2; Top -= (newH - oldH) / 2; }
            UpdateSizeMenu();
        }

        void Step(double factor) { ApplyScale(scale * factor, true); SaveState(); }

        void UpdateSizeMenu()
        {
            for (int i = 0; i < miSizes.Count; i++) miSizes[i].Checked = Math.Abs(scale * 100 - sizePcts[i]) < 1.0;
            if (tray != null) tray.Text = "EVO HUD " + Ver + "  -  size " + Math.Round(scale * 100) + "%";
        }

        bool OnGrip()
        {
            Point p = Mouse.GetPosition(view);
            return p.X > HudView.W - 34 && p.Y > view.Height - 34;
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (locked) return;
            if (OnGrip()) { resizing = true; CaptureMouse(); return; }
            try { DragMove(); } catch { }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (resizing) { ApplyScale(Mouse.GetPosition(this).X / HudView.W, false); return; }
            if (!locked) Cursor = OnGrip() ? Cursors.SizeNWSE : Cursors.SizeAll;
        }

        void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!resizing) return;
            resizing = false; ReleaseMouseCapture(); SaveState();
        }

        void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (locked) return;
            Step(e.Delta > 0 ? 1.05 : 1 / 1.05);
            e.Handled = true;
        }

        // Locked: click-through and never takes focus from the game.
        // Unlocked: normal window that can take focus, so the game's mouse capture doesn't block dragging.
        void ApplyClickThrough()
        {
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TOOLWINDOW;
            ex = locked ? (ex | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE) : (ex & ~(WS_EX_TRANSPARENT | WS_EX_NOACTIVATE));
            SetWindowLong(hwnd, GWL_EXSTYLE, ex);
        }

        void ToggleLock()
        {
            locked = !locked;
            view.Unlocked = !locked;
            ApplyClickThrough();
            if (locked) SaveState();
            else { Activate(); Topmost = false; Topmost = true; }
            miLock.Text = locked ? "Unlock position (Ctrl+Shift+L)" : "Lock position (Ctrl+Shift+L)";
            if (locked) { resizing = false; Cursor = Cursors.Arrow; }
            tray.ShowBalloonTip(2500, "EVO HUD", locked ? "Locked. Position and size saved." :
                "Unlocked: drag to move, scroll or drag the bottom-right corner to resize. Ctrl+Shift+L (or double-click the tray icon) to lock.",
                WF.ToolTipIcon.None);
            view.InvalidateVisual();
        }

        void ResetPosition()
        {
            Left = (SystemParameters.PrimaryScreenWidth - HudView.W * scale) / 2;
            Top = SystemParameters.PrimaryScreenHeight - view.Height * scale - 40;
            SaveState();
        }

        void SaveState()
        {
            cfg.Set("X", Left.ToString("0", CultureInfo.InvariantCulture));
            cfg.Set("Y", Top.ToString("0", CultureInfo.InvariantCulture));
            cfg.Set("Scale", scale.ToString("0.###", CultureInfo.InvariantCulture));
            cfg.Set("Demo", useDemo ? "1" : "0");
            try { cfg.Save(); } catch { }
        }

        void BuildTray()
        {
            tray = new WF.NotifyIcon();
            tray.Text = "EVO HUD";
            try
            {
                var bmp = new SD.Bitmap(32, 32);
                using (var g = SD.Graphics.FromImage(bmp))
                {
                    g.Clear(SD.Color.FromArgb(14, 14, 18));
                    g.FillRectangle(new SD.SolidBrush(SD.Color.FromArgb(225, 6, 0)), 0, 0, 6, 32);
                    g.DrawString("E", new SD.Font("Segoe UI", 16, SD.FontStyle.Bold), SD.Brushes.White, 8, 3);
                }
                tray.Icon = SD.Icon.FromHandle(bmp.GetHicon());
            }
            catch { tray.Icon = SD.SystemIcons.Application; }
            var menu = new WF.ContextMenuStrip();
            miLock = new WF.ToolStripMenuItem("Unlock position (Ctrl+Shift+L)", null, delegate { ToggleLock(); });
            miDemo = new WF.ToolStripMenuItem("Demo mode", null, delegate { useDemo = !useDemo; miDemo.Checked = useDemo; });
            miDemo.Checked = useDemo;
            menu.Items.Add(miLock);
            var size = new WF.ToolStripMenuItem("Size");
            foreach (int pct in new int[] { 50, 75, 100, 125, 150, 200, 250 })
            {
                int p = pct;
                var mi = new WF.ToolStripMenuItem(p + "%", null, delegate { ApplyScale(p / 100.0, true); SaveState(); });
                size.DropDownItems.Add(mi); miSizes.Add(mi); sizePcts.Add(p);
            }
            size.DropDownItems.Add(new WF.ToolStripSeparator());
            size.DropDownItems.Add(new WF.ToolStripMenuItem("Bigger (Ctrl+Shift++)", null, delegate { Step(1.1); }));
            size.DropDownItems.Add(new WF.ToolStripMenuItem("Smaller (Ctrl+Shift+-)", null, delegate { Step(1 / 1.1); }));
            menu.Items.Add(size);
            menu.Items.Add(new WF.ToolStripMenuItem("Reset position", null, delegate { ResetPosition(); }));
            menu.Items.Add(miDemo);
            miAutoHide = new WF.ToolStripMenuItem("Hide when not driving", null, delegate
            {
                autoHide = !autoHide; miAutoHide.Checked = autoHide;
                cfg.Set("AutoHide", autoHide ? "1" : "0"); SaveState();
            });
            miAutoHide.Checked = autoHide;
            menu.Items.Add(miAutoHide);
            menu.Items.Add(new WF.ToolStripMenuItem("Quit", null, delegate { Close(); }));
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ToggleLock(); };
            tray.Visible = true;
            UpdateSizeMenu();
        }

        void Tick(object sender, EventArgs e)
        {
            Tele t = view.T;
            if (useDemo) demo.Read(t);
            else
            {
                bool ok = evo.Read(t);
                if (!ok || !t.Live)
                {
                    t.Gas = t.Brake = t.Clutch = 0; t.AbsOn = t.TcOn = t.ShiftUp = false;
                }
            }
            UpdateFuel(t);
            view.AbsDuty += ((t.AbsOn ? 1.0 : 0.0) - view.AbsDuty) * 0.04;
            view.TcDuty += ((t.TcOn ? 1.0 : 0.0) - view.TcDuty) * 0.04;
            view.Push();
            UpdateVisibility(t);
            if (Visibility == Visibility.Visible) view.InvalidateVisual();
        }

        bool ShouldShow(Tele t)
        {
            if (!locked || useDemo || !autoHide) return true;
            if (!t.Live) return false;                          // game closed, loading or frozen
            if (t.Status == 2) sawLiveStatus = true;
            if (t.Status == 1 || t.Status == 3) return false;   // replay / pause
            if (t.Status == 0 && sawLiveStatus) return false;   // back to menus (only trusted once we've seen "live")
            if (hideInPitBox && t.InPitBox && t.SpeedKmh < 2) return false;   // setup screen / garage
            if (hideUnfocused && !GameFocused()) return false;
            return true;
        }

        bool GameFocused()
        {
            uint pid;
            GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            if (pid == OwnPid) return true;
            if (pid != lastFgPid)
            {
                lastFgPid = pid;
                try { lastFgIsGame = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName.Equals(gameProcess, StringComparison.OrdinalIgnoreCase); }
                catch { lastFgIsGame = false; }
            }
            return lastFgIsGame;
        }

        // Debounced so the HUD doesn't flicker on short transitions.
        void UpdateVisibility(Tele t)
        {
            bool want = ShouldShow(t);
            bool shown = Visibility == Visibility.Visible;
            if (want == shown) { visChangeSince = DateTime.UtcNow; return; }
            if ((DateTime.UtcNow - visChangeSince).TotalMilliseconds < (want ? 150 : 500)) return;
            Visibility = want ? Visibility.Visible : Visibility.Hidden;
            visChangeSince = DateTime.UtcNow;
        }

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
            view.PerLap = per;
            view.LapsLeft = (per > 0.01 && t.Fuel > 0) ? t.Fuel / per : 0;
        }
    }

    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (Array.IndexOf(args, "--probe") >= 0) return Probe(dir);
            bool demo = Array.IndexOf(args, "--demo") >= 0;
            var cfg = new Settings(Path.Combine(dir, "settings.ini"));
            var app = new Application();
            var win = new HudWindow(cfg, dir, demo);
            win.Show();
            return app.Run();
        }

        // Writes probe.log for ~40 s: raw values from the game so offsets/units can be verified.
        static int Probe(string dir)
        {
            var src = new EvoSource(); var t = new Tele();
            using (var w = new StreamWriter(Path.Combine(dir, "probe.log"), false))
            {
                var end = DateTime.UtcNow.AddSeconds(40);
                while (DateTime.UtcNow < end)
                {
                    bool ok = src.Read(t);
                    w.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + (ok ? src.Probe() : "NOT CONNECTED: " + src.Error));
                    w.Flush();
                    System.Threading.Thread.Sleep(250);
                }
            }
            return 0;
        }
    }
}
