# Full-page menu scene

The menu no longer draws a separate cutout in a right-hand rectangle. Its continuous navy ground, atmosphere, enlarged red infantry, round Gatling and touching VS emblem are one full-page background. The same ground extends beneath the buttons, without a slab boundary, divider, inset panel or letterbox bars.

`Painter.AssetCover` fills the viewport while preserving the image aspect ratio and center-cropping surrounding atmosphere/ground for wider screens. Essential unit silhouettes remain visible at the verified desktop and wide-phone sizes. Wide touch menus use narrower left controls and put Help in the safe top-right corner to keep Play off the soldier.

Source/provenance: `assets/art-source/menu-full-scene-v1.png` and adjacent JSON contain the generated background and exact built-in imagegen prompt. Runtime key `menu:scene` loads `assets/imported/menu_scene.png`; future imports preserve it. Gameplay artwork remains unchanged.

Actual renders are in `artifacts/full-page-menu/`: 1280×720 desktop, 1920×1080 desktop, 1200×540 touch, fresh/saved menu and visible focus. Native desktop, touch and supplied-UI regression flows passed. Menu-specific flows exercise keyboard Play, save/exit, Continue and touch Help. No physical Android device was used.
