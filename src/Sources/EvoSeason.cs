// Reads the event definition AC EVO writes when you join a server or start an event (Saved Games\ACE\last_season.seasondefinition,
// a Protocol Buffers file) to learn the length of the sessions BEFORE they start: it is the only place where an online server's
// qualifying and race length can be found, so the fuel for the race is known already in practice and qualifying.
// Only read, never written. What was found by decoding it (AC EVO 0.9.1):
//   a session is a message with its name in field 2 ("Practice", "Qualifying", "Race", ...);
//   its settings are at 25 (Any) -> 2 -> 200, and there  3 = duration in ms (timed session), 5 = number of laps (lap race), 4 = estimated duration.
//   seen: a 10 minute qualifying has 3 = 600000, a 10 lap race has 5 = 10.
//   seen online (AC EVO 0.9.x, "InstantRace" race session): neither 3 nor 5, only 4 = 600000 for a 10 minute timed race,
//   so 4 is used for the race when 3 and 5 are missing.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimracingBR
{
    public class EvoSeason
    {
        class Node
        {
            public int F;                 // field number
            public ulong V;               // value of a varint field
            public string S;              // text
            public List<Node> C;          // nested message
            public Node Child(int f) { if (C != null) foreach (Node n in C) if (n.F == f) return n; return null; }
        }

        readonly string path;
        DateTime lastWrite = DateTime.MinValue, next = DateTime.MinValue;

        public int RaceSeconds;           // race length when it is a timed race, 0 = unknown
        public int RaceLaps;              // race length when it is a lap race, 0 = unknown
        public int QualySeconds;          // length of the qualifying, 0 = unknown

        public EvoSeason(string file)
        {
            path = file ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games", "ACE", "last_season.seasondefinition");
        }

        // cheap: looks at the file's date at most every 2 seconds and decodes it only when it changed
        public void Poll()
        {
            if (DateTime.UtcNow < next) return;
            next = DateTime.UtcNow.AddSeconds(2);
            try
            {
                if (!File.Exists(path)) return;
                DateTime w = File.GetLastWriteTimeUtc(path);
                if (w == lastWrite) return;
                byte[] b;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    b = new byte[fs.Length];
                    int got = 0; while (got < b.Length) { int n = fs.Read(b, got, b.Length - got); if (n <= 0) break; got += n; }
                }
                lastWrite = w;
                Decode(b);
            }
            catch { }       // optional information: never let it break the telemetry
        }

        public void Decode(byte[] b)
        {
            RaceSeconds = 0; RaceLaps = 0; QualySeconds = 0;
            List<Node> root = Parse(b, 0, b.Length, 0);
            if (root != null) Walk(root);
        }

        void Walk(List<Node> nodes)
        {
            foreach (Node n in nodes)
            {
                if (n.C == null) continue;
                Node name = n.Child(2);
                if (name != null && name.S != null && (name.S == "Race" || name.S == "Qualifying")) Session(name.S, n);
                Walk(n.C);
            }
        }

        // the session's settings: 25 -> 2 -> 200
        void Session(string kind, Node s)
        {
            Node spec = s.Child(25); if (spec == null) return;
            Node value = spec.Child(2); if (value == null) return;
            Node cfg = value.Child(200); if (cfg == null) return;
            Node dur = cfg.Child(3), laps = cfg.Child(5), est = cfg.Child(4);
            int ms = dur != null && dur.V > 0 && dur.V < 100UL * 3600 * 1000 ? (int)(dur.V / 1000) : 0;
            int lp = laps != null && laps.V > 1 && laps.V < 10000 ? (int)laps.V : 0;
            int es = est != null && est.V > 0 && est.V < 100UL * 3600 * 1000 ? (int)(est.V / 1000) : 0;
            if (kind == "Qualifying") { QualySeconds = ms; return; }
            if (ms > 0) { RaceSeconds = ms; RaceLaps = 0; }
            else if (lp > 0) { RaceLaps = lp; RaceSeconds = 0; }
            else if (es > 0) { RaceSeconds = es; RaceLaps = 0; }      // InstantRace on online servers: only 4 is set
        }

        // ---- a small Protocol Buffers decoder: nested messages are recognised by trying to parse them -------------------------
        static bool ReadVarint(byte[] b, ref int p, int end, out ulong v)
        {
            v = 0; int sh = 0;
            while (p < end && sh < 70) { byte x = b[p++]; v |= (ulong)(x & 0x7f) << sh; if ((x & 0x80) == 0) return true; sh += 7; }
            return false;
        }

        static bool Texty(byte[] b, int s, int e) { if (e <= s) return false; for (int i = s; i < e; i++) if (b[i] < 32 || b[i] > 126) return false; return true; }

        // null when [s, e) is not a valid message
        static List<Node> Parse(byte[] b, int s, int e, int depth)
        {
            if (depth > 12) return null;
            var list = new List<Node>();
            int p = s;
            while (p < e)
            {
                ulong key; if (!ReadVarint(b, ref p, e, out key)) return null;
                int wt = (int)(key & 7); ulong f = key >> 3;
                if (f == 0 || f > 5000) return null;
                var n = new Node { F = (int)f };
                if (wt == 0) { ulong v; if (!ReadVarint(b, ref p, e, out v)) return null; n.V = v; }
                else if (wt == 1) { if (p + 8 > e) return null; p += 8; }
                else if (wt == 5) { if (p + 4 > e) return null; p += 4; }
                else if (wt == 2)
                {
                    ulong l; if (!ReadVarint(b, ref p, e, out l)) return null;
                    if (l > (ulong)(e - p)) return null;
                    int s2 = p, e2 = p + (int)l;
                    if (Texty(b, s2, e2)) n.S = Encoding.ASCII.GetString(b, s2, e2 - s2);
                    else if (e2 > s2) n.C = Parse(b, s2, e2, depth + 1);      // not a message: stays a leaf with no children
                    p = e2;
                }
                else return null;
                list.Add(n);
            }
            return list.Count > 0 ? list : null;
        }
    }
}
