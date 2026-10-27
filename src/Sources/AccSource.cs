// Assetto Corsa Competizione: Local\acpmf_physics / Local\acpmf_graphics / Local\acpmf_static (ACC shared memory SDK).
// The physics page has the same layout as AC EVO's. The original Assetto Corsa uses the same map names with
// a different graphics layout, so this source only opens them while ACC itself is running.
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimracingBR
{
    // Prefix of SPageFileGraphic (pack = 4, wchar_t = 2 bytes). Only used to compute offsets at runtime.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AccGfx
    {
        public int packetId, status, session;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] currentTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] lastTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] bestTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] split;
        public int completedLaps, position, iCurrentTime, iLastTime, iBestTime;
        public float sessionTimeLeft, distanceTraveled;
        public int isInPit, currentSectorIndex, lastSectorTime, numberOfLaps;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] tyreCompound;
        public float replayTimeMultiplier, normalizedCarPosition;
        public int activeCars;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 180)] public float[] carCoordinates;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)] public int[] carID;
        public int playerCarID;
        public float penaltyTime;
        public int flag, penalty, idealLineOn, isInPitLane;
        public float surfaceGrip;
        public int mandatoryPitDone;
        public float windSpeed, windDirection;
        public int isSetupMenuVisible, mainDisplayIndex, secondaryDisplayIndex;
        public int TC, TCCut, EngineMap, ABS;
        public float fuelXLap;
        public int rainLights, flashingLights, lightsStage;
        public float exhaustTemperature;
        public int wiperLV, DriverStintTotalTimeLeft, DriverStintTimeLeft, rainTyres, sessionIndex;
        public float usedFuel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] deltaLapTime;
        public int iDeltaLapTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] estimatedLapTime;
        public int iEstimatedLapTime, isDeltaPositive, iSplit, isValidLap;
        public float fuelEstimatedLaps;
    }

    // Prefix of SPageFileStatic.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AccStatic
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] smVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] acVersion;
        public int numberOfSessions, numCars;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] carModel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] track;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] playerName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] playerSurname;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 66)] public byte[] playerNick;
        public int sectorCount;
        public float maxTorque, maxPower;
        public int maxRpm;
        public float maxFuel;
    }

    public class AccSource : ShmSource
    {
        static int G(string f) { return (int)Marshal.OffsetOf(typeof(AccGfx), f); }
        static int S(string f) { return (int)Marshal.OffsetOf(typeof(AccStatic), f); }
        static readonly int G_STATUS = G("status"), G_LAPMS = G("iCurrentTime"), G_INPIT = G("isInPit"), G_PITLANE = G("isInPitLane"),
            G_SETUP = G("isSetupMenuVisible"), G_TC = G("TC"), G_TCCUT = G("TCCut"), G_ABS = G("ABS"),
            G_FUELLAP = G("fuelXLap"), G_LAPSLEFT = G("fuelEstimatedLaps"), G_LAPS = G("completedLaps"), G_POS = G("position"),
            G_DRIVERS = G("activeCars"), G_SESSIONTYPE = G("session"), G_TIMELEFT = G("sessionTimeLeft"), G_NUMLAPS = G("numberOfLaps"), G_MAP = G("EngineMap"), G_NPOS = G("normalizedCarPosition"), G_SECTOR = G("currentSectorIndex"), G_LAST = G("iLastTime"), G_BEST = G("iBestTime"), G_VALID = G("isValidLap");
        static readonly int GFX_LEN = G("fuelEstimatedLaps") + 4;
        static readonly int S_MAXRPM = S("maxRpm"), S_MAXFUEL = S("maxFuel"), S_SMVER = S("smVersion"), S_TRACK = S("track"), S_CAR = S("carModel");
        static readonly int STATIC_LEN = S("maxFuel") + 4;

        const int ACC_BRAKEBIAS = 564;   // SPageFilePhysics.brakeBias
        readonly double steerLock;       // ACC only gives steering as -1..1; full lock-to-lock rotation in degrees
        DateTime lastChange = DateTime.MinValue;
        int lastPacket = -1;

        public AccSource(Settings cfg)
            : base(new[] { "Local\\acpmf_physics", "Local\\acpmf_graphics", "Local\\acpmf_static" }, new[] { AcPhys.LEN, GFX_LEN, STATIC_LEN })
        {
            steerLock = cfg.Num("AccSteerLock", 540);
        }

        public override string Name { get { return "ACC"; } }
        public override string[] Processes { get { return new[] { "AC2-Win64-Shipping" }; } }
        protected override bool RequireProcess { get { return true; } }

        public override bool Read(Tele t)
        {
            if (!Fetch()) { t.Live = false; t.Note = "WAITING FOR ACC"; return false; }
            byte[] pb = Pages[0], gb = Pages[1], sb = Pages[2];

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
            if (t.MaxRpm <= 0) t.MaxRpm = BitConverter.ToInt32(sb, S_MAXRPM);
            t.SpeedKmh = BitConverter.ToSingle(pb, AcPhys.SPEED);
            t.SteerDeg = (float)(BitConverter.ToSingle(pb, AcPhys.STEER) * steerLock / 2);
            t.AbsOn = BitConverter.ToInt32(pb, AcPhys.ABSACT) != 0;
            t.TcOn = BitConverter.ToInt32(pb, AcPhys.TCACT) != 0;
            t.Fuel = BitConverter.ToSingle(pb, AcPhys.FUEL);
            t.FuelPerLap = BitConverter.ToSingle(gb, G_FUELLAP);
            t.LapMs = BitConverter.ToInt32(gb, G_LAPMS);
            t.ShiftUp = t.MaxRpm > 0 && t.Rpm >= t.MaxRpm * 0.97;
            t.TcLevel = BitConverter.ToInt32(gb, G_TC); t.TcCutLevel = BitConverter.ToInt32(gb, G_TCCUT);
            t.AbsLevel = BitConverter.ToInt32(gb, G_ABS);
            t.HasTcCut = true;
            t.Status = BitConverter.ToInt32(gb, G_STATUS);
            t.InPitBox = BitConverter.ToInt32(gb, G_INPIT) != 0 || BitConverter.ToInt32(gb, G_SETUP) != 0;
            t.InSetup = BitConverter.ToInt32(gb, G_SETUP) != 0;
            float np = BitConverter.ToSingle(gb, G_NPOS);
            t.TrackPos = np >= 0 && np <= 1 ? np : -1;
            t.Sector = BitConverter.ToInt32(gb, G_SECTOR);
            t.LapValid = BitConverter.ToInt32(gb, G_VALID) != 0;
            t.InPitLane = BitConverter.ToInt32(gb, G_PITLANE) != 0;
            t.LastLapMs = LapOrZero(BitConverter.ToInt32(gb, G_LAST));
            t.BestLapMs = LapOrZero(BitConverter.ToInt32(gb, G_BEST));
            t.HasLeader = false; Array.Clear(t.SessionBestSecMs, 0, 3);
            t.Drivers = BitConverter.ToInt32(gb, G_DRIVERS);
            t.TrackId = Str(sb, S_TRACK, 66, true) + "|" + Str(sb, S_CAR, 66, true);
            t.ClearExtras();
            FillTyres(t, pb);
            t.LapCount = BitConverter.ToInt32(gb, G_LAPS);
            t.Position = Pos(BitConverter.ToInt32(gb, G_POS), t.Drivers);
            float maxFuel = BitConverter.ToSingle(sb, S_MAXFUEL);
            t.FuelCap = maxFuel > 0 && maxFuel < 1000 ? maxFuel : 0;
            t.SessionIsRace = BitConverter.ToInt32(gb, G_SESSIONTYPE) == 2;
            float left = BitConverter.ToSingle(gb, G_TIMELEFT);
            t.SessionTimeLeftMs = left > 0 && left < 3.6e8f ? (int)left : -1;
            int nl = BitConverter.ToInt32(gb, G_NUMLAPS);
            t.SessionLaps = nl > 0 && nl < 1000 ? nl : 0;
            t.EngineMap = BitConverter.ToInt32(gb, G_MAP);
            float abb = BitConverter.ToSingle(pb, ACC_BRAKEBIAS);
            t.BrakeBias = abb > 0.2f && abb < 0.95f ? abb : -1;
            if (float.IsNaN(t.Fuel) || t.Fuel < 0 || t.Fuel > 1000) t.Fuel = 0;
            if (float.IsNaN(t.FuelPerLap) || t.FuelPerLap < 0 || t.FuelPerLap > 100) t.FuelPerLap = 0;
            return true;
        }

        public override string Probe()
        {
            byte[] pb = Pages[0], gb = Pages[1], sb = Pages[2];
            return string.Format(CultureInfo.InvariantCulture,
                "smVer={0} pktP={1} gas={2:0.00} brk={3:0.00} clu={4:0.00} gear={5} rpm={6} maxRpmP={7} maxRpmS={8} spd={9:0.0} " +
                "steer={10:0.000} tcAct={11} absAct={12} tcF={13:0.000} absF={14:0.000} fuel={15:0.00} maxFuel={16:0.0} " +
                "fuelXLap={17:0.000} lapsLeft={18:0.0} lapMs={19} laps={20} tc={21} tcCut={22} abs={23} " +
                "status={24} inPit={25} pitLane={26} setup={27} npos={28:0.0000} sector={29} lastLap={30} bestLap={31} valid={32}",
                System.Text.Encoding.Unicode.GetString(sb, S_SMVER, 30).TrimEnd('\0'),
                BitConverter.ToInt32(pb, 0), BitConverter.ToSingle(pb, AcPhys.GAS), BitConverter.ToSingle(pb, AcPhys.BRAKE),
                BitConverter.ToSingle(pb, AcPhys.CLUTCH), BitConverter.ToInt32(pb, AcPhys.GEAR), BitConverter.ToInt32(pb, AcPhys.RPM),
                BitConverter.ToInt32(pb, AcPhys.MAXRPM), BitConverter.ToInt32(sb, S_MAXRPM), BitConverter.ToSingle(pb, AcPhys.SPEED),
                BitConverter.ToSingle(pb, AcPhys.STEER), BitConverter.ToInt32(pb, AcPhys.TCACT), BitConverter.ToInt32(pb, AcPhys.ABSACT),
                BitConverter.ToSingle(pb, AcPhys.TC), BitConverter.ToSingle(pb, AcPhys.ABS),
                BitConverter.ToSingle(pb, AcPhys.FUEL), BitConverter.ToSingle(sb, S_MAXFUEL),
                BitConverter.ToSingle(gb, G_FUELLAP), BitConverter.ToSingle(gb, G_LAPSLEFT), BitConverter.ToInt32(gb, G_LAPMS),
                BitConverter.ToInt32(gb, G_LAPS), BitConverter.ToInt32(gb, G_TC), BitConverter.ToInt32(gb, G_TCCUT), BitConverter.ToInt32(gb, G_ABS),
                BitConverter.ToInt32(gb, G_STATUS), BitConverter.ToInt32(gb, G_INPIT), BitConverter.ToInt32(gb, G_PITLANE),
                BitConverter.ToInt32(gb, G_SETUP), BitConverter.ToSingle(gb, G_NPOS), BitConverter.ToInt32(gb, G_SECTOR),
                BitConverter.ToInt32(gb, G_LAST), BitConverter.ToInt32(gb, G_BEST), BitConverter.ToInt32(gb, G_VALID));
        }
    }
}
