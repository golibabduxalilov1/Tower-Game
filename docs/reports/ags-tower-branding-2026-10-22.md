# AGS Tower Windows branding

The Windows executable is now AGS Tower.exe. Assembly product/title, Windows manifest identity, window caption, both menu translations and armory headings use AGS Tower. The existing profile directory remains unchanged so saved progress is retained.

Generated a navy/cobalt and gold turret application icon. Source: docs/store/assets/ags-tower-source.png; 512px preview and seven-resolution Windows ICO are beside it. The ICO is embedded in the executable and copied to Content/app.ico. DesktopIcon sets both small and large native window icons, since SDL window icons are independent of the executable resource. Menu title scales to the available column width, including the compact touch layout. UI/capture scripts launch the renamed executable.

Validation: Windows Release publish succeeded; published app launched and exited normally; native window probe verified the AGS Tower caption and both WM_GETICON handles. Executable product metadata is AGS Tower and its embedded icon was extracted to artifacts/ags-tower/exe-icon.png. Desktop, touch and supplied UI scripts passed. Compact menu screenshot: artifacts/ags-tower/menu-touch.png.
