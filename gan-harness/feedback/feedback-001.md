# Independent evaluation — illustrated eleven-column refresh, iteration 1

Date: 2026-10-21. Evaluator: `/root/eleven_column_maps` after completing the separate content slice. No UI/rendering implementation edits were made by this evaluator. Visual judgement uses actual native MonoGame screenshots, not source-only assumptions.

## Scope and evidence

Contract: `docs/design-direction.md`, the user's first four baseline screenshots and fifth rich woodland tower-defense reference. The request is a substantial improvement in environment, weapons, infantry and UI/UX, with exactly eleven playable columns.

Inspected screenshots in `artifacts/design-refresh/`: `menu.png`, `campaign.png`, `briefing.png`, `battle.png`, `tower.png`, `armory.png`, `enemies.png`, `loading.png`, `restore.png`, `phone-16x9.png`, `phone-wide.png`.

Viewport evidence covers 1280×720 desktop, 1920×1080 desktop help/armory, 1920×1080 density-3 phone preview, and 1200×540 wide phone preview. The previews are native desktop windows using touch layout, not physical Android-device evidence.

The first campaign/briefing captures have foliage extending into header/labels. Root and rendering generator identified and fixed that crop issue in source before this pass; it is excluded from the remaining findings below, but replacement captures are required before final acceptance.

## Verdict

**Material improvement, needs another visual iteration. Score 6.8/10; not yet accepted at a 7.0 finish threshold.**

| Dimension | Score | Assessment |
| --- | ---: | --- |
| Design quality | 6.5 | Meadow, woodland borders, metal hardware and gold typography now communicate a coherent game. Broad blank olive side vistas still dominate battle compositions. |
| Originality | 7.0 | Code-generated layered tree crowns, turret hardware, infantry equipment and gates are distinct, with no copied reference assets. |
| Craft | 6.0 | Turret/shadow/material details are a large step up. Invalid-placement feedback overlaps phone controls, and wide-scene environmental framing looks unfinished. |
| Functionality | 8.0 | Eleven-column content, legal placement, three-level weapon data and both preview layouts are present. Core tests pass; raw touch-device and full native input coverage remain outside these screenshots. |

Weighted 0.30/0.20/0.30/0.20 = 6.75, rounded 6.8.

## Required fixes

### [P2] Invalid-placement error crosses the phone confirmation controls

Evidence: `phone-16x9.png`. The red Uzbek message `Bu yerga qurib bo‘lmaydi` lies across the lower border of the confirmation panel and overlaps the visual button area. This is precisely the error state a player sees while deciding whether to move the placement or cancel; it must remain clearly separate from those actions.

Source: `src/Game.UI/DefenseGame.Battle.cs`, `DrawBattle()` draws its invalid-preview line at `board.Bottom - 34` after `DrawBar()` has already placed `BuildConfirmRect()` into that area.

Acceptance: invalid and unaffordable previews show a readable, non-overlapping error line above or within a dedicated confirmation-panel text row at 1920×1080 density 3 and 1200×540 density 1.4. Confirm and cancel retain at least 48 dp hit areas.

### [P2] Battle side vistas remain large flat olive bars

Evidence: `battle.png`, `tower.png`, `phone-16x9.png`, `phone-wide.png`. The central 11×8 playable field has textured grass and a detailed border, but large left/right screen areas are solid olive with a few isolated tree sprites attached to the outer edge. The phone16:9 shot shows roughly 400 px of largely empty flat color on each side. That is a conspicuous part of the actual game surface, and it falls short of the user's reference's integrated, richly illustrated scenery.

Source: `src/Game.Rendering/Battlefield.cs`, `Map()` fills all bounds with one `p.Box()` color and the side-vista branch only draws a few trees at the far outer bounds.

Acceptance: retain all eleven playable columns and square cells, but compose the surrounding areas as a continuous illustrated environment—textured ground, layered clusters/canopies, stones, ferns, paths or other thematic decoration—rather than opaque blank gutters. Detail must remain cached and not obscure playable cells, entry/exit gates or HUD controls.

## Positive observations to preserve

- Turrets now have distinct steel/bronze body shading, shadows, barrel groups, rockets, coils, bolts and recognizable silhouettes. The armory view makes the improvement over the tiny flat baseline icons unambiguous.
- Infantry and vehicle help icons have clearer helmets, backpacks, guns, armor, tracks and cockpit details.
- Main-menu typography and the three-weapon diorama are stronger and more appropriate to a game than the old outlined shield.
- Currency, wave, pause/speed and dock hierarchy scan clearly; gold selected states separate selection from the green world.
- The new phone dock no longer shrinks six weapon buttons when confirmation appears. `hit` is now rounded upward before rectangles are formed. Screenshot proportions are consistent with the target; no measured sub-48-dp finding is asserted in this pass.
- Restore messaging explicitly explains the changed grid and gives a recovery action.

## Correctness review

- All eight campaign maps are 11×8 and content validation passed for 800 waves, mandatory ground routes, rails, enemy/tower coverage and boss schedule.
- Compact campaign balance runs completed 24/24 Classic and 24/24 Extended map×difficulty combinations with the deterministic legal-command AutoPlayer. This is simulation evidence, not human-usability certification.
- `SessionSnapshot.MapWidth/MapHeight` are captured and validated before restoration; legacy 24-column or metadata-free snapshots cannot silently relocate units into the compact map.
- `MapDefinition.HealthScale` is finite/bounded, authored into JSON and used by both campaign and Endless health generation, preventing a scale discontinuity at wave101.
- The existing isolated malformed-envelope probe was rerun: null checksum, missing Data and corrupt primary with valid backup no longer throw unhandled exceptions.
- Core executable tests passed **33/33** during this independent pass.

No new critical/high simulation or persistence regression was found in the examined changes. The previously documented profile-write error masking remains an inherited P2 issue (`RecordProgress()` followed by a successful `SaveBattle()` clears the error); it is not treated as a new visual-refresh defect.

## Next evaluation evidence

Rebuild after the crop fixes and provide updated campaign/briefing, desktop battle, invalid-preview phone16:9 and phone-wide captures. Also capture settings at density3 after the latest button/text changes. Recheck native UI scripts after layout changes; source inspection alone cannot close this finish gate.
