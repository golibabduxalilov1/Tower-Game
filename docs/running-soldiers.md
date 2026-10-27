# Directional running soldiers

Humanoid enemies now use native running cycles from the user's supplied Fieldrunners2 archive instead of a static procedural overhead shape.

| Game type | Supplied animation | Poses | Directions |
|---|---|---:|---:|
| Infantry | Enemy_Soldier_Light |264|11|
| Runner | Enemy_Soldier_Anti_Slow_Light |198|11|
| Heavy | Enemy_Soldier_Heavy |475|19|

The importer retains original RGB/alpha, foot anchors and health-bar anchors in native-sized frames. Directions cover0–180degrees; the opposite half is mirrored. The body remains upright in screen space, with the matching perspective pose rather than rotating a flat sprite. Direction changes blend as the enemy travels around a turn.

Stride phase advances with actual distance travelled. Slow effects reduce stride rate,2×speed accelerates it, and zero movement during pause freezes the pose. A weak-key presentation cache avoids retaining removed enemies. No movement rules, speed, health, hitbox or combat balance changed. Enemy icons in Help and the armory use these same models.

Verification checked all937nonempty frames, distinct gait poses in every direction, native gameplay movement and pause/resume. Pixel comparison confirms the soldier region changes while moving and stays identical while paused. Native evidence and `verification.json` are in `artifacts/running-soldiers`.

`soldiers-running.gif` is a24-frame capture of the actual MonoGame renderer's four-direction animation study (`--scene=soldiers`), demonstrating the same atlases used in gameplay. The existing native input script `tests/ui/running-flow.txt` drives the real first wave for movement/pause captures. Android build verified; physical devices not tested.
