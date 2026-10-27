# Enemy artwork and motion

All 11 enemy kinds now use native directional artwork from the supplied Fieldrunners2 archive. The original three humanoid run cycles remain; the other eight types no longer depend on flat procedural vehicle illustrations. Native source pixels, alpha and direction-specific perspective are retained.

The importer composites the supplied plane and helicopter propeller frames at their named ASC attachment points. The heavy transport has two independently positioned rotors. Health bars use each source's named health anchor, which differs between units. The light truck represents the rail carrier and the heavy rocket transport represents the rail boss; their gameplay and rail movement remain unchanged.

Ground suspension follows distance travelled, turns ease toward the movement direction, and air hover and propellers use simulation time. Pause freezes battle motion. Enemy icons use the same artwork and have separate sizing so bosses fit the available space. Source colors distinguish red/cyan infantry, olive vehicles, orange heavy armor and the silver rocket transport from navy player towers.

Validation: Windows publish and Android ARM64 Release build passed without warnings or errors. All 11 atlases are within Reach texture limits and all declared frames contain visible pixels. Plane and both helicopter types have distinct propeller poses. The native running/pause/resume UI flow passed, and the paused unit region remained identical across 45 frames. Evidence: `artifacts/enemy-upgrade/verification.json`, `tests/ui/running-flow.txt.log`, and actual native captures in `artifacts/enemy-upgrade/`.

Artwork provenance is recorded by the importer in `assets/imported/provenance.json`. These are user-supplied third-party assets, not newly authored original character art.
