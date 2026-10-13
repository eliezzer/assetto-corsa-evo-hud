# EvoHud

**English** · [Português](README.pt-BR.md)

Telemetry overlay for **Assetto Corsa EVO**: pedal input graph and bars (clutch, brake, throttle) that highlight when ABS/TC cut in, ABS/TC/TCC levels, gear, speed, shift lights, a rotating steering wheel and a fuel row (current fuel, consumption per lap, laps remaining).

It reads the game's shared memory directly (`Local\acevo_pmf_physics` / `Local\acevo_pmf_graphics`). No SimHub and nothing to install: it compiles with the .NET Framework that ships with Windows.

## Download

Grab `EvoHud-v1.0-windows.zip` from the [Releases](../../releases) page, extract it and run `EvoHud.exe`. Keep the `assets` folder next to the `.exe` (it holds the wheel image).

## Use

1. Run `EvoHud.exe`.
2. The game must run in **windowed** or **borderless** mode. Overlays can't draw over exclusive fullscreen.
3. The HUD only shows while you are driving. It hides in menus, replays, pause, while parked in the pit box (setup screen) and when the game is not the foreground window.
4. A red "E" icon appears in the system tray (next to the clock). Right-click it for:
   - **Unlock / Lock position**: unlock to drag the HUD (also `Ctrl+Shift+L`, or double-click the tray icon).
   - **Reset position**, **Demo mode**, **Hide when not driving**, **Quit**.

To start it with Windows, put a shortcut to `EvoHud.exe` in the Startup folder (`Win+R`, then `shell:startup`).

## Configure

`settings.ini` is created next to the `.exe` (edit it while the app is closed):

| Key | Default | What it does |
|---|---|---|
| `Scale` | `1.0` | HUD size |
| `Opacity` | `0.94` | Transparency |
| `Wheel` | `assets\wheel.png` | Wheel image (square PNG, transparent background, centered) |
| `InvertSteer` | `0` | Reverses the wheel rotation |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | blue / red / green | Pedal colors |
| `ColorAbs` / `ColorTc` | orange / yellow | Color while ABS / TC are cutting in |
| `ColorShiftA` / `ColorShiftB` | red / dark | Shift-point flash colors |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowGear`, `ShowWheel`, `ShowFuel`, `AidColors` | `1` | Turn parts of the HUD on or off |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Auto-hide rules |

## Change the steering wheel

Drop a square PNG with a transparent background at `assets\wheel.png`. For a photo on a white background:

```
powershell -ExecutionPolicy Bypass -File tools\cutout.ps1 C:\path\wheel.jpg assets\wheel.png
```

## Build

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Produces `dist\EvoHud.exe` (plus `dist\assets`).

## Diagnostics

`EvoHud.exe --probe` writes `probe.log` for 40 seconds with the raw values read from the game. `EvoHud.exe --demo` starts with demo data.

## Notes

The shared-memory layout comes from third-party documentation of AC EVO, which is still in Early Access and may change between updates. If something stops updating after a game patch, run `--probe` and open an issue with the log.
