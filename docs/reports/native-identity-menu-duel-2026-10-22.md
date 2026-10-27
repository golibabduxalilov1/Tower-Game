# Menu confrontation preserving game-unit identities

The rejected generic realistic soldier/turret illustration and the blurry enlarged running sprite are no longer the primary menu artwork. The menu now uses a high-resolution illustration referenced directly from the actual in-game red infantry and navy round Gatling. It preserves the rounded dark helmet, compact red uniform, black rifle, bowl-shaped tower, three separate blue gun housings and brass muzzle tips. One soldier aims right; one tower aims left. Cool blue lighting and transparent surroundings blend into the navy menu.

This is a new large menu illustration of those unit identities, not a claim that the original small game sprite contains additional detail. Gameplay sprites and animations remain unchanged. The original battle atlases remain the fallback if the optional menu illustration is unavailable.

Source image and full built-in imagegen prompt/provenance: `assets/art-source/menu-native-duel-v1.png` and adjacent JSON. Runtime asset: `assets/imported/menu_duel-native-hd.png`, manifest key `menu:duel-native-hd`. The importer preserves this asset on future runs. Rejected original generated art is retained as source history but is not referenced or newly imported.

Actual MonoGame evidence: `artifacts/native-identity-duel/desktop.png`, `wide.png`, `touch.png`, and `saved-menu.png`. The menu flow checks fresh keyboard focus, Play, save/exit and Continue. Controls and the 16×12 battle board are unchanged.
