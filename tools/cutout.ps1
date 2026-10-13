# Turns a wheel photo on a white background into a square, centered PNG with transparency.
# Removes the outer background AND enclosed white areas (holes in the wheel), then de-fringes edges.
# Usage: powershell -ExecutionPolicy Bypass -File tools\cutout.ps1 <input.jpg|png> <output.png>
param([string]$In, [string]$Out)
$ErrorActionPreference = 'Stop'
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System; using System.Collections.Generic; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class Cutout {
    public static void Run(string inPath, string outPath) {
        Bitmap src = new Bitmap(inPath);
        int w = src.Width, h = src.Height;
        Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, w, h);
        src.Dispose();
        BitmapData d = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, bmp.PixelFormat);
        int st = d.Stride; byte[] b = new byte[st * h]; Marshal.Copy(d.Scan0, b, 0, b.Length);

        // 1) near-white, near-neutral pixels
        bool[] white = new bool[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
            int p = y * st + x * 4; int bl = b[p], gr = b[p + 1], rd = b[p + 2];
            int mn = Math.Min(rd, Math.Min(gr, bl)), mx = Math.Max(rd, Math.Max(gr, bl));
            white[y * w + x] = mn > 178 && mx - mn < 28;
        }
        // 2) connected white regions: background if touching the border or large (the wheel holes)
        bool[] bg = new bool[w * h]; int[] lbl = new int[w * h]; int next = 1;
        var q = new Queue<int>(); var comp = new List<int>();
        for (int s = 0; s < w * h; s++) {
            if (!white[s] || lbl[s] != 0) continue;
            comp.Clear(); bool edge = false; lbl[s] = next; q.Enqueue(s);
            while (q.Count > 0) {
                int i = q.Dequeue(); comp.Add(i); int x = i % w, y = i / w;
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1) edge = true;
                if (x > 0 && white[i - 1] && lbl[i - 1] == 0) { lbl[i - 1] = next; q.Enqueue(i - 1); }
                if (x < w - 1 && white[i + 1] && lbl[i + 1] == 0) { lbl[i + 1] = next; q.Enqueue(i + 1); }
                if (y > 0 && white[i - w] && lbl[i - w] == 0) { lbl[i - w] = next; q.Enqueue(i - w); }
                if (y < h - 1 && white[i + w] && lbl[i + w] == 0) { lbl[i + w] = next; q.Enqueue(i + w); }
            }
            if (edge || comp.Count >= 220) foreach (int i in comp) bg[i] = true;
            next++;
        }
        // 3) distance (in px) from background, up to 3
        int[] dist = new int[w * h]; for (int i = 0; i < w * h; i++) dist[i] = bg[i] ? 0 : 99;
        for (int pass = 1; pass <= 3; pass++)
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
                int i = y * w + x; if (dist[i] != 99) continue;
                if ((x > 0 && dist[i - 1] == pass - 1) || (x < w - 1 && dist[i + 1] == pass - 1) ||
                    (y > 0 && dist[i - w] == pass - 1) || (y < h - 1 && dist[i + w] == pass - 1)) dist[i] = pass;
            }
        // 4) apply: clear background, un-mix white from the edge band
        for (int i = 0; i < w * h; i++) {
            int p = (i / w) * st + (i % w) * 4;
            if (bg[i]) { b[p] = b[p + 1] = b[p + 2] = 0; b[p + 3] = 0; continue; }
            if (dist[i] > 3) continue;
            int mn = Math.Min(b[p], Math.Min(b[p + 1], b[p + 2]));
            double a = Math.Max(0, Math.Min(1, (255.0 - mn) / (255.0 - 70.0)));
            if (dist[i] == 1) a = Math.Min(a, 0.85);
            if (a < 0.04) { b[p + 3] = 0; continue; }
            for (int c = 0; c < 3; c++) b[p + c] = (byte)Math.Max(0, Math.Min(255, (b[p + c] - 255.0 * (1 - a)) / a));
            b[p + 3] = (byte)(a * 255);
        }
        Marshal.Copy(b, 0, d.Scan0, b.Length); bmp.UnlockBits(d);

        // 5) crop to a square centered on the wheel
        int minx = w, maxx = 0, miny = h, maxy = 0;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if (b[y * st + x * 4 + 3] > 8) {
            if (x < minx) minx = x; if (x > maxx) maxx = x; if (y < miny) miny = y; if (y > maxy) maxy = y; }
        int side = (int)(Math.Max(maxx - minx, maxy - miny) * 1.04);
        double cx = (minx + maxx) / 2.0, cy = (miny + maxy) / 2.0;
        using (Bitmap o = new Bitmap(side, side, PixelFormat.Format32bppArgb)) {
            using (Graphics g = Graphics.FromImage(o)) { g.Clear(Color.Transparent); g.DrawImage(bmp, (int)(side / 2.0 - cx), (int)(side / 2.0 - cy), w, h); }
            o.Save(outPath, ImageFormat.Png);
        }
        bmp.Dispose();
    }
}
"@
[Cutout]::Run((Resolve-Path $In).Path, [IO.Path]::GetFullPath($Out))
Write-Host "OK -> $Out"

