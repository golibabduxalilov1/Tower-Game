# Controls inside the battlefield

The landscape now fills the rendering canvas. Money, settings, score/wave, lives, pause/speed and the weapon shop overlay the map instead of reserving dark top and bottom bands. Weapon icons align at bottom right; pause/speed remain at bottom left. Safe insets constrain controls while scenery continues behind them.

The square16×12grid uses a narrow decorative verge. At1280×720 the playable grid is approximately914×686pixels, compared with approximately627×470pixels before the overlay layout. Side foliage consequently shrinks from roughly326px to183px per side. Tower art also grows22%, and enemy art50%, relative to the new cell size. Stone-free campaign maps and simulation rules remain unchanged.

Hit testing intercepts individual controls instead of swallowing the complete top/bottom bands. Selected tower details and wave actions stay above the shop. Native desktop, touch and new-menu UI flows all pass. Scripted verification now ignores desktop mouse input so outside clicks cannot mutate a run. Actual rendered desktop and touch-preview evidence is stored in `artifacts/overlay-design`; physical Android hardware was not tested.
