# QALQON

C# + MonoGame tower defense for Windows and Android. Version 0.3 adapts the supplied artwork into weapon icons, directional tower sprites, themed scenery and a campaign world map, alongside procedural woodland environments and a compact **16 × 12** campaign board. Source artwork provenance is recorded in `docs/supplied-assets.md`.

## Run on Windows

After publishing, open `artifacts/windows-x64/Qalqon.exe`. From source:

```powershell
.\scripts\run.ps1
```

Choose a map and difficulty, select a weapon and place it on a free cell. The route must remain open. On touch, preview the location and confirm placement. Select a built tower to upgrade, sell or change targeting priority.

Controls: 1–6 selects a tower, Space pauses, F changes speed, U upgrades, Delete asks to sell, Esc closes selection/dialog or pauses. Mouse wheel zooms, middle-drag pans. Touch supports pinch zoom and two-finger pan. F11 toggles fullscreen. Key bindings can be changed in Settings.

## Build and verify

Requires the pinned .NET SDK (`global.json`). Scripts use `.tools/dotnet/dotnet.exe` when present. MonoGame packages restore into `.tools/nuget`.

```powershell
.\scripts\build.ps1
.\scripts\publish-windows.ps1
.\scripts\capture-design.ps1
```

Android additionally requires the .NET Android workload, Android SDK and JDK. Configure `ANDROID_HOME`/`JAVA_HOME`, then use `scripts/publish-android.ps1`. Publisher signing is configured through the documented environment variables in that script; local debug signing is not a store release.

All eight maps have 100 authored waves, three difficulties and Classic/Extended/Endless modes. The shared core is testable without a window. Fonts use Noto Sans (OFL); licenses are in `licenses/`.

The 16-column, 12-row content replaces the previous map layouts. See docs/grid.md for coordinates and reference cell codes. Profile unlocks/settings remain valid; active battles from the old layouts are not silently moved onto the new grid. The game displays a restore/recovery screen for an incompatible active battle.
