// Automobilista 2: "$pcars2$" (Project CARS 2 format). Needs Options > System > Shared Memory = "Project CARS 2".
// Layout from Support\SharedMemory\...\SharedMemory.h shipped with the game (SHARED_MEMORY_VERSION 14).
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimracingBR
{
    // Mirror of the whole SharedMemory struct (natural alignment, no member wider than 4 bytes; C++ bool = 1 byte).
    // Groups of same-typed fields are folded into one array where only the offsets after them matter.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Ams2Shm
    {
        public uint mVersion, mBuildVersionNumber, mGameState, mSessionState, mRaceState;
        public int mViewedParticipantIndex, mNumParticipants;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 100)] public byte[] mParticipantInfo;   // ParticipantInfo = 100 bytes
        public float mUnfilteredThrottle, mUnfilteredBrake, mUnfilteredSteering, mUnfilteredClutch;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mCarName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mCarClassName;
        public uint mLapsInEvent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTrackLocation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTrackVariation;
        public float mTrackLength;
        public int mNumSectors;
        public byte mLapInvalidated;
        public float mBestLapTime, mLastLapTime, mCurrentTime, mSplitTimeAhead, mSplitTimeBehind, mSplitTime,
                     mEventTimeRemaining, mPersonalFastestLapTime, mWorldFastestLapTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)] public float[] mSectorTimes;   // current/fastest/personal/world x 3
        public uint mHighestFlagColour, mHighestFlagReason, mPitMode, mPitSchedule, mCarFlags;
        public float mOilTempCelsius, mOilPressureKPa, mWaterTempCelsius, mWaterPressureKPa, mFuelPressureKPa,
                     mFuelLevel, mFuelCapacity, mSpeed, mRpm, mMaxRPM, mBrake, mThrottle, mClutch, mSteering;
        public int mGear, mNumGears;
        public float mOdometerKM;
        public byte mAntiLockActive;
        public int mLastOpponentCollisionIndex;
        public float mLastOpponentCollisionMagnitude;
        public byte mBoostActive;
        public float mBoostAmount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)] public float[] mMotion;        // 7 vectors (orientation .. extents centre)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public uint[] mTyreFlagsTerrain; // mTyreFlags[4], mTerrain[4]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public float[] mTyres;          // 16 float[4] arrays (mTyreY .. mTyreInternalAirTemp)
        public uint mCrashState;
        public float mAeroDamage, mEngineDamage;
        public float mAmbientTemperature, mTrackTemperature, mRainDensity, mWindSpeed, mWindDirectionX, mWindDirectionY, mCloudBrightness;
        public uint mSequenceNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public float[] mWheels;         // wheel pos Y, susp travel, susp velocity, air pressure
        public float mEngineSpeed, mEngineTorque;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public float[] mWings;
        public float mHandBrake;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8 * 64)] public float[] mParticipantTimes; // sector / fastest / last lap times
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mLapsInvalidated;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public uint[] mRaceStates;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public uint[] mPitModes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 3)] public float[] mOrientations;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public float[] mSpeeds;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 64)] public byte[] mCarNames;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64 * 64)] public byte[] mCarClassNames;
        public int mEnforcedPitStopLap;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTranslatedTrackLocation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTranslatedTrackVariation;
        public float mBrakeBias, mTurboBoostPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 40)] public byte[] mTyreCompound;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4 * 64)] public uint[] mParticipantFlags; // pit schedules, flag colours/reasons, nationalities
        public float mSnowDensity;
        // AMS2 additions (v10+)
        public float mSessionDuration;
        public int mSessionAdditionalLaps;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)] public float[] mTyreTempLCR;
        public uint mDrsState;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public float[] mRideHeight;
        public uint mJoyPad0, mDPad;
        public int mAntiLockSetting, mTractionControlSetting;
        public int mErsDeploymentMode;
        public byte mErsAutoModeEnabled;
        public float mClutchTemp, mClutchWear;
        public byte mClutchOverheated, mClutchSlipping;
        public int mYellowFlagState;
        public byte mSessionIsPrivate;
        public int mLaunchStage;
    }

    public class Ams2Source : ShmSource
    {
        const uint ExpectedVersion = 14;
        // mGameState
        const uint GAME_EXITED = 0, GAME_FRONT_END = 1, GAME_INGAME_PLAYING = 2, GAME_INGAME_PAUSED = 3,
                   GAME_INGAME_INMENU_TIME_TICKING = 4, GAME_INGAME_RESTARTING = 5, GAME_INGAME_REPLAY = 6, GAME_FRONT_END_REPLAY = 7;
        // mPitMode
        const uint PIT_MODE_IN_PIT = 2, PIT_MODE_IN_GARAGE = 4;
        const uint CAR_ABS = 1 << 4, CAR_TCS = 1 << 6;

        static int O(string f) { return (int)Marshal.OffsetOf(typeof(Ams2Shm), f); }
        static readonly int VERSION = O("mVersion"), GAMESTATE = O("mGameState"), U_GAS = O("mUnfilteredThrottle"),
            U_BRAKE = O("mUnfilteredBrake"), U_STEER = O("mUnfilteredSteering"), U_CLUTCH = O("mUnfilteredClutch"),
            CURTIME = O("mCurrentTime"), PITMODE = O("mPitMode"), CARFLAGS = O("mCarFlags"), FUEL = O("mFuelLevel"),
            FUELCAP = O("mFuelCapacity"), SPEED = O("mSpeed"), RPM = O("mRpm"), MAXRPM = O("mMaxRPM"), GAS = O("mThrottle"),
            BRAKE = O("mBrake"), STEER = O("mSteering"), GEAR = O("mGear"), ABSACT = O("mAntiLockActive"),
            SEQ = O("mSequenceNumber"), ABSSET = O("mAntiLockSetting"), TCSET = O("mTractionControlSetting"),
            VIEWED = O("mViewedParticipantIndex"), NUMPART = O("mNumParticipants"), PART = O("mParticipantInfo"),
            TRACKLEN = O("mTrackLength"), BESTLAP = O("mBestLapTime"), LASTLAP = O("mLastLapTime"), INVALID = O("mLapInvalidated"),
            PTIMES = O("mParticipantTimes"), SESSIONSTATE = O("mSessionState"), LAPSINEVENT = O("mLapsInEvent"), EVENTLEFT = O("mEventTimeRemaining"), BRAKEBIAS = O("mBrakeBias"), DRSSTATE = O("mDrsState"), TRACKLOC = O("mTrackLocation"), TRACKVAR = O("mTrackVariation"), CARNAME = O("mCarName");
        // ParticipantInfo (100 bytes): bool active, char name[64], float worldPos[3], float lapDistance, uint racePosition, ...
        const int PART_LEN = 100, P_ACTIVE = 0, P_LAPDIST = 80, P_RACEPOS = 84, P_LAPSDONE = 88, P_SECTOR = 96, MAXP = 64;
        static readonly int LEN = Marshal.SizeOf(typeof(Ams2Shm));

        readonly double steerLock;       // AMS2 only gives steering as -1..1; full lock-to-lock rotation in degrees
        DateTime lastChange = DateTime.MinValue;
        uint lastSeq = uint.MaxValue;

        public Ams2Source(Settings cfg) : base(new[] { "$pcars2$" }, new[] { LEN })
        {
            steerLock = cfg.Num("Ams2SteerLock", 540);
        }

        public override string Name { get { return "AMS2"; } }
        public override string[] Processes { get { return new[] { "AMS2AVX", "AMS2" }; } }

        float F(int off) { return BitConverter.ToSingle(Pages[0], off); }
        int I(int off) { return BitConverter.ToInt32(Pages[0], off); }
        uint U(int off) { return BitConverter.ToUInt32(Pages[0], off); }

        public override bool Read(Tele t)
        {
            if (!Fetch()) { t.Live = false; t.Note = "WAITING FOR AMS2"; return false; }

            uint seq = U(SEQ);
            if (seq != lastSeq) { lastSeq = seq; lastChange = DateTime.UtcNow; }
            uint gs = U(GAMESTATE);
            t.Live = (DateTime.UtcNow - lastChange).TotalSeconds < 2.0 && gs != GAME_EXITED;
            t.Note = t.Live ? (U(VERSION) != ExpectedVersion ? "AMS2 SHM v" + U(VERSION) : "") : "NO LIVE DATA";

            // the raw pedals, so the trace shows what the driver does (not what the auto clutch / aids do)
            t.Gas = Clamp01(F(U_GAS));
            t.Brake = Clamp01(F(U_BRAKE));
            t.Clutch = Clamp01(F(U_CLUTCH));
            t.Gear = I(GEAR) + 1;                  // -1 R, 0 N, 1.. -> 0 R, 1 N, 2..
            if (t.Gear < 0) t.Gear = 0;
            t.Rpm = (int)F(RPM);
            t.MaxRpm = (int)F(MAXRPM);
            t.SpeedKmh = F(SPEED) * 3.6f;
            t.SteerDeg = (float)(F(U_STEER) * steerLock / 2);
            t.AbsOn = Pages[0][ABSACT] != 0;
            t.TcOn = false;                        // AMS2 does not publish TC activity
            float cap = F(FUELCAP), lvl = F(FUEL);
            t.Fuel = (cap > 0 && lvl >= 0 && lvl <= 1.01f) ? lvl * cap : 0;
            t.FuelPerLap = 0;                      // not published: the HUD measures it lap by lap
            float cur = F(CURTIME);
            t.LapMs = cur > 0 ? (int)(cur * 1000) : 0;
            t.ShiftUp = t.MaxRpm > 0 && t.Rpm >= t.MaxRpm * 0.97;
            t.AbsLevel = I(ABSSET); t.TcLevel = I(TCSET); t.TcCutLevel = -1;
            t.HasTcCut = false;
            t.Status = gs == GAME_INGAME_PLAYING ? 2
                     : (gs == GAME_INGAME_PAUSED || gs == GAME_INGAME_INMENU_TIME_TICKING) ? 3
                     : (gs == GAME_INGAME_REPLAY || gs == GAME_FRONT_END_REPLAY) ? 1 : 0;
            uint pit = U(PITMODE);
            t.InPitBox = pit == PIT_MODE_IN_GARAGE || pit == PIT_MODE_IN_PIT;
            t.InSetup = false;
            t.InPitLane = pit != 0;
            FillLaps(t);
            return true;
        }

        float LapFraction()
        {
            int vi = Math.Max(0, Math.Min(MAXP - 1, I(VIEWED))); float len = F(TRACKLEN);
            return len > 100 ? F(PART + vi * PART_LEN + P_LAPDIST) / len : -1;
        }

        // lap position, sector, lap times and the race leader's times (mParticipantTimes = 8 arrays of 64 floats:
        // sector 1-3 current, sector 1-3 fastest, fastest lap, last lap)
        void FillLaps(Tele t)
        {
            int vi = I(VIEWED), n = Math.Max(0, Math.Min(MAXP, I(NUMPART)));
            t.TrackPos = -1; t.Sector = -1;
            if (vi >= 0 && vi < MAXP)
            {
                float f = LapFraction();
                if (f >= 0 && f <= 1.01f) t.TrackPos = Math.Min(1f, f);
                t.Sector = I(PART + vi * PART_LEN + P_SECTOR);
            }
            t.LapValid = Pages[0][INVALID] == 0;
            t.BestLapMs = LapOrZero((long)(F(BESTLAP) * 1000));
            t.LastLapMs = LapOrZero((long)(F(LASTLAP) * 1000));
            t.HasLeader = true; t.LeaderBestMs = 0; t.LeaderLastMs = 0;
            t.SessionBestMs = 0; Array.Clear(t.SessionBestSecMs, 0, 3); t.Drivers = n;
            for (int i = 0; i < n; i++)
            {
                if (Pages[0][PART + i * PART_LEN + P_ACTIVE] == 0) continue;
                int best = LapOrZero((long)(F(PTIMES + (6 * MAXP + i) * 4) * 1000));
                if (best > 0 && (t.SessionBestMs == 0 || best < t.SessionBestMs)) t.SessionBestMs = best;
                for (int k = 0; k < 3; k++)
                {
                    int sec = LapOrZero((long)(F(PTIMES + ((3 + k) * MAXP + i) * 4) * 1000));
                    if (sec > 0 && (t.SessionBestSecMs[k] == 0 || sec < t.SessionBestSecMs[k])) t.SessionBestSecMs[k] = sec;
                }
                if (U(PART + i * PART_LEN + P_RACEPOS) == 1)
                {
                    t.LeaderBestMs = best;
                    t.LeaderLastMs = LapOrZero((long)(F(PTIMES + (7 * MAXP + i) * 4) * 1000));
                }
            }
            t.TrackId = Str(Pages[0], TRACKLOC, 64, false) + "/" + Str(Pages[0], TRACKVAR, 64, false) + "|" + Str(Pages[0], CARNAME, 64, false);
            t.ClearExtras();
            t.FuelCap = F(FUELCAP) > 0 && F(FUELCAP) < 1000 ? F(FUELCAP) : 0;
            t.SessionIsRace = U(SESSIONSTATE) == 5;
            uint lapsInEvent = U(LAPSINEVENT);
            t.SessionLaps = lapsInEvent > 0 && lapsInEvent < 10000 ? (int)lapsInEvent : 0;
            float left = F(EVENTLEFT);
            t.SessionTimeLeftMs = left > 0 && left < 360000 ? (int)(left * 1000) : -1;
            if (vi >= 0 && vi < MAXP) { t.LapCount = (int)U(PART + vi * PART_LEN + P_LAPSDONE); t.Position = Pos((int)U(PART + vi * PART_LEN + P_RACEPOS), t.Drivers); }
            float bb = F(BRAKEBIAS);
            t.BrakeBias = bb > 0.2f && bb < 0.95f ? bb : -1;
            uint drs = U(DRSSTATE);
            t.Drs = (drs & 1) == 0 ? -1 : ((drs & 8) != 0 ? 2 : ((drs & 4) != 0 ? 1 : 0));
        }

        public override string Probe()
        {
            uint flags = U(CARFLAGS);
            return string.Format(CultureInfo.InvariantCulture,
                "ver={0} size={1} seq={2} game={3} pit={4} uGas={5:0.00} uBrk={6:0.00} uClu={7:0.00} uSteer={8:0.000} " +
                "gas={9:0.00} brk={10:0.00} steer={11:0.000} gear={12} rpm={13:0} maxRpm={14:0} spd={15:0.0} " +
                "fuel={16:0.000}x{17:0.0}L lapT={18:0.000} absAct={19} absSet={20} tcSet={21} flags=0x{22:X} (abs={23} tcs={24}) pos={25:0.0000} sector={26} best={27:0.000} last={28:0.000} invalid={29}",
                U(VERSION), LEN, U(SEQ), U(GAMESTATE), U(PITMODE), F(U_GAS), F(U_BRAKE), F(U_CLUTCH), F(U_STEER),
                F(GAS), F(BRAKE), F(STEER), I(GEAR), F(RPM), F(MAXRPM), F(SPEED) * 3.6f,
                F(FUEL), F(FUELCAP), F(CURTIME), Pages[0][ABSACT], I(ABSSET), I(TCSET), flags,
                (flags & CAR_ABS) != 0 ? 1 : 0, (flags & CAR_TCS) != 0 ? 1 : 0,
                LapFraction(), I(PART + Math.Max(0, Math.Min(MAXP - 1, I(VIEWED))) * PART_LEN + P_SECTOR), F(BESTLAP), F(LASTLAP), Pages[0][INVALID]);
        }
    }
}
