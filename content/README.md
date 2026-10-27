# QALQON campaign content

`campaign.json` is the shipped authoritative content pack, format 1, content version **2.0.0**. It is embedded in `Game.Content`; no network or writable asset directory is required.

The eight **11-column × 8-row** maps contain explicit cell arrays, entrance/exit coordinates, air waypoints, contiguous rail cells, two special towers each and 100 explicit wave definitions each. This deliberately supersedes the PDF's original 24×16 default: the user requested eleven columns during the visual redesign. These are newly authored layouts, not scaled screenshots or resampled coordinates. All four special towers appear across the campaign. The basic four towers are always available.

Version 2 changes battlefield coordinates. A version-1 active battle must not silently place its old towers/enemies into these new layouts; recovery must reject an incompatible battle while retaining profile progression and settings.

Reproduce the authored content from the repository root:

```powershell
.\.tools\dotnet\dotnet.exe run --project src/Game.Tools -- --generate content/campaign.json
.\.tools\dotnet\dotnet.exe run --project src/Game.Tools
```

The second command rebuilds the library with the newly exported embedded data and validates it. `CampaignAuthoring.cs` contains the original authored coordinate layouts. Ground routes hold entrance and exit points and use BFS; air routes follow their waypoints; rails contain every traversed cell. No procedural random map layout is generated at runtime.

Campaign populations follow the requested N formula, with each enemy represented as a one-count group so the total is never multiplied by the number of entrances. The final unit in every tenth wave is one compatible boss. Air appears from wave 6, rail from wave 8, and stronger ground types are introduced gradually.

The smaller board gives flight/rail units less exposure and has fewer construction cells. Campaign health therefore uses `C × (1 + 0.075 × (w - 1)) × 1.012^(w - 1)`, where `C = 0.78` on ground/air maps, `C = 0.62` on maps containing a railway, and `C = 0.50` on Marsh Border because its causeway leaves only 34 legal construction cells. This coefficient is persisted as each map's `HealthScale`; both campaign generation and the core's bounded seed-based Endless generator use it, preventing an artificial health jump at wave 101. Spawn interval is `max(0.25, 1.02 - 0.006 × w)`. The railway coefficients were reduced after compact-board AutoPlayer sweeps exposed late rail-heavy losses. Base tower damage/prices, base enemy stats, rewards, difficulties and wave population remain unchanged. This is an explicit compact-grid balance revision; do not reuse version-1 playthrough reports as proof of version-2 balance.

The validator requires exactly eleven columns and eight rows and checks known cell types, special tower coverage, unique map/route IDs, required ground paths, contiguous rails, valid air waypoints, compatible spawn routes, all enemy kinds, consecutive campaign waves, total population, boss timing, finite health/timing and rewards.

Compact-grid validation on 2026-10-21: all eight authored maps passed the validator. The deterministic AutoPlayer completed all 100 waves in **24/24 Classic** and **24/24 Extended** map×difficulty combinations using legal build/upgrade commands. Logs are `artifacts/balance-compact-classic.txt` and `artifacts/balance-compact-extended.txt`. These are headless reproducible playthroughs, not physical-device or human-usability certification.

Map order defines campaign unlock order:

1. Yashil vodiy / Green Valley
2. Ikki oqim / Twin Streams
3. Qum qal’asi / Sand Fortress
4. Kristall kon / Crystal Mine
5. Tosh darvozalar / Stone Gates
6. Muz markazi / Ice Core
7. Botqoq chegarasi / Marsh Border
8. Vulkan halqasi / Volcanic Ring
