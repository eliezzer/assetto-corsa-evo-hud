# EvoHud

**English** · [Português](README.pt-BR.md)

Telemetry overlay for **Assetto Corsa EVO**: pedal input graph and bars (clutch, brake, throttle) that highlight when ABS/TC cut in, ABS/TC/TCC levels, gear, speed, shift lights, a rotating steering wheel and a fuel row (current fuel, consumption per lap, laps remaining).

It reads the game's shared memory directly (`Local\acevo_pmf_physics` / `Local\acevo_pmf_graphics`). No SimHub and nothing to install: it compiles with the .NET Framework that ships with Windows.

> [!IMPORTANT]
> **The overlay only appears while the car is on track.** It stays hidden in menus, replays, pause, and while the car is parked in the pit box or garage (setup screen). To make it show up: **engage 1st gear and press the throttle** so the car starts moving.
> If you see nothing after launching the game, that is expected: it is not broken. Start driving and it will appear. It hides again when you stop in the pit box, pause, or leave the session.

## Download

Grab `EvoHud-v1.1.2-windows.zip` from the [Releases](../../releases) page, extract it and run `EvoHud.exe`. Keep the `assets` folder next to the `.exe` (it holds the wheel image).

## Use

1. Run `EvoHud.exe`.
2. The game must run in **windowed** or **borderless** mode. Overlays can't draw over exclusive fullscreen.
3. The HUD only shows while you are driving. It hides in menus, replays, pause, while parked in the pit box (setup screen) and when the game is not the foreground window.
4. A red "E" icon appears in the system tray (next to the clock). Right-click it for:
   - **Unlock / Lock position**: unlock to drag the HUD (also `Ctrl+Shift+L`, or double-click the tray icon).
   - **Settings...**, **Size**, **Reset position**, **Demo mode**, **Hide when not driving**, **Quit**.

**No clutch pedal?** Untick **Clutch** in **Settings...** (or set `ShowClutch=0`). With an automatic clutch the game moves the clutch value by itself, so the blue bar would look stuck. The bar and its graph line disappear and the other two bars re-center.

To start it with Windows, put a shortcut to `EvoHud.exe` in the Startup folder (`Win+R`, then `shell:startup`).

## Choose what to see

Right-click the tray icon and pick **Settings...** (or run `EvoHud.exe --settings`). Tick only what you want on the HUD. The HUD reflows by itself: hidden blocks take no space and the overlay shrinks to fit. Everything applies instantly, is saved, and the HUD shows demo data while the window is open so you can see what each option does.

| Group | Options |
|---|---|
| Pedals | Input graph, pedal bars, clutch (bar and graph line), ABS / TC / TCC levels and activity |
| Dash | Gear, speed, shift lights, steering wheel |
| Fuel | Fuel in the tank, consumption per lap, laps remaining |
| Appearance | **Size** (40% to 300%) and **transparency** sliders, plus **Reset to defaults** |

With the input graph, the ABS / TC / TCC chips sit in a row below the fuel strip. Without the input graph the HUD goes compact: the pedal bars sit beside a fuel card, with the ABS / TC / TCC chips in a row right below the fuel lines.

The window follows your Windows language (Portuguese or English).
## Resize

Pick the size you want, from 40% to 300% of the default. The size is saved and restored next time.

- **Tray icon, right-click, Size**: 50%, 75%, 100%, 125%, 150%, 200%, 250%, or Bigger / Smaller.
- **Hotkeys (work any time, even while playing)**: `Ctrl+Shift++` makes it bigger, `Ctrl+Shift+-` makes it smaller (main keys or numpad).
- **With the mouse**: unlock the HUD (`Ctrl+Shift+L`), then **scroll the mouse wheel** over it, or **drag the yellow corner at the bottom-right**. Lock it again when done.

You can also set it by hand with `Scale` in `settings.ini` (for example `Scale=1.5`).
## Configure

`settings.ini` is created next to the `.exe` (edit it while the app is closed):

| Key | Default | What it does |
|---|---|---|
| `Scale` | `1.0` | HUD size, 0.4 to 3.0 (saved when you resize it) |
| `Opacity` | `0.94` | Transparency |
| `Wheel` | `assets\wheel.png` | Wheel image (square PNG, transparent background, centered) |
| `InvertSteer` | `0` | Reverses the wheel rotation |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | blue / red / green | Pedal colors |
| `ColorAbs` / `ColorTc` | orange / yellow | Color while ABS / TC are cutting in |
| `ColorShiftA` / `ColorShiftB` | red / dark | Shift-point flash colors |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Show or hide each part (easier from the Settings window) |
| `AidColors` | `1` | Recolor pedals while ABS / TC are cutting in |
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
