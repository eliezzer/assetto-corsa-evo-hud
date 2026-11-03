// Entry point: SimracingBR.exe [--demo] [--settings] [--probe] [--dump] [--cars]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace SimracingBR
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            if (Array.IndexOf(args, "--probe") >= 0) return Probe(dir);
            if (Array.IndexOf(args, "--dump") >= 0) return Dump(dir);
            if (Array.IndexOf(args, "--cars") >= 0) return RecordCars(dir);
            bool demo = Array.IndexOf(args, "--demo") >= 0;
            var cfg = new Settings(Path.Combine(dir, "settings.ini"));
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;   // the overlays can all be off: the tray icon keeps the app alive
            var ui = new OverlayApp(cfg, dir, demo);

            int rc = app.Run();
            GC.KeepAlive(ui);
            return rc;
        }

        // Writes a "dump" folder for 5 minutes with what AC EVO puts in shared memory, to find out whether it publishes anything about
        // the other drivers. Run it while driving (a few laps, ideally with other cars on the track). Every second it logs the size and a
        // checksum of each map it can open (the known ones and a few guesses), the player's fields and the two undocumented arrays
        // (timingState / sessionState) of the graphics page; it also saves whole copies of each map at a few moments.
        static int Dump(string dir)
        {
            string outDir = Path.Combine(dir, "dump");
            Directory.CreateDirectory(outDir);
            string[] names = { "acevo_pmf_physics", "acevo_pmf_graphics", "acevo_pmf_static", "acevo_pmf_timing", "acevo_pmf_session",
                               "acevo_pmf_leaderboard", "acevo_pmf_cars", "acevo_pmf_opponents", "acevo_pmf_standings", "acevo_pmf_telemetry",
                               "acevo_pmf_realtime", "acevo_pmf_server", "acevo_pmf_events", "acevo_pmf_drivers" };
            var files = new System.IO.MemoryMappedFiles.MemoryMappedFile[names.Length];
            var views = new System.IO.MemoryMappedFiles.MemoryMappedViewAccessor[names.Length];
            int gfx = Array.IndexOf(names, "acevo_pmf_graphics");
            int tOff = (int)Marshal.OffsetOf(typeof(EvoGfx), "timingState"), sOff = (int)Marshal.OffsetOf(typeof(EvoGfx), "sessionState");
            int nposOff = (int)Marshal.OffsetOf(typeof(EvoGfx), "npos"), lapOff = (int)Marshal.OffsetOf(typeof(EvoGfx), "currentLapTimeMs");
            var snapAt = new System.Collections.Generic.HashSet<int> { 5, 30, 60, 90, 120, 150, 180, 240, 290 };
            var last = new string[names.Length];
            using (var w = new StreamWriter(Path.Combine(outDir, "dump.log"), false))
            {
                w.WriteLine("timingState offset " + tOff + ", sessionState offset " + sOff);
                var start = DateTime.UtcNow;
                for (int sec = 0; sec < 300; sec++)
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (views[i] == null)
                        {
                            foreach (string prefix in new[] { "Local\\", "" })
                            {
                                try
                                {
                                    files[i] = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(prefix + names[i], System.IO.MemoryMappedFiles.MemoryMappedFileRights.Read);
                                    views[i] = files[i].CreateViewAccessor(0, 0, System.IO.MemoryMappedFiles.MemoryMappedFileAccess.Read);
                                    w.WriteLine("t=" + sec + " OPENED " + prefix + names[i] + " capacity=" + views[i].Capacity);
                                    break;
                                }
                                catch { }
                            }
                        }
                        if (views[i] == null) continue;
                        try
                        {
                            var buf = new byte[views[i].Capacity];
                            views[i].ReadArray<byte>(0, buf, 0, buf.Length);
                            uint h = 2166136261; foreach (byte b in buf) { h ^= b; h *= 16777619; }
                            string line = names[i] + " size=" + buf.Length + " hash=" + h.ToString("x8");
                            if (i == gfx && buf.Length > sOff + 256)
                            {
                                line += " npos=" + BitConverter.ToSingle(buf, nposOff).ToString("0.0000", CultureInfo.InvariantCulture) + " lapMs=" + BitConverter.ToInt32(buf, lapOff);
                                line += "\r\n             timingState=" + BitConverter.ToString(buf, tOff, 256).Replace("-", "");
                                line += "\r\n             sessionState=" + BitConverter.ToString(buf, sOff, 256).Replace("-", "");
                            }
                            if (line != last[i]) { w.WriteLine("t=" + sec + " " + line); last[i] = line; }
                            if (snapAt.Contains(sec)) File.WriteAllBytes(Path.Combine(outDir, names[i] + "_" + sec + "s.bin"), buf);
                        }
                        catch (Exception e) { w.WriteLine("t=" + sec + " " + names[i] + " read failed: " + e.Message); views[i] = null; }
                    }
                    w.Flush();
                    var wait = start.AddSeconds(sec + 1) - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero) System.Threading.Thread.Sleep(wait);
                }
            }
            return 0;
        }

        // AC EVO: writes cars.log with every car's world coordinates (5 per second) and the sector / lap times the overlay
        // measures for the other drivers from them (60 per second), to compare with the game's results file at the end of the
        // session. Runs until AC EVO has been closed for a minute (at most 3 hours); can run next to the overlay.
        static int RecordCars(string dir)
        {
            var src = new AcEvoSource(); var t = new Tele();
            var trk = new SectorTracker(null, 8);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var ic = CultureInfo.InvariantCulture;
            using (var w = new StreamWriter(Path.Combine(dir, "cars.log"), false))
            {
                trk.RivalTimed = (k, what, ms) => w.WriteLine(clock.ElapsedMilliseconds + " T " + k + " " + what + " " + ms.ToString("0", ic));
                var end = DateTime.UtcNow.AddHours(3); var lastLive = DateTime.UtcNow;
                long nextRaw = 0, nextFlush = 0;
                var seen = new float[60]; var updates = new int[60];
                while (DateTime.UtcNow < end && (DateTime.UtcNow - lastLive).TotalSeconds < 60)
                {
                    if (src.Read(t)) lastLive = DateTime.UtcNow;
                    trk.Update(t);
                    for (int i = 0; i < 60; i++) if (src.X[i] != seen[i]) { seen[i] = src.X[i]; updates[i]++; }
                    long now = clock.ElapsedMilliseconds;
                    if (now >= nextRaw)
                    {
                        nextRaw = now + 200;
                        var sb = new System.Text.StringBuilder();
                        sb.Append(now).Append(" P npos=").Append(t.TrackPos.ToString("0.0000", ic)).Append(" lapMs=").Append(t.LapMs)
                          .Append(" last=").Append(t.LastLapMs).Append(" best=").Append(t.BestLapMs).Append(" valid=").Append(t.LapValid ? 1 : 0)
                          .Append(" pit=").Append(t.InPitLane ? 1 : 0).Append(" live=").Append(t.Live ? 1 : 0).Append(" drivers=").Append(t.Drivers)
                          .Append(" slots=").Append(src.Slots).Append(" line=").Append(src.Line.Ready ? src.Line.LengthM.ToString("0", ic) : "learning")
                          .Append(" seg=").Append(trk.CurSeg).Append(" track='").Append(t.TrackId).Append("' |");
                        for (int i = 0; i < src.Slots; i++)
                        {
                            if (src.X[i] == 0 && src.Y[i] == 0 && src.Z[i] == 0) continue;
                            sb.Append(' ').Append(i).Append('(').Append(src.X[i].ToString("0.0", ic)).Append(',').Append(src.Y[i].ToString("0.0", ic)).Append(',').Append(src.Z[i].ToString("0.0", ic));
                            if (i > 0 && t.CarSlots > i) sb.Append(" p=").Append(t.CarPos[i].ToString("0.0000", ic)).Append(" d=").Append(src.Dist[i].ToString("0.0", ic));
                            sb.Append(" u=").Append(updates[i]).Append(')');
                            updates[i] = 0;
                        }
                        w.WriteLine(sb.ToString());
                    }
                    if (now >= nextFlush) { nextFlush = now + 1000; w.Flush(); }
                    System.Threading.Thread.Sleep(15);
                }
                src.Line.Save();
            }
            return 0;
        }

        // Writes probe.log for 5 minutes: raw values from every supported game so offsets/units can be verified,
        // plus what the sectors tracker makes of them (drive a few laps while it runs).
        static int Probe(string dir)
        {
            var cfg = new Settings(Path.Combine(dir, "settings.ini"));
            var games = new GameSources(cfg); var t = new Tele();
            var trk = new SectorTracker(null, 8);
            var fuel = new FuelModel(null);
            using (var w = new StreamWriter(Path.Combine(dir, "probe.log"), false))
            {
                var end = DateTime.UtcNow.AddSeconds(300);
                int n = 0;
                while (DateTime.UtcNow < end)
                {
                    if (n++ % 5 == 0) w.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + games.Probe(t));
                    games.Read(t);
                    trk.Update(t);
                    fuel.Update(t);
                    if (n % 5 == 1)
                    {
                        w.WriteLine("             fuel: raceLenSec=" + t.RaceLengthSec + " raceLenLaps=" + t.RaceLengthLaps + " source=" + fuel.RaceSource +
                            " raceMin=" + fuel.RaceMinutes + " raceLaps=" + fuel.RaceLaps.ToString("0.00", CultureInfo.InvariantCulture) +
                            " perLap=" + fuel.PerLap.ToString("0.000", CultureInfo.InvariantCulture) + " bestMs=" + fuel.BestValidLapMs +
                            " raceFuel=" + fuel.RaceFuel.ToString("0.0", CultureInfo.InvariantCulture));
                        w.WriteLine("             tracker: pos=" + t.TrackPos.ToString("0.000", CultureInfo.InvariantCulture) + " lapMs=" + t.LapMs +
                            " seg=" + trk.CurSeg + " sec=" + trk.CurSec + " pitLane=" + t.InPitLane + " valid=" + t.LapValid +
                            " clean=" + (trk.Why.Length == 0) + " why='" + trk.Why + "' lapsRecorded=" + trk.LapsDone +
                            " ownBest=" + trk.OwnBestMs + " ownLast=" + trk.OwnLastMs + " gameBest=" + t.BestLapMs + " gameLast=" + t.LastLapMs +
                            " bounds=" + (trk.BoundsKnown ? "learned" : "thirds"));
                        w.WriteLine("             extras: fuel=" + t.Fuel.ToString("0.0", CultureInfo.InvariantCulture) + " cap=" + t.FuelCap.ToString("0", CultureInfo.InvariantCulture) + " perLap=" + t.FuelPerLap.ToString("0.00", CultureInfo.InvariantCulture) +
                            " lapCount=" + t.LapCount + " race=" + t.SessionIsRace + " raceLaps=" + t.SessionLaps + " timeLeftMs=" + t.SessionTimeLeftMs + " drivers=" + t.Drivers +
                            " tc=" + t.TcLevel + "/" + t.TcMax + " tcc=" + t.TcCutLevel + "/" + t.TcCutMax + " abs=" + t.AbsLevel + "/" + t.AbsMax + " esc=" + t.EscLevel + "/" + t.EscMax +
                            " map=" + t.EngineMap + "/" + t.EngineMapMax + " bias=" + t.BrakeBias.ToString("0.000", CultureInfo.InvariantCulture) + " diff=" + t.DiffPower.ToString("0.00", CultureInfo.InvariantCulture) + "/" + t.DiffCoast.ToString("0.00", CultureInfo.InvariantCulture) +
                            " batt=" + t.BatteryPct.ToString("0", CultureInfo.InvariantCulture) + " energy=" + t.EnergyPct.ToString("0", CultureInfo.InvariantCulture) + " drs=" + t.Drs +
                            " psi=" + t.TyrePsi[0].ToString("0.0", CultureInfo.InvariantCulture) + "/" + t.TyrePsi[1].ToString("0.0", CultureInfo.InvariantCulture) + "/" + t.TyrePsi[2].ToString("0.0", CultureInfo.InvariantCulture) + "/" + t.TyrePsi[3].ToString("0.0", CultureInfo.InvariantCulture) +
                            " tyreTemp=" + t.TyreTemp[0].ToString("0", CultureInfo.InvariantCulture) + "/" + t.TyreTemp[1].ToString("0", CultureInfo.InvariantCulture) + "/" + t.TyreTemp[2].ToString("0", CultureInfo.InvariantCulture) + "/" + t.TyreTemp[3].ToString("0", CultureInfo.InvariantCulture));
                        w.Flush();
                    }
                    System.Threading.Thread.Sleep(50);
                }
            }
            return 0;
        }
    }
}
