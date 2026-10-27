# Wave and battle economy alignment

Source: docs/wave-round-money-system.txt (user supplied reference, examples explicitly not an original Fieldrunners wave database).

Campaign 3.2.0 removes the automatic 10 + wave/5 completion cash payment. Default WaveDefinition.Reward is zero, all eight authored campaigns have zero completion bonus, and procedurally generated Endless rounds have zero completion bonus. An explicit per-wave Reward remains supported for special levels. Kills still award once, escapes award nothing, score is separate, and selling/purchasing/upgrading affect battle money only. Existing player balances are preserved.

Victory and HUD denominators now use the map's authored wave count, rather than hard-coded 100. StartWave refuses a finite round after the final round. Snapshot validation accepts victory at a map's actual final round. Unlock progression uses min(50, previous map round count), so short campaigns cannot trap progression. Existing eight campaigns retain 100 authored rounds: the reference does not prescribe exact counts for these original maps. Content validation supports 1..100 consecutive rounds. Zero-wave developer stress fixtures retain their existing support in the core.

The previous combat economy uses eight internal battle units per reference dollar. Existing prices and kill rewards retain that consistent scale; this update does not silently reprice saved battles or introduce a premium coin wallet. No fictitious original enemy schedules, Medic sprites or new Time Trial/Puzzle/Sudden Death modes were added from the descriptive examples. Existing Classic/Extended survival rules and Endless remain the available modes.

Regression: 40 core and 22 integration tests pass, including finite limits 20/50/60/80, exact zero/explicit bonus accounting, score independence, final victory restore and Endless continuation. Existing tests cover delayed pending spawns, death rewards once, escaped projectile targets, refunds, upgrades and pause/restore. Native UI scripts and Release package builds are also run for this change.
