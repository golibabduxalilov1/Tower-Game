# Weapon feedback and stale-shot cleanup

A final kill could enter Intermission with a surviving Mortar projectile. Since intermission skips projectile updates, that shell stayed visible for the entire break. Wave completion now returns all remaining projectiles to the pool and clears target locks before the break/victory. Existing homing missiles still cancel when their target dies, with no late rewards.

Turrets update their aim every simulation tick while reloading, and clear a dead/out-of-range target immediately rather than waiting for cooldown expiry. Idle turrets return to a neutral heading. Rate of fire and damage are unchanged.

Shot effects last 180ms. Gatling now has a moving bright tracer, muzzle flare, impact spark and visible recoil. Missile/Mortar have launch flashes and rockets have a larger body/exhaust. Effects render above enemy sprites. Recoil is driven by the actual shot effect, so lethal shots still show a firing response after the target is removed. Simulation time retains pause behavior.

Validation: 42/42 core tests and 22/22 integration tests passed, including new regressions for live tracking during cooldown, immediate dead-target clearing, a mortar in flight when the final enemy dies, capture/restore during the resulting break and clean next-wave startup. All three standard native UI scripts plus the entry approach capture script passed. artifacts/entry-approach/approach-b.png shows the enhanced muzzle/impact feedback. Windows Release and local Android packages rebuilt.
