# Ground entrance approach

Ground enemies now spawn three logical cells outside their entrance and run continuously along the existing textured approach. Infantry take about 2.5 seconds to cover this extra distance. Speed, health and wave release intervals are unchanged. Air and rail routes keep their existing movement.

This is a simulation stage, not a presentation offset: towers can acquire, damage and slow approaching units within their normal radius; missiles can follow targets outside the grid. Remaining-distance priority includes the approach. The four entry directions use the correct running heading, and any leftover movement carries into the flow field without a jump.

The saved Entering flag defaults to false for existing battles. Validation accepts only the bounded, straight approach corridor for entering ground units. Aim and projectile coordinates allow the same three-cell margin. Existing saves resume inside the board; subsequent ground spawns use the approach.

Validation: 39/39 core tests, 22/22 integration tests, desktop/touch/supplied UI flows and the dedicated entry-approach UI flow pass. Coverage includes all four borders, continuous handoff, early Gatling/Missile acquisition and damage, projectile save/restore, malformed coordinates and paused state. Paused screenshot SHA256 hashes match. Real-window captures under artifacts/entry-approach show soldiers running before the flag while both entrance towers fire.

Windows and Android Release packages were rebuilt. Android remains locally signed for testing.
