# High-resolution foliage

Replaced the low-resolution forest assets with four generated transparent sprites, native450–691pixels per side. Source atlas and provenance are retained in `assets/art-source`. The importer slices at transparent gutters and preserves RGB/alpha. Actual sprites are reduced for display rather than enlarged from small archive crops.

Side placements are ordered by depth and have subtle contact shadows. The old archive garden strip is no longer used when HD foliage is loaded. Terrain edges blend with transparency, removing the vertical crop seams; gate approaches now continue to the viewport edge. No change to16×12cells, tower placement or route simulation.

Checked all55image dimensions and alpha layers, including transparent padding around the four HD sprites. Actual MonoGame screenshots at1920×1080are in `artifacts/hd-nature`: battle, preparation and touch-density preview. Windows and Android Release builds verified; physical Android devices were not tested.
