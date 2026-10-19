// Le Mans Ultimate: native "LMU_Data" shared memory (Support\SharedMemoryInterface in the game folder, rFactor 2 plugin
// structs). The game's structs use pack(4); the outer SharedMemoryObjectOut wrapper uses default packing, so its
// size_t member is 8-aligned. Offsets are computed below from mirrors of the structs we read.
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SimracingBR
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LmuAppState      // ApplicationStateV01
    {
        public long mAppWindow;
        public uint mWidth, mHeight, mRefreshRate, mWindowed;
        public byte mOptionsLocation;   // 0 main UI, 1 track loading, 2 monitor, 3 on track
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)] public byte[] mOptionsPage;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 204)] public byte[] mExpansion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LmuScoringInfo   // ScoringInfoV01
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTrackName;
        public int mSession;
        public double mCurrentET, mEndET;
        public int mMaxLaps;
        public double mLapDist;
        public long mResultsStream;
        public int mNumVehicles;
        public byte mGamePhase;
        public sbyte mYellowFlagState;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public sbyte[] mSectorFlag;
        public byte mStartLight, mNumRedLights, mInRealtime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] mPlayerName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mPlrFileName;
        public double mDarkCloud, mRaining, mAmbientTemp, mTrackTemp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mWind;
        public double mMinPathWetness, mMaxPathWetness;
        public byte mGameMode, mIsPasswordProtected;
        public ushort mServerPort;
        public uint mServerPublicIP;
        public int mMaxPlayers;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] mServerName;
        public float mStartET;
        public double mAvgPathWetness;
        public float mSessionTimeRemaining, mTimeOfDay;
        public byte mIsFixedSetup, mTrackGripLevel, mCloudCoverage, mTrackLimitsStepsPerPenalty, mTrackLimitsStepsPerPoint;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 187)] public byte[] mExpansion;
        public long mVehicle;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LmuVehScoring    // VehicleScoringInfoV01
    {
        public int mID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] mDriverName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mVehicleName;
        public short mTotalLaps;
        public sbyte mSector, mFinishStatus;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)] public double[] mLapDistToCurSector2;   // mLapDist .. mCurSector2
        public short mNumPitstops, mNumPenalties;
        public byte mIsPlayer;
        public sbyte mControl;
        public byte mInPits, mPlace;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] mVehicleClass;
        public double mTimeBehindNext;
        public int mLapsBehindNext;
        public double mTimeBehindLeader;
        public int mLapsBehindLeader;
        public double mLapStartET;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] public double[] mMotion;    // pos, vel, accel, ori[3], rot, rot accel
        public byte mHeadlights, mPitState, mServerScored, mIndividualPhase;
        public int mQualification;
        public double mTimeIntoLap, mEstimatedLapTime;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] public byte[] mPitGroup;
        public byte mFlag, mUnderYellow, mCountLapFlag, mInGarageStall;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] mUpgradePack;
        public float mPitLapDist, mBestLapSector1, mBestLapSector2;
        public ulong mSteamID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] mVehFilename;
        public short mAttackMode;
        public byte mFuelFraction, mDRSState;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] mExpansion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LmuWheel         // TelemWheelV01
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public double[] mSuspensionDeflectionToPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mTemperature;
        public double mWear;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] mTerrainName;
        public byte mSurfaceType, mFlat, mDetached, mStaticUndeflectedRadius;
        public double mVerticalTireDeflection, mWheelYLocation, mToe, mTireCarcassTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mTireInnerLayerTemperature;
        public float mOptimalTemp;
        public byte mCompoundIndex, mCompoundType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] public byte[] mExpansion;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LmuTelem         // TelemInfoV01 (up to the wheels)
    {
        public int mID;
        public double mDeltaTime, mElapsedTime;
        public int mLapNumber;
        public double mLapStartET;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mVehicleName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTrackName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mPos;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mLocalVel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)] public double[] mLocalAccelOriRot;   // accel, ori[3], rot
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mLocalRotAccel;
        public int mGear;
        public double mEngineRPM, mEngineWaterTemp, mEngineOilTemp, mClutchRPM,
                      mUnfilteredThrottle, mUnfilteredBrake, mUnfilteredSteering, mUnfilteredClutch,
                      mFilteredThrottle, mFilteredBrake, mFilteredSteering, mFilteredClutch,
                      mSteeringShaftTorque, mFront3rdDeflection, mRear3rdDeflection, mFrontWingHeight,
                      mFrontRideHeight, mRearRideHeight, mDrag, mFrontDownforce, mRearDownforce, mFuel, mEngineMaxRPM;
        public byte mScheduledStops, mOverheating, mDetached, mHeadlights;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public byte[] mDentSeverity;
        public double mLastImpactET, mLastImpactMagnitude;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mLastImpactPos;
        public double mEngineTorque;
        public int mCurrentSector;
        public byte mSpeedLimiter, mMaxGears, mFrontTireCompoundIndex, mRearTireCompoundIndex;
        public double mFuelCapacity;
        public byte mFrontFlapActivated, mRearFlapActivated, mRearFlapLegalStatus, mIgnitionStarter;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] public byte[] mFrontTireCompoundName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)] public byte[] mRearTireCompoundName;
        public byte mSpeedLimiterAvailable, mAntiStallActivated;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)] public byte[] mUnused;
        public float mVisualSteeringWheelRange;
        public double mRearBrakeBias, mTurboBoostPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public float[] mPhysicsToGraphicsOffset;
        public float mPhysicalSteeringWheelRange;
        public double mDeltaBest, mBatteryChargeFraction, mElectricBoostMotorTorque, mElectricBoostMotorRPM,
                      mElectricBoostMotorTemperature, mElectricBoostWaterTemperature;
        public byte mElectricBoostMotorState, mLapInvalidated, mABSActive, mTCActive, mSpeedLimiterActive, mWiperState,
                    mTC, mTCMax, mTCSlip, mTCSlipMax, mTCCut, mTCCutMax, mABS, mABSMax,
                    mMotorMap, mMotorMapMax, mMigration, mMigrationMax, mFrontAntiSway, mFrontAntiSwayMax,
                    mRearAntiSway, mRearAntiSwayMax, mLiftAndCoastProgress, mTrackLimitsSteps;
        public float mRegen, mSoC, mVirtualEnergy, mTimeGapCarAhead, mTimeGapCarBehind, mTimeGapPlaceAhead, mTimeGapPlaceBehind;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)] public byte[] mVehicleModel;
        public byte mVehicleClass, mVehicleChampionship;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] mExpansion;
        public byte mWheel;   // first byte of TelemWheelV01 mWheel[4]
    }

    public class LmuSource : ShmSource
    {
        const int MAX_VEH = 104, MAX_PATH = 260, SME_MAX = 16, STREAM = 65536;
        static int Align(int v, int a) { return (v + a - 1) / a * a; }
        static int Off(Type t, string f) { return (int)Marshal.OffsetOf(t, f); }

        // SharedMemoryObjectOut { generic; paths; scoring; telemetry }
        static readonly int GEN_VERSION = SME_MAX * 4;                                     // long gameVersion
        static readonly int GEN_APP = GEN_VERSION + 8;                                     // after float FFBTorque
        static readonly int GEN_LEN = GEN_APP + Marshal.SizeOf(typeof(LmuAppState));
        static readonly int APP_LOCATION = GEN_APP + Off(typeof(LmuAppState), "mOptionsLocation");
        static readonly int SCORING = Align(GEN_LEN + 5 * MAX_PATH, 8);
        static readonly int SI_LEN = Marshal.SizeOf(typeof(LmuScoringInfo));
        static readonly int VEH = SCORING + Align(SI_LEN, 8) + 8;                          // after size_t scoringStreamSize
        static readonly int VEH_LEN = Marshal.SizeOf(typeof(LmuVehScoring));
        static readonly int TELEMETRY = Align(VEH + MAX_VEH * VEH_LEN + STREAM, 8);
        static readonly int TI = TELEMETRY + 4;                                            // after activeVehicles, playerVehicleIdx, playerHasVehicle
        static readonly int TI_LEN = Off(typeof(LmuTelem), "mWheel") + 4 * Marshal.SizeOf(typeof(LmuWheel));

        static int S(string f) { return Off(typeof(LmuScoringInfo), f); }
        static int V(string f) { return Off(typeof(LmuVehScoring), f); }
        static int T(string f) { return Off(typeof(LmuTelem), f); }
        static readonly int S_NUMVEH = S("mNumVehicles"), S_REALTIME = S("mInRealtime"), S_PHASE = S("mGamePhase");
        static readonly int S_LAPLEN = S("mLapDist"), S_SESSION = S("mSession"), S_MAXLAPS = S("mMaxLaps"), S_ENDET = S("mEndET"), S_CURET = S("mCurrentET");
        // VehicleScoringInfoV01.mLapDist .. mCurSector2: lapDist, pathLateral, trackEdge, bestS1, bestS2, bestLap, lastS1, lastS2, lastLap, curS1, curS2
        static readonly int V_TIMES = V("mLapDistToCurSector2"), V_PLACE = V("mPlace"), V_TOTALLAPS = V("mTotalLaps"), V_DRS = V("mDRSState");
        static readonly int V_ID = V("mID"),V_ISPLAYER = V("mIsPlayer"), V_INPITS = V("mInPits"), V_GARAGE = V("mInGarageStall"), V_PITSTATE = V("mPitState");
        static readonly int T_ID = T("mID"), T_ET = T("mElapsedTime"), T_LAP = T("mLapNumber"), T_LAPSTART = T("mLapStartET"),
            T_VEL = T("mLocalVel"), T_GEAR = T("mGear"), T_RPM = T("mEngineRPM"), T_GAS = T("mUnfilteredThrottle"),
            T_BRAKE = T("mUnfilteredBrake"), T_STEER = T("mUnfilteredSteering"), T_CLUTCH = T("mUnfilteredClutch"),
            T_FUEL = T("mFuel"), T_MAXRPM = T("mEngineMaxRPM"), T_FUELCAP = T("mFuelCapacity"),
            T_WHEELRANGE = T("mPhysicalSteeringWheelRange"), T_ABSACT = T("mABSActive"), T_TCACT = T("mTCActive"),
            T_TC = T("mTC"), T_TCMAX = T("mTCMax"), T_TCCUT = T("mTCCut"), T_TCCUTMAX = T("mTCCutMax"),
            T_ABS = T("mABS"), T_ABSMAX = T("mABSMax"), T_SECTOR = T("mCurrentSector"), T_LAPINV = T("mLapInvalidated"),
            T_TRACK = T("mTrackName"), T_VEHNAME = T("mVehicleName"), T_MAP = T("mMotorMap"), T_MAPMAX = T("mMotorMapMax"),
            T_REARBIAS = T("mRearBrakeBias"), T_SOC = T("mSoC"), T_VE = T("mVirtualEnergy");

        readonly double steerLock;
        readonly byte[] si = new byte[SI_LEN], veh = new byte[MAX_VEH * VEH_LEN], th = new byte[4], tb = new byte[TI_LEN];
        DateTime lastChange = DateTime.MinValue;
        double lastET = double.NaN, lastDist = double.NaN, distET;
        int vehIdx = -1;

        public LmuSource(Settings cfg) : base(new[] { "LMU_Data" }, new[] { GEN_LEN })
        {
            steerLock = cfg.Num("LmuSteerLock", 540);
        }

        public override string Name { get { return "LMU"; } }
        public override string[] Processes { get { return new[] { "Le Mans Ultimate" }; } }

        public override bool Read(Tele t)
        {
            if (!Fetch() || !ReadRange(0, SCORING, si, SI_LEN) || !ReadRange(0, TELEMETRY, th, 4))
            { t.Live = false; t.Note = "WAITING FOR LMU"; return false; }

            // the player's telemetry slot, and the matching scoring entry (same mID)
            bool hasCar = th[2] != 0;
            int idx = th[1];
            bool ok = hasCar && idx < MAX_VEH && ReadRange(0, TI + (long)idx * TI_LEN, tb, TI_LEN);
            int nVeh = Math.Max(0, Math.Min(MAX_VEH, BitConverter.ToInt32(si, S_NUMVEH)));
            vehIdx = -1;
            if (ok && nVeh > 0 && ReadRange(0, VEH, veh, nVeh * VEH_LEN))
            {
                int id = BitConverter.ToInt32(tb, T_ID);
                for (int i = 0; i < nVeh; i++)
                    if (veh[i * VEH_LEN + V_ISPLAYER] != 0 || BitConverter.ToInt32(veh, i * VEH_LEN + V_ID) == id) { vehIdx = i; if (veh[i * VEH_LEN + V_ISPLAYER] != 0) break; }
            }
            if (!ok) { t.Live = false; t.Note = "NO CAR"; return true; }

            double et = BitConverter.ToDouble(tb, T_ET);
            if (et != lastET) { lastET = et; lastChange = DateTime.UtcNow; }
            t.Live = (DateTime.UtcNow - lastChange).TotalSeconds < 2.0;   // the clock stops when paused
            t.Note = t.Live ? "" : "NO LIVE DATA";

            t.Gas = Clamp01((float)D(T_GAS));
            t.Brake = Clamp01((float)D(T_BRAKE));
            t.Clutch = Clamp01((float)D(T_CLUTCH));
            t.Gear = BitConverter.ToInt32(tb, T_GEAR) + 1;      // -1 R, 0 N, 1.. -> 0 R, 1 N, 2..
            if (t.Gear < 0) t.Gear = 0;
            t.Rpm = (int)D(T_RPM);
            t.MaxRpm = (int)D(T_MAXRPM);
            double vx = D(T_VEL), vy = D(T_VEL + 8), vz = D(T_VEL + 16);
            t.SpeedKmh = (float)(Math.Sqrt(vx * vx + vy * vy + vz * vz) * 3.6);
            float range = BitConverter.ToSingle(tb, T_WHEELRANGE);
            t.SteerDeg = (float)(D(T_STEER) * (range > 90 && range < 2000 ? range : steerLock) / 2);
            t.AbsOn = tb[T_ABSACT] != 0;
            t.TcOn = tb[T_TCACT] != 0;
            t.AbsLevel = tb[T_ABSMAX] > 0 ? tb[T_ABS] : -1;
            t.TcLevel = tb[T_TCMAX] > 0 ? tb[T_TC] : -1;
            t.TcCutLevel = tb[T_TCCUTMAX] > 0 ? tb[T_TCCUT] : -1;
            t.HasTcCut = tb[T_TCCUTMAX] > 0;
            t.Fuel = (float)D(T_FUEL);
            if (float.IsNaN(t.Fuel) || t.Fuel < 0 || t.Fuel > 1000) t.Fuel = 0;
            t.FuelPerLap = 0;                                   // not published: the HUD measures it lap by lap
            double lapT = et - D(T_LAPSTART);
            t.LapMs = lapT > 0 && lapT < 3600 ? (int)(lapT * 1000) : 0;
            t.ShiftUp = t.MaxRpm > 0 && t.Rpm >= t.MaxRpm * 0.97;
            t.Status = si[S_REALTIME] != 0 ? 2 : 0;              // at the monitor / garage screen otherwise
            t.InPitBox = vehIdx >= 0 && veh[vehIdx * VEH_LEN + V_GARAGE] != 0;
            t.InSetup = t.InPitBox;
            FillLaps(t, et, nVeh);
            FillExtras(t, nVeh);
            return true;
        }

        // session, fuel and electronics for the fuel / electronics overlays
        void FillExtras(Tele t, int nVeh)
        {
            t.ClearExtras();
            t.FuelCap = (float)D(T_FUELCAP);
            if (float.IsNaN(t.FuelCap) || t.FuelCap < 0 || t.FuelCap > 1000) t.FuelCap = 0;
            int session = BitConverter.ToInt32(si, S_SESSION);
            t.SessionIsRace = session >= 10 && session <= 13;
            int maxLaps = BitConverter.ToInt32(si, S_MAXLAPS);
            t.SessionLaps = maxLaps > 0 && maxLaps < 10000 ? maxLaps : 0;
            double endET = BitConverter.ToDouble(si, S_ENDET), curET = BitConverter.ToDouble(si, S_CURET), left = endET - curET;
            t.SessionTimeLeftMs = left > 0 && left < 360000 ? (int)(left * 1000) : -1;
            if (vehIdx >= 0) t.LapCount = BitConverter.ToInt16(veh, vehIdx * VEH_LEN + V_TOTALLAPS);
            t.TcMax = tb[T_TCMAX] > 0 ? tb[T_TCMAX] : -1; t.TcCutMax = tb[T_TCCUTMAX] > 0 ? tb[T_TCCUTMAX] : -1; t.AbsMax = tb[T_ABSMAX] > 0 ? tb[T_ABSMAX] : -1;
            if (tb[T_MAPMAX] > 0) { t.EngineMap = tb[T_MAP]; t.EngineMapMax = tb[T_MAPMAX]; }
            double rear = D(T_REARBIAS);
            t.BrakeBias = rear > 0.05 && rear < 0.8 ? (float)(1 - rear) : -1;
            float soc = BitConverter.ToSingle(tb, T_SOC), ve = BitConverter.ToSingle(tb, T_VE);
            t.BatteryPct = soc >= 0 && soc <= 1.001f ? soc * 100 : -1;
            t.EnergyPct = ve >= 0 && ve <= 1.001f ? ve * 100 : -1;
            if (vehIdx >= 0 && veh[vehIdx * VEH_LEN + V_DRS] != 0) t.Drs = veh[vehIdx * VEH_LEN + V_DRS] >= 2 ? 2 : 1;
        }


        // Lap position, sector, lap times and the leader's times. The scoring page (lap distance) refreshes only a few
        // times a second, so the position is dead-reckoned from the telemetry speed in between.
        void FillLaps(Tele t, double et, int nVeh)
        {
            t.TrackPos = -1; t.InPitLane = false; t.HasLeader = true; t.LeaderBestMs = 0; t.LeaderLastMs = 0; t.SessionBestMs = 0; Array.Clear(t.SessionBestSecMs, 0, 3); t.Drivers = nVeh;
            t.Sector = BitConverter.ToInt32(tb, T_SECTOR);
            t.LapValid = tb[T_LAPINV] == 0;
            t.TrackId = Str(tb, T_TRACK, 64, false) + "|" + Str(tb, T_VEHNAME, 64, false);
            double len = BitConverter.ToDouble(si, S_LAPLEN);
            if (vehIdx >= 0)
            {
                int vo = vehIdx * VEH_LEN;
                double d = BitConverter.ToDouble(veh, vo + V_TIMES);
                if (d != lastDist) { lastDist = d; distET = et; }
                double est = d + Math.Max(0, Math.Min(0.5, et - distET)) * t.SpeedKmh / 3.6;
                if (len > 100 && est >= 0 && est <= len * 1.02) t.TrackPos = (float)Math.Min(1.0, est / len);
                t.InPitLane = veh[vo + V_INPITS] != 0;
                t.BestLapMs = LapOrZero((long)(BitConverter.ToDouble(veh, vo + V_TIMES + 5 * 8) * 1000));
                t.LastLapMs = LapOrZero((long)(BitConverter.ToDouble(veh, vo + V_TIMES + 8 * 8) * 1000));
            }
            for (int i = 0; i < nVeh; i++)
            {
                int best = LapOrZero((long)(BitConverter.ToDouble(veh, i * VEH_LEN + V_TIMES + 5 * 8) * 1000));
                if (best > 0 && (t.SessionBestMs == 0 || best < t.SessionBestMs)) t.SessionBestMs = best;
                int s1 = LapOrZero((long)(BitConverter.ToDouble(veh, i * VEH_LEN + V_TIMES + 3 * 8) * 1000));
                if (s1 > 0 && (t.SessionBestSecMs[0] == 0 || s1 < t.SessionBestSecMs[0])) t.SessionBestSecMs[0] = s1;
                if (veh[i * VEH_LEN + V_PLACE] == 1)
                {
                    t.LeaderBestMs = best;
                    t.LeaderLastMs = LapOrZero((long)(BitConverter.ToDouble(veh, i * VEH_LEN + V_TIMES + 8 * 8) * 1000));
                }
            }
        }

        double D(int off) { return BitConverter.ToDouble(tb, off); }

        public override string Probe()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "ver={0} loc={1} realtime={2} phase={3} nVeh={4} hasCar={5} idx={6} veh={7} layout=[gen {8} scoring {9} veh {10}x{11} telem {12} ti {13}] " +
                "et={14:0.00} lap={15} gas={16:0.00} brk={17:0.00} clu={18:0.00} steer={19:0.000} range={20:0} gear={21} rpm={22:0} maxRpm={23:0} " +
                "fuel={24:0.00}/{25:0.0} absAct={26} tcAct={27} tc={28}/{29} tcCut={30}/{31} abs={32}/{33} garage={34} inPits={35} pitState={36} lapDist={37:0.0}/{38:0.0} sector={39} bestLap={40:0.000} lastLap={41:0.000} lapInvalid={42}",
                BitConverter.ToInt32(Pages[0], GEN_VERSION), Pages[0][APP_LOCATION], si[S_REALTIME], si[S_PHASE],
                BitConverter.ToInt32(si, S_NUMVEH), th[2], th[1], vehIdx,
                GEN_LEN, SCORING, VEH_LEN, MAX_VEH, TELEMETRY, TI_LEN,
                D(T_ET), BitConverter.ToInt32(tb, T_LAP), D(T_GAS), D(T_BRAKE), D(T_CLUTCH), D(T_STEER),
                BitConverter.ToSingle(tb, T_WHEELRANGE), BitConverter.ToInt32(tb, T_GEAR), D(T_RPM), D(T_MAXRPM),
                D(T_FUEL), D(T_FUELCAP), tb[T_ABSACT], tb[T_TCACT], tb[T_TC], tb[T_TCMAX], tb[T_TCCUT], tb[T_TCCUTMAX], tb[T_ABS], tb[T_ABSMAX],
                vehIdx >= 0 ? veh[vehIdx * VEH_LEN + V_GARAGE] : -1, vehIdx >= 0 ? veh[vehIdx * VEH_LEN + V_INPITS] : -1,
                vehIdx >= 0 ? veh[vehIdx * VEH_LEN + V_PITSTATE] : -1,
                vehIdx >= 0 ? BitConverter.ToDouble(veh, vehIdx * VEH_LEN + V_TIMES) : -1, BitConverter.ToDouble(si, S_LAPLEN),
                BitConverter.ToInt32(tb, T_SECTOR),
                vehIdx >= 0 ? BitConverter.ToDouble(veh, vehIdx * VEH_LEN + V_TIMES + 40) : -1,
                vehIdx >= 0 ? BitConverter.ToDouble(veh, vehIdx * VEH_LEN + V_TIMES + 64) : -1, tb[T_LAPINV]);
        }
    }
}
