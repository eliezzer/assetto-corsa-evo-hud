# SimracingBR Overlay

**English** · [Português](README.pt-BR.md)

A set of telemetry overlays for sim racing games, drawn over the game (windowed or borderless). One app, one tray icon, and you choose which overlays to show:

| Overlay | What it shows | Default |
|---|---|---|
| **Pedals** | Pedal input graph and bars (clutch, brake, throttle) that highlight when ABS/TC cut in, ABS / TC / TCC levels, brake bias, gear, speed, shift lights, a rotating steering wheel, fuel row | on |
| **Sectors** (BETA) | Current / best / last lap, the leader's best lap, and S1 / S2 / S3 split into sub-sectors colored purple / green / gray, like the CMRT app of Assetto Corsa 1 | on |
| **Fuel** (BETA) | Fuel in the tank, consumption per lap, laps left, and the **suggested fuel for the whole race** | off |
| **Electronics** | ABS / TC / TCC / ESC chips, brake bias, engine map, differential, hybrid battery, virtual energy, DRS | off |
| ~~Tyres~~ | Tyre pressure advisor: switched off for now (see [below](#tyre-pressure-advisor-switched-off)) | – |

| Game | Data source | Status |
|---|---|---|
| **Assetto Corsa EVO** | `Local\acevo_pmf_*` | Ready, tested in game |
| **Assetto Corsa Competizione** | `Local\acpmf_*` | Implemented, not yet tested in game |
| **Automobilista 2** | `$pcars2$` (Project CARS 2 format) | Implemented, not yet tested in game |
| **Le Mans Ultimate** | `LMU_Data` (LMU's native shared memory) | Implemented, not yet tested in game |

The app detects which game is running by itself. No SimHub and nothing to install: it compiles with the .NET Framework that ships with Windows.

> [!IMPORTANT]
> **The track overlays only appear while the car is on track.** They stay hidden in menus, replays, pause, and while the car is parked in the pit box or garage. If you see nothing after launching the game, that is expected: start driving. The fuel overlay can also be shown on the game's setup screen (see [When to show](#fuel-overlay-beta)).

## Download

A ready-to-run build for Windows (nothing to install: it only needs the .NET Framework that ships with Windows). Download the zip from a release, extract it and run `SimracingBR.exe`, keeping the `assets` folder next to it.

- [SimracingBR-overlay releases](https://github.com/eliezzer/SimracingBR-overlay/releases) (v0.2.0)
- [assetto-corsa-evo-hud releases](https://github.com/eliezzer/assetto-corsa-evo-hud/releases) (v2.0.0, the same program)

Both repositories hold the same code, and the version the program shows in the tray tooltip is its own (0.2.0). The program is not code-signed, so Windows may show a SmartScreen warning the first time: choose *More info* → *Run anyway*. You can also build it yourself, see [Build](#build).

## Per-game setup

- **AC EVO, ACC and LMU**: nothing to do, shared memory is on by default.
- **Automobilista 2**: in *Options → System*, set **Shared Memory** to **Project CARS 2**.
- Run the game **windowed** or **borderless** (overlays can't draw over exclusive fullscreen).

## Use

1. Run `SimracingBR.exe` (keep the `assets` folder next to it).
2. A green and yellow "S" icon appears in the system tray. Right-click it for:
   - **Settings...**: a side menu with one entry per overlay and an on/off switch on its right. Click a name to see that overlay's settings on the right; the pedals overlay is selected by default. Changes apply instantly and the overlays show demo data while the window is open.
   - **Unlock / Lock position**: unlock to drag the overlays (also `Ctrl+Shift+L`, or double-click the tray icon). You can also hover the mouse over an overlay and click the **UNLOCK** / **LOCK** button in its top-right corner.
   - **Overlays**, **Size**, **Reset positions**, **Demo mode**, **Hide when not driving**, **Quit**.
3. Resize with `Ctrl+Shift++` / `Ctrl+Shift+-` (acts on the overlay under the mouse, or the last one you used), or unlock and scroll / drag the yellow corner.

Every overlay has its own position, size and transparency. BETA means it works but is still being tuned.

## Pedals overlay

Pedal graph and bars, ABS / TC / TCC chips (they light up while the aid cuts in), a **BB** chip with the brake bias, gear, speed, shift lights, steering wheel and a fuel row. In Settings choose what is shown, size, overall transparency and background transparency (the background can go almost invisible while the pedals stay solid). The **TCC** chip only shows for games and cars that have that setting, and the **BB** chip only when the game publishes the brake bias.

## Sectors overlay (BETA)

Current / best / last lap, the leader's best lap (LMU and AMS2 only) and S1 / S2 / S3 with 8 sub-sectors each (1 to 12 in Settings).

The games only publish the 3 main sectors (some not even that), so the overlay times every stretch itself from the lap position and its own clock; every game publishes the position. The colors work like the CMRT app of Assetto Corsa 1: each stretch is painted as soon as you drive it, and everything goes back to dark after the finish line:

- **purple**: your time is the best of the session on that stretch;
- **green**: your own best on that stretch, but another driver was faster;
- **gray**: slower than your best on that stretch;
- **dark**: not driven yet in this lap.

"Best of the session" is compared with **all drivers** only where the game publishes their times (main sectors in AMS2, sector 1 in LMU). Where it doesn't (sub-sectors, AC EVO, ACC), purple means your own best and only shows while you are alone in the session; with other drivers around your best is green. There is an option to paint your own best purple anyway.

The current lap time is red when the lap is invalid and purple while you are ahead of your best lap; BEST is purple when it is the session's fastest lap (LMU / AMS2). The main-sector boundaries are learned from the game's sector changes (ACC, LMU, AMS2) and remembered per track; AC EVO does not publish the sector, so it uses thirds of the lap.

## Fuel overlay (BETA)

Fuel in the tank (with a level bar), consumption per lap, laps left on the tank, and **SUGGESTED FUEL**: the fuel for the **whole race** (never "what to add").

```
suggested fuel = (race laps + safety margin) * consumption per lap
race laps      = the race's laps, or: race time / your fastest valid lap + 0.5   (timed race)
```

- **Consumption** comes from the game when it has it, otherwise it is measured over clean laps. It is remembered per track and car.
- **Fastest valid lap**: the best valid lap seen on that track and car in any session (the qualifying lap counts), remembered between runs. Invalid laps and laps through the pit lane don't count.
- **Safety margin**: 1 lap of fuel by default (0 to 3 in Settings).
- **Race length**, in this order: what you set in Settings (race laps or race minutes) → the game's event definition (see [How AC EVO is read](#how-ac-evo-is-read), works for online servers before the race starts) → the log of a solo event → the race session itself (the clock at the start of the race) → the last race the overlay saw. If none is known it asks you to set it.
- **When to show**: on track, on the game's **setup screen**, or both (both by default). On the setup screen the overlay appears where you place it and goes away when you leave it, so the number is there while you type the setup. The setup screen is detected per game: AC EVO (car still with the engine off, found with `--probe`; the garage main menu has the engine idling), ACC (`isSetupMenuVisible`), LMU (garage stall). The game's setup files are never touched.

## Electronics overlay

ABS / TC / TCC / ESC chips that light up while the aid acts, plus brake bias, engine map, differential (power / coast), hybrid battery, virtual energy (LMU) and DRS. Only what the car and the game publish is shown: an AC EVO GT3 shows ABS, TC, brake bias and differential; a hypercar in LMU adds engine map, battery and energy.

## What each game provides

| Data | AC EVO | ACC | AMS2 | LMU |
|---|---|---|---|---|
| Pedals, gear, RPM, speed | ✓ | ✓ | ✓ | ✓ |
| Wheel rotation | ✓ (game degrees) | `AccSteerLock` | `Ams2SteerLock` | ✓ (game wheel range) |
| ABS / TC levels | ✓ | ✓ | ✓ | ✓ |
| Maximum of the ABS / TC levels | ✓ | – | – | ✓ |
| TCC, ESC level | ✓ | TCC | – | TCC when the car has it |
| ABS active / TC active | ✓ / ✓ | ✓ / ✓ | ✓ / – | ✓ / ✓ |
| Brake bias | ✓ | ✓ | ✓ | ✓ |
| Engine map | – | ✓ | – | ✓ |
| Differential | ✓ | – | – | – |
| Battery / virtual energy | battery (if the car has KERS) | – | – | ✓ / ✓ |
| DRS | available | – | ✓ | ✓ |
| Fuel per lap | from the game | from the game | measured | measured |
| Tank capacity | ✓ | ✓ | ✓ | ✓ |
| Position on the lap | ✓ | ✓ | ✓ | ✓ |
| Sector index | – | ✓ | ✓ | ✓ |
| Best / last lap, lap valid | ✓ | ✓ | ✓ | ✓ |
| Leader / session best lap | – | – | ✓ | ✓ |
| Race laps / time left | – (see below) | ✓ | ✓ | ✓ |
| Drivers in the session | ✓ | ✓ | ✓ | ✓ |
| Setup screen | inferred | ✓ | – | garage |

ACC and AMS2 only publish steering as -1..1, so the overlay converts it with a lock-to-lock rotation (default `540`). Set `AccSteerLock=` / `Ams2SteerLock=` in `settings.ini` to match your car.

Only **AC EVO** was checked against real sessions. For the other games the reads are written from their documented structures and the `--probe` output is how to verify them.

### How AC EVO is read

AC EVO publishes the player's data in shared memory but nothing about the other cars, and not the length of the race. Two of its own files are read (read-only, never written) to fill the gap:

- `Saved Games\ACE\last_season.seasondefinition`: the event definition the game rewrites when you join a server or start an event (Protocol Buffers). Per session the settings are at `25 -> 2 -> 200`: field 3 is the duration in ms of a timed session and field 5 the number of laps of a lap race. Seen: a 10 minute qualifying has `3 = 600000`, a 10 lap race has `5 = 10`. **Timed races have not been seen yet**, so that case is a guess.
- `Saved Games\ACE\Logs\log-*.txt`: for a solo Race Weekend the "Game Started!" line has the length of every session.

## Configure

`settings.ini` is created next to the `.exe` (edit it while the app is closed; most things are easier in **Settings**). Every overlay stores `<Prefix>X`, `<Prefix>Y`, `<Prefix>Scale`, `<Prefix>Opacity` and `<Prefix>Enabled`, with prefixes `Sectors`, `Fuel`, `Elec` (the pedals overlay uses plain `X`, `Y`, `Scale`, `Opacity`, and `PedalsEnabled`).

| Key | Default | What it does |
|---|---|---|
| `Scale` | `1.0` | Pedals overlay size, 0.4 to 3.0 |
| `Opacity` | `0.94` | Pedals overlay opacity (whole overlay: pedals, gear, wheel and background), 0.2 to 1.0 |
| `BgOpacity` | `1.0` | Pedals overlay panel background only, 0.0 to 1.0, multiplied by `Opacity` (lower it to keep the pedals solid over a see-through background) |
| `Wheel` | `assets\wheel.png` | Steering wheel image (square PNG, transparent background) |
| `InvertSteer` | `0` | Flip the wheel rotation |
| `AccSteerLock` / `Ams2SteerLock` / `LmuSteerLock` | `540` | Lock-to-lock rotation in degrees (LMU uses the game's value when present) |
| `ColorClutch` / `ColorBrake` / `ColorThrottle` | blue / red / green | Pedal colors |
| `ColorAbs` / `ColorTc` | orange / yellow | Color while ABS/TC cut in |
| `ColorShiftA` / `ColorShiftB` | red / dark | Shift-point flash |
| `ShowGraph`, `ShowBars`, `ShowClutch`, `ShowAids`, `ShowBias`, `ShowGear`, `ShowSpeed`, `ShowShiftLights`, `ShowWheel`, `ShowFuel`, `ShowFuelPerLap`, `ShowLapsLeft` | `1` | Show or hide each part of the pedals overlay |
| `AidColors` | `1` | Recolor the pedals while ABS / TC cut in |
| `AutoHide`, `HideInPitBox`, `HideWhenGameNotFocused` | `1` | Hide rules of the track overlays |
| `SectorsSubs` | `8` | Sub-sectors per sector, 1 to 12 |
| `SectorsShowTimes`, `SectorsShowLeader`, `SectorsShowSectorTimes` | `1` | Parts of the sectors overlay |
| `SectorsPurpleBest` | `0` | Paint your own best purple even when the game doesn't show the other drivers |
| `ColorSecPurple` / `ColorSecGreen` / `ColorSecSlower` | purple / green / gray | Sector colors |
| `FuelVis` | `2` | Fuel overlay shown: `0` on track, `1` setup screen, `2` both |
| `FuelShowFuel`, `FuelShowPerLap`, `FuelShowLapsLeft`, `FuelShowSuggested` | `1` | Parts of the fuel overlay |
| `FuelMargin` | `1.0` | Safety margin in laps of fuel |
| `FuelRaceLaps` / `FuelRaceMinutes` | `0` | Race length you set (`0` = automatic) |
| `ElecShowAbs`, `ElecShowTc`, `ElecShowTcc`, `ElecShowEsc`, `ElecShowBias`, `ElecShowMap`, `ElecShowDiff`, `ElecShowBattery`, `ElecShowEnergy`, `ElecShowDrs` | `1` | Parts of the electronics overlay |

The file also keeps what the app learns: `FuelBest_*` and `FuelPer_*` (fastest valid lap and consumption per track and car), `FuelLastRace*` (the last race seen) and `SecBounds_*` (sector boundaries per track).

## Build

```
powershell -ExecutionPolicy Bypass -File build.ps1
```

Produces `dist\SimracingBR.exe` (+ `dist\assets`).

## Diagnostics

- `SimracingBR.exe --demo` starts with demo data.
- `SimracingBR.exe --probe` writes `probe.log` for 5 minutes with the raw values of every supported game, the lap tracker state (`tracker:` line) and the fuel / electronics values (`extras:` line). Run it with the car on track and attach the file if something looks wrong.
- `SimracingBR.exe --dump` (AC EVO) writes a `dump` folder for 5 minutes with copies of the shared memory pages, to look for data the game might publish.

## Layout

```
src/Program.cs                 entry point (--demo, --probe, --dump)
src/Core/Model.cs              telemetry model (Tele), shared-memory base class, settings
src/Core/SectorTracker.cs      sector / sub-sector timing and colors
src/Core/FuelModel.cs          consumption, fastest valid lap, race length, suggested fuel
src/Core/TyreModel.cs          tyre pressure advisor (switched off)
src/Sources/AcEvoSource.cs     Assetto Corsa EVO
src/Sources/EvoSeason.cs       AC EVO: event definition (race length)
src/Sources/EvoLog.cs          AC EVO: log of a solo event (race length)
src/Sources/AccSource.cs       Assetto Corsa Competizione
src/Sources/Ams2Source.cs      Automobilista 2
src/Sources/LmuSource.cs       Le Mans Ultimate
src/Sources/GameSources.cs     game auto-detection + demo data
src/UI/OverlayApp.cs           tray, hotkeys, Settings window, which overlay shows when
src/UI/OverlayWindow.cs        base of every overlay window (lock, drag, resize, click-through)
src/UI/Chrome.cs               look shared by the overlays
src/UI/HudView.cs, PedalsWindow.cs      pedals overlay
src/UI/SectorsView.cs, SectorsWindow.cs sectors overlay
src/UI/FuelView.cs, FuelWindow.cs       fuel overlay
src/UI/ElecView.cs, ElecWindow.cs       electronics overlay
src/UI/TyreView.cs, TyreWindow.cs       tyre overlay (switched off)
```

To add a game: write an `ITelemetrySource` (usually derived from `ShmSource`) that fills in `Tele`, and add it to `GameSources`. To add an overlay: derive a window from `OverlayWindow`, give it a view that implements `IOverlayView`, and add it to `OverlayApp`.

## Tyre pressure advisor (switched off)

Not available at the moment: it is commented out in `OverlayApp.cs` until it works well. The code (`TyreModel`, `TyreView`, `TyreWindow`) is kept. It shows the hot pressure of the four tyres and their temperature and, after two valid laps, the **cold pressure to put in the setup** so that the hot pressure ends up at a target (27.0 psi by default): `advice = target * cold / hot`, where cold is the pressure at the start of the session and hot the average over the last valid lap. Example: setup 25.5, hot 27.9 → 27.0 × 25.5 / 27.9 = 24.7.
