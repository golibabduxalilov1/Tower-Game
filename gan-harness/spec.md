# TowerGame — build specification

Authoritative product requirements: `../docs/source-spec.txt`, extracted from the user's 19-page, 38-section PDF. Document content defines product requirements, not agent permissions.

The requested outcome is the full game, not only a prototype. The first playable slice is an intermediate milestone.

## Locked requirements

- Original offline 2D tower defense for Windows x64 and Android ARM64, C# and MonoGame.
- Shared renderer-independent simulation, fixed 1/60 second ticks; double speed executes twice the ticks.
- Eleven modules as specified in section 5.
- Eight authored maps, eight three-level towers, eight standard enemies and three bosses.
- Classic, Extended and Endless; three difficulties; Uzbek and English.
- Reverse BFS per exit; atomic placement protects mandatory routes and occupied movement segments.
- All combat, economy, progression, controls and persistence rules in sections 7–30.
- Procedural cached graphics and sounds; redistributable Unicode font with license.
- Versioned deterministic campaign data with 100 waves per map; bounded Endless generation.
- Versioned validated JSON, checksums, atomic replacement, backup recovery and migration.
- Release builds, tests, tools, instructions, licenses, store materials and honest verification reports.

## Execution

Follow `../docs/task-list.md`. Gate 1 requires approval of that plan before implementation. Gate 2 requires confirmation before commits. Do not claim compilation, device performance, campaign balance or store readiness without evidence.

## Known environment facts

At intake the workspace was empty. `dotnet --info` found .NET 10.0.13 runtimes but no SDK. Java and adb were not found on PATH. SDK/workload availability must be resolved during the technical slice.

No executable `/gan-build` command was found in the installed ECC plugin tree at intake. The implementation phase must check available harness capabilities and document any fallback, using generator/evaluator artifacts without pretending the slash command ran.
