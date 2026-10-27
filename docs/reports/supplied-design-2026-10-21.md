# Supplied artwork adaptation 0.3.0

Integrated33 chosen image assets from the user's two archives, including8weapon icons and456directional tower frames. Existing campaign simulation,11x8boards and authored waves remain authoritative. Asset provenance is recorded in `docs/supplied-assets.md`.

Replaced the earlier rectangular control style with rounded outlined icons, metal buttons and live gold price plaques. HUD follows the user's pasted placement description. Added a world-map campaign selector, main-menu audio controls and local achievement display, pause restart confirmation, safe tower-info navigation and next-level action after victory.

Validation:33/33 core tests,21/21 integration tests, and all3native UI scripts passed. Windows and Android arm64 Release builds completed without warnings/errors. Actual window captures are in `artifacts/supplied-design`; the phone previews simulate touch density on Windows, not physical-device testing. The independent evaluator's map seam, result overlap and tower-info saving findings were fixed and re-reviewed without remaining blockers.

Windows executable: `artifacts/windows-x64/Qalqon.exe` (requires installed.NET10). Android AAB: `artifacts/android-arm64/Qalqon-0.3.0-local-Signed.aab` uses local signing, not publisher/store signing.
