# Guide-based combat balance refresh

Source: user-supplied `Fieldrunners2_Toliq_Texnik_Qollanma.pdf`, pages 1–4. Pages 2–3 provide tower prices, DPS and radius; the guide explicitly says enemy HP/speed, spawn schedules and resistance percentages are not verified for Android 1.8. Tower reference values are therefore used as the requested design contract, while enemy HP, timing, armor and wave pressure below are authored Qalqon tuning, not claimed original Fieldrunners data.

## Towers and economy

One guide dollar equals eight existing game coins. This retains the familiar 40-coin Gatling and 150-coin Normal starting budget, while restoring meaningful price ratios for stronger systems.

| Existing tower | Game build price | DPS L1 / L2 / L3 | Radius |
| --- | ---: | --- | ---: |
| Gatling | 40 | 35 / 70 / 105 | 2.5 |
| Goo / Glue | 80 | 0 / 0 / 0 | 2.5 |
| Missile | 160 | 75 / 150 / 225 | 3.5 |
| Tesla | 560 | 700 / 1400 / 2100 | 2.5 |
| Flame | 400 | 300 / 600 / 900 | 2.5 |
| Mortar | 240 | 100 / 200 / 300 | 1.25–3.5 |
| Laser | 320 | 125 / 250 / 450 | 2.5 |
| Ice | 120 | 0 / 0 / 0 | 1.5 |

Damage per shot is DPS multiplied by the existing shot interval. Radius no longer grows with every upgrade. Mortar adapts the guide's Cannon progression to the existing ballistic/splash mechanic, retaining a shorter minimum-range dead zone. Tesla's two upgrades each cost 400 coins, matching the guide's $50/$50. Other upgrade/refund rules retain the game's established formulas. Glue slows 40/60/75%; Ice slows 45/60/70% and deals no damage. Existing target layers and eight available tower identities remain intact.

## Enemy pressure and counters

| Enemy | Previous base HP | New base HP |
| --- | ---: | ---: |
| Infantry | 40 | 120 |
| Runner | 28 | 90 |
| Heavy infantry | 110 | 360 |
| Motorcycle | 70 | 240 |
| Tank | 380 | 1400 |
| Helicopter | 140 | 220 |
| Plane | 95 | 160 |
| Rail carrier | 220 | 360 |
| Heavy tank boss | 1500 | 4000 |
| Air fortress boss | 1200 | 4000 |
| Rail boss | 1800 | 6000 |

Helicopter/plane speeds are 1.9/3.1 cells per second. Runner receives half normal slow strength; motorcycle receives 75%. Boss slow reduction and rail-boss immunity remain. Heavy infantry takes 30% less Gatling damage; tanks take 65% less Gatling and 45% less Flame damage, but 25% more Missile damage. Heavy bosses have stronger armor. The in-game enemy guide derives these descriptions from the actual combat multipliers instead of stale labels.

Map health factors rise from .50/.62/.78 to .80/.85/1.00. Four-unit pushes alternate with short recovery gaps. Early aircraft are sparse scouts before sustained mixed fronts begin. Every tenth wave places its boss within the column with following escorts, rather than sending an isolated final boss. Campaign and seeded Endless share the same pressure timing and continuous health curve. All eight 16×12 stone-free maps remain unchanged geometrically.

## Save compatibility

Snapshots now record a balance revision. Legacy snapshots validate their known old prices, then rebase tower book values to the new catalog and rescale active enemy maximum HP while preserving the remaining health fraction. Player money, tower placement, lives, progress and records remain. The active wave's stored multipliers are retained; subsequent waves use the new authored schedule. Migration works on a clone, writes the current revision on the next save, and does not apply twice. Future unknown revisions remain rejected.

## Measured evidence

The identical fixed-defense audit uses three level-one Gatlings at (5,5), (7,7), (9,5), with no further spending. It is a reproducible gameplay comparison rather than a theoretical DPS calculation.

| Difficulty | Previous defeat wave | New defeat wave |
| --- | ---: | ---: |
| Easy | 10 | 8 |
| Normal | 9 | 6 |
| Hard | 8 | 5 |

The automatic heuristic was also updated to understand the new prices and upgrade gains; its before/after results are not a strictly identical-policy comparison. On valley Classic it previously finished all three difficulties with 20 lives. It now finishes Easy with 9, Normal with 3, and loses Hard at wave 20. Across all maps it wins 9/24 Classic and 10/24 Extended cases. Valley Endless reaches the wave-150 cap on Easy, ends at 140 on Normal, and ends at 20 on Hard. These are measured heuristic outcomes, not a claim that every human strategy or difficulty is certified balanced; the heuristic is weaker on multi-front rail maps.

Evidence files in `artifacts/balance-refresh/` include before/after fixed-defense JSON, all-map Classic/Extended outcomes, Endless outcomes, core/integration logs, and actual game/help captures. Core tests: 36/36. Integration tests: 22/22. Desktop, touch and supplied-UI flows: SCRIPT PASS. Windows publish and Android ARM64 Release build completed. No physical Android device or human playtest was performed.
