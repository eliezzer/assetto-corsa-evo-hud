// Reads the AC EVO log (Saved Games\ACE\Logs\log-*.txt) to learn the session lengths of a solo Race Weekend.
// When a solo event starts the game writes one line such as
//   Game Started! GameModeType_RACE_WEEKEND | Suzuka East Time Attack Practice  0 seconds @..., Qualifying  300 seconds @..., Warmup  0 seconds @..., Race  600 seconds @... | ks_bmw_m4_gt3 | ...
// so the length of the race is known during practice and qualifying. Online servers don't write that line: the lengths stay unknown.
// The file is only ever read (shared), never written.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SimracingBR
{
    public class EvoLog
    {
        static readonly Regex Race = new Regex(@"\bRace\s+(\d+)\s+(seconds|laps)\b", RegexOptions.Compiled);

        readonly string dir;
        string path = "";
        long pos;
        string partial = "";
        DateTime next = DateTime.MinValue;

        public int RaceSeconds;      // length of the race of the event being played, 0 = unknown / not a solo event
        public int RaceLaps;         // the same when the race is set in laps

        public EvoLog(string logDir)
        {
            dir = logDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "ACE", "Logs");
        }

        // cheap: looks at the file at most every 3 seconds and only reads what was added since the last time
        public void Poll()
        {
            if (DateTime.UtcNow < next) return;
            next = DateTime.UtcNow.AddSeconds(3);
            try
            {
                if (!Directory.Exists(dir)) return;
                string newest = ""; DateTime best = DateTime.MinValue;
                foreach (string f in Directory.GetFiles(dir, "log-*.txt"))
                {
                    DateTime w = File.GetLastWriteTimeUtc(f);
                    if (w > best) { best = w; newest = f; }
                }
                if (newest.Length == 0) return;
                if (newest != path) { path = newest; pos = 0; partial = ""; RaceSeconds = 0; RaceLaps = 0; }
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (fs.Length < pos) { pos = 0; partial = ""; }       // the file was replaced
                    if (fs.Length == pos) return;
                    fs.Seek(pos, SeekOrigin.Begin);
                    var buf = new byte[fs.Length - pos];
                    int n = fs.Read(buf, 0, buf.Length);
                    pos += n;
                    Feed(partial + Encoding.UTF8.GetString(buf, 0, n));
                }
            }
            catch { }       // the log is optional: never let it break the telemetry
        }

        // takes the text read so far (the last line may be unfinished)
        public void Feed(string text)
        {
            string[] lines = text.Split('\n');
            partial = lines[lines.Length - 1];
            for (int i = 0; i < lines.Length - 1; i++) Line(lines[i]);
        }

        void Line(string l)
        {
            if (l.IndexOf("Game Started!", StringComparison.Ordinal) >= 0)
            {
                RaceSeconds = 0; RaceLaps = 0;
                Match m = Race.Match(l);
                if (m.Success)
                {
                    int v = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (m.Groups[2].Value == "laps") RaceLaps = v; else RaceSeconds = v;
                }
            }
            // the server browser means we are going online: an older solo event no longer applies
            else if (l.IndexOf("] [server] [info] added ", StringComparison.Ordinal) >= 0)
            { RaceSeconds = 0; RaceLaps = 0; }
        }
    }
}
