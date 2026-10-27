# Third-party components

Every external dependency, why it is used, the pinned version and its license. The license texts
in this folder are copied next to the executables by both release builds (Windows `licenses/`,
Android `assets/licenses/`).

| Component | Version | Purpose | License | Text |
|---|---|---|---|---|
| MonoGame.Framework.DesktopGL | 3.8.5.1 | Windows window, input, OpenGL rendering, audio | Ms-PL (parts MIT) | `MonoGame-LICENSE.txt` |
| MonoGame.Framework.Android | 3.8.5.1 | Android activity, touch, OpenGL ES, audio | Ms-PL (parts MIT) | `MonoGame-LICENSE.txt` |
| SDL2 (`SDL2.dll`, via MonoGame) | bundled by MonoGame 3.8.5.1 | Desktop windowing and input | zlib | `SDL2-LICENSE.txt` |
| OpenAL Soft (`openal.dll`, `libopenal.so`, via MonoGame) | bundled by MonoGame 3.8.5.1 | Audio output | LGPL 2 (dynamically linked, unmodified) | `OpenAL-Soft-COPYING.txt` |
| NVorbis (via MonoGame) | bundled by MonoGame 3.8.5.1 | Ogg decoding (unused by the game, shipped by MonoGame) | MIT | `NVorbis-LICENSE.txt` |
| .NET runtime / .NET for Android | 10.0 | Runtime, base libraries, JSON, cryptography | MIT | `dotnet-runtime-LICENSE.txt` |
| Noto Sans (`assets/fonts/NotoSans.ttf`) | as bundled | Glyph atlas for Uzbek/English text (rasterised at build time) | SIL OFL 1.1 | `NotoSans-OFL.txt` |

Build-time only (not shipped): Pillow (Python) generates the font atlas; Microsoft OpenJDK 21 and the
Android SDK build the Android package.

OpenAL Soft is LGPL: it is shipped as an unmodified, separately replaceable shared library. Its
source is available from https://github.com/kcat/openal-soft. Keep this notice with every release.

Selected weapon icons, directional tower sprites, campaign artwork and map decorations come from
the user's supplied Fieldrunners 2 archives. See `docs/supplied-assets.md` and
`assets/imported/provenance.json` for source entries and hashes. These assets are not Qalqon
originals; this inventory does not grant a redistribution license.

The remaining procedural graphics, application icon, maps, waves and sound effects are produced
by this project's code. The glyph atlas uses the Noto Sans font listed above.

The latest high-resolution forest trees, berry shrub and fern/flower sprites were generated with
image_gen.imagegen for this project. Their original atlas and generation/hash record are preserved
under assets/art-source/foliage-hd-v1.*; they are distinct from the supplied archive artwork.
