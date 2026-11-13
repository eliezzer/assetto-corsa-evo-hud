# Draws the app icon (assets\icon.ico, plus assets\icon.png for previews): a steering wheel on a dark tile,
# with the green / yellow of the tray icon. Multi-size ICO with PNG entries (16 to 256 px).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$assets = Join-Path $root 'assets'

function Draw([int]$s) {
    $bmp = New-Object System.Drawing.Bitmap $s, $s, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'; $g.Clear([System.Drawing.Color]::Transparent)
    $k = $s / 256.0
    # dark rounded tile
    $r = 52 * $k; $tile = New-Object System.Drawing.Drawing2D.GraphicsPath
    $w = $s - 1
    $tile.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90); $tile.AddArc($w - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $tile.AddArc($w - 2 * $r, $w - 2 * $r, 2 * $r, 2 * $r, 0, 90); $tile.AddArc(0, $w - 2 * $r, 2 * $r, 2 * $r, 90, 90); $tile.CloseFigure()
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush ([System.Drawing.Point]::new(0, 0)), ([System.Drawing.Point]::new(0, $s)),
        ([System.Drawing.Color]::FromArgb(255, 34, 34, 42)), ([System.Drawing.Color]::FromArgb(255, 12, 12, 16))
    $g.FillPath($bg, $tile)
    # green stripe on the left, like the overlays
    $g.SetClip($tile); $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 0, 156, 59))), 0, 0, [single](22 * $k), $s); $g.ResetClip()

    $cx = 136 * $k; $cy = 128 * $k; $R = 84 * $k
    $rimW = [Math]::Max(1.6, 26 * $k)
    $white = [System.Drawing.Color]::FromArgb(255, 238, 238, 242)
    $pen = New-Object System.Drawing.Pen $white, $rimW
    $g.DrawEllipse($pen, [single]($cx - $R), [single]($cy - $R), [single](2 * $R), [single](2 * $R))
    if ($s -ge 24) {
        # spokes and hub
        $sp = New-Object System.Drawing.Pen $white, ([Math]::Max(1.2, 20 * $k))
        $sp.StartCap = 'Round'; $sp.EndCap = 'Round'
        $g.DrawLine($sp, [single]($cx - $R + 6 * $k), [single]($cy + 6 * $k), [single]($cx - 22 * $k), [single]($cy + 6 * $k))
        $g.DrawLine($sp, [single]($cx + 22 * $k), [single]($cy + 6 * $k), [single]($cx + $R - 6 * $k), [single]($cy + 6 * $k))
        $g.DrawLine($sp, [single]$cx, [single]($cy + 30 * $k), [single]$cx, [single]($cy + $R - 6 * $k))
        $hub = 34 * $k
        $g.FillEllipse((New-Object System.Drawing.SolidBrush $white), [single]($cx - $hub), [single]($cy + 6 * $k - $hub), [single](2 * $hub), [single](2 * $hub))
        $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 24, 24, 30))), [single]($cx - $hub * 0.55), [single]($cy + 6 * $k - $hub * 0.55), [single](1.1 * $hub), [single](1.1 * $hub))
    }
    # yellow centre marker at 12 o'clock
    $mw = [Math]::Max(2.0, 26 * $k); $mh = [Math]::Max(2.0, 34 * $k)
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 214, 0))), [single]($cx - $mw / 2), [single]($cy - $R - $mh / 2), [single]$mw, [single]$mh)
    $g.Dispose()
    return $bmp
}

$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = @()
foreach ($s in $sizes) {
    $b = Draw $s; $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    if ($s -eq 256) { $b.Save((Join-Path $assets 'icon.png'), [System.Drawing.Imaging.ImageFormat]::Png) }
    $b.Dispose(); $pngs += , $ms.ToArray()
}
# ICO container: header, one directory entry per size, then the PNG images
$out = New-Object System.IO.MemoryStream; $bw = New-Object System.IO.BinaryWriter $out
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $d = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$d); $bw.Write([byte]$d); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$pngs[$i].Length); $bw.Write([uint32]$offset)
    $offset += $pngs[$i].Length
}
foreach ($p in $pngs) { $bw.Write($p) }
$bw.Flush(); [System.IO.File]::WriteAllBytes((Join-Path $assets 'icon.ico'), $out.ToArray())
Write-Host "OK -> $assets\icon.ico"
