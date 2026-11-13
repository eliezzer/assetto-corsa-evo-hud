// MOZA wheelbase temperatures, read from the log MOZA Pit House writes every 30 s while it runs:
//   %LOCALAPPDATA%\MOZA Pit House\Motor_Temprature_Log.log
//   2026-11-12 18:36:11: 控制器温度[40°C]  MOS温度[36.38°C]  电机定子温度[28.75°C]
// (controller / MOSFET / motor stator). Pit House owns the base's serial port, so this is the way to get them
// without closing it. Temperatures move slowly, so a 30 s sample is enough.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SimracingBR
{
    public class WheelBaseSource
    {
        static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MOZA Pit House", "Motor_Temprature_Log.log");
        static readonly Regex Line = new Regex(@"^(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d):[^\[]*\[([\d.]+)\u00B0C\][^\[]*\[([\d.]+)\u00B0C\][^\[]*\[([\d.]+)\u00B0C\]", RegexOptions.Compiled);
        const int TailBytes = 1024;
        static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(120);   // Pit House writes every 30 s; older = Pit House closed

        DateTime nextRead = DateTime.MinValue, stamp = DateTime.MinValue;
        float controller = -1, mosfet = -1, motor = -1;

        public void Read(Tele t)
        {
            if (DateTime.UtcNow >= nextRead) { nextRead = DateTime.UtcNow.AddSeconds(3); Poll(); }
            bool fresh = stamp != DateTime.MinValue && DateTime.Now - stamp < MaxAge && motor > 0;
            t.BaseMotorC = fresh ? motor : -1;
            t.BaseMosfetC = fresh ? mosfet : -1;
            t.BaseControllerC = fresh ? controller : -1;
        }

        void Poll()
        {
            try
            {
                var fi = new FileInfo(LogPath);
                if (!fi.Exists) return;
                byte[] buf;
                using (var fs = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0, fs.Length - TailBytes);
                    fs.Seek(start, SeekOrigin.Begin);
                    buf = new byte[fs.Length - start];
                    int n = 0; while (n < buf.Length) { int r = fs.Read(buf, n, buf.Length - n); if (r <= 0) break; n += r; }
                }
                string[] lines = Encoding.UTF8.GetString(buf).Split('\n');
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    Match m = Line.Match(lines[i].Trim());
                    if (!m.Success) continue;
                    DateTime ts;
                    if (!DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out ts)) continue;
                    stamp = ts;
                    controller = Num(m.Groups[2].Value); mosfet = Num(m.Groups[3].Value); motor = Num(m.Groups[4].Value);
                    return;
                }
            }
            catch { }   // being written / locked: try again on the next poll
        }

        static float Num(string s) { float v; return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : -1; }
    }
}
