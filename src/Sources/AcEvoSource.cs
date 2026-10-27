// Assetto Corsa EVO: Local\acevo_pmf_physics / Local\acevo_pmf_graphics.
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimracingBR
{
    // Prefix of the AC EVO graphics page (pack = 4). Only used to compute offsets at runtime.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EvoGfx
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

    // Physics page shared by AC EVO and ACC (same layout, pack = 4).
    public static class AcPhys
    {
        public const int PACKET = 0, GAS = 4, BRAKE = 8, FUEL = 12, GEAR = 16, RPM = 20, STEER = 24, SPEED = 28,
                         TC = 204, ABS = 252, AUTOSHIFT = 264, CLUTCH = 364, MAXRPM = 588, TCACT = 672, ABSACT = 676,
                         PRESSURE = 88, CORETEMP = 152;   // wheelsPressure[4] (psi), tyreCoreTemperature[4] (C)
        public const int LEN = 800;
    }

    public class AcEvoSource : ShmSource
    {
        static int G(string f) { return (int)Marshal.OffsetOf(typeof(EvoGfx), f); }
        static readonly int G_STEERDEG = G("steerDegrees"), G_STEERPCT = G("steeringPercent"), G_GEAR = G("gearInt"),
            G_TCACT = G("tcActive"), G_ABSACT = G("absActive"), G_FUEL = G("fuelLiterCurrentQuantity"),
            G_FUELLAP = G("fuelLiterPerLap"), G_LAPMS = G("currentLapTimeMs"), G_LAPSPOSS = G("lapsPossibleWithFuel"),
            G_RPM = G("rpm"), G_ELEC = G("electronics"), G_SHIFTUP = G("isChangeUp"),
            G_CLUTCHPCT = G("clutchPercent"), G_STATUS = G("status"), G_PITBOX = G("isInPitBox"), G_PITLANE = G("isInPitLane"), G_LOCATION = G("carLocation"),
            G_DRIVERS = G("totalDrivers"), G_POS = G("currentPos"), G_LAPCOUNT = G("totalLapCount"), G_FUELPCT = G("fuelLiterCurrentQuantityPercent"), G_SESSION = G("sessionState"),
            G_ELECMAX = G("electronicsMax"), G_DIFFP = G("diffPowerRaw"), G_DIFFC = G("diffCoastRaw"), G_KERS = G("kersCurrentPerc"), G_HASKERS = G("hasKers"), G_DRSAVAIL = G("isDrsAvailable"), G_NPOS = G("npos"), G_LASTLAP = G("lastLaptimeMs"), G_BESTLAP = G("bestLaptimeMs"), G_VALID = G("isValidLap"), G_CAR = G("carModel");
        static readonly int GFX_LEN = G("isValidLap") + 1;

        readonly EvoLog log = new EvoLog(null);       // session lengths of a solo event (see EvoLog.cs)
        readonly EvoSeason season = new EvoSeason(null);   // lengths of the sessions of the event / server being played (see EvoSeason.cs)
        DateTime lastChange = DateTime.MinValue;
        int lastPacket = -1;

        // the static page (4 KB) has the track at 136 and its layout at 169 (found with --dump); only used to tell tracks apart
        const int STATIC_LEN = 4096, S_TRACK = 136, S_LAYOUT = 169;
        public AcEvoSource() : base(new[] { "Local\\acevo_pmf_physics", "Local\\acevo_pmf_graphics", "Local\\acevo_pmf_static" }, new[] { AcPhys.LEN, GFX_LEN, STATIC_LEN }) { }

        public override string Name { get { return "AC EVO"; } }
        public override string[] Processes { get { return new[] { "AssettoCorsaEVO" }; } }

        public override bool Read(Tele t)
        {
            if (!Fetch()) { t.Live = false; t.Note = "WAITING FOR AC EVO"; return false; }
            byte[] pb = Pages[0], gb = Pages[1];

            int packet = BitConverter.ToInt32(pb, AcPhys.PACKET);
            if (packet != lastPacket) { lastPacket = packet; lastChange = DateTime.UtcNow; }
            t.Live = (DateTime.UtcNow - lastChange).TotalSeconds < 2.0;
            t.Note = t.Live ? "" : "NO LIVE DATA";

            t.Gas = Clamp01(BitConverter.ToSingle(pb, AcPhys.GAS));
            t.Brake = Clamp01(BitConverter.ToSingle(pb, AcPhys.BRAKE));
            t.Clutch = Clamp01(BitConverter.ToSingle(pb, AcPhys.CLUTCH));
            t.Gear = BitConverter.ToInt32(pb, AcPhys.GEAR);
            t.Rpm = BitConverter.ToInt32(pb, AcPhys.RPM);
            t.MaxRpm = BitConverter.ToInt32(pb, AcPhys.MAXRPM);
            t.SpeedKmh = BitConverter.ToSingle(pb, AcPhys.SPEED);
            t.AbsOn = BitConverter.ToInt32(pb, AcPhys.ABSACT) != 0 || gb[G_ABSACT] != 0;
            t.TcOn = BitConverter.ToInt32(pb, AcPhys.TCACT) != 0 || gb[G_TCACT] != 0;
            t.SteerDeg = BitConverter.ToInt32(gb, G_STEERDEG);
            t.Fuel = BitConverter.ToSingle(gb, G_FUEL);
            t.FuelPerLap = BitConverter.ToSingle(gb, G_FUELLAP);
            t.LapMs = BitConverter.ToInt32(gb, G_LAPMS);
            // the game's own shift indicator; fall back to rpm if it never fires
            t.ShiftUp = gb[G_SHIFTUP] != 0 || (t.MaxRpm > 0 && t.Rpm >= t.MaxRpm * 0.97);
            t.TcLevel = (sbyte)gb[G_ELEC]; t.TcCutLevel = (sbyte)gb[G_ELEC + 1]; t.AbsLevel = (sbyte)gb[G_ELEC + 2];
            t.HasTcCut = true;
            t.Status = BitConverter.ToInt32(gb, G_STATUS);
            t.InPitBox = gb[G_PITBOX] != 0;
            t.InSetup = t.Rpm == 0 && t.SpeedKmh < 1 && (t.Status == 2 || t.Status == 3);   // seen with --probe: the setup screen has the engine off, the garage menu has it idling; also covers the starting grid
            float np = BitConverter.ToSingle(gb, G_NPOS);
            t.TrackPos = np >= 0 && np <= 1 ? np : -1;
            t.Sector = -1;                                   // not published
            t.LapValid = gb[G_VALID] != 0;
            t.InPitLane = gb[G_PITLANE] != 0;
            t.LastLapMs = LapOrZero(BitConverter.ToInt32(gb, G_LASTLAP));
            t.BestLapMs = LapOrZero(BitConverter.ToInt32(gb, G_BESTLAP));
            t.HasLeader = false; Array.Clear(t.SessionBestSecMs, 0, 3);
            t.Drivers = (int)BitConverter.ToUInt32(gb, G_DRIVERS);
            t.TrackId = Str(Pages[2], S_TRACK, 32, false) + "/" + Str(Pages[2], S_LAYOUT, 32, false) + "|" + Str(gb, G_CAR, 33, false);
            // session / fuel / electronics for the fuel and electronics overlays
            t.ClearExtras();
            FillTyres(t, pb);
            t.LapCount = BitConverter.ToInt32(gb, G_LAPCOUNT);
            t.Position = Pos((int)BitConverter.ToUInt32(gb, G_POS), t.Drivers);
            float fpct = BitConverter.ToSingle(gb, G_FUELPCT);
            t.FuelCap = (fpct > 0.01f && fpct <= 1.001f && t.Fuel > 0) ? t.Fuel / fpct : 0;
            t.SessionIsRace = Str(Pages[2], 36, 24, false) == "Race";     // solo only: online it keeps the name of the session joined (seen "Qualifying" all race)
            t.SessionTimeLeftMs = ParseClockMs(Str(gb, G_SESSION + 33, 20, false));
            t.SessionLaps = 0;                                       // not published
            // the event definition (online servers too) first, the log of a solo event otherwise: known in practice and qualifying
            season.Poll(); log.Poll();
            bool fromSeason = season.RaceSeconds > 0 || season.RaceLaps > 0;
            t.RaceLengthSec = fromSeason ? season.RaceSeconds : log.RaceSeconds; t.RaceLengthLaps = fromSeason ? season.RaceLaps : log.RaceLaps;
            t.TcMax = (sbyte)gb[G_ELECMAX]; t.TcCutMax = (sbyte)gb[G_ELECMAX + 1]; t.AbsMax = (sbyte)gb[G_ELECMAX + 2];
            t.EscLevel = (sbyte)gb[G_ELEC + 3]; t.EscMax = (sbyte)gb[G_ELECMAX + 3];
            t.EngineMap = -1; t.EngineMapMax = -1;
            float bb = BitConverter.ToSingle(gb, G_ELEC + 8);
            t.BrakeBias = bb > 0.2f && bb < 0.95f ? bb : -1;
            float dp = BitConverter.ToSingle(gb, G_DIFFP), dc = BitConverter.ToSingle(gb, G_DIFFC);
            bool diff = (dp > 0 || dc > 0) && dp <= 1.001f && dc <= 1.001f;
            t.DiffPower = diff ? dp : -1; t.DiffCoast = diff ? dc : -1;
            float kers = BitConverter.ToSingle(gb, G_KERS);
            t.BatteryPct = gb[G_HASKERS] != 0 ? (kers <= 1.001f ? kers * 100 : kers) : -1;
            t.EnergyPct = -1;
            t.Drs = gb[G_DRSAVAIL] != 0 ? 1 : -1;
            if (float.IsNaN(t.Fuel) || t.Fuel < 0 || t.Fuel > 1000) t.Fuel = 0;
            if (float.IsNaN(t.FuelPerLap) || t.FuelPerLap < 0 || t.FuelPerLap > 100) t.FuelPerLap = 0;
            return true;
        }

        public override string Probe()
        {
            byte[] pb = Pages[0], gb = Pages[1];
            return string.Format(CultureInfo.InvariantCulture,
                "pktP={0} gas={1:0.00} brk={2:0.00} clu={3:0.00} gearP={4} gearG={5} rpmP={6} rpmG={7} maxRpm={8} spd={9:0.0} " +
                "steerRaw={10:0.000} steerDegG={11} steerPctG={12:0.000} tcF={13:0.000} absF={14:0.000} tcAct={15} absAct={16} " +
                "tcG={17} absG={18} fuel={19:0.00} fuelLapG={20:0.000} lapsPoss={21:0.0} lapMs={22} physFuel={23:0.00} " +
                "elec=[tc {24} tcCut {25} abs {26} esc {27} ebb {28} bb {29:0.000}] " +
                "npos={37:0.0000} lastLap={38} bestLap={39} valid={40} status={30} pitBox={31} pitLane={32} location={33} shiftUp={34} cluG={35:0.000} autoShift={36} " +
                "staticSession='{41}' currentPos={42}",
                BitConverter.ToInt32(pb, 0), BitConverter.ToSingle(pb, AcPhys.GAS), BitConverter.ToSingle(pb, AcPhys.BRAKE),
                BitConverter.ToSingle(pb, AcPhys.CLUTCH), BitConverter.ToInt32(pb, AcPhys.GEAR), BitConverter.ToInt16(gb, G_GEAR),
                BitConverter.ToInt32(pb, AcPhys.RPM), BitConverter.ToUInt16(gb, G_RPM), BitConverter.ToInt32(pb, AcPhys.MAXRPM),
                BitConverter.ToSingle(pb, AcPhys.SPEED), BitConverter.ToSingle(pb, AcPhys.STEER),
                BitConverter.ToInt32(gb, G_STEERDEG), BitConverter.ToSingle(gb, G_STEERPCT),
                BitConverter.ToSingle(pb, AcPhys.TC), BitConverter.ToSingle(pb, AcPhys.ABS),
                BitConverter.ToInt32(pb, AcPhys.TCACT), BitConverter.ToInt32(pb, AcPhys.ABSACT), gb[G_TCACT], gb[G_ABSACT],
                BitConverter.ToSingle(gb, G_FUEL), BitConverter.ToSingle(gb, G_FUELLAP),
                BitConverter.ToSingle(gb, G_LAPSPOSS), BitConverter.ToInt32(gb, G_LAPMS),
                BitConverter.ToSingle(pb, AcPhys.FUEL),
                (sbyte)gb[G_ELEC], (sbyte)gb[G_ELEC + 1], (sbyte)gb[G_ELEC + 2], (sbyte)gb[G_ELEC + 3], (sbyte)gb[G_ELEC + 4],
                BitConverter.ToSingle(gb, G_ELEC + 8),
                BitConverter.ToInt32(gb, G_STATUS), gb[G_PITBOX], gb[G_PITLANE], BitConverter.ToInt32(gb, G_LOCATION), gb[G_SHIFTUP],
                BitConverter.ToSingle(gb, G_CLUTCHPCT), BitConverter.ToInt32(pb, AcPhys.AUTOSHIFT),
                BitConverter.ToSingle(gb, G_NPOS), BitConverter.ToInt32(gb, G_LASTLAP), BitConverter.ToInt32(gb, G_BESTLAP), gb[G_VALID],
                Str(Pages[2], 36, 24, false), BitConverter.ToUInt32(gb, G_POS));      // online the static session name stays the one joined
        }
    }
}
