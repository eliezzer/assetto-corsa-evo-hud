// The racing line of a track in world coordinates, learned from the player's own laps (world x/y/z against the game's
// lap position 0..1). Used where the game publishes where the other cars are but not their lap positions (AC EVO):
// projecting a car onto this line gives its lap position, so the sectors overlay can time the other drivers itself.
// Remembered per track in tracks\<hash>.line next to the exe.
using System;
using System.IO;

namespace SimracingBR
{
    public class TrackLine
    {
        public const int N = 2048;                   // points on the lap (2.5 m apart on a 5 km track)
        const int Window = 40;                       // a car is looked for this many points around where it was

        readonly float[] x = new float[N], y = new float[N], z = new float[N];   // learned means, NaN = no sample yet
        readonly int[] cnt = new int[N];
        readonly float[] px = new float[N], py = new float[N], pz = new float[N]; // the same with the holes filled in
        int filled; bool holes = true, changed;
        string file = "";
        DateTime nextSave = DateTime.MaxValue;

        public bool Ready { get { return filled >= N * 97 / 100; } }
        public double LengthM { get; private set; }

        public TrackLine() { Clear(); }

        void Clear()
        {
            for (int i = 0; i < N; i++) { x[i] = y[i] = z[i] = float.NaN; cnt[i] = 0; }
            filled = 0; holes = true; changed = false; LengthM = 0;
        }

        // a new track: load what was learned there before (key = track + layout)
        public void Use(string key, string dir)
        {
            Save();
            Clear();
            uint h = 2166136261;
            foreach (char c in key) { h ^= c; h *= 16777619; }
            file = Path.Combine(Path.Combine(dir, "tracks"), h.ToString("x8") + ".line");
            try
            {
                if (File.Exists(file))
                    using (var r = new BinaryReader(File.OpenRead(file)))
                    {
                        if (r.ReadInt32() != N) return;
                        for (int i = 0; i < N; i++) { x[i] = r.ReadSingle(); y[i] = r.ReadSingle(); z[i] = r.ReadSingle(); cnt[i] = r.ReadInt32(); if (cnt[i] > 0) filled++; }
                    }
            }
            catch { Clear(); }
            changed = false;
        }

        public void Save()
        {
            if (!changed || file.Length == 0 || !Ready) return;
            changed = false; nextSave = DateTime.UtcNow.AddMinutes(2);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using (var w = new BinaryWriter(File.Create(file)))
                {
                    w.Write(N);
                    for (int i = 0; i < N; i++) { w.Write(x[i]); w.Write(y[i]); w.Write(z[i]); w.Write(cnt[i]); }
                }
            }
            catch { }
        }

        // one sample of the player driving normally (on track, valid lap, moving)
        public void Learn(float pos, float wx, float wy, float wz)
        {
            int i = Math.Max(0, Math.Min(N - 1, (int)(pos * N)));
            if (cnt[i] == 0) { x[i] = wx; y[i] = wy; z[i] = wz; filled++; holes = true; if (Ready && nextSave == DateTime.MaxValue) nextSave = DateTime.UtcNow; }
            else
            {
                float k = 1f / Math.Min(cnt[i] + 1, 30);     // running mean that keeps following the line the player drives
                x[i] += (wx - x[i]) * k; y[i] += (wy - y[i]) * k; z[i] += (wz - z[i]) * k;
                if (!holes) { px[i] = x[i]; py[i] = y[i]; pz[i] = z[i]; }
            }
            if (cnt[i] < 1000) cnt[i]++;
            changed = true;
            if (DateTime.UtcNow >= nextSave) Save();
        }

        // fill the points never sampled from their neighbours
        void Fill()
        {
            holes = false;
            int first = -1;
            for (int i = 0; i < N; i++) if (cnt[i] > 0) { first = i; break; }
            if (first < 0) return;
            for (int k = 0; k < N; k++)
            {
                int i = (first + k) % N;
                if (cnt[i] > 0) { px[i] = x[i]; py[i] = y[i]; pz[i] = z[i]; continue; }
                int a = (i - 1 + N) % N, gap = 1;
                while (cnt[a] == 0) { a = (a - 1 + N) % N; gap++; }
                int b = (i + 1) % N, len = gap + 1;
                while (cnt[b] == 0) { b = (b + 1) % N; len++; }
                float f = (float)gap / len;
                px[i] = x[a] + (x[b] - x[a]) * f; py[i] = y[a] + (y[b] - y[a]) * f; pz[i] = z[a] + (z[b] - z[a]) * f;
            }
            double l = 0;
            for (int i = 0; i < N; i++) { int j = (i + 1) % N; l += Math.Sqrt(Sq(px[j] - px[i]) + Sq(py[j] - py[i]) + Sq(pz[j] - pz[i])); }
            LengthM = l;
        }

        static double Sq(double v) { return v * v; }

        // lap position (0..1) of a world point and how far it is from the line (m); hint = the point it was near last time (-1 = search everywhere)
        public float Project(float wx, float wy, float wz, ref int hint, out float dist)
        {
            dist = float.MaxValue;
            if (!Ready) { hint = -1; return -1; }
            if (holes) Fill();
            int best = -1; double bd = double.MaxValue, bu = 0;
            bool local = hint >= 0;
            int from = local ? hint - Window : 0, to = local ? hint + Window : N - 1;
            for (int s = from; s <= to; s++)
            {
                int i = (s % N + N) % N, j = (i + 1) % N;
                double ax = px[i], ay = py[i], az = pz[i], dx = px[j] - ax, dy = py[j] - ay, dz = pz[j] - az;
                double ll = dx * dx + dy * dy + dz * dz;
                double u = ll > 0 ? ((wx - ax) * dx + (wy - ay) * dy + (wz - az) * dz) / ll : 0;
                u = Math.Max(0, Math.Min(1, u));
                double d = Sq(wx - ax - u * dx) + Sq(wy - ay - u * dy) + Sq(wz - az - u * dz);
                if (d < bd) { bd = d; best = s; bu = u; }
            }
            if (local && (best - from < 3 || to - best < 3)) { hint = -1; return Project(wx, wy, wz, ref hint, out dist); }   // left the window: look everywhere
            hint = (best % N + N) % N;
            dist = (float)Math.Sqrt(bd);
            double p = (hint + 0.5 + bu) / N;
            return (float)(p >= 1 ? p - 1 : p);
        }
    }
}
