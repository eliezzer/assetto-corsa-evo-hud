# Builds dist\SimracingBR.exe with the C# compiler that ships with Windows (no installs needed).
# With -Release 2.3.0 it also packs dist\SimracingBR-v2.3.0-windows.zip (exe + assets), the file attached to a GitHub release.
param([string]$Release = '')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw   = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$wpf  = "$fw\WPF"
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

$refs = @("$wpf\PresentationCore.dll", "$wpf\PresentationFramework.dll", "$wpf\WindowsBase.dll",
          "$fw\System.Xaml.dll", "$fw\System.Drawing.dll", "$fw\System.Windows.Forms.dll") | ForEach-Object { "/r:$_" }
$src  = Get-ChildItem -Path "$root\src" -Filter *.cs -Recurse | ForEach-Object { $_.FullName }

# the app icon (assets\icon.ico, drawn by tools\make-icon.ps1) is embedded in the exe; the tray uses it too
& "$fw\csc.exe" /nologo /target:winexe /optimize+ /nowarn:108 /win32icon:"$root\assets\icon.ico" /out:"$dist\SimracingBR.exe" @refs @src
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

New-Item -ItemType Directory -Force "$dist\assets" | Out-Null
Copy-Item "$root\assets\wheel.png" "$dist\assets\wheel.png" -Force
Write-Host "OK -> $dist\SimracingBR.exe"

if ($Release) {
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $zip = Join-Path $dist "SimracingBR-v$Release-windows.zip"
    if (Test-Path $zip) { Remove-Item $zip }
    $z = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
    try {
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z, "$dist\SimracingBR.exe", 'SimracingBR.exe')
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z, "$dist\assets\wheel.png", 'assets/wheel.png')
    } finally { $z.Dispose() }
    Write-Host "OK -> $zip"
}
