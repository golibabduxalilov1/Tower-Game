# Evaluation rubric

Evaluate each slice against the source specification and its acceptance criteria. Record evidence in `feedback/feedback-NNN.md`, including commands, outcomes, defects and unresolved items.

| Area | Weight | Required evidence |
| --- | ---: | --- |
| Simulation correctness | 25 | Placement rollback, BFS, targeting, combat effects, economy, deterministic timing tests |
| Content and progression | 20 | Eight valid maps, tower/enemy/boss coverage, campaign definitions, unlock and record tests |
| Playable presentation | 15 | Running Windows game, readable procedural visuals, menus/HUD, both languages and input coverage |
| Persistence and lifecycle | 15 | Round trip, interrupted/corrupt save, backup, migrations, pause/background handling |
| Platform and distribution | 15 | Windows release, Android ARM64 release, licenses, build instructions and store materials |
| Performance and balance | 10 | Stress measurements, physical device reports, campaign playthrough evidence |

Score each area 0–10 and compute weighted total. A score of 7 is an iteration threshold, not proof of full delivery. Missing mandatory requirements remain open regardless of score. Critical/high correctness or security findings block acceptance.

Use headless tests for the core. Use native runtime checks for MonoGame UI; browser-only evaluation cannot validate this game. Separate tested results, code inspection and unverified claims.

Required scenarios include blocked final route, trapped enemy, occupied segment, insufficient money, upgrades/refunds, damage resistance, overlapping slow timers, one reward per death, wave completion, defeat precedence, save recovery/migration, seed reproducibility, 1x/2x equivalence, 100-wave run, long Endless run and every map's content validation.

Final acceptance additionally requires all twelve conditions in section 36 of the PDF. Physical-device and store/account-dependent work must be explicitly reported if unavailable.
