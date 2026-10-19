# SimracingBR Overlay

**English** · [Português](README.pt-BR.md)

Telemetry overlay for sim racing games: pedal input graph and bars (clutch, brake, throttle) that highlight when ABS/TC cut in, ABS/TC/TCC levels, gear, speed, shift lights, a rotating steering wheel and a fuel row (current fuel, consumption per lap, laps remaining).

| Game | Data source | Status |
|---|---|---|
| **Assetto Corsa EVO** | `Local\acevo_pmf_*` | Ready (same code as [EvoHud](https://github.com/eliezzer/assetto-corsa-evo-hud)) |
| **Assetto Corsa Competizione** | `Local\acpmf_*` | Implemented, not yet tested in game |
| **Automobilista 2** | `$pcars2$` (Project CARS 2 format) | Implemented, not yet tested in game |
| **Le Mans Ultimate** | `LMU_Data` (LMU's native shared memory) | Implemented, not yet tested in game |

The app detects which game is running by itself. No SimHub and nothing to install: it compiles with the .NET Framework that ships with Windows.

> [!IMPORTANT]
> **The overlay only appears while the car is on track.** It stays hidden in menus, replays, pause, and while the car is parked in the pit box or garage. If you see nothing after launching the game, that is expected: start driving.

## Per-game setup

- **AC EVO, ACC and LMU**: nothing to do, shared memory is on by default.
- **Automobilista 2**: in *Options → System*, set **Shared Memory** to **Project CARS 2**.
- Run the game **windowed** or **borderless** (overlays can't draw over exclusive fullscreen).

## Use

1. Run `SimracingBR.exe` (keep the `assets` folder next to it).
2. A green and yellow "S" icon appears in the system tray. Right-click it for:
   - **Settings...**: choose what the HUD shows, size and transparency.
   - **Unlock / Lock position**: unlock to drag the HUD (also `Ctrl+Shift+L`, or double-click the tray icon). You can also hover the mouse over the HUD and click the **UNLOCK** / **LOCK** button that appears in its top-right corner.
   - **Size**, **Reset position**, **Demo mode**, **Hide when not driving**, **Quit**.
3. Resize with `Ctrl+Shift++` / `Ctrl+Shift+-`, or unlock and scroll / drag the yellow corner.

The **TCC** (TC cut) chip only shows for games and cars that have that setting (AC EVO, ACC, and some LMU cars).

## What each game provides

| Data | AC EVO | ACC | AMS2 | LMU |
|---|---|---|---|---|
| Pedals, gear, RPM, speed | ✓ | ✓ | ✓ | ✓ |
| Wheel rotation | ✓ (game degrees) | `AccSteerLock` | `Ams2SteerLock` | ✓ (game wheel range) |
| ABS / TC levels | ✓ | ✓ | ✓ | ✓ |
| TCC level | ✓ | ✓ | – | when the car has it |
| ABS active | ✓ | ✓ | ✓ | ✓ |
| TC active | ✓ | ✓ | – (not published) | ✓ |
| Fuel per lap | from the game | from the game | measured by the HUD | measured by the HUD |

ACC and AMS2 only publish steering as -1..1, so the HUD converts it with a lock-to-lock rotation (default `540`). Set `AccSteerLock=` / `Ams2SteerLock=` in `settings.ini` to match your car.

## Configure

`settings.ini` is created next to the `.exe` (edit it while the app is closed):

| Key | Default | What it does |
|---|---|---|
| `Scale` | `1.0` | HUD size, 0.4 to 3.0 |
| `Opacity` | `0.94` | Window opacity |
| `Wheel` | `assets\wheel.png` | Steering wheel image (square PNG, transparent background) |
| `InvertSteer` | `0` | Flip the wheel rotation |
| `AccSteerLock` / `Ams2SteerLock` / `LmuSteerLock` | `540` | Lock-to-lock rotation in degrees (LMU uses the game's value when present) |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | blue / red / green | Pedal colors |
| `ColorAbs` / `ColorTc` | orange / yellow | Color while ABS/TC cut in |
| `ColorShiftA` / `ColorShiftB` | red / dark | Shift-point flash |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Show or hide each part |
| `AidColors` | `1` | Recolor the pedals while ABS / TC cut in |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Hide rules |

## Build

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Produces `dist\SimracingBR.exe` (+ `dist\assets`).

## Diagnostics

`SimracingBR.exe --probe` writes `probe.log` for 40 s with the raw values of every supported game. Run it with the car on track and attach the file if something looks wrong. `SimracingBR.exe --demo` starts with demo data.

## Layout

```
src/Core/Model.cs           shared telemetry model, shared-memory base class, settings
src/Sources/AcEvoSource.cs  Assetto Corsa EVO
src/Sources/AccSource.cs    Assetto Corsa Competizione
src/Sources/Ams2Source.cs   Automobilista 2
src/Sources/LmuSource.cs    Le Mans Ultimate
src/Sources/GameSources.cs  game auto-detection + demo data
src/UI/HudView.cs           HUD drawing
src/UI/HudWindow.cs         window, tray, hotkeys, settings, auto-hide
```

To add a game: write an `ITelemetrySource` (usually derived from `ShmSource`) that fills in `Tele`, and add it to `GameSources`.

## Sectors overlay

A second overlay (switch it on/off and configure it in **Settings**, next to the pedals overlay): current / best / last lap, the leader's best lap (LMU and AMS2) and S1 / S2 / S3 split into sub-sectors.

The games only publish the 3 main sectors, so the overlay times the sub-sectors itself from the lap position and the lap time (every game publishes both). Colors (the way the CMRT app does it in Assetto Corsa 1; they are painted as you drive each stretch and go back to dark after the finish line): **purple** = your time is the best of the session on that stretch (compared with all drivers only where the game publishes their times; in a game that does not, it is purple only while you are the only driver in the session, otherwise your best is green), **green** = your own best there but another driver was faster, **gray** = slower than your best there, **dark** = not driven yet in this lap.

Notes: the main-sector boundaries are learned from the game's sector changes (ACC, LMU, AMS2) and remembered per track in `settings.ini`; AC EVO does not publish the sector, so it uses thirds of the lap. `--probe` now also logs the lap position, sector and lap times; `--dump` (AC EVO) writes a `dump` folder for 5 minutes to look for data about the other drivers. Keys: `SectorsSubs`, `SectorsShowTimes`, `SectorsShowLeader`, `SectorsShowSectorTimes`, `SectorsOpacity`, `SectorsScale`, `PedalsEnabled`, `SectorsEnabled`, `ColorSecPurple` / `ColorSecGreen` / `ColorSecYellow`.

## Fuel and electronics overlays

Two more overlays, off by default (switch them on in **Settings**; the pedals overlay is unchanged):

- **Fuel**: fuel in the tank (with a level bar), consumption per lap, laps left on the tank and the **fuel for the whole race** (never "what to add"). The suggestion is `(race laps + margin) * consumption`, where the race laps are the race's total laps, or the total race time divided by your fastest valid lap on that track and car (remembered between runs, so the qualifying lap counts). Set the race length in laps or minutes in Settings, or leave it on auto: the game only tells it in the race session itself (AC EVO: the session clock at the start of the race), in a solo Race Weekend the race length is read from the game log (`Saved GamesACEogs`, the "game started!" line, read-only) so it is known already in practice and qualifying, and the overlay remembers the last online race it saw, so the total is ready in the next qualifying. Settings: safety margin in laps, race laps, race minutes.
- **Electronics**: ABS / TC / TCC / ESC chips that light up while the aid acts, plus brake bias, engine map, differential, hybrid battery, virtual energy (LMU) and DRS. Only what the car and the game publish is shown (an AC EVO GT3 shows ABS, TC, brake bias and differential; a hypercar in LMU adds map, battery and energy).

`--probe` logs the values both overlays read (`extras:` line). What each game publishes: AC EVO (race length only as the time left, from the session clock), ACC (laps, time left, engine map, brake bias), LMU (laps, time left, map, brake bias, battery, virtual energy, DRS), AMS2 (laps, time left, brake bias, DRS). Only AC EVO was checked against a real session.

## Tyre pressure overlay (switched off for now)

Not available at the moment: the overlay is commented out in `OverlayApp.cs` until it works well. The code (`TyreModel`, `TyreView`, `TyreWindow`) is kept. What it does: the hot pressure of the four tyres (green within 0.5 psi of the target, amber within 1, red beyond), their core temperature, and, after two valid laps, the **cold pressure to put in the setup** so that the hot pressure ends up at the target (27.0 psi by default).

`advice = target * cold / hot`: the cold pressure is the one the car shows standing in the pits at the start of the session (or what you type in Settings), the hot one is the average over the last valid lap. Example: setup 25.5, hot 27.9 -> 27.0 * 25.5 / 27.9 = 24.7. The out lap and invalid laps don't count; if the pressure is still rising it says so (check again after another lap). Needs a game that publishes the tyre pressure: AC EVO and ACC (physics page, `wheelsPressure` / `tyreCoreTemperature`). Settings: target, number of valid laps, optional setup pressure per wheel.

### Showing the fuel and tyre overlays on the game's setup screen

The fuel and tyre overlays have a **When to show** choice in Settings: on track, on the setup screen, or both (setup screen by default). On the setup screen they appear where you place them (unlock with `Ctrl+Shift+L` to drag) and go away when you leave it, so the numbers are there while you type the setup. How the setup screen is detected: AC EVO (in the pit box, car still, engine off: found with `--probe`, the garage main menu has the engine idling), ACC (`isSetupMenuVisible`), LMU (garage stall). The game's setup files aren't touched.

The **Sectors** and **Fuel** overlays are marked **BETA** in the settings menu and the tray menu: they work, but are still being tuned.

In a **solo Race Weekend** of AC EVO the race length is read from the game's own log (`Saved Games\ACE\Logs\log-*.txt`, the "Game Started!" line; the file is only read) so the fuel overlay knows it already in practice and qualifying. Online servers don't write that line: there the overlay uses your setting, or the last race it saw.

For **online servers** the race length is read from `Saved Games\ACE\last_season.seasondefinition`, the event definition the game rewrites when you join a server (read only). It is a Protocol Buffers file; for AC EVO 0.9.1 the settings of each session are at `25 -> 2 -> 200`: field 3 is the duration in ms of a timed session, field 5 the number of laps of a lap race (seen: 10 minute qualifying = 600000, 10 lap race = 10). Timed races have not been seen yet, so that case is a guess.
