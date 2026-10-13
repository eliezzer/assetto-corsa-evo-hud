# Builds dist\EvoHud.exe with the C# compiler that ships with Windows (no installs needed).
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw   = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$wpf  = "$fw\WPF"
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

$refs = @("$wpf\PresentationCore.dll", "$wpf\PresentationFramework.dll", "$wpf\WindowsBase.dll",
          "$fw\System.Xaml.dll", "$fw\System.Drawing.dll", "$fw\System.Windows.Forms.dll") | ForEach-Object { "/r:$_" }

& "$fw\csc.exe" /nologo /target:winexe /optimize+ /out:"$dist\EvoHud.exe" @refs "$root\src\EvoHud.cs"
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

New-Item -ItemType Directory -Force "$dist\assets" | Out-Null
Copy-Item "$root\assets\wheel.png" "$dist\assets\wheel.png" -Force
Write-Host "OK -> $dist\EvoHud.exe"
