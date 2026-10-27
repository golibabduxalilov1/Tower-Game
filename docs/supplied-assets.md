# Supplied artwork integration

## High-resolution foliage replacement

The latest forest foliage is newly generated, distinct from the supplied archive artwork. Source atlas `assets/art-source/foliage-hd-v1.png` and its hash record are preserved in the workspace. Four transparent sprites are sliced at their gutters without resizing: two oak variants, a berry shrub and a fern/flower cluster. Their native dimensions range from450to691pixels per side, sufficient for the actual1080pedge decorations. Rendering uses the new sprites, depth-sorted placement and soft grounding shadows; the old small forest circles and strip crops are fallbacks only. Desert scenery still uses the supplied archive images.

Terrain transparency fades the outer tile edge into the continuous background, removing visible rectangular seams. Approach paths extend to the viewport edge, and foliage leaves their entrances/exits clear. The16×12grid and gameplay placement rules remain unchanged.

The current artwork selection comes from the two archives supplied by the user:

- `Fieldrunners2_barcha_dizaynlar (6).zip`
- `04_Iconlar_UI_va_ranglar.zip`

These resources are described by their archive documentation as extracted Fieldrunners 2 artwork. They are not newly created Qalqon originals. Their inclusion records the user's supplied materials and does not establish redistribution rights.

`scripts/import-supplied-art.py` selects181source entries and exports54archive-derived PNG assets plus4generated foliage sprites into `assets/imported/`. It combines original RGB images with their matching grayscale alpha masks. ASC records are parsed only as frame coordinates and anchors; no archive scripts, shaders, binaries, or executable files are run. Original binary map layouts are not interpreted: existing Qalqon map definitions, collision cells, routes, and16×12boards remain authoritative.

`manifest.json` lists the generated asset names and atlas layout. `provenance.json` lists each original ZIP entry, its archive, and SHA-256. `contact-sheet.png` is a visual inventory for review.

## Selected artwork

Eight weapon icons use `icon:` followed by `Gatling`, `Goo`, `Missile`, `Tesla`, `Flame`, `Mortar`, `Laser`, or `Ice`. Their blank yellow plaques can receive live game prices. Goo is mapped to the archive's Glue weapon, Tesla to Lightning, and Mortar to Cannonball.

Each `tower:` asset contains three upgrade levels and nineteen directions from 0 through 180 degrees. The other half of the viewing circle is mirrored at runtime. Component origins from ASC metadata align the base and turret. Native source pixels are now preserved by cropping only transparent padding into192px frames, rather than downsampling the composition. Each atlas is3648×576pixels, below the4096texture dimension limit used by the desktop Reach profile. The manifest's `drawSize` preserves the existing on-screen scale and anchor placement.

At load time, blue metal receives a darker navy color grade on both tower and icon textures. The hue mask preserves gold hardware, green icon faces, highlights and transparency. Icons receive a mild sharpening pass only on opaque interior pixels, avoiding transparent-edge halos. Disk source textures and their provenance remain unmodified by this runtime styling. The contact sheet inventories imported source art; actual styled game captures are in `artifacts/navy-design`.

The current battlefield uses individually placed native forest canopies, berry bushes, shrubs and a garden edge; desert maps use varied palms and cacti. Foliage fragments extracted from garden strips receive a foliage mask to remove adjacent fence/soil fragments and square crop boundaries. The old tiled procedural forest and repeated edge-tree row have been removed. Source images are not enlarged beyond1.25×in side decorations. Seeded placement stays fixed per map and avoids ground-route entrances/exits. All side foliage remains outside buildable cells; thin top/bottom garden edges may overhang the verge. Unused archived crystal/rock overlays are retained as inventory but are not rendered on the stone-free campaign boards. Island and sea background halves support campaign selection.

## Loading and fallback

The application copies PNG files and `manifest.json` to `Content/imported/`. `Painter.LoadSuppliedArt` loads textures once, premultiplies transparency for SpriteBatch, and registers texture disposal. `Painter.Asset` draws aspect-preserving UI artwork; `Painter.SuppliedTower` selects a level and directional frame. Missing optional files preserve the existing illustrated procedural renderer.

To regenerate the selection, run the importer with Pillow installed and both ZIP archives in the user's Downloads folder. Only normalized output filenames under `assets/imported/` are written; ZIP paths are never extracted directly.


Humanoid enemy models now use the supplied directional running cycles. Three soldier atlases preserve937poses and original foot/health anchors. See docs/running-soldiers.md for source mapping and native animation verification.
